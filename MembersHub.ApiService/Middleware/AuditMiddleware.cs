using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace MembersHub.ApiService.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip audit logging for certain endpoints
        if (ShouldSkipAuditing(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var requestBody = await CaptureRequestBodyAsync(context.Request);
        
        // Create a copy of the response stream to capture the response
        var originalResponseBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            var responseContent = await CaptureResponseBodyAsync(responseBody);
            
            // Copy the response back to the original stream
            await responseBody.CopyToAsync(originalResponseBodyStream);
            
            // Log the audit information
            await LogAuditTrailAsync(context, requestBody, responseContent, startTime, endTime);
        }
    }

    private bool ShouldSkipAuditing(string path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/openapi",
            "/metrics",
            "/favicon.ico",
            "/_framework",
            "/_content"
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> CaptureRequestBodyAsync(HttpRequest request)
    {
        try
        {
            if (request.ContentLength == 0 || !request.HasFormContentType && request.ContentType != "application/json")
            {
                return string.Empty;
            }

            request.EnableBuffering();
            var buffer = new byte[request.ContentLength ?? 0];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;

            var requestBody = Encoding.UTF8.GetString(buffer);
            
            // Sanitize sensitive information
            return SanitizeRequestBody(requestBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture request body");
            return string.Empty;
        }
    }

    private async Task<string> CaptureResponseBodyAsync(MemoryStream responseBody)
    {
        try
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            
            // Limit response size to prevent huge logs
            if (responseContent.Length > 5000)
            {
                return responseContent[..5000] + "... [TRUNCATED]";
            }
            
            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture response body");
            return string.Empty;
        }
    }

    private string SanitizeRequestBody(string requestBody)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requestBody))
                return requestBody;

            // Parse as JSON and sanitize sensitive fields
            var jsonDocument = JsonDocument.Parse(requestBody);
            var sanitized = SanitizeJsonElement(jsonDocument.RootElement);
            
            return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            // If not valid JSON, just return as is (probably form data or other content)
            return requestBody;
        }
    }

    private object SanitizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var prop in element.EnumerateObject())
                {
                    var key = prop.Name.ToLowerInvariant();
                    if (IsSensitiveField(key))
                    {
                        obj[prop.Name] = "[HIDDEN]";
                    }
                    else
                    {
                        obj[prop.Name] = SanitizeJsonElement(prop.Value);
                    }
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(SanitizeJsonElement).ToArray();

            case JsonValueKind.String:
                return element.GetString() ?? "";

            case JsonValueKind.Number:
                return element.GetDecimal();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();

            default:
                return null;
        }
    }

    private bool IsSensitiveField(string fieldName)
    {
        var sensitiveFields = new[]
        {
            "password",
            "passwordhash",
            "newpassword",
            "oldpassword",
            "confirmpassword",
            "token",
            "secret",
            "key",
            "authorization"
        };

        return sensitiveFields.Any(field => fieldName.Contains(field));
    }

    private async Task LogAuditTrailAsync(HttpContext context, string requestBody, string responseContent, 
        DateTime startTime, DateTime endTime)
    {
        try
        {
            using var scope = context.RequestServices.CreateScope();
            var auditService = scope.ServiceProvider.GetService<IAuditService>();
            var httpContextInfo = scope.ServiceProvider.GetService<IHttpContextInfoService>();

            if (auditService == null || httpContextInfo == null)
                return;

            var user = context.User;
            int? userId = null;
            string? username = null;
            string? fullName = null;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
                {
                    userId = id;
                }
                
                username = user.FindFirst(ClaimTypes.Name)?.Value;
                var firstNameClaim = user.FindFirst(ClaimTypes.GivenName)?.Value ?? "";
                var lastNameClaim = user.FindFirst(ClaimTypes.Surname)?.Value ?? "";
                fullName = user.FindFirst("FullName")?.Value ?? $"{firstNameClaim} {lastNameClaim}".Trim();
            }

            var duration = (endTime - startTime).TotalMilliseconds;
            var method = context.Request.Method;
            var path = context.Request.Path + context.Request.QueryString;
            var statusCode = context.Response.StatusCode;
            
            var description = $"HTTP {method} {path} - {statusCode} ({duration:F0}ms)";

            // Determine audit action based on HTTP method and status
            var auditAction = DetermineAuditAction(method, statusCode, path);

            var auditDetails = new
            {
                method,
                path = path.ToString(),
                statusCode,
                duration,
                requestBody = string.IsNullOrWhiteSpace(requestBody) ? null : requestBody,
                responseBody = string.IsNullOrWhiteSpace(responseContent) ? null : responseContent,
                timestamp = startTime
            };

            await auditService.LogAsync(
                auditAction,
                "HTTP",
                null, // entityId
                path, // entityName
                description,
                userId,
                username ?? "Άγνωστος",
                fullName ?? "Άγνωστος Χρήστης",
                httpContextInfo.GetIpAddress(),
                httpContextInfo.GetUserAgent(),
                null, // oldValues
                JsonSerializer.Serialize(auditDetails, new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit trail for {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        }
    }

    private AuditAction DetermineAuditAction(string method, int statusCode, string path)
    {
        // Failed requests
        if (statusCode >= 400)
        {
            return path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) && statusCode == 401
                ? AuditAction.LoginFailed
                : AuditAction.UnauthorizedAccess;
        }

        // Success responses based on HTTP method
        return method.ToUpperInvariant() switch
        {
            "GET" => AuditAction.View,
            "POST" when path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) => AuditAction.Login,
            "POST" when path.Contains("/auth/logout", StringComparison.OrdinalIgnoreCase) => AuditAction.Logout,
            "POST" => AuditAction.Create,
            "PUT" or "PATCH" => AuditAction.Update,
            "DELETE" => AuditAction.Delete,
            _ => AuditAction.View
        };
    }
}