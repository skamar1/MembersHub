using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MembersHub.Application.Services;
using MembersHub.Core.Interfaces;

namespace MembersHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register inner MemberService
        services.AddScoped<MemberService>();

        // Register IMemberService with automatic Redis detection
        // If IDistributedCache is available, use cached wrapper; otherwise use direct service
        services.AddScoped<IMemberService>(sp =>
        {
            var innerService = sp.GetRequiredService<MemberService>();

            // Try to get IDistributedCache - if available, use caching
            var cache = sp.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            if (cache != null)
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedMemberService>>();
                var loggerFactory = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
                loggerFactory?.CreateLogger("MembersHub.Application.DependencyInjection")
                    .LogInformation("Redis distributed cache detected - using CachedMemberService with Redis caching");
                return new CachedMemberService(innerService, cache, logger);
            }

            // No cache available - use direct service
            var fallbackLoggerFactory = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            fallbackLoggerFactory?.CreateLogger("MembersHub.Application.DependencyInjection")
                .LogInformation("No distributed cache available - using MemberService without caching");
            return innerService;
        });

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IMembershipTypeService, MembershipTypeService>();

        // Add more services here as we implement them
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<ICashBoxDeliveryService, CashBoxDeliveryService>();
        services.AddScoped<ICashierHandoverService, CashierHandoverService>();

        return services;
    }
}