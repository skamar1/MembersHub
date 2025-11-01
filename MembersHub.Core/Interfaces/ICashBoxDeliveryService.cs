using MembersHub.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MembersHub.Core.Interfaces;

/// <summary>
/// Υπηρεσία διαχείρισης παραδόσεων ταμείου εισπρακτόρων
/// </summary>
public interface ICashBoxDeliveryService
{
    /// <summary>
    /// Δημιουργία νέας παράδοσης ταμείου
    /// </summary>
    Task<CashBoxDelivery> CreateDeliveryAsync(int collectorId, int receivedBy, string? notes = null);

    /// <summary>
    /// Λήψη όλων των παραδόσεων ενός εισπράκτορα
    /// </summary>
    Task<List<CashBoxDelivery>> GetCollectorDeliveriesAsync(int collectorId);

    /// <summary>
    /// Λήψη της τελευταίας παράδοσης ενός εισπράκτορα
    /// </summary>
    Task<CashBoxDelivery?> GetLastDeliveryAsync(int collectorId);

    /// <summary>
    /// Υπολογισμός τρέχοντος υπολοίπου εισπράκτορα
    /// </summary>
    Task<(decimal Collections, decimal Expenses, decimal Net)> CalculateCurrentBalanceAsync(int collectorId);

    /// <summary>
    /// Υπολογισμός υπολοίπου για συγκεκριμένη περίοδο
    /// </summary>
    Task<(decimal Collections, decimal Expenses, decimal Net)> CalculateBalanceForPeriodAsync(
        int collectorId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Λήψη παράδοσης με βάση τον αριθμό
    /// </summary>
    Task<CashBoxDelivery?> GetByDeliveryNumberAsync(string deliveryNumber);

    /// <summary>
    /// Λήψη παράδοσης με βάση το ID
    /// </summary>
    Task<CashBoxDelivery?> GetByIdAsync(int id);

    /// <summary>
    /// Δημιουργία αριθμού παράδοσης
    /// </summary>
    Task<string> GenerateDeliveryNumberAsync();
}
