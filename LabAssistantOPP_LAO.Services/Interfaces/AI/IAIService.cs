using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace Business_Logic.Interfaces.AI
{
    public interface IAIService
    {
        // RAG Service Integration
        Task<IngestResult> IngestPDFAsync(IFormFile pdfFile, int assignmentId);
        Task<CodeReviewResult> ReviewCodeAsync(int assignmentId, string studentCode);
        Task<TestCaseSuggestionResult> SuggestTestCasesAsync(int assignmentId);

        // Legacy Methods (for compatibility)
        Task<bool> IngestPDFAsync(int assignmentId, IFormFile pdfFile);
        Task<CodeReviewResult> ReviewStudentSubmissionAsync(int assignmentId, string studentCode);
    }

    public class IngestResult
    {
        public bool Success { get; set; }
        public int AssignmentId { get; set; }
        public int Chunks { get; set; }
        public string Source { get; set; } = "";
        public string Status { get; set; } = "";
        public string Error { get; set; } = "";
        public string RawResponse { get; set; } = "";
    }

    public class CodeReviewResult
    {
        public bool ReviewAllowed { get; set; }
        public int AssignmentId { get; set; }
        public string Review { get; set; } = "";
        public bool HasErrors { get; set; }
        public int ErrorCount { get; set; }
        public string Summary { get; set; } = "";
        public string Error { get; set; } = "";
        public string RawResponse { get; set; } = "";
    }

    public class TestCaseSuggestionResult
    {
        public bool Success { get; set; }
        public int AssignmentId { get; set; }
        public List<TestCaseSuggestion> TestCases { get; set; } = new();
        public string Suggestions { get; set; } = "";
        public string Error { get; set; } = "";
        public string RawResponse { get; set; } = "";
    }

    public class TestCaseSuggestion
    {
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
    }
}
