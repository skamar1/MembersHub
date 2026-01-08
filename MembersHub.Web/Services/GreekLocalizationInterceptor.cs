using MudBlazor;
using Microsoft.Extensions.Localization;

namespace MembersHub.Web.Services;

public class GreekLocalizationInterceptor : ILocalizationInterceptor
{
    private readonly Dictionary<string, string> _translations = new()
    {
        // DataGrid Filter Operators
        { "MudDataGrid_Contains", "περιέχει" },
        { "MudDataGrid_NotContains", "δεν περιέχει" },
        { "MudDataGrid_Equals", "ισούται" },
        { "MudDataGrid_NotEquals", "δεν ισούται" },
        { "MudDataGrid_StartsWith", "αρχίζει με" },
        { "MudDataGrid_EndsWith", "τελειώνει με" },
        { "MudDataGrid_IsEmpty", "είναι κενό" },
        { "MudDataGrid_IsNotEmpty", "δεν είναι κενό" },
        { "MudDataGrid_GreaterThan", "μεγαλύτερο από" },
        { "MudDataGrid_GreaterThanOrEqual", "μεγαλύτερο ή ίσο" },
        { "MudDataGrid_LessThan", "μικρότερο από" },
        { "MudDataGrid_LessThanOrEqual", "μικρότερο ή ίσο" },

        // Common UI elements
        { "MudDataGrid_Filter", "Φίλτρο" },
        { "MudDataGrid_FilterValue", "Τιμή φίλτρου" },
        { "MudDataGrid_Clear", "Καθαρισμός" },
        { "MudDataGrid_Apply", "Εφαρμογή" },
        { "MudDataGrid_Cancel", "Ακύρωση" },
        { "MudDataGrid_Columns", "Στήλες" },
        { "MudDataGrid_Sort", "Ταξινόμηση" },
        { "MudDataGrid_Unsort", "Αφαίρεση ταξινόμησης" },
        { "MudDataGrid_Hide", "Απόκρυψη" },
        { "MudDataGrid_Group", "Ομαδοποίηση" },
        { "MudDataGrid_Ungroup", "Αφαίρεση ομαδοποίησης" },

        // Pagination - MudTable
        { "MudTablePager_RowsPerPage", "Γραμμές ανά σελίδα:" },
        { "MudTablePager_FirstPageTooltip", "Πρώτη σελίδα" },
        { "MudTablePager_PreviousPageTooltip", "Προηγούμενη σελίδα" },
        { "MudTablePager_NextPageTooltip", "Επόμενη σελίδα" },
        { "MudTablePager_LastPageTooltip", "Τελευταία σελίδα" },

        // Pagination - MudDataGrid
        { "MudDataGridPager_RowsPerPage", "Γραμμές ανά σελίδα:" },
        { "MudDataGridPager_InfoFormat", "{first_item}-{last_item} από {all_items}" },

        // Dialog
        { "MudDialog_Ok", "OK" },
        { "MudDialog_Cancel", "Ακύρωση" },

        // File Upload
        { "MudFileUpload_DropZone", "Σύρετε αρχεία εδώ ή κάντε κλικ" },
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
