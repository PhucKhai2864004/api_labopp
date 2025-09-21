using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.EntityFrameworkCore;
using Business_Logic.Interfaces.AI;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;

namespace Business_Logic.Services.AI
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;
        private readonly string _ragServiceUrl;
        private readonly string _aiServiceUrl;
        private readonly string _geminiApiKey;
        private readonly LabOopChangeV6Context _context;

        public AIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AIService> logger,
            LabOopChangeV6Context context)
        {
            _httpClient = httpClient;
            _logger = logger;
            _ragServiceUrl = configuration["AIServices:RAGServiceUrl"] ?? "http://localhost:3001";
            _aiServiceUrl = configuration["AIServices:AIServiceUrl"] ?? "http://localhost:3000";
            _geminiApiKey = configuration["AIServices:GeminiApiKey"] ?? "AIzaSyBS8hjoz4IFozf3-aAerqAUFPzP5Z3qBVY";
            _context = context;
            
            // Set timeout to 120 seconds for RAG operations
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        #region RAG Service Integration

        public async Task<IngestResult> IngestPDFAsync(IFormFile pdfFile, int assignmentId)
        {
            try
            {
                _logger.LogInformation($"Ingesting PDF for assignment {assignmentId}");

                using var formData = new MultipartFormDataContent();
                using var fileStream = pdfFile.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);

                formData.Add(new StringContent(assignmentId.ToString()), "assignmentId");
                formData.Add(streamContent, "pdfFile", pdfFile.FileName);

                var response = await _httpClient.PostAsync($"{_ragServiceUrl}/ingest", formData);

                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"RAG ingest response: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to ingest PDF: {response.StatusCode} - {responseString}");
                    return new IngestResult 
                    { 
                        Success = false, 
                        Error = $"Failed to ingest PDF: {response.StatusCode}",
                        RawResponse = responseString // Luôn trả về raw để debug
                    };
                }

                // Parse JSON response
                try
                {
                    var ragResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

                    var ingestResult = new IngestResult
                    {
                        Success = true,
                        AssignmentId = assignmentId,
                        Chunks = 0,
                        Source = "pdf",
                        RawResponse = responseString
                    };

                    // Extract thông tin từ response
                    if (ragResponse.TryGetProperty("assignmentId", out var idElement))
                    {
                        var idString = idElement.GetString();
                        if (int.TryParse(idString, out int parsedId))
                        {
                            ingestResult.AssignmentId = parsedId;
                        }
                        else
                        {
                            ingestResult.AssignmentId = assignmentId;
                        }
                    }

                    if (ragResponse.TryGetProperty("chunks", out var chunksElement))
                    {
                        ingestResult.Chunks = chunksElement.GetInt32();
                    }

                    if (ragResponse.TryGetProperty("status", out var statusElement))
                    {
                        var status = statusElement.GetString();
                        if (status == "alreadyExists")
                        {
                            ingestResult.Status = "Already exists";
                        }
                        else if (status == "success")
                        {
                            ingestResult.Status = "Success";
                        }
                    }

                    _logger.LogInformation($"✅ Successfully ingested PDF for assignment {assignmentId}");
                    return ingestResult;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Failed to parse RAG response JSON: {responseString}");
                    return new IngestResult
                    {
                        Success = true,
                        AssignmentId = assignmentId,
                        RawResponse = responseString,
                        Status = "Success (raw response)"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ingesting PDF for assignment {assignmentId}");
                return new IngestResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<TestCaseSuggestionResult> SuggestTestCasesAsync(int assignmentId)
        {
            try
            {
                _logger.LogInformation($"Suggesting test cases for assignment {assignmentId}");

                // Kiểm tra xem assignment có RAG data không
                var hasRAGData = await CheckAssignmentHasRAGDataAsync(assignmentId);
                if (!hasRAGData)
                {
                    _logger.LogWarning($"No RAG data found for assignment {assignmentId}");
                    return new TestCaseSuggestionResult
                    {
                        Success = false,
                        Error = "No RAG context available for this assignment. Please ingest PDF first."
                    };
                }

                // Lấy test cases từ database (chỉ input + expected output cho sinh viên)
                var existingTestCases = await _context.TestCases
                    .Where(tc => tc.AssignmentId == assignmentId)
                    .Select(tc => new { 
                        Input = tc.Input, 
                        ExpectedOutput = tc.ExpectedOutput 
                    })
                    .ToListAsync();

                // Gọi RAG service để suggest test cases (cho sinh viên tự test)
                var suggestionRequest = new
                {
                    assignmentId = assignmentId.ToString(),
                    existingTestCases = existingTestCases
                };

                var jsonContent = JsonSerializer.Serialize(suggestionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_ragServiceUrl}/suggest-testcases", content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"RAG test case suggestion response: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to suggest test cases: {response.StatusCode} - {responseString}");
                    return new TestCaseSuggestionResult
                    {
                        Success = false,
                        Error = $"Test case suggestion failed: {response.StatusCode} - {responseString}"
                    };
                }

                // Parse suggestion response
                try
                {
                    _logger.LogInformation($"Parsing response: {responseString}");
                    
                    // Try to parse as dynamic object first
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseString);

                    var suggestionResult = new TestCaseSuggestionResult
                    {
                        Success = true,
                        AssignmentId = assignmentId,
                        RawResponse = responseString
                    };

                    // First try to extract from the main response
                    if (responseObj.TryGetProperty("testCases", out var testCasesElement))
                    {
                        _logger.LogInformation($"Found testCases element with {testCasesElement.GetArrayLength()} items");
                        
                        var testCases = new List<TestCaseSuggestion>();
                        foreach (var testCaseElement in testCasesElement.EnumerateArray())
                        {
                            var testCase = new TestCaseSuggestion();
                            
                            if (testCaseElement.TryGetProperty("input", out var inputElement))
                            {
                                testCase.Input = inputElement.GetString() ?? "";
                            }
                            
                            if (testCaseElement.TryGetProperty("expectedOutput", out var outputElement))
                            {
                                testCase.ExpectedOutput = outputElement.GetString() ?? "";
                            }
                            
                            testCases.Add(testCase);
                        }
                        
                        suggestionResult.TestCases = testCases;
                        _logger.LogInformation($"Extracted {suggestionResult.TestCases.Count} test cases from main response");
                    }
                    else
                    {
                        _logger.LogWarning("No testCases property found in main response, trying rawResponse");
                        
                        // Try to extract from rawResponse (which contains the actual LLM response)
                        if (responseObj.TryGetProperty("rawResponse", out var rawResponseElement))
                        {
                            var rawResponseString = rawResponseElement.GetString();
                            _logger.LogInformation($"Found rawResponse: {rawResponseString}");
                            
                            try
                            {
                                var rawResponseObj = JsonSerializer.Deserialize<JsonElement>(rawResponseString);
                                
                                if (rawResponseObj.TryGetProperty("testCases", out var rawTestCasesElement))
                                {
                                    _logger.LogInformation($"Found testCases in rawResponse with {rawTestCasesElement.GetArrayLength()} items");
                                    
                                    var testCases = new List<TestCaseSuggestion>();
                                    foreach (var testCaseElement in rawTestCasesElement.EnumerateArray())
                                    {
                                        var testCase = new TestCaseSuggestion();
                                        
                                        if (testCaseElement.TryGetProperty("input", out var inputElement))
                                        {
                                            testCase.Input = inputElement.GetString() ?? "";
                                        }
                                        
                                        if (testCaseElement.TryGetProperty("expectedOutput", out var outputElement))
                                        {
                                            testCase.ExpectedOutput = outputElement.GetString() ?? "";
                                        }
                                        
                                        testCases.Add(testCase);
                                    }
                                    
                                    suggestionResult.TestCases = testCases;
                                    _logger.LogInformation($"Extracted {suggestionResult.TestCases.Count} test cases from rawResponse");
                                }
                                else
                                {
                                    _logger.LogWarning("No testCases found in rawResponse either");
                                    suggestionResult.TestCases = new List<TestCaseSuggestion>();
                                }
                                
                                // Extract suggestions from rawResponse
                                if (rawResponseObj.TryGetProperty("suggestions", out var rawSuggestionsElement))
                                {
                                    suggestionResult.Suggestions = rawSuggestionsElement.GetString() ?? "";
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Failed to parse rawResponse JSON");
                                suggestionResult.TestCases = new List<TestCaseSuggestion>();
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No rawResponse property found");
                            suggestionResult.TestCases = new List<TestCaseSuggestion>();
                        }
                    }

                    // Extract suggestions from main response if not already set
                    if (string.IsNullOrEmpty(suggestionResult.Suggestions) && responseObj.TryGetProperty("suggestions", out var suggestionsElement))
                    {
                        suggestionResult.Suggestions = suggestionsElement.GetString() ?? "";
                    }

                    _logger.LogInformation($"✅ Successfully suggested test cases for assignment {assignmentId}");
                    return suggestionResult;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Failed to parse test case suggestion response JSON: {responseString}");
                    return new TestCaseSuggestionResult
                    {
                        Success = true,
                        AssignmentId = assignmentId,
                        TestCases = new List<TestCaseSuggestion>(),
                        Suggestions = "Raw response: " + responseString,
                        RawResponse = responseString
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error suggesting test cases for assignment {assignmentId}");
                return new TestCaseSuggestionResult
                {
                    Success = false,
                    Error = $"Internal error: {ex.Message}"
                };
            }
        }

        public async Task<CodeReviewResult> ReviewCodeAsync(int assignmentId, int submissionId)
        {
            try
            {
                _logger.LogInformation($"Reviewing code for assignment {assignmentId}, submission {submissionId}");

                // 1) Validate prerequisites: must have testcases and grading results
                var hasTestCases = await _context.TestCases.AnyAsync(tc => tc.AssignmentId == assignmentId);
                if (!hasTestCases)
                {
                    _logger.LogWarning("Review blocked: no testcases found for assignment {AssignmentId}", assignmentId);
                    return new CodeReviewResult
                    {
                        ReviewAllowed = false,
                        AssignmentId = assignmentId,
                        SubmissionId = submissionId,
                        Error = "GRADING_REQUIRED: No input/output testcases found. Head Subject must upload testcases before AI review."
                    };
                }

                var gradingResults = await _context.TestCaseResults
                    .Include(r => r.TestCase)
                    .Where(r => r.StudentLabAssignmentId == submissionId)
                    .ToListAsync();

                if (gradingResults == null || gradingResults.Count == 0)
                {
                    _logger.LogWarning("Review blocked: no grading results for submission {SubmissionId}", submissionId);
                    return new CodeReviewResult
                    {
                        ReviewAllowed = false,
                        AssignmentId = assignmentId,
                        SubmissionId = submissionId,
                        Error = "GRADING_REQUIRED: Submission has not been graded yet. Please run grading before AI review."
                    };
                }

                // 2) Extract code from submission ZIP
                string extractedCode = await ExtractCodeFromSubmissionAsync(submissionId);
                
                if (string.IsNullOrEmpty(extractedCode))
                {
                    return new CodeReviewResult
                    {
                        ReviewAllowed = false,
                        AssignmentId = assignmentId,
                        SubmissionId = submissionId,
                        Error = "Could not extract code from submission"
                    };
                }

                _logger.LogInformation($"Extracted code length: {extractedCode.Length}");

                // 3) Prepare testCases and actuals payloads for AI
                var testCases = await _context.TestCases
                    .Where(tc => tc.AssignmentId == assignmentId)
                    .Select(tc => new { input = tc.Input, expectedOutput = tc.ExpectedOutput })
                    .ToListAsync();

                var actuals = gradingResults
                    .Select(r => new
                    {
                        input = r.TestCase?.Input ?? string.Empty,
                        expectedOutput = r.TestCase?.ExpectedOutput ?? string.Empty,
                        actualOutput = r.ActualOutput ?? string.Empty,
                        passed = (bool)r.IsPassed
                    })
                    .ToList();

                // Gọi RAG service để review code
                var reviewRequest = new
                {
                    assignmentId = assignmentId.ToString(),
                    submissionId = submissionId.ToString(),
                    extractedCode,
                    testCases,
                    actuals
                };

                var jsonContent = JsonSerializer.Serialize(reviewRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_ragServiceUrl}/review-code", content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"RAG review response: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to review code: {response.StatusCode} - {responseString}");
                    return new CodeReviewResult
                    {
                        ReviewAllowed = false,
                        AssignmentId = assignmentId,
                        SubmissionId = submissionId,
                        Error = $"Review failed: {response.StatusCode} - {responseString}"
                    };
                }

                // Parse review response
                try
                {
                    var reviewResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

                    var reviewResult = new CodeReviewResult
                    {
                        ReviewAllowed = true,
                        AssignmentId = assignmentId,
                        SubmissionId = submissionId,
                        RawResponse = responseString
                    };

                    // Extract review data
                    if (reviewResponse.TryGetProperty("review", out var reviewElement))
                    {
                        reviewResult.Review = reviewElement.GetString() ?? "";
                    }

                    if (reviewResponse.TryGetProperty("hasErrors", out var hasErrorsElement))
                    {
                        reviewResult.HasErrors = hasErrorsElement.GetBoolean();
                    }

                    if (reviewResponse.TryGetProperty("errorCount", out var errorCountElement))
                    {
                        reviewResult.ErrorCount = errorCountElement.GetInt32();
                    }

                    if (reviewResponse.TryGetProperty("summary", out var summaryElement))
                    {
                        reviewResult.Summary = summaryElement.GetString();
                    }

                    _logger.LogInformation($"✅ Successfully reviewed code for assignment {assignmentId}, submission {submissionId}");
                    return reviewResult;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Failed to parse review response JSON: {responseString}");
                    return new CodeReviewResult
                    {
                        ReviewAllowed = true,
                        AssignmentId = assignmentId,
                        SubmissionId = submissionId,
                        Review = responseString,
                        RawResponse = responseString
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reviewing code for assignment {assignmentId}, submission {submissionId}");
                return new CodeReviewResult
                {
                    ReviewAllowed = false,
                    AssignmentId = assignmentId,
                    SubmissionId = submissionId,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Extract code from submission ZIP file
        /// </summary>
        private async Task<string> ExtractCodeFromSubmissionAsync(int submissionId)
        {
            try
            {
                _logger.LogInformation($"Extracting code from submission {submissionId}");

                var submission = await _context.StudentLabAssignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                {
                    _logger.LogWarning($"Submission {submissionId} not found");
                    return string.Empty;
                }

                if (string.IsNullOrWhiteSpace(submission.SubmissionZip))
                {
                    _logger.LogWarning($"Submission {submissionId} has no zip path");
                    return string.Empty;
                }

                // submission_zip is stored as a path relative to wwwroot
                var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var zipAbsolutePath = Path.GetFullPath(Path.Combine(wwwroot, submission.SubmissionZip.Replace('/', Path.DirectorySeparatorChar)));

                if (!System.IO.File.Exists(zipAbsolutePath))
                {
                    _logger.LogWarning($"Zip file not found at {zipAbsolutePath}");
                    return string.Empty;
                }

                // Create temp working directory
                var tempRoot = Path.Combine(Path.GetTempPath(), "lao_review", Guid.NewGuid().ToString("N"));
                var extractedDir = Path.Combine(tempRoot, "extracted");
                Directory.CreateDirectory(extractedDir);

                // Security settings
                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".java", ".cpp", ".py"
                };
                const long perFileLimitBytes = 2L * 1024 * 1024; // 2MB per file
                const long totalLimitBytes = 50L * 1024 * 1024;  // 50MB total

                long totalBytes = 0;

                using (var zipStream = System.IO.File.OpenRead(zipAbsolutePath))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            continue; // skip directories

                        // Path traversal protection
                        var normalized = entry.FullName.Replace('\\', '/');
                        if (normalized.Contains(".."))
                            continue;

                        var ext = Path.GetExtension(entry.Name);
                        if (!allowedExtensions.Contains(ext))
                            continue;

                        if (entry.Length <= 0 || entry.Length > perFileLimitBytes)
                            continue;

                        if (totalBytes + entry.Length > totalLimitBytes)
                            break;

                        var destinationPath = Path.Combine(extractedDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        var destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrEmpty(destinationDir))
                            Directory.CreateDirectory(destinationDir);

                        using var entryStream = entry.Open();
                        using var outStream = System.IO.File.Create(destinationPath);
                        await entryStream.CopyToAsync(outStream);
                        totalBytes += entry.Length;
                    }
                }

                // Read back allowed files and concatenate
                var builder = new StringBuilder();
                if (Directory.Exists(extractedDir))
                {
                    foreach (var filePath in Directory.EnumerateFiles(extractedDir, "*.*", SearchOption.AllDirectories)
                        .Where(p => allowedExtensions.Contains(Path.GetExtension(p))))
                    {
                        var relative = Path.GetRelativePath(extractedDir, filePath).Replace('\\', '/');
                        builder.AppendLine($"// File: {relative}");
                        var content = await System.IO.File.ReadAllTextAsync(filePath);
                        builder.AppendLine(content);
                        builder.AppendLine();
                    }
                }

                // Cleanup (best-effort)
                try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true); } catch { }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting code from submission {submissionId}");
                return string.Empty;
            }
        }

        private async Task<bool> CheckAssignmentHasRAGDataAsync(int assignmentId)
        {
            try
            {
                _logger.LogInformation($"Checking RAG data for assignment {assignmentId} at {_ragServiceUrl}/check-assignment/{assignmentId}");
                
                var response = await _httpClient.GetAsync($"{_ragServiceUrl}/check-assignment/{assignmentId}");
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"RAG check response: {response.StatusCode} - {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    var checkResponse = JsonSerializer.Deserialize<JsonElement>(responseString);
                    if (checkResponse.TryGetProperty("exists", out var existsElement))
                    {
                        var exists = existsElement.GetBoolean();
                        _logger.LogInformation($"Assignment {assignmentId} exists in RAG: {exists}");
                        return exists;
                    }
                }

                _logger.LogWarning($"Failed to check RAG data for assignment {assignmentId}: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking RAG data for assignment {assignmentId}");
                return false;
            }
        }

        private async Task<string> GetAssignmentContextAsync(int assignmentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_ragServiceUrl}/assignment-info/{assignmentId}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var infoResponse = JsonSerializer.Deserialize<JsonElement>(responseString);
                    if (infoResponse.TryGetProperty("context", out var contextElement))
                    {
                        return contextElement.GetString() ?? "";
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting assignment context for {assignmentId}");
                return "";
            }
        }

        #endregion

        #region Legacy Methods (for compatibility)

        public async Task<bool> IngestPDFAsync(int assignmentId, IFormFile pdfFile)
        {
            var result = await IngestPDFAsync(pdfFile, assignmentId);
            return result.Success;
        }

        #endregion
    }
}

