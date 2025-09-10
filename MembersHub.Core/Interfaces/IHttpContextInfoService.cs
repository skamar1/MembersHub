namespace MembersHub.Core.Interfaces;

public interface IHttpContextInfoService
{
    string? GetIpAddress();
    string? GetUserAgent();
    string? GetRequestPath();
    string GetRequestMethod();
    bool IsAvailable { get; }
}