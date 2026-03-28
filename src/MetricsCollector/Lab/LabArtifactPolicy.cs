namespace MetricsCollector.Lab;

/// <summary>
/// Apaga clones e saída temporária do CK após o Lab02S01 para não ocupar disco.
/// Defina <c>LAB02_KEEP_ARTIFACTS=1</c> (ou true/yes) para preservar <c>artifacts/clones</c> e <c>artifacts/ck_out</c>.
/// </summary>
public static class LabArtifactPolicy
{
    public static bool KeepArtifacts =>
        Truthy(Environment.GetEnvironmentVariable("LAB02_KEEP_ARTIFACTS"));

    private static bool Truthy(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim();
        return raw.Equals("1", StringComparison.OrdinalIgnoreCase)
               || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
               || raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    public static void TryDeleteTree(string path, string label)
    {
        try
        {
            if (!Directory.Exists(path))
                return;
            Directory.Delete(path, recursive: true);
            Console.WriteLine($"Removido ({label}): {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Aviso: não foi possível apagar {label} em {path}: {ex.Message}");
        }
    }
}
