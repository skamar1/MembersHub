using MembersHub.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MembersHub.Core.Interfaces;

/// <summary>
/// Κεντρική υπηρεσία οικονομικής διαχείρισης
/// </summary>
public interface IFinancialService
{
    // Payment Management
    Task<Payment> CreatePaymentAsync(Payment payment);
    Task<Payment> UpdatePaymentAsync(Payment payment);
    Task<Payment?> GetPaymentByIdAsync(int paymentId);
    Task<List<Payment>> GetMemberPaymentsAsync(int memberId, int year = 0);
    Task<List<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<bool> DeletePaymentAsync(int paymentId);
    
    // Subscription Management  
    Task<Subscription> CreateSubscriptionAsync(int memberId, int year, int month, decimal amount);
    Task<Subscription> UpdateSubscriptionAsync(Subscription subscription);
    Task<List<Subscription>> GetMemberSubscriptionsAsync(int memberId);
    Task<List<Subscription>> GetOverdueSubscriptionsAsync();
    Task<List<Subscription>> GetSubscriptionsByStatusAsync(SubscriptionStatus status);
    Task<decimal> GetMemberOutstandingBalanceAsync(int memberId);
    
    // Invoice Management
    Task<Invoice> CreateInvoiceAsync(Invoice invoice);
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
    Task<List<Invoice>> GetMemberInvoicesAsync(int memberId);
    Task<List<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status);
    Task<string> GenerateInvoiceNumberAsync();
    Task<bool> SendInvoiceEmailAsync(int invoiceId);
    
    // Payment Plans
    Task<PaymentPlan> CreatePaymentPlanAsync(PaymentPlan paymentPlan);
    Task<PaymentPlan> UpdatePaymentPlanAsync(PaymentPlan paymentPlan);
    Task<List<PaymentPlan>> GetMemberPaymentPlansAsync(int memberId);
    Task<List<PaymentInstallment>> GetOverdueInstallmentsAsync();
    Task<PaymentInstallment> ProcessInstallmentPaymentAsync(int installmentId, Payment payment);
    
    // Financial Reporting
    Task<FinancialSummary> GetFinancialSummaryAsync(DateTime periodStart, DateTime periodEnd);
    Task<List<MonthlyFinancialData>> GetMonthlyDataAsync(int year);
    Task<List<MonthlyFinancialData>> GetYearlyTrendsAsync(int years = 5);
    Task<Dictionary<string, decimal>> GetIncomeBySourceAsync(DateTime startDate, DateTime endDate);
    Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate);
    
    // Transaction Management
    Task<FinancialTransaction> CreateTransactionAsync(FinancialTransaction transaction);
    Task<List<FinancialTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<FinancialTransaction>> GetMemberTransactionsAsync(int memberId);
    Task<decimal> GetCurrentBalanceAsync();
    
    // Analytics & Statistics
    Task<decimal> GetTotalIncomeAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetNetProfitAsync(DateTime startDate, DateTime endDate);
    Task<int> GetTotalPaidMembersAsync(int year, int month = 0);
    Task<int> GetTotalOverdueMembersAsync();
    Task<decimal> GetAveragePaymentAmountAsync(DateTime startDate, DateTime endDate);
    Task<List<(string PaymentMethod, int Count, decimal Total)>> GetPaymentMethodStatsAsync(DateTime startDate, DateTime endDate);
}