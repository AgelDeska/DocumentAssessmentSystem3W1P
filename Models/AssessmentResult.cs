using System.Text.Json.Serialization;

public class AssessmentResult
{
    public string DocumentName { get; set; } = string.Empty;

    public decimal OverallScore { get; set; }

    public string ScoreLabel { get; set; } = string.Empty;

    public string ScoreSummary { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("documentAnalysis")]
    public DocumentAnalysisResult DocumentAnalysis { get; set; } = new();

    public AssessmentCategory WinningConcept { get; set; } = new();

    public AssessmentCategory WinningTeam { get; set; } = new();

    public AssessmentCategory WinningSystem { get; set; } = new();

    public AssessmentCategory Performance { get; set; } = new();

    public class DocumentAnalysisResult
    {
        public int TotalPagesUploaded { get; set; }

        public int TotalBlankPages { get; set; }

        public List<int> BlankPageNumbers { get; set; } = [];

        public List<PageCharacterCount> PageCharacterCounts { get; set; } = [];

        public List<DocumentAnomaly> Anomalies { get; set; } = [];

        public List<RedundantContent> RedundantContent { get; set; } = [];
    }

    public class PageCharacterCount
    {
        public int Page { get; set; }

        public int CharacterCount { get; set; }

        public bool IsBlank { get; set; }
    }

    public class DocumentAnomaly
    {
        public int Page { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Excerpt { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;
    }

    public class RedundantContent
    {
        public List<int> Pages { get; set; } = [];

        public string Description { get; set; } = string.Empty;
    }

    public class AssessmentCategory
    {
        public decimal Score { get; set; }

        public string Reason { get; set; } = string.Empty;

        public List<ChecklistItem> Checklist { get; set; } = [];

        public List<CrossStepAssessment> CrossStepConsistency { get; set; } = [];

        public List<string> Weaknesses { get; set; } = [];

        public List<string> Recommendations { get; set; } = [];
    }

    public class ChecklistItem
    {
        public string Step { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;

        public int? Page { get; set; }
    }

    public class CrossStepAssessment
    {
        public string FromStep { get; set; } = string.Empty;

        public string ToStep { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;

        public int? Page { get; set; }
    }
}