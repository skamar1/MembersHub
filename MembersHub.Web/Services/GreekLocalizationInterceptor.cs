using MudBlazor;
using Microsoft.Extensions.Localization;

namespace MembersHub.Web.Services;

public class GreekLocalizationInterceptor : ILocalizationInterceptor
{
    private readonly Dictionary<string, string> _translations = new()
    {
        // DataGrid Filter Operators
        { "MudDataGrid.Contains", "περιέχει" },
        { "MudDataGrid.NotContains", "δεν περιέχει" },
        { "MudDataGrid.Equal", "ισούται" },
        { "MudDataGrid.NotEqual", "δεν ισούται" },
        { "MudDataGrid.StartsWith", "αρχίζει με" },
        { "MudDataGrid.EndsWith", "τελειώνει με" },
        { "MudDataGrid.Empty", "είναι κενό" },
        { "MudDataGrid.NotEmpty", "δεν είναι κενό" },
        { "MudDataGrid.GreaterThan", "μεγαλύτερο από" },
        { "MudDataGrid.GreaterThanOrEqual", "μεγαλύτερο ή ίσο" },
        { "MudDataGrid.LessThan", "μικρότερο από" },
        { "MudDataGrid.LessThanOrEqual", "μικρότερο ή ίσο" },

        // Common UI elements
        { "MudDataGrid.Filter", "Φίλτρο" },
        { "MudDataGrid.FilterValue", "Τιμή φίλτρου" },
        { "MudDataGrid.Clear", "Καθαρισμός" },
        { "MudDataGrid.Apply", "Εφαρμογή" },
        { "MudDataGrid.Cancel", "Ακύρωση" },
        { "MudDataGrid.Columns", "Στήλες" },
        { "MudDataGrid.Sort", "Ταξινόμηση" },
        { "MudDataGrid.Unsort", "Αφαίρεση ταξινόμησης" },
        { "MudDataGrid.Hide", "Απόκρυψη" },
        { "MudDataGrid.Group", "Ομαδοποίηση" },
        { "MudDataGrid.Ungroup", "Αφαίρεση ομαδοποίησης" },

        // Pagination
        { "MudTablePager.RowsPerPage", "Γραμμές ανά σελίδα:" },
        { "MudTablePager.FirstPageTooltip", "Πρώτη σελίδα" },
        { "MudTablePager.PreviousPageTooltip", "Προηγούμενη σελίδα" },
        { "MudTablePager.NextPageTooltip", "Επόμενη σελίδα" },
        { "MudTablePager.LastPageTooltip", "Τελευταία σελίδα" },

        // Dialog
        { "MudDialog.Ok", "OK" },
        { "MudDialog.Cancel", "Ακύρωση" },

        // File Upload
        { "MudFileUpload.DropZone", "Σύρετε αρχεία εδώ ή κάντε κλικ" },
    };

    public LocalizedString Handle(string key, params object[] arguments)
    {
        if (_translations.TryGetValue(key, out var translation))
        {
            return new LocalizedString(key, translation);
        }

        return new LocalizedString(key, key);
    }
}
