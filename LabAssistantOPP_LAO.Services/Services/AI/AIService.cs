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

namespace Business_Logic.Services.AI
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;
        private readonly string _ragServiceUrl;
        private readonly string _aiServiceUrl;
        private readonly string _geminiApiKey;

        public AIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _ragServiceUrl = configuration["AIServices:RAGServiceUrl"] ?? "http://localhost:3001";
            _aiServiceUrl = configuration["AIServices:AIServiceUrl"] ?? "http://localhost:3000";
            _geminiApiKey = configuration["AIServices:GeminiApiKey"] ?? "AIzaSyDxFNK8N6Y9bkLkNwhoENVhq-gNHH3UrnY";
            
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

                // Gọi RAG service để suggest test cases (chỉ cần assignmentId)
                var suggestionRequest = new
                {
                    assignmentId = assignmentId.ToString()
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

                // TODO: Cần inject DbContext để lấy submission data
                // Tạm thời sử dụng placeholder logic
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

                // Gọi RAG service để review code
                var reviewRequest = new
                {
                    assignmentId = assignmentId.ToString(),
                    submissionId = submissionId.ToString(),
                    extractedCode
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
                // TODO: Implement actual logic to:
                // 1. Get submission from database
                // 2. Extract ZIP file
                // 3. Read all code files
                // 4. Return concatenated code
                
                _logger.LogInformation($"Extracting code from submission {submissionId}");
                
                // Placeholder implementation
                // In real implementation, this would:
                // - Query database for submission
                // - Get file path to ZIP
                // - Extract ZIP to temp directory
                // - Read all .java, .cpp, .py files
                // - Concatenate with file headers
                
                return "// Placeholder extracted code\npublic class Main {\n    public static void main(String[] args) {\n        // Student code would be here\n    }\n}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting code from submission {submissionId}");
                return "";
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

