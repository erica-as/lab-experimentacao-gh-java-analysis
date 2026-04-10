namespace MetricsCollector.Models
{
    public class RepositoryData
    {
        public string? Name { get; set; } 
        public int Stars { get; set; } 
        public DateTime CreatedAt { get; set; } 
        public int AgeInYears { get; set; } 
        public string? Url { get; set; } 
        public int ReleasesCount { get; set; }
        public double AvgCbo { get; set; } 
        public double AvgDit { get; set; } 
        public double AvgLcom { get; set; }

        /// <summary>Nº de linhas de classe no class.csv após CK; 0 = ainda não processado (útil com --ck-resume).</summary>
        public int CkClassRows { get; set; }

        /// <summary>Total de linhas de código .java (LOC); 0 = ainda não calculado (--loc-all).</summary>
        public int TotalLoc { get; set; }

        /// <summary>Linhas de comentário .java (heurística: inicia com //, /*, ou * em bloco); 0 = ainda não calculado.</summary>
        public int CommentLines { get; set; }
    }
}