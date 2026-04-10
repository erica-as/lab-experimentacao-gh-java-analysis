namespace MetricsCollector.Lab;

/// <summary>
/// Conta linhas de código e linhas de comentário em arquivos .java de forma recursiva.
/// Heurística de comentário: linha (trimada) começa com //, /*, ou * (interior de bloco).
/// </summary>
public static class LocCounter
{
    public static (int totalLoc, int commentLines) CountJavaFiles(string projectDir)
    {
        var totalLoc = 0;
        var commentLines = 0;

        foreach (var file in Directory.EnumerateFiles(projectDir, "*.java", SearchOption.AllDirectories))
        {
            try
            {
                foreach (var line in File.ReadLines(file))
                {
                    totalLoc++;
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith('*'))
                        commentLines++;
                }
            }
            catch
            {
                // arquivo ilegível — ignora sem interromper o lote
            }
        }

        return (totalLoc, commentLines);
    }
}
