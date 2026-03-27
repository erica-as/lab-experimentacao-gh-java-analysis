using System.Diagnostics;

namespace MetricsCollector.Lab;

public static class GitRepositoryCloner
{
    /// <summary>git clone --depth 1 (um único branch remoto).</summary>
    public static async Task CloneIfNeededAsync(string cloneUrl, string destinationDir, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(Path.Combine(destinationDir, ".git")))
        {
            Console.WriteLine($"Clone já existe, reutilizando: {destinationDir}");
            return;
        }

        if (Directory.Exists(destinationDir))
            Directory.Delete(destinationDir, recursive: true);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationDir) ?? ".");

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("clone");
        psi.ArgumentList.Add("--depth");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add(cloneUrl);
        psi.ArgumentList.Add(destinationDir);

        Console.WriteLine($"git clone -> {destinationDir}");
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Não foi possível iniciar git.");
        await proc.WaitForExitAsync(cancellationToken);
        var err = await proc.StandardError.ReadToEndAsync(cancellationToken);
        _ = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"git clone falhou (exit {proc.ExitCode}): {err}");
    }
}
