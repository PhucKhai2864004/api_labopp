# Review Code Flow Update - SubmissionId Implementation

## 🎯 Tổng quan thay đổi

Đã cập nhật flow review code từ việc sử dụng `studentCode` (string) sang `submissionId` (int) để phù hợp với logic thực tế của hệ thống.

## 🔄 Flow mới

### **Trước đây (Sai):**
```
Frontend → Submit studentCode (string) → AIController → AIService → RAG Service
```

### **Bây giờ (Đúng):**
```
Frontend → Submit submissionId (int) → AIController → AIService → Extract ZIP → RAG Service
```

## 📝 Chi tiết thay đổi

### 1. **DTO Changes**

**File:** `LabAssistantOPP_LAO.DTO/DTOs/AI/ReviewCodeRequest.cs`

```csharp
// Trước:
public class ReviewCodeRequest
{
    public int AssignmentId { get; set; }
    public string StudentCode { get; set; } = "";
}

// Sau:
public class ReviewCodeRequest
{
    public int AssignmentId { get; set; }
    public int SubmissionId { get; set; }  // ✅ Thay đổi
}
```

### 2. **Interface Changes**

**File:** `LabAssistantOPP_LAO.Services/Interfaces/AI/IAIService.cs`

```csharp
// Trước:
Task<CodeReviewResult> ReviewCodeAsync(int assignmentId, string studentCode);

// Sau:
Task<CodeReviewResult> ReviewCodeAsync(int assignmentId, int submissionId);  // ✅ Thay đổi
```

### 3. **Service Implementation**

**File:** `LabAssistantOPP_LAO.Services/Services/AI/AIService.cs`

```csharp
// Trước:
public async Task<CodeReviewResult> ReviewCodeAsync(int assignmentId, string studentCode)
{
    // Gửi trực tiếp studentCode đến RAG service
    var reviewRequest = new { assignmentId, studentCode };
}

// Sau:
public async Task<CodeReviewResult> ReviewCodeAsync(int assignmentId, int submissionId)
{
    // 1. Extract code từ submission ZIP
    string extractedCode = await ExtractCodeFromSubmissionAsync(submissionId);
    
    // 2. Gửi extractedCode đến RAG service
    var reviewRequest = new { assignmentId, submissionId, extractedCode };
}
```

### 4. **Controller Changes**

**File:** `LabAssistantOPP_LAO.WebApi/Controllers/AI/AIController.cs`

```csharp
// Trước:
if (string.IsNullOrWhiteSpace(request.StudentCode))
    return BadRequest("StudentCode is required");

var result = await _aiService.ReviewCodeAsync(request.AssignmentId, request.StudentCode);

// Sau:
if (request.SubmissionId <= 0)
    return BadRequest("SubmissionId is required");

var result = await _aiService.ReviewCodeAsync(request.AssignmentId, request.SubmissionId);
```

### 5. **RAG Service Changes**

**File:** `rag-cli-nodejs/rag-service.js`

```javascript
// Trước:
app.post('/review-code', async (req, res) => {
    const { assignmentId, studentCode } = req.body;
    // Xử lý studentCode trực tiếp
});

// Sau:
app.post('/review-code', async (req, res) => {
    const { assignmentId, submissionId, extractedCode } = req.body;
    // Xử lý extractedCode từ submission
});
```

## 🚀 API Endpoint mới

### **Request:**
```http
POST /api/ai/review-code
Content-Type: application/json

{
    "assignmentId": 4,
    "submissionId": 123
}
```

### **Response:**
```json
{
    "success": true,
    "message": "Code review completed successfully",
    "data": {
        "assignmentId": 4,
        "submissionId": 123,
        "review": "Detailed review text...",
        "hasErrors": true,
        "errorCount": 3,
        "summary": "Overall assessment...",
        "rawResponse": "..."
    }
}
```

## 🔧 TODO: Implementation cần hoàn thiện

### **1. ExtractCodeFromSubmissionAsync Method**

Hiện tại đang sử dụng placeholder. Cần implement:

```csharp
private async Task<string> ExtractCodeFromSubmissionAsync(int submissionId)
{
    // 1. Query database để lấy submission
    var submission = await _context.Submissions
        .Include(s => s.Files)
        .FirstOrDefaultAsync(s => s.Id == submissionId);
    
    // 2. Lấy file path của ZIP
    var zipFilePath = submission.ZipFilePath;
    
    // 3. Extract ZIP to temp directory
    var tempDir = Path.GetTempPath() + Guid.NewGuid();
    ZipFile.ExtractToDirectory(zipFilePath, tempDir);
    
    // 4. Read all code files (.java, .cpp, .py, etc.)
    var codeFiles = Directory.GetFiles(tempDir, "*.java", SearchOption.AllDirectories);
    
    // 5. Concatenate với file headers
    var extractedCode = "";
    foreach (var file in codeFiles)
    {
        extractedCode += $"// File: {Path.GetFileName(file)}\n";
        extractedCode += await File.ReadAllTextAsync(file);
        extractedCode += "\n\n";
    }
    
    return extractedCode;
}
```

### **2. Database Integration**

Cần inject `LabOopChangeV6Context` vào `AIService`:

```csharp
public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;
    private readonly LabOopChangeV6Context _context;  // ✅ Thêm DbContext
    
    public AIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AIService> logger,
        LabOopChangeV6Context context)  // ✅ Inject DbContext
    {
        _httpClient = httpClient;
        _logger = logger;
        _context = context;  // ✅ Assign DbContext
        // ... existing code
    }
}
```

### **3. Frontend Integration**

Cần cập nhật frontend để:

1. **Lấy submissionId** từ submission hiện tại
2. **Gọi API** với submissionId thay vì studentCode
3. **Hiển thị kết quả** review

```javascript
// Frontend example
const reviewCode = async (assignmentId, submissionId) => {
    const response = await fetch('/api/ai/review-code', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            assignmentId: assignmentId,
            submissionId: submissionId  // ✅ Sử dụng submissionId
        })
    });
    
    const result = await response.json();
    // Hiển thị review result
};
```

## 🎯 Benefits của thay đổi

### ✅ **Đúng logic nghiệp vụ**
- Sinh viên nộp file ZIP, không phải paste code
- Review dựa trên file thực tế, không phải text input

### ✅ **Bảo mật tốt hơn**
- Không expose code qua API parameters
- Code được extract từ file system

### ✅ **Scalable**
- Có thể handle nhiều file trong ZIP
- Dễ dàng mở rộng cho nhiều ngôn ngữ

### ✅ **Audit trail**
- Có submissionId để track
- Có thể log chi tiết quá trình review

## 🔄 Migration Plan

### **Phase 1: Backend Update** ✅ (Đã hoàn thành)
- [x] Update DTOs
- [x] Update Interfaces
- [x] Update Services
- [x] Update Controllers
- [x] Update RAG Service

### **Phase 2: Database Integration** 🔄 (Cần hoàn thiện)
- [ ] Inject DbContext vào AIService
- [ ] Implement ExtractCodeFromSubmissionAsync
- [ ] Test với real submission data

### **Phase 3: Frontend Update** 🔄 (Cần thực hiện)
- [ ] Update API calls
- [ ] Update UI components
- [ ] Test end-to-end flow

### **Phase 4: Testing & Validation** 🔄 (Cần thực hiện)
- [ ] Unit tests
- [ ] Integration tests
- [ ] End-to-end tests

## 🚨 Breaking Changes

### **API Changes:**
- `POST /api/ai/review-code` now requires `submissionId` instead of `studentCode`
- Response now includes `submissionId` field

### **Frontend Impact:**
- All existing frontend code calling review-code API needs to be updated
- Need to pass `submissionId` instead of `studentCode`

### **Legacy Support:**
- Legacy `ReviewStudentSubmissionAsync` method is deprecated
- Returns error message directing to use new method

## 💡 Next Steps

1. **Hoàn thiện ExtractCodeFromSubmissionAsync** với real database integration
2. **Update frontend** để sử dụng submissionId
3. **Test end-to-end** với real submission data
4. **Documentation** cho frontend developers
5. **Migration guide** cho existing integrations
