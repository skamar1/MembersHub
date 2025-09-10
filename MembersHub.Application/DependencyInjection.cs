using Microsoft.Extensions.DependencyInjection;
using MembersHub.Application.Services;
using MembersHub.Core.Interfaces;

namespace MembersHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuditService, AuditService>();
        
        // Add more services here as we implement them
        // services.AddScoped<IPaymentService, PaymentService>();
        // services.AddScoped<ISubscriptionService, SubscriptionService>();
        // services.AddScoped<IExpenseService, ExpenseService>();
        
        return services;
    }
}