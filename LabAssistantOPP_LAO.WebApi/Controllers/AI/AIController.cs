using LabAssistantOPP_LAO.DTO.DTOs.Grading;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LabAssistantOPP_LAO.DTO.DTOs.AI;
using Business_Logic.Interfaces.AI;

namespace LabAssistantOPP_LAO.WebApi.Controllers.AI
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;
        private readonly LabOopChangeV6Context _context;

        public AIController(IAIService aiService, ILogger<AIController> logger, LabOopChangeV6Context context)
        {
            _aiService = aiService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Upload PDF đề bài và ingest vào RAG system (chỉ Head Subject)
        /// </summary>
        [HttpPost("ingest-pdf")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Head Subject")]
        public async Task<IActionResult> IngestPDF([FromForm] IngestPdfRequest request)
        {
            try
            {
                if (request.PdfFile == null || request.PdfFile.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("PDF file is required"));

                // Validate assignment exists in database
                var assignment = await _context.LabAssignments.FindAsync(request.AssignmentId);
                if (assignment == null)
                    return NotFound(ApiResponse<object>.ErrorResponse($"Assignment with ID {request.AssignmentId} not found"));

                // 1. Lưu metadata vào Assignment_Document
                var fileName = $"{Guid.NewGuid()}_{request.PdfFile.FileName}";
                var filePath = $"/uploads/pdf/{fileName}";
                
                var document = new AssignmentDocument
                {
                    AssignmentId = request.AssignmentId,
                    FileName = request.PdfFile.FileName,
                    FilePath = filePath,
                    MimeType = request.PdfFile.ContentType,
                    UploadedBy = null, // TODO: Get from current user
                    UploadedAt = DateTime.UtcNow
                };

                _context.AssignmentDocuments.Add(document);
                await _context.SaveChangesAsync();

                // 2. Lưu file vật lý
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pdf");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var physicalFilePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(physicalFilePath, FileMode.Create))
                {
                    await request.PdfFile.CopyToAsync(stream);
                }

                // 3. Gọi RAG service để xử lý
                var result = await _aiService.IngestPDFAsync(request.PdfFile, request.AssignmentId);

                if (result.Success)
                {
                    // 4. Tạo VectorIndex record
                    var vectorIndex = new VectorIndex
                    {
                        Provider = "qdrant",
                        IndexName = "assignments",
                        ExternalId = $"assignment_{request.AssignmentId}",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.VectorIndices.Add(vectorIndex);
                    await _context.SaveChangesAsync();

                    // 5. Tạo AssignmentIngest record
                    var assignmentIngest = new AssignmentIngest
                    {
                        AssignmentId = request.AssignmentId,
                        DocumentId = document.Id,
                        VectorIndexId = vectorIndex.Id,
                        ChunkSize = 1000, // Default chunk size
                        ChunkOverlap = 200, // Default overlap
                        ChunksIngested = result.Chunks,
                        LastChunkedAt = DateTime.UtcNow,
                        Status = "Success",
                        Message = "PDF successfully ingested and chunked"
                    };

                    _context.AssignmentIngests.Add(assignmentIngest);
                    await _context.SaveChangesAsync();

                    return Ok(ApiResponse<object>.SuccessResponse(
                        new { 
                            assignmentId = request.AssignmentId,
                            assignmentTitle = assignment.Title,
                            documentId = document.Id,
                            vectorIndexId = vectorIndex.Id,
                            ingestId = assignmentIngest.Id,
                            chunks = result.Chunks,
                            source = result.Source,
                            status = result.Status,
                            message = "PDF ingested successfully with metadata saved",
                            rawResponse = result.RawResponse
                        },
                        "PDF has been processed and stored in RAG system with metadata"
                    ));
                }
                else
                {
                    // Nếu RAG service fail, xóa document đã tạo
                    _context.AssignmentDocuments.Remove(document);
                    await _context.SaveChangesAsync();
                    
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.Error));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting PDF");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to ingest PDF"));
            }
        }

        /// <summary>
        /// Review code của student khi nộp bài (chỉ Student)
        /// </summary>
        [HttpPost("review-code")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReviewCode([FromBody] ReviewCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.StudentCode))
                    return BadRequest(ApiResponse<object>.ErrorResponse("StudentCode is required"));

                var result = await _aiService.ReviewCodeAsync(request.AssignmentId, request.StudentCode);

                if (result.ReviewAllowed)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(
                        new
                        {
                            assignmentId = result.AssignmentId,
                            review = result.Review,
                            hasErrors = result.HasErrors,
                            errorCount = result.ErrorCount,
                            summary = result.Summary,
                            rawResponse = result.RawResponse
                        },
                        "Code review completed successfully"
                    ));
                }
                else
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.Error));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing code");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to review code"));
            }
        }

        /// <summary>
        /// Suggest test cases for assignment based on RAG context
        /// </summary>
        [HttpPost("suggest-testcases")]
        [Authorize(Roles = "Student,Teacher,Head Subject")]
        public async Task<IActionResult> SuggestTestCases([FromBody] SuggestTestCasesRequest request)
        {
            try
            {
                if (request.AssignmentId <= 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("AssignmentId is required"));

                var result = await _aiService.SuggestTestCasesAsync(request.AssignmentId);

                if (result.Success)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(
                        new
                        {
                            assignmentId = result.AssignmentId,
                            testCases = result.TestCases,
                            suggestions = result.Suggestions,
                            rawResponse = result.RawResponse
                        },
                        "Test cases suggested successfully"
                    ));
                }
                else
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.Error));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting test cases");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to suggest test cases"));
            }
        }

        /// <summary>
        /// Health check cho AI services
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return Ok(ApiResponse<object>.SuccessResponse(
                new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    services = new
                    {
                        ragService = "http://localhost:3001",
                        aiService = "http://localhost:3000"
                    }
                },
                "AI services are running"
            ));
        }
    }

    public class ReviewCodeRequest
    {
        public int AssignmentId { get; set; }
        public string StudentCode { get; set; } = "";
    }
}
