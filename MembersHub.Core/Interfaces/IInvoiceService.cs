using MembersHub.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MembersHub.Core.Interfaces;

/// <summary>
/// Υπηρεσία διαχείρισης τιμολογίων
/// </summary>
public interface IInvoiceService
{
    // Invoice CRUD Operations
    Task<Invoice> CreateInvoiceAsync(int memberId, List<InvoiceItem> items, string? notes = null);
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
    Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
    Task<List<Invoice>> GetMemberInvoicesAsync(int memberId);
    Task<List<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status);
    Task<List<Invoice>> GetOverdueInvoicesAsync();
    Task<bool> DeleteInvoiceAsync(int invoiceId);
    
    // Invoice Processing
    Task<Invoice> SendInvoiceAsync(int invoiceId);
    Task<Invoice> MarkInvoiceAsPaidAsync(int invoiceId, List<Payment> payments);
    Task<Invoice> AddPaymentToInvoiceAsync(int invoiceId, Payment payment);
    Task<Invoice> CancelInvoiceAsync(int invoiceId, string reason);
    
    // Invoice Generation
    Task<string> GenerateInvoiceNumberAsync();
    Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);
    Task<string> GenerateInvoiceHtmlAsync(int invoiceId);
    
    // Bulk Operations
    Task<List<Invoice>> CreateBulkSubscriptionInvoicesAsync(int year, int month);
    Task<List<Invoice>> GetInvoicesDueSoonAsync(int daysAhead = 7);
    Task<int> SendReminderEmailsAsync();
    
    // Statistics
    Task<decimal> GetTotalInvoicedAmountAsync(int year, int month = 0);
    Task<decimal> GetTotalOutstandingAmountAsync();
    Task<Dictionary<InvoiceStatus, int>> GetInvoiceStatusCountsAsync();
    Task<List<(int MemberId, string MemberName, decimal Amount)>> GetTopDebtorsAsync(int limit = 10);
}