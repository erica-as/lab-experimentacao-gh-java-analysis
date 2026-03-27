using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using System.Globalization;
using MetricsCollector.Models;

namespace MetricsCollector
{
    public static class CsvExporter
    {
        public static void SaveToCsv(List<RepositoryData> data, string fileName)
        {
            // Localiza a raiz do repositório procurando um arquivo .sln subindo a árvore de diretórios
            string FindRepoRoot()
            {
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (dir != null)
                {
                    var slnFiles = dir.GetFiles("*.sln");
                    if (slnFiles.Length > 0) return dir.FullName;
                    dir = dir.Parent;
                }
                return Directory.GetCurrentDirectory();
            }

            var repoRoot = FindRepoRoot();
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
