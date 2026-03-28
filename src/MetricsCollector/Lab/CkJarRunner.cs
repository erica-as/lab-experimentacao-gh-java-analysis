using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace MetricsCollector.Lab;

/// <summary>
/// Executa o JAR do CK (mauricioaniche/ck) e agrega CBO/DIT/LCOM médios a partir de class.csv.
/// </summary>
public static class CkJarRunner
{
    public sealed record CkAggregates(double AvgCbo, double AvgDit, double AvgLcom, int ClassRows);

    /// <summary>
    /// CK 0.7+ grava <c>class.csv</c> ou <c>{identificador}class.csv</c> no diretório de saída.
    /// </summary>
    public static string ResolveClassCsvPath(string ckOutputDir)
    {
        var classic = Path.Combine(ckOutputDir, "class.csv");
        if (File.Exists(classic))
            return classic;

        var prefixed = Directory.GetFiles(ckOutputDir, "*class.csv", SearchOption.AllDirectories);
        if (prefixed.Length == 1)
            return prefixed[0];
        if (prefixed.Length > 1)
            throw new InvalidOperationException(
                $"Vários *class.csv em {ckOutputDir}: use CK que gera um único ficheiro ou ajuste o runner. Ficheiros: {string.Join(", ", prefixed.Select(Path.GetFileName))}.");

        throw new InvalidOperationException($"CK não gerou class.csv nem *class.csv em {ckOutputDir}");
    }

    private sealed class CkClassRow
    {
        [Name("cbo")]
        public double Cbo { get; set; }

        [Name("dit")]
        public double Dit { get; set; }

        [Name("lcom")]
        public double Lcom { get; set; }
    }

    public static string ResolveJavaExecutable()
    {
        var home = Environment.GetEnvironmentVariable("JAVA_HOME")?.Trim();
        if (!string.IsNullOrEmpty(home))
        {
            var win = Path.Combine(home, "bin", "java.exe");
            var unix = Path.Combine(home, "bin", "java");
            if (File.Exists(win)) return win;
            if (File.Exists(unix)) return unix;
        }

        return OperatingSystem.IsWindows() ? "java.exe" : "java";
    }

    public static async Task RunCkAsync(string ckJarPath, string clonedProjectDir, string ckOutputDir,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ckJarPath))
            throw new FileNotFoundException("CK_JAR não encontrado.", ckJarPath);

        Directory.CreateDirectory(ckOutputDir);
        var ckOutFull = Path.GetFullPath(ckOutputDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        // CK 0.7.0 faz outputDir + "class.csv" sem File.separator; sem barra final vira ".../PastaNomeclass.csv".
        // Com barra final, fica ".../PastaNome/class.csv" como Path.Combine espera.
        var ckOutArgForJava = ckOutFull + Path.DirectorySeparatorChar;

        foreach (var stale in Directory.GetFiles(ckOutFull, "*.csv", SearchOption.AllDirectories))
        {
            try
            {
                File.Delete(stale);
            }
            catch
            {
                /* ignorar ficheiros bloqueados */
            }
        }

        var java = ResolveJavaExecutable();
        var psi = new ProcessStartInfo
        {
            FileName = java,
            WorkingDirectory = ckOutFull,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-jar");
        psi.ArgumentList.Add(Path.GetFullPath(ckJarPath));
        psi.ArgumentList.Add(Path.GetFullPath(clonedProjectDir));
        psi.ArgumentList.Add("false"); // use jars
        psi.ArgumentList.Add("0"); // max files / partição
        psi.ArgumentList.Add("false"); // variable/field metrics (muito volume)
        psi.ArgumentList.Add(ckOutArgForJava);

        Console.WriteLine($"CK: {java} -jar … → {ckOutFull}");
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Não foi possível iniciar Java.");
        var stdoutT = proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrT = proc.StandardError.ReadToEndAsync(cancellationToken);
        // Evita deadlock: enchedor de stderr/stdout bloqueia o JVM se WaitForExit vier antes da leitura.
        await Task.WhenAll(proc.WaitForExitAsync(cancellationToken), stdoutT, stderrT).ConfigureAwait(false);
        var stdout = await stdoutT.ConfigureAwait(false);
        var stderr = await stderrT.ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stdout))
            Console.WriteLine(stdout.TrimEnd());
        if (!string.IsNullOrWhiteSpace(stderr))
            Console.Error.WriteLine("CK stderr: " + stderr.TrimEnd());
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"CK exit {proc.ExitCode}: {stderr}");

        try
        {
            _ = ResolveClassCsvPath(ckOutFull);
        }
        catch (InvalidOperationException ex)
        {
            var listing = Directory.Exists(ckOutFull)
                ? string.Join(", ", Directory.GetFiles(ckOutFull).Select(Path.GetFileName))
                : "(dir inexistente)";
            throw new InvalidOperationException($"{ex.Message} (ficheiros em {ckOutFull}: {listing})", ex);
        }
    }

    public static CkAggregates AggregateFromClassCsv(string ckOutputDir)
    {
        var classCsv = ResolveClassCsvPath(ckOutputDir);
        using var reader = new StreamReader(classCsv);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<CkClassRow>().ToList();
        if (rows.Count == 0)
            throw new InvalidOperationException("class.csv sem linhas de dados.");

        return new CkAggregates(
            rows.Average(r => r.Cbo),
            rows.Average(r => r.Dit),
            rows.Average(r => r.Lcom),
            rows.Count);
    }

    /// <summary>Copia CSVs do CK e grava um resumo textual para o lab.</summary>
    public static void WriteEvidencePack(string repoRoot, string repoNameWithOwner, string ckOutputDir)
    {
        var safe = RepoLayout.SafeDirName(repoNameWithOwner);
        var dest = Path.Combine(repoRoot, "data", "lab02s01_ck_evidence", safe);
        Directory.CreateDirectory(dest);

        foreach (var src in Directory.GetFiles(ckOutputDir, "*.csv", SearchOption.AllDirectories))
            File.Copy(src, Path.Combine(dest, Path.GetFileName(src)), overwrite: true);

        var ag = AggregateFromClassCsv(ckOutputDir);
        var summary = $"""
            Lab02S01 — evidência CK ({repoNameWithOwner})
            Gerado em {DateTimeOffset.Now:O}
            Classes analisadas: {ag.ClassRows}
            Médias (nível classe, class.csv):
              AvgCbo  = {ag.AvgCbo:F4}
              AvgDit  = {ag.AvgDit:F4}
              AvgLcom = {ag.AvgLcom:F4}
            """;
        File.WriteAllText(Path.Combine(dest, "SUMARIO.txt"), summary.Trim());
    }
}
