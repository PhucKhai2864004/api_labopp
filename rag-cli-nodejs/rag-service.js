const express = require('express');
const multer = require('multer');
const cors = require('cors');
const { QdrantClient } = require('@qdrant/js-client-rest');
const { PDFExtract } = require('pdf.js-extract');
const { pipeline } = require('stream');
const { promisify } = require('util');
const fetch = require('node-fetch');
const { v4: uuidv4 } = require('uuid');

const app = express();
const port = 3001;

// Middleware
app.use(cors());
app.use(express.json({ limit: '50mb' }));
app.use(express.urlencoded({ extended: true, limit: '50mb' }));

// Multer configuration for file uploads
const storage = multer.memoryStorage();
const upload = multer({ 
    storage: storage,
    limits: { fileSize: 10 * 1024 * 1024 } // 10MB limit
});

// Initialize Qdrant client
const qdrantClient = new QdrantClient({ url: 'http://localhost:6333' });

// Debug logging function
function debugLog(message, data = null) {
    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] DEBUG: ${message}`);
    if (data) {
        console.log(`[${timestamp}] DEBUG DATA:`, JSON.stringify(data, null, 2));
    }
}

// Initialize PDF extractor
const pdfExtract = new PDFExtract();
const options = {};

// Chunking configuration
const chunkingConfig = {
    chunkSize: 2000,
    chunkOverlap: 100
};

// Health check endpoint
app.get('/health', (req, res) => {
    debugLog('Health check requested');
    res.json({ 
        status: 'healthy', 
        timestamp: new Date().toISOString(),
        services: {
            qdrant: 'connected',
            ollama: 'available'
        }
    });
});

// Create Qdrant collection if it doesn't exist
async function ensureCollectionExists() {
    try {
        debugLog('Checking if assignments collection exists');
        await qdrantClient.getCollection('assignments');
        debugLog('Assignments collection already exists');
        return true;
    } catch (error) {
        debugLog('Collection check error', { error: error.message, status: error.status });
        
        if (error.status === 404 || error.message?.includes('doesn\'t exist')) {
            debugLog('Assignments collection does not exist, creating...');
            try {
                await qdrantClient.createCollection('assignments', {
                    vectors: {
                        size: 768, // Dimension for nomic-embed-text model
                        distance: 'Cosine'
                    }
                });
                debugLog('Assignments collection created successfully');
                return true;
            } catch (createError) {
                debugLog('Failed to create collection', { error: createError.message, stack: createError.stack });
                return false;
            }
        } else {
            debugLog('Error checking collection', { error: error.message, stack: error.stack });
            return false;
        }
    }
}

// Check if assignment exists in Qdrant
async function checkAssignmentExists(assignmentId) {
    try {
        debugLog('Checking assignment existence', { assignmentId, type: typeof assignmentId });
        
        // Convert to string if needed (Qdrant stores as string)
        const assignmentIdStr = assignmentId.toString();
        debugLog('Converted assignmentId to string', { original: assignmentId, converted: assignmentIdStr });
        
        // Get ALL points first to debug
        const allPointsResponse = await qdrantClient.scroll('assignments', {
            limit: 100
        });
        
        debugLog('All points in collection', { 
            totalPoints: allPointsResponse.points?.length || 0,
            assignmentIds: allPointsResponse.points?.map(p => p.payload.assignmentId) || []
        });
        
        // Filter by exact assignmentId
        const response = await qdrantClient.scroll('assignments', {
            filter: {
                must: [
                    { key: 'assignmentId', match: { value: assignmentIdStr } }
                ]
            },
            limit: 100
        });
        
        const exists = response.points && response.points.length > 0;
        debugLog('Assignment existence check result', { 
            assignmentId, 
            assignmentIdStr,
            exists, 
            pointsFound: response.points?.length || 0,
            filteredPoints: response.points?.map(p => ({ id: p.id, payload: p.payload })) || [],
            allPointsCount: allPointsResponse.points?.length || 0
        });
        return exists;
    } catch (error) {
        debugLog('Error checking assignment existence', { error: error.message, assignmentId, stack: error.stack });
        return false;
    }
}

// Ingest PDF and create embeddings
app.post('/ingest', upload.single('pdfFile'), async (req, res) => {
    try {
        const { assignmentId } = req.body;
        const pdfFile = req.file;

        debugLog('PDF ingestion request received', { assignmentId, fileName: pdfFile?.originalname, fileSize: pdfFile?.size });

        if (!pdfFile || !assignmentId) {
            debugLog('Missing required fields', { hasFile: !!pdfFile, hasAssignmentId: !!assignmentId });
            return res.status(400).json({
                success: false,
                error: 'PdfFile and AssignmentId required'
            });
        }

        // Ensure collection exists
        debugLog('Ensuring Qdrant collection exists');
        const collectionExists = await ensureCollectionExists();
        if (!collectionExists) {
            debugLog('Failed to ensure collection exists');
            return res.status(500).json({
                success: false,
                error: 'Failed to create or access Qdrant collection'
            });
        }

        // Check if assignment already exists
        debugLog('Checking if assignment exists in Qdrant', { assignmentId });
        const alreadyExists = await checkAssignmentExists(assignmentId);
        if (alreadyExists) {
            debugLog('Assignment already exists, returning early', { assignmentId });
            return res.json({
                success: true,
                assignmentId: assignmentId,
                chunks: 0,
                status: 'already_exists',
                message: 'Assignment already exists in RAG system'
            });
        }

        // Extract text from PDF
        debugLog('Extracting text from PDF');
        const data = await pdfExtract.extractBuffer(pdfFile.buffer, options);
        const text = data.pages.map(page => page.content.map(item => item.str).join(' ')).join('\n');
        debugLog('PDF text extracted', { textLength: text.length, pages: data.pages.length });

        // Split text into chunks
        debugLog('Splitting text into chunks', { chunkSize: chunkingConfig.chunkSize, overlap: chunkingConfig.chunkOverlap });
        const chunks = [];
        for (let i = 0; i < text.length; i += chunkingConfig.chunkSize - chunkingConfig.chunkOverlap) {
            const chunk = text.slice(i, i + chunkingConfig.chunkSize);
            if (chunk.trim().length > 0) {
                chunks.push(chunk);
            }
        }
        debugLog('Text chunking completed', { totalChunks: chunks.length });

        // Generate embeddings for each chunk
        debugLog('Starting embedding generation', { totalChunks: chunks.length });
        const embeddings = [];
        for (let i = 0; i < chunks.length; i++) {
            const chunk = chunks[i];
            debugLog(`Generating embedding for chunk ${i + 1}/${chunks.length}`, { chunkLength: chunk.length });
            
            // Call Ollama for embedding
            const embeddingResponse = await fetch('http://localhost:11434/api/embeddings', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    model: 'nomic-embed-text',
                    prompt: chunk
                })
            });

            if (!embeddingResponse.ok) {
                const errorText = await embeddingResponse.text();
                debugLog('Ollama embedding failed', { status: embeddingResponse.status, statusText: embeddingResponse.statusText, error: errorText });
                throw new Error(`Ollama embedding failed: ${embeddingResponse.status} ${embeddingResponse.statusText} - ${errorText}`);
            }

            const embeddingData = await embeddingResponse.json();
            debugLog(`Embedding generated successfully for chunk ${i + 1}`, { embeddingLength: embeddingData.embedding?.length });
            embeddings.push(embeddingData.embedding);
        }

        // Store in Qdrant
        debugLog('Storing embeddings in Qdrant', { totalPoints: chunks.length });
        
        // Ensure assignmentId is always string for consistency
        const assignmentIdStr = assignmentId.toString();
        debugLog('Normalized assignmentId', { original: assignmentId, normalized: assignmentIdStr });
        
        const points = chunks.map((chunk, index) => ({
            id: uuidv4(), // Use UUID instead of string ID
            vector: embeddings[index],
            payload: {
                assignmentId: assignmentIdStr, // Always use string
                chunkId: index,
                text: chunk,
                source: 'pdf_ingestion'
            }
        }));

        await qdrantClient.upsert('assignments', {
            points: points
        });
        debugLog('Successfully stored embeddings in Qdrant', { assignmentId, totalChunks: chunks.length });

        debugLog('PDF ingestion completed successfully', { assignmentId, chunks: chunks.length });
        res.json({
            success: true,
            assignmentId: assignmentId,
            chunks: chunks.length,
            status: 'ingested',
            message: 'PDF successfully ingested and chunked'
        });

    } catch (error) {
        debugLog('Error in PDF ingestion', { error: error.message, stack: error.stack });
        console.error('Error in PDF ingestion:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Test ingest endpoint (simplified for testing)
app.post('/test-ingest', upload.single('pdfFile'), async (req, res) => {
    try {
        const { assignmentId } = req.body;
        const pdfFile = req.file;

        if (!pdfFile) {
            return res.status(400).json({
                success: false,
                error: 'PdfFile required'
            });
        }

        const testAssignmentId = assignmentId || `test_${Date.now()}`;

        // Extract text from PDF
        const data = await pdfExtract.extractBuffer(pdfFile.buffer, options);
        const text = data.pages.map(page => page.content.map(item => item.str).join(' ')).join('\n');

        // Simple chunking for test
        const chunks = [];
        for (let i = 0; i < text.length; i += 1000) {
            const chunk = text.slice(i, i + 1000);
            if (chunk.trim().length > 0) {
                chunks.push(chunk);
            }
        }

        res.json({
            success: true,
            assignmentId: testAssignmentId,
            chunks: chunks.length,
            status: 'test_ingested',
            message: 'Test PDF processed successfully'
        });

    } catch (error) {
        console.error('Error in test PDF ingestion:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Extract code from submission (placeholder - should be implemented based on your submission storage)
async function extractCodeFromSubmission(submissionId) {
    try {
        debugLog('Extracting code from submission', { submissionId });
        
        // TODO: Implement actual code extraction from submission
        // This should:
        // 1. Get submission from database using submissionId
        // 2. Extract ZIP file content
        // 3. Find Java files and extract code
        // 4. Return concatenated code
        
        // For now, return placeholder
        return `// Placeholder: Code from submission ${submissionId}
// TODO: Implement actual code extraction from submission storage
public class Main {
    public static void main(String[] args) {
        System.out.println("Hello from submission " + submissionId);
    }
}`;
    } catch (error) {
        debugLog('Error extracting code from submission', { error: error.message, submissionId });
        throw new Error(`Failed to extract code from submission ${submissionId}: ${error.message}`);
    }
}

// Similarity search
async function similaritySearch(query, assignmentId, limit = 5) {
    try {
        debugLog('Starting similarity search', { query: query.substring(0, 100) + '...', assignmentId, limit });
        
        // Generate embedding for query
        const embeddingResponse = await fetch('http://localhost:11434/api/embeddings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                model: 'nomic-embed-text',
                prompt: query
            })
        });

        if (!embeddingResponse.ok) {
            const errorText = await embeddingResponse.text();
            debugLog('Query embedding failed', { status: embeddingResponse.status, error: errorText });
            throw new Error('Failed to generate query embedding');
        }

        const embeddingData = await embeddingResponse.json();
        const queryEmbedding = embeddingData.embedding;
        debugLog('Query embedding generated', { embeddingLength: queryEmbedding.length });

        // Search in Qdrant
        const searchResponse = await qdrantClient.search('assignments', {
            vector: queryEmbedding,
            filter: {
                must: [
                    { key: 'assignmentId', match: { value: assignmentId } }
                ]
            },
            limit: limit
        });

        debugLog('Similarity search completed', { resultsFound: searchResponse.length, topScore: searchResponse[0]?.score });
        
        return searchResponse.map(point => ({
            id: point.id,
            score: point.score,
            text: point.payload.text,
            chunkId: point.payload.chunkId
        }));

    } catch (error) {
        debugLog('Error in similarity search', { error: error.message, assignmentId });
        console.error('Error in similarity search:', error);
        throw error;
    }
}

// Review student code
app.post('/review-code', async (req, res) => {
    try {
        const { assignmentId, studentCode, algorithmType = 'General', language = 'Java' } = req.body;

        debugLog('Code review request received', { assignmentId, hasStudentCode: !!studentCode, algorithmType, language });

        if (!assignmentId || !studentCode) {
            debugLog('Missing required fields for code review', { hasAssignmentId: !!assignmentId, hasStudentCode: !!studentCode });
            return res.status(400).json({
                success: false,
                error: 'AssignmentId and StudentCode required'
            });
        }

        // Get relevant context from RAG
        debugLog('Getting RAG context for code review', { assignmentId });
        const context = await similaritySearch('code review', assignmentId, 3);
        const contextText = context.map(c => c.text).join('\n\n');

        // Prepare prompt for code review
        const prompt = `You are an expert programming instructor reviewing student code.

Assignment Context:
${contextText}

Student Code:
${studentCode}

Language: ${language}
Algorithm Type: ${algorithmType}

Please provide a comprehensive code review including:
1. Code quality assessment
2. Logic analysis
3. Potential improvements
4. Best practices suggestions
5. Any errors or issues found

Format your response as JSON with the following structure:
{
  "review": "detailed review text",
  "hasErrors": true/false,
  "errorCount": number,
  "summary": "brief summary"
}`;

        // Call Ollama for code review
        debugLog('Calling Ollama for code review', { assignmentId, promptLength: prompt.length });
        const llmResponse = await fetch('http://localhost:11434/api/generate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                model: 'llama3.2:3b',
                prompt: prompt,
                stream: false
            })
        });

        if (!llmResponse.ok) {
            const errorText = await llmResponse.text();
            debugLog('Ollama code review failed', { status: llmResponse.status, error: errorText });
            throw new Error('Failed to generate code review');
        }

        const llmData = await llmResponse.json();
        debugLog('Ollama code review response received', { responseLength: llmData.response?.length });
        
        let reviewResult;

        try {
            // Try to parse JSON response
            reviewResult = JSON.parse(llmData.response);
            debugLog('Code review JSON parsed successfully', { hasErrors: reviewResult.hasErrors, errorCount: reviewResult.errorCount });
        } catch (parseError) {
            // If JSON parsing fails, create a structured response
            debugLog('Code review JSON parsing failed, using fallback', { parseError: parseError.message });
            reviewResult = {
                review: llmData.response,
                hasErrors: false,
                errorCount: 0,
                summary: "AI review completed"
            };
        }

        debugLog('Code review completed successfully', { assignmentId, hasErrors: reviewResult.hasErrors, errorCount: reviewResult.errorCount });
        res.json({
            success: true,
            assignmentId: assignmentId,
            reviewAllowed: true,
            review: reviewResult.review,
            hasErrors: reviewResult.hasErrors,
            errorCount: reviewResult.errorCount,
            summary: reviewResult.summary
        });

    } catch (error) {
        debugLog('Error in code review', { error: error.message, assignmentId });
        console.error('Error in code review:', error);
        res.status(500).json({
            success: false,
            error: error.message,
            reviewAllowed: false
        });
    }
});

// Suggest test cases for assignment
app.post('/suggest-testcases', async (req, res) => {
    try {
        const { assignmentId } = req.body;

        debugLog('Test case suggestion request received', { assignmentId });

        if (!assignmentId) {
            debugLog('Missing required fields for test case suggestion', { hasAssignmentId: !!assignmentId });
            return res.status(400).json({
                success: false,
                error: 'AssignmentId is required'
            });
        }

        // Check if assignment has RAG data
        debugLog('Checking if assignment has RAG data for test case suggestion', { assignmentId });
        const hasRAGData = await checkAssignmentExists(assignmentId);
        
        if (!hasRAGData) {
            return res.status(403).json({
                success: false,
                error: 'No RAG context available for this assignment. Please ingest the assignment PDF first.',
                suggestionsAllowed: false
            });
        }

        // Get relevant context from RAG
        debugLog('Getting RAG context for test case generation', { assignmentId });
        const context = await similaritySearch('test case generation', assignmentId, 5);
        const contextText = context.map(c => c.text).join('\n\n');

        // Prepare prompt for test case generation
        const prompt = `You are an expert programming instructor helping students create test cases.

Assignment Context:
${contextText}

Based on the assignment requirements, suggest 5-10 test cases that would help verify the solution.

For each test case, provide:
1. Input values (consider normal cases, edge cases, and boundary conditions)
2. Expected output

IMPORTANT: Respond with ONLY valid JSON, no markdown formatting, no code blocks.

Format your response as JSON with the following structure:
{
  "testCases": [
    {
      "input": "simple input description",
      "expectedOutput": "expected result description"
    }
  ],
  "suggestions": "general testing advice and tips"
}

Focus on:
- Edge cases (empty input, null values, maximum values)
- Boundary conditions (minimum/maximum valid inputs)
- Normal cases (typical expected inputs)
- Error conditions (invalid inputs if applicable)

Keep input and expectedOutput as simple text descriptions, not complex objects.

Remember: Return ONLY the JSON object, no markdown, no code blocks.`;

        // Call Gemini API for test case generation
        const geminiApiKey = process.env.GEMINI_API_KEY || 'AIzaSyDxFNK8N6Y9bkLkNwhoENVhq-gNHH3UrnY';
        const geminiResponse = await fetch(`https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key=${geminiApiKey}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                contents: [{
                    parts: [{
                        text: prompt
                    }]
                }]
            })
        });

        if (!geminiResponse.ok) {
            const errorText = await geminiResponse.text();
            debugLog('Gemini test case generation failed', { status: geminiResponse.status, error: errorText });
            throw new Error(`Gemini API request failed: ${geminiResponse.status}`);
        }

        const geminiData = await geminiResponse.json();
        const responseText = geminiData.candidates?.[0]?.content?.parts?.[0]?.text || '';
        debugLog('Gemini test case response received', { responseLength: responseText?.length });

        // Parse JSON response with robust sanitizer
        let parsed;
        try {
            // First try direct JSON parse
            parsed = JSON.parse(responseText);
            debugLog('Direct JSON parse successful', { testCasesCount: parsed.testCases?.length || 0 });
        } catch (e1) {
            debugLog('Direct JSON parse failed, trying extractJson', { error: e1.message });
            try {
                // Try extractJson sanitizer
                parsed = extractJson(responseText);
                debugLog('ExtractJson successful', { testCasesCount: parsed?.testCases?.length || 0 });
            } catch (e2) {
                debugLog('ExtractJson also failed, using fallback', { error: e2.message });
                return res.status(502).json({ 
                    success: false, 
                    error: 'LLM did not return valid JSON',
                    rawResponse: responseText
                });
            }
        }

        // Validate schema
        if (!validateTestCases(parsed)) {
            debugLog('Test case validation failed', { parsed });
            return res.status(502).json({ 
                success: false, 
                error: 'Invalid test case schema',
                rawResponse: responseText
            });
        }

        debugLog('Test case validation successful', { testCasesCount: parsed.testCases?.length || 0 });
        
        res.json({
            success: true,
            assignmentId: assignmentId,
            testCases: parsed.testCases || [],
            suggestions: parsed.suggestions || "",
            rawResponse: responseText
        });

    } catch (error) {
        debugLog('Error suggesting test cases', { error: error.message, assignmentId: req.body.assignmentId });
        console.error('Error suggesting test cases:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Delete assignment data from Qdrant
app.delete('/assignment/:assignmentId', async (req, res) => {
    try {
        const { assignmentId } = req.params;
        const assignmentIdStr = assignmentId.toString();
        
        debugLog('Deleting assignment data', { assignmentId, assignmentIdStr });
        
        // Delete points for this assignment
        await qdrantClient.delete('assignments', {
            filter: {
                must: [
                    { key: 'assignmentId', match: { value: assignmentIdStr } }
                ]
            }
        });
        
        res.json({
            success: true,
            message: `Deleted assignment ${assignmentId} data from Qdrant`,
            assignmentId: assignmentId
        });
    } catch (error) {
        debugLog('Error deleting assignment', { error: error.message, assignmentId: req.params.assignmentId });
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Create collection endpoint
app.post('/create-collection', async (req, res) => {
    try {
        debugLog('Creating Qdrant collection manually');
        const success = await ensureCollectionExists();
        
        if (success) {
            res.json({
                success: true,
                message: 'Collection created successfully',
                collection: 'assignments'
            });
        } else {
            res.status(500).json({
                success: false,
                error: 'Failed to create collection'
            });
        }
    } catch (error) {
        debugLog('Error creating collection', { error: error.message });
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Extract JSON from LLM response safely
function extractJson(text) {
    if (!text) return null;
    
    // If there's ```json ... ```
    const fence = text.match(/```(?:json)?\s*([\s\S]*?)\s*```/i);
    const body = fence ? fence[1] : text;
    
    // Cut from first { to last }
    const start = body.indexOf('{');
    const end = body.lastIndexOf('}');
    if (start === -1 || end === -1 || end < start) return null;
    
    const raw = body.slice(start, end + 1).trim();
    return JSON.parse(raw);
}

// Validate test case schema
function validateTestCases(data) {
    if (!data || typeof data !== 'object') return false;
    if (!Array.isArray(data.testCases)) return false;
    
    for (const testCase of data.testCases) {
        if (!testCase || typeof testCase !== 'object') return false;
        if (!testCase.input || typeof testCase.input !== 'string') return false;
        if (!testCase.expectedOutput || typeof testCase.expectedOutput !== 'string') return false;
    }
    
    return true;
}

// Debug endpoint to check Qdrant data
app.get('/debug/assignment/:assignmentId', async (req, res) => {
    try {
        const { assignmentId } = req.params;
        debugLog('Debug request for assignment', { assignmentId, type: typeof assignmentId });
        
        // Get ALL points first (no filter)
        debugLog('Getting all points from collection');
        const allPointsResponse = await qdrantClient.scroll('assignments', {
            limit: 100
        });
        
        debugLog('All points retrieved', { 
            totalPoints: allPointsResponse.points?.length || 0,
            payloads: allPointsResponse.points?.map(p => p.payload) || []
        });
        
        // Check if assignment exists
        const exists = await checkAssignmentExists(assignmentId);
        
        // Get filtered points for this assignment
        const response = await qdrantClient.scroll('assignments', {
            filter: {
                must: [
                    { key: 'assignmentId', match: { value: assignmentId.toString() } }
                ]
            },
            limit: 100
        });
        
        res.json({
            success: true,
            assignmentId: assignmentId,
            assignmentIdType: typeof assignmentId,
            exists: exists,
            totalPoints: response.points?.length || 0,
            allPointsCount: allPointsResponse.points?.length || 0,
            allPointsPayloads: allPointsResponse.points?.map(p => p.payload) || [],
            filteredPoints: response.points?.map(p => ({
                id: p.id,
                payload: p.payload,
                vector: p.vector ? `[${p.vector.slice(0, 3).join(', ')}...]` : null
            })) || [],
            collectionInfo: {
                name: 'assignments',
                totalPoints: allPointsResponse.points?.length || 0
            }
        });
    } catch (error) {
        debugLog('Error in debug endpoint', { error: error.message, assignmentId: req.params.assignmentId, stack: error.stack });
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Check if assignment exists in RAG system
app.get('/check-assignment/:assignmentId', async (req, res) => {
    try {
        const { assignmentId } = req.params;
        debugLog('Check assignment request received', { assignmentId });
        
        const exists = await checkAssignmentExists(assignmentId);
        
        debugLog('Assignment existence check result', { assignmentId, exists });
        
        res.json({
            success: true,
            assignmentId: assignmentId,
            exists: exists,
            message: exists ? 'Assignment found in RAG system' : 'Assignment not found in RAG system'
        });
    } catch (error) {
        debugLog('Error checking assignment existence', { error: error.message, assignmentId: req.params.assignmentId });
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Get assignment context
app.get('/context/:assignmentId', async (req, res) => {
    try {
        const { assignmentId } = req.params;
        
        const hasRAGData = await checkAssignmentExists(assignmentId);
        
        if (!hasRAGData) {
            return res.status(404).json({
                success: false,
                error: 'No RAG context found for this assignment'
            });
        }

        // Get all chunks for this assignment
        const response = await qdrantClient.scroll('assignments', {
            filter: {
                must: [
                    { key: 'assignmentId', match: { value: assignmentId } }
                ]
            },
            limit: 100
        });

        const chunks = response.points.map(point => ({
            id: point.id,
            text: point.payload.text,
            chunkId: point.payload.chunkId
        }));

        res.json({
            success: true,
            assignmentId: assignmentId,
            chunks: chunks,
            totalChunks: chunks.length
        });

    } catch (error) {
        console.error('Error getting assignment context:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Get assignment info and context
app.get('/assignment-info/:assignmentId', async (req, res) => {
    try {
        const { assignmentId } = req.params;
        debugLog('Assignment info request received', { assignmentId });
        
        const hasRAGData = await checkAssignmentExists(assignmentId);
        
        if (!hasRAGData) {
            return res.status(404).json({
                success: false,
                error: 'No RAG context found for this assignment'
            });
        }

        // Get all chunks for this assignment
        const response = await qdrantClient.scroll('assignments', {
            filter: {
                must: [
                    { key: 'assignmentId', match: { value: assignmentId.toString() } }
                ]
            },
            limit: 100
        });

        const chunks = response.points.map(point => ({
            id: point.id,
            text: point.payload.text,
            chunkId: point.payload.chunkId
        }));

        // Combine all text for context
        const context = chunks.map(chunk => chunk.text).join('\n\n');

        res.json({
            success: true,
            assignmentId: assignmentId,
            exists: true,
            totalChunks: chunks.length,
            context: context,
            chunks: chunks
        });

    } catch (error) {
        debugLog('Error getting assignment info', { error: error.message, assignmentId: req.params.assignmentId });
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Start server
app.listen(port, () => {
    console.log(`🚀 RAG Service running on port ${port}`);
    console.log(`📖 Health check: http://localhost:${port}/health`);
    console.log(`📄 Ingest PDF: POST http://localhost:${port}/ingest`);
    console.log(`🔍 Review code: POST http://localhost:${port}/review`);
    console.log(`📦 Extract code: POST http://localhost:${port}/extract-code`);
    console.log(`🔍 Check assignment: GET http://localhost:${port}/check-assignment/:assignmentId`);
    console.log(`📋 Assignment info: GET http://localhost:${port}/assignment-info/:assignmentId`);
    console.log(`✅ Connected to existing Qdrant collection`);
});
