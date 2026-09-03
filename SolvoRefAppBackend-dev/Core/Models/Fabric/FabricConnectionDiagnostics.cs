namespace Core.Models.Fabric
{
    public class FabricConnectionDiagnostics
    {
        public string DataSource { get; set; } = string.Empty;
        public string InitialCatalog { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string SuserSname { get; set; } = string.Empty;
        public string OriginalLogin { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CurrentUser { get; set; } = string.Empty;
        public string SourceTable { get; set; } = string.Empty;
        public List<string> StatusesFilter { get; set; } = [];
        public int MatchingStatusRowCount { get; set; }
        public int UsableRequiredFieldsRowCount { get; set; }
        public List<FabricJobStatusCount> StatusCounts { get; set; } = [];
        public List<FabricJobStatusCount> AllStatusCounts { get; set; } = [];
        public List<FabricTableCandidate> TableCandidates { get; set; } = [];
        public List<string> DiagnosticsErrors { get; set; } = [];
        public List<FabricConnectionDiagnosticsRow> SampleRows { get; set; } = [];
        public List<FabricConnectionDiagnosticsRow> UnfilteredSampleRows { get; set; } = [];
    }

    public class FabricTableCandidate
    {
        public string Schema { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class FabricJobStatusCount
    {
        public string JobStatus { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class FabricConnectionDiagnosticsRow
    {
        public string JobCode { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string MainCountry { get; set; } = string.Empty;
        public string JobStatus { get; set; } = string.Empty;
    }
}
