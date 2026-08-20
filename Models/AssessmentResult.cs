public class AssessmentResult
{
    public string DocumentName { get; set; } = string.Empty;

    public decimal OverallScore { get; set; }

    public string ScoreLabel { get; set; } = string.Empty;

    public string ScoreSummary { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public AssessmentCategory WinningConcept { get; set; } = new();

    public AssessmentCategory WinningTeam { get; set; } = new();

    public AssessmentCategory WinningSystem { get; set; } = new();

    public AssessmentCategory Performance { get; set; } = new();

    public class AssessmentCategory
    {
        public decimal Score { get; set; }

        public string Reason { get; set; } = string.Empty;

        public List<ChecklistItem> Checklist { get; set; } = [];

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
}
