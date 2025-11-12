# DateTime Fixes Needed - Μετατροπή σε Ελληνική Ώρα

## Σύνοψη Προβλήματος
Η εφαρμογή χρησιμοποιεί `DateTime.Now` και `DateTime.Today` που επιστρέφουν Local Time.
Για PostgreSQL με `timestamp with time zone`, χρειαζόμαστε UTC timestamps.
Για τον χρήστη θέλουμε να εμφανίζουμε Ελληνική ώρα (Europe/Athens).

## Λύση
Χρήση του `TimeZoneService` που δημιουργήθηκε:
- `TimeZone.GetGreekNow()` - Τρέχουσα ώρα Ελλάδας
- `TimeZone.GetGreekToday()` - Σημερινή ημερομηνία Ελλάδας (00:00) σε UTC
- `TimeZone.ConvertToUtc(greekDateTime)` - Μετατροπή Ελληνικής ώρας σε UTC
- `TimeZone.ConvertToGreekTime(utcDateTime)` - Μετατροπή UTC σε Ελληνική ώρα

---

## 🔴 ΚΡΙΣΙΜΕΣ ΑΛΛΑΓΕΣ (Backend Services)

### 1. **CashierHandoverService.cs**
**Αρχείο:** `MembersHub.Application/Services/CashierHandoverService.cs`

#### Γραμμή 83: CreateHandoverAsync
```csharp
// ❌ ΛΑΘΟΣ
var periodEnd = DateTime.Now;

// ✅ ΣΩΣΤΟ - Inject TimeZoneService
var periodEnd = _timeZone.ConvertToUtc(_timeZone.GetGreekNow());
```

#### Γραμμή 98: CreatedDate
```csharp
// ❌ ΛΑΘΟΣ
CreatedDate = DateTime.Now

// ✅ ΣΩΣΤΟ
CreatedDate = _timeZone.ConvertToUtc(_timeZone.GetGreekNow())
```

#### Γραμμή 119: ConfirmHandoverAsync
```csharp
// ❌ ΛΑΘΟΣ
handover.ConfirmedDate = DateTime.Now;

// ✅ ΣΩΣΤΟ
handover.ConfirmedDate = _timeZone.ConvertToUtc(_timeZone.GetGreekNow());
```

**Επίπτωση:** Οι ώρες παράδοσης ταμείου θα είναι λάθος timezone

---

### 2. **ExpenseService.cs**
**Αρχείο:** `MembersHub.Application/Services/ExpenseService.cs`

#### Γραμμή 344: GenerateExpenseNumberAsync
```csharp
// ❌ ΛΑΘΟΣ
var currentYear = DateTime.Now.Year;

// ✅ ΣΩΣΤΟ
var currentYear = _timeZone.GetGreekNow().Year;
```

#### Γραμμή 573: GetMonthlyExpensesAsync
```csharp
// ❌ ΛΑΘΟΣ
var startDate = new DateTime(year, month, 1);

// ✅ ΣΩΣΤΟ
var startDate = DateTime.SpecifyKind(new DateTime(year, month, 1), DateTimeKind.Utc);
```

#### Γραμμές 628-632: ValidateExpenseAsync
```csharp
// ❌ ΛΑΘΟΣ
if (expense.Date > DateTime.Now.Date)
    errors.Add("Η ημερομηνία δεν μπορεί να είναι μελλοντική");

if (expense.Date < DateTime.Now.AddYears(-2))
    errors.Add("Η ημερομηνία δεν μπορεί να είναι παλαιότερη των 2 ετών");

// ✅ ΣΩΣΤΟ
var greekToday = _timeZone.GetGreekNow().Date;
if (_timeZone.ConvertToGreekTime(expense.Date).Date > greekToday)
    errors.Add("Η ημερομηνία δεν μπορεί να είναι μελλοντική");

if (_timeZone.ConvertToGreekTime(expense.Date).Date < greekToday.AddYears(-2))
    errors.Add("Η ημερομηνία δεν μπορεί να είναι παλαιότερη των 2 ετών");
```

#### Γραμμή 647: GenerateTransactionNumberAsync
```csharp
// ❌ ΛΑΘΟΣ
var currentYear = DateTime.Now.Year;

// ✅ ΣΩΣΤΟ
var currentYear = _timeZone.GetGreekNow().Year;
```

#### Γραμμή 709: NotifyExpenseDecisionAsync
```csharp
// ❌ ΛΑΘΟΣ
<p><strong>Ημερομηνία απόφασης:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>

// ✅ ΣΩΣΤΟ
<p><strong>Ημερομηνία απόφασης:</strong> {_timeZone.FormatGreekDateTime(_timeZone.ConvertToUtc(_timeZone.GetGreekNow()))}</p>
```

**Επίπτωση:** Αριθμοί εξόδων λάθος έτος, validations λάθος

---

### 3. **PaymentService.cs**
**Αρχείο:** `MembersHub.Application/Services/PaymentService.cs`

#### Γραμμή 119: GetTodayCollectionsAsync
```csharp
// ❌ ΛΑΘΟΣ
var today = DateTime.Today;
var tomorrow = today.AddDays(1);

// ✅ ΣΩΣΤΟ
var greekToday = _timeZone.GetGreekNow().Date;
var todayUtcStart = _timeZone.ConvertToUtc(greekToday);
var todayUtcEnd = _timeZone.ConvertToUtc(greekToday.AddDays(1).AddSeconds(-1));
```

#### Γραμμή 258: GenerateReceiptNumberAsync
```csharp
// ❌ ΛΑΘΟΣ
var year = DateTime.Now.Year;

// ✅ ΣΩΣΤΟ
var year = _timeZone.GetGreekNow().Year;
```

**Επίπτωση:** "Σημερινές εισπράξεις" λάθος timezone, αριθμοί αποδείξεων λάθος έτος

---

### 4. **SubscriptionService.cs**
**Αρχείο:** `MembersHub.Application/Services/SubscriptionService.cs`

#### Γραμμή 197: GenerateSubscriptionsAsync
```csharp
// ❌ ΛΑΘΟΣ
var dueDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

// ✅ ΣΩΣΤΟ
var dueDate = _timeZone.ConvertToUtc(new DateTime(year, month, DateTime.DaysInMonth(year, month)));
```

#### Γραμμή 244: GetOverdueSubscriptionsAsync
```csharp
// ❌ ΛΑΘΟΣ
var today = DateTime.Today;

// ✅ ΣΩΣΤΟ
var todayUtc = _timeZone.GetGreekToday();
```

#### Γραμμή 327: SendOverdueNotificationsAsync
```csharp
// ❌ ΛΑΘΟΣ
var today = DateTime.Today;

// ✅ ΣΩΣΤΟ
var todayUtc = _timeZone.GetGreekToday();
```

#### Γραμμή 463: ValidateSubscriptionAsync
```csharp
// ❌ ΛΑΘΟΣ
if (subscription.Year < 2020 || subscription.Year > DateTime.Now.Year + 5)

// ✅ ΣΩΣΤΟ
if (subscription.Year < 2020 || subscription.Year > _timeZone.GetGreekNow().Year + 5)
```

#### Γραμμή 505: GetSubscriptionStatusSummaryAsync
```csharp
// ❌ ΛΑΘΟΣ
? (DateTime.Today - subscription.DueDate).Days

// ✅ ΣΩΣΤΟ
? (_timeZone.GetGreekNow().Date - _timeZone.ConvertToGreekTime(subscription.DueDate).Date).Days
```

**Επίπτωση:** Ληξιπρόθεσμες συνδρομές υπολογίζονται λάθος

---

## 🟡 ΜΕΤΡΙΕΣ ΑΛΛΑΓΕΣ (Razor Components)

### 5. **CashierHandoverPage.razor**
**Αρχείο:** `MembersHub.Web/Components/Pages/CashierHandoverPage.razor`

Χρειάζεται TimeZoneService injection και χρήση του για date ranges.

---

### 6. **MemberSubscriptionStatus.razor**
**Αρχείο:** `MembersHub.Web/Components/Pages/MemberSubscriptionStatus.razor`

Εμφάνιση ημερομηνιών σε Ελληνική ώρα.

---

### 7. **PaymentManagement.razor**
**Αρχείο:** `MembersHub.Web/Components/Financial/PaymentManagement.razor`

Date ranges και φίλτρα.

---

### 8. **ExpenseDialog.razor**
**Αρχείο:** `MembersHub.Web/Components/Dialogs/ExpenseDialog.razor`

Επιλογή ημερομηνίας εξόδου.

---

### 9. **NewPayment.razor**
**Αρχείο:** `MembersHub.Web/Components/Pages/NewPayment.razor`

Ημερομηνία πληρωμής default σε σημερινή Ελληνική.

---

### 10. **SubscriptionManagement.razor**
**Αρχείο:** `MembersHub.Web/Components/Financial/SubscriptionManagement.razor**

Εμφάνιση due dates.

---

### 11. **GenerateSubscriptionsDialog.razor**
**Αρχείο:** `MembersHub.Web/Components/Financial/GenerateSubscriptionsDialog.razor`

Επιλογή έτους/μήνα.

---

### 12. **AuditLogs.razor**
**Αρχείο:** `MembersHub.Web/Components/Pages/AuditLogs.razor`

Εμφάνιση timestamps σε Ελληνική ώρα.

---

### 13. **PaymentDialog.razor**, **PendingSubscriptionsDialog.razor**, **MemberFinancialCardDialog.razor**
Εμφάνιση ημερομηνιών.

---

## ✅ ΗΔΗ ΔΙΟΡΘΩΜΕΝΑ

- ✅ `TimeZoneService.cs` - Δημιουργήθηκε
- ✅ `FinancialDashboard.razor` - Ενημερώθηκε
- ✅ `CollectorCard.razor` - Ενημερώθηκε
- ✅ `FinancialService.cs` - Ενημερώθηκε (μερικώς)
- ✅ `MembersHubContext.cs` - Seed data έχει ήδη UTC (DateTimeKind.Utc)

---

## 🔧 ΒΗΜΑΤΑ ΥΛΟΠΟΙΗΣΗΣ

### Βήμα 1: Προσθήκη TimeZoneService σε Application Layer
```csharp
// MembersHub.Application/DependencyInjection.cs
services.AddSingleton<TimeZoneService>();
```

### Βήμα 2: Μετακίνηση TimeZoneService
Μετακίνηση από `MembersHub.Web/Services` → `MembersHub.Application/Services`
ώστε να είναι διαθέσιμο και στα backend services.

### Βήμα 3: Inject σε κάθε Service
```csharp
private readonly TimeZoneService _timeZone;

public CashierHandoverService(..., TimeZoneService timeZone)
{
    _timeZone = timeZone;
}
```

### Βήμα 4: Αντικατάσταση DateTime.Now/Today
Βλέπε παραπάνω αναλυτικές αλλαγές.

### Βήμα 5: Testing
- Test με πραγματικά δεδομένα
- Verify ότι οι ώρες εμφανίζονται σωστά
- Verify ότι τα queries επιστρέφουν σωστά αποτελέσματα

---

## ⚠️ ΣΗΜΑΝΤΙΚΕΣ ΣΗΜΕΙΩΣΕΙΣ

1. **PostgreSQL timestamp with time zone**: Αποθηκεύει πάντα UTC, μετατρέπει αυτόματα
2. **MudBlazor DatePicker**: Επιστρέφει local DateTime - χρειάζεται ConvertToUtc
3. **Entity Framework**: Όταν διαβάζει από DB, επιστρέφει UTC DateTime
4. **Εμφάνιση στον χρήστη**: Πάντα μετατροπή σε Greek time πριν εμφανιστεί
5. **Comparisons**: Πάντα σύγκριση UTC με UTC, ή Greek με Greek

---

## 📝 ΠΑΡΑΔΕΙΓΜΑ ΡΟΗΣ

```csharp
// 1. User επιλέγει ημερομηνία στο UI (π.χ. 15/01/2025)
DateTime userSelectedDate = datePicker.Value; // Local: 15/01/2025 00:00:00 (Kind=Unspecified)

// 2. Μετατροπή σε UTC για αποθήκευση
DateTime utcDate = _timeZone.ConvertToUtc(userSelectedDate); // UTC: 14/01/2025 22:00:00 (Greece is UTC+2)

// 3. Αποθήκευση στη βάση
expense.Date = utcDate;
await _context.SaveChangesAsync();

// 4. Ανάγνωση από βάση
var expense = await _context.Expenses.FindAsync(id); // expense.Date is UTC

// 5. Εμφάνιση στον χρήστη
string displayDate = _timeZone.FormatGreekDateTime(expense.Date); // "15/01/2025 00:00"
```

---

## 🎯 ΠΡΟΤΕΡΑΙΟΤΗΤΕΣ

1. **ΥΨΗΛΗ**: Backend Services (CashierHandoverService, ExpenseService, PaymentService, SubscriptionService)
2. **ΜΕΣΑΙΑ**: Dialogs που δημιουργούν δεδομένα (ExpenseDialog, PaymentDialog, NewPayment)
3. **ΧΑΜΗΛΗ**: Read-only components που μόνο εμφανίζουν (AuditLogs, Reports)

---

## 📊 ΣΤΑΤΙΣΤΙΚΑ

- **Συνολικά αρχεία με DateTime.Now/Today**: 24
- **Backend Services που χρειάζονται αλλαγή**: 4
- **Razor Components που χρειάζονται αλλαγή**: 15
- **Ήδη διορθωμένα**: 5
- **Εναπομένοντα**: 19

---

**Τελευταία ενημέρωση:** 2025-01-08
**Status:** 🔴 Σε εξέλιξη
