using System.Collections.Generic;
using System.Threading.Tasks;
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IPaymentService
{
    Task<Payment> CreatePaymentAsync(Payment payment);
    Task<Payment?> GetByReceiptNumberAsync(string receiptNumber);
    Task<IEnumerable<Payment>> GetMemberPaymentsAsync(int memberId);
    Task<IEnumerable<Payment>> GetCollectorPaymentsAsync(int collectorId);
    Task<decimal> GetTodayCollectionsAsync(int? collectorId = null);
    Task<byte[]> GenerateReceiptPdfAsync(int paymentId);
    Task SendReceiptAsync(int paymentId);
}