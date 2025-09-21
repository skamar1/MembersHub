using System.Collections.Generic;
using System.Threading.Tasks;
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface ISubscriptionService
{
    // Basic CRUD operations
    Task<Subscription?> GetByIdAsync(int id);
    Task<IEnumerable<Subscription>> GetMemberSubscriptionsAsync(int memberId);
    Task<IEnumerable<Subscription>> GetSubscriptionsForPeriodAsync(int year, int? month = null);
    Task<Subscription> CreateSubscriptionAsync(Subscription subscription);
    Task UpdateSubscriptionAsync(Subscription subscription);
    Task DeleteSubscriptionAsync(int id);

    // Bulk operations
    Task<int> GenerateMonthlySubscriptionsAsync(int year, int month);
    Task<IEnumerable<Subscription>> GetPendingSubscriptionsAsync();
    Task<IEnumerable<Subscription>> GetOverdueSubscriptionsAsync();
    Task<int> SendPaymentRemindersAsync(int? memberId = null);
    Task<int> MarkOverdueSubscriptionsAsync();

    // Reporting
    Task<decimal> GetMonthlyRevenueAsync(int year, int month);
    Task<decimal> GetOutstandingAmountAsync();
    Task<Dictionary<int, decimal>> GetMemberOutstandingBalancesAsync();

    // Status management
    Task UpdateSubscriptionStatusAsync(int subscriptionId, SubscriptionStatus status);
    Task<int> AutoUpdateSubscriptionStatusesAsync();
}