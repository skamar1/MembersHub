namespace MembersHub.Shared.Utilities;

public static class GreekNameUtils
{
    /// <summary>
    /// Αφαιρεί το τελικό "ς" από Ελληνικά ονόματα για χρήση σε καλωσορίσματα
    /// π.χ. "Γιάννης" → "Γιάννη", "Κώστας" → "Κώστα"
    /// </summary>
    /// <param name="name">Το όνομα προς επεξεργασία</param>
    /// <returns>Το όνομα χωρίς το τελικό "ς" αν υπάρχει</returns>
    public static string RemoveGreekSuffix(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        // Αφαιρούμε το τελικό "ς" μόνο αν υπάρχει
        if (name.EndsWith("ς") && name.Length > 1)
        {
            return name[..^1];
        }

        return name;
    }

    /// <summary>
    /// Επεξεργάζεται το πλήρες όνομα αφαιρώντας το "ς" από όνομα και επίθετο αν χρειάζεται
    /// </summary>
    /// <param name="fullName">Το πλήρες όνομα</param>
    /// <returns>Το επεξεργασμένο όνομα</returns>
    public static string ProcessGreekFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return fullName;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = RemoveGreekSuffix(parts[i]);
        }

        return string.Join(" ", parts);
    }
}