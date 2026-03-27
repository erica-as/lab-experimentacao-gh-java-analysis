using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using MetricsCollector.Lab;
using MetricsCollector.Models;

namespace MetricsCollector
{
    public static class CsvExporter
    {
        public static List<RepositoryData> LoadFromCsv(string fileName)
        {
            var repoRoot = RepoLayout.FindRepoRoot();
            var outputDir = Path.Combine(repoRoot, "data");
            var fullPath = Path.Combine(outputDir, Path.GetFileName(fileName));
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"CSV não encontrado: {fullPath}. Corra antes a coleta ou use o caminho certo.");

            using var reader = new StreamReader(fullPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<RepositoryData>().ToList();
        }

        public static void SaveToCsv(List<RepositoryData> data, string fileName)
        {
            var repoRoot = RepoLayout.FindRepoRoot();
            var outputDir = Path.Combine(repoRoot, "data");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            var outputFileName = Path.GetFileName(fileName);
            var fullPath = Path.Combine(outputDir, outputFileName);

            using (var writer = new StreamWriter(fullPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(data);
            }
            Console.WriteLine($"Arquivo {fullPath} gerado com sucesso.");
        }
    }
}
