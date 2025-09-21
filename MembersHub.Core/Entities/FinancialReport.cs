using System;
using System.Collections.Generic;

namespace MembersHub.Core.Entities;

/// <summary>
/// Οικονομικές αναφορές και στατιστικά
/// </summary>
public class FinancialReport
{
    public int Id { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public FinancialReportType Type { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int GeneratedBy { get; set; }
    public string ReportData { get; set; } = string.Empty; // JSON με τα δεδομένα
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual User Generator { get; set; } = null!;
}

/// <summary>
/// Συνοπτικά οικονομικά στατιστικά για dashboards
/// </summary>
public class FinancialSummary
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    // Έσοδα
    public decimal TotalIncome { get; set; }
    public decimal MembershipIncome { get; set; }
    public decimal EventIncome { get; set; }
    public decimal OtherIncome { get; set; }
    
    // Έξοδα
    public decimal TotalExpenses { get; set; }
    public decimal OperationalExpenses { get; set; }
    public decimal EquipmentExpenses { get; set; }
    public decimal OtherExpenses { get; set; }
    
    // Υπόλοιπα
    public decimal NetProfit { get; set; }
    public decimal CurrentBalance { get; set; }
    
    // Συνδρομές
    public int TotalMembers { get; set; }
    public int PaidMembers { get; set; }
    public int PendingPayments { get; set; }
    public int OverduePayments { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    
    // Στατιστικά πληρωμών
    public int TotalPayments { get; set; }
    public int CashPayments { get; set; }
    public int CardPayments { get; set; }
    public int BankTransfers { get; set; }
    
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Μηνιαία οικονομικά δεδομένα για trends
/// </summary>
public class MonthlyFinancialData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetProfit { get; set; }
    public int NewMembers { get; set; }
    public int TotalMembers { get; set; }
    public decimal AveragePayment { get; set; }
}

public enum FinancialReportType
{
    Monthly,
    Quarterly,
    Yearly,
    Custom,
    MembershipReport,
    PaymentReport,
    ExpenseReport,
    ProfitLoss
}