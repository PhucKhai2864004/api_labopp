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
const qdrantClient = new QdrantClient({ url: 'http://qdrant:6333' });

// Debug logging function
function debugLog(message, data = null) {
    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] DEBUG: ${message}`);
    if (data) {
        console.log(`[${timestamp}] DEBUG DATA:`, JSON.stringify(data, null, 2));
    }
}

// Resolve Gemini API key from environment
function getGeminiApiKey() {
    return process.env.GEMINI_API_KEY || process.env.GOOGLE_API_KEY || '';
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
            const embeddingResponse = await fetch('http://ollama:11434/api/embeddings', {
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
        const embeddingResponse = await fetch('http://ollama:11434/api/embeddings', {
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
        const { assignmentId, submissionId, extractedCode, algorithmType = 'General', language = 'Java', testCases = [], actuals = [] } = req.body;

        debugLog('Code review request received', { assignmentId, submissionId, hasExtractedCode: !!extractedCode, algorithmType, language, testCasesCount: Array.isArray(testCases) ? testCases.length : 0, actualsCount: Array.isArray(actuals) ? actuals.length : 0 });

        if (!assignmentId || !submissionId || !extractedCode) {
            debugLog('Missing required fields for code review', { hasAssignmentId: !!assignmentId, hasSubmissionId: !!submissionId, hasExtractedCode: !!extractedCode });
            return res.status(400).json({
                success: false,
                error: 'AssignmentId, SubmissionId, and ExtractedCode required'
            });
        }

        // Get relevant context from RAG
        debugLog('Getting RAG context for code review', { assignmentId, submissionId });
        const context = await similaritySearch('code review', assignmentId, 3);
        const contextText = context.map(c => c.text).join('\n\n');

        // Guard: if submission likely mismatches assignment (no/weak context), return INVALID_ASSIGNMENT
        const topScore = context?.[0]?.score ?? 0;
        if (!context || context.length === 0 || topScore < 0.2) {
            debugLog('Context too weak or missing for this assignment; marking as INVALID_ASSIGNMENT', { topScore, contextCount: context?.length || 0 });
            return res.json({
                success: true,
                assignmentId: assignmentId,
                submissionId: submissionId,
                reviewAllowed: true,
                review: 'Submission does not match the assignment requirements.',
                hasErrors: true,
                errorCount: 1,
                summary: 'Code does not align with the stored assignment context.',
                rawResponse: JSON.stringify({
                    status: 'INVALID_ASSIGNMENT',
                    issues: [
                        { type: 'requirement', message: 'Submission content does not match the assignment requirements.', description: 'The code appears unrelated to the assignment context stored for this assignmentId.', file: null, line: null }
                    ],
                    hasErrors: true,
                    errorCount: 1,
                    summary: 'Code does not align with the stored assignment context.'
                })
            });
        }

        // Prepare prompt for code review (concise strict JSON, no emojis/markdown, no code fixes). Include ground-truth I/O and observed outputs when provided.
        // Limit included cases to keep prompt size reasonable
        const MAX_CASES = 10;
        const trimmedTestCases = Array.isArray(testCases) ? testCases.slice(0, MAX_CASES) : [];
        const trimmedActuals = Array.isArray(actuals) ? actuals.slice(0, MAX_CASES) : [];

        const testCasesBlock = trimmedTestCases.length > 0
            ? `Ground truth tests (input/expected):\n${trimmedTestCases.map((tc, i) => `#${i+1} input: ${String(tc.input).slice(0, 500)}\nexpected: ${String(tc.expectedOutput).slice(0, 500)}`).join('\n\n')}`
            : 'Ground truth tests: none provided';

        const actualsBlock = trimmedActuals.length > 0
            ? `Observed outputs from grading (input/actual[+status]):\n${trimmedActuals.map((a, i) => `#${i+1} input: ${String(a.input).slice(0, 500)}\nactual: ${String(a.actualOutput || '').slice(0, 500)}\npassed: ${a.passed === true}`).join('\n\n')}`
            : 'Observed outputs: none provided';

        const prompt = `You are an expert programming instructor reviewing student code.

Assignment Context:
${contextText}

Student Code (from submission ${submissionId}):
${extractedCode}

Language: ${language}
Algorithm Type: ${algorithmType}

${testCasesBlock}

${actualsBlock}

Task:
- Analyze the code strictly against the assignment context.
- Do NOT propose code fixes or provide rewritten code.
- If the submission does not implement the assignment requirements, set status to "INVALID_ASSIGNMENT" and describe the missing requirements.
- Detect and report concrete issues only (syntax, logic, requirement mismatches). Prefer precision over volume.
- Absolutely NO emojis, NO markdown, NO code fences.

Output:
Return ONLY a valid JSON object with this exact schema (no extra fields):
{
  "status": "OK" | "SYNTAX_ERROR" | "LOGIC_ERROR" | "INVALID_ASSIGNMENT",
  "issues": [
    {
      "type": "syntax" | "logic" | "requirement",
      "message": string,
      "description": string,  // short 1-2 sentence description of the faulty code
      "file": string | null,
      "line": number | null
    }
  ],
  "evidence": [
    {
      "input": string,
      "expectedOutput": string,
      "actualOutput": string,
      "reason": string
    }
  ],
  "ioCoverage": { "provided": number, "used": number, "failed": number },
  "hasErrors": boolean,
  "errorCount": number,
  "summary": string
}

Notes:
- Keep description short and focused on the faulty code (no suggestions/fixes).
- If evidence is provided (testCases/actuals), prioritize logic evaluation from them. Classify LOGIC_ERROR if a significant portion of used cases fail.
- If helpful, include at most one short inline code excerpt (<= 120 chars) inside description, still as plain text, no markdown.`;
        
        // Call Gemini for code review (align with suggest-testcases)
        const geminiApiKey = getGeminiApiKey();
        if (!geminiApiKey) {
            debugLog('Missing Gemini API key for review-code');
            return res.status(500).json({ success: false, error: 'Missing Gemini API key' });
        }

        const geminiModel = process.env.GENAI_MODEL_REVIEW || process.env.GENAI_MODEL || 'gemini-2.5-flash';
        const geminiBase = process.env.GENAI_BASE || 'https://generativelanguage.googleapis.com';

        debugLog('Calling Gemini for code review', { assignmentId, model: geminiModel, promptLength: prompt.length });
        const payload = {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ contents: [{ parts: [{ text: prompt }] }] })
        };

        // Try v1 first, then fallback to v1beta on 404
        let geminiResponse = await fetch(`${geminiBase}/v1/models/${geminiModel}:generateContent?key=${geminiApiKey}`, payload);
        if (geminiResponse.status === 404) {
            debugLog('Gemini v1 endpoint returned 404, retrying with v1beta', { model: geminiModel });
            geminiResponse = await fetch(`${geminiBase}/v1beta/models/${geminiModel}:generateContent?key=${geminiApiKey}`, payload);
        }

        if (!geminiResponse.ok) {
            const errorText = await geminiResponse.text();
            debugLog('Gemini code review failed', { status: geminiResponse.status, error: errorText });
            throw new Error(`Gemini API request failed: ${geminiResponse.status}`);
        }

        const geminiData = await geminiResponse.json();
        const responseText = geminiData.candidates?.[0]?.content?.parts?.[0]?.text || '';
        debugLog('Gemini code review response received', { responseLength: responseText?.length });

        // Parse JSON strictly; if invalid, return 502 to caller
        let parsed;
        try {
            parsed = JSON.parse(responseText);        } catch (parseError) {
            debugLog('Code review JSON parsing failed', { parseError: parseError.message, responsePreview: responseText.slice(0, 200) });
            return res.status(502).json({ 
                success: false,
                error: 'LLM did not return valid JSON',
                rawResponse: responseText
            });
        }

        // Minimal mapping for outer API while returning minified JSON inside rawResponse
        const minified = JSON.stringify(parsed);
        const hasErrors = !!parsed.hasErrors;
        const errorCount = typeof parsed.errorCount === 'number' ? parsed.errorCount : (Array.isArray(parsed.issues) ? parsed.issues.length : 0);
        const summary = typeof parsed.summary === 'string' ? parsed.summary : '';

        debugLog('Code review completed successfully', { assignmentId, submissionId, hasErrors: hasErrors, errorCount: errorCount });
        res.json({
            success: true,
            assignmentId: assignmentId,
            submissionId: submissionId,
            reviewAllowed: true,
            review: summary,
            hasErrors: hasErrors,
            errorCount: errorCount,
            summary: summary,
            rawResponse: minified
        });

    } catch (error) {
        debugLog('Error in code review', { error: error.message, stack: error.stack });
        console.error('Error in code review:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Suggest test cases for assignment
app.post('/suggest-testcases', async (req, res) => {
    try {
        const { assignmentId, outputStyle, style, hints, existingTestCases = [] } = req.body;

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

        // Extract test cases from RAG context (from assignment PDF)
        const extractedTestCases = extractTestCasesFromContext(contextText);
        debugLog('Extracted test cases from RAG context', { assignmentId, extractedCount: extractedTestCases.length });
        
        // Get input/output examples from request body (from database)
        debugLog('Getting input/output examples from request body', { assignmentId, examplesCount: existingTestCases.length });
        const examplesText = existingTestCases.length > 0 ? 
            `\n\nInput/Output Examples (from database):\n${existingTestCases.map(ex => `Input: ${ex.Input}\nExpected: ${ex.ExpectedOutput}`).join('\n\n')}` : 
            '';
            
        // Combine extracted test cases from PDF with database examples
        const allTestCases = [...extractedTestCases, ...existingTestCases];
        const testCasesText = allTestCases.length > 0 ? 
            `\n\nAvailable Test Cases Reference:\n${allTestCases.map((tc, i) => `${i+1}. ${tc.name || tc.description || 'Test Case'}: ${tc.description || tc.ExpectedOutput || ''}`).join('\n')}` : 
            '';

        // Auto-detect style from RAG context + examples
        const detectedStyle = detectAssignmentStyle(contextText + examplesText);
        const effectiveStyle = (style || outputStyle || detectedStyle || 'default').toString();

        debugLog('Style detection', { 
            detectedStyle, 
            userStyle: style || outputStyle, 
            effectiveStyle,
            contextPreview: contextText.substring(0, 200) + '...'
        });

        // Prepare prompt for test case generation (for students to self-test)
        const baseIntro = `You are an expert programming instructor helping students create test cases for self-testing their code before submission.`;

        const styles = {
            terminal_menu: `\nFocus on terminal menu interactions for management systems. Each test case must include step-by-step inputs a student performs in a console app.\nReturn ONLY valid JSON, no markdown/code fences.\nSchema:\n{\n  "testCases": [\n    {\n      "name": string,\n      "description": string,\n      "steps": [string],\n      "expectedOutput": string\n    }\n  ],\n  "suggestions": string\n}`,
                         algorithm_io: `\nThis is a basic algorithmic problem (Fibonacci, matrix operations, BMI calculation, arithmetic, date comparison, math calculations). Provide concise input/output cases.\nReturn ONLY valid JSON, no markdown/code fences.\nSchema:\n{\n  "testCases": [\n    {\n      "name": string,\n      "input": string,\n      "expectedOutput": string,\n      "description": string\n    }\n  ],\n  "suggestions": string\n}`,
            sorting_array: `\nThis is a sorting problem (Bubble Sort, Selection Sort, etc.). Include edge cases: empty array, single element, already sorted, reverse sorted, duplicates.\nUse array literals as strings for input and expectedOutput (e.g., "[3,1,2]").\nReturn ONLY valid JSON, no markdown/code fences.\nSchema:\n{\n  "testCases": [\n    {\n      "name": string,\n      "input": string,\n      "expectedOutput": string,\n      "description": string\n    }\n  ],\n  "suggestions": string\n}`,
            string_ops: `\nThis is a string processing problem. Cover empty string, whitespace, case sensitivity, special characters.\nReturn ONLY valid JSON, no markdown/code fences.\nSchema as algorithm_io.`,
            data_structure: `\nThis is a data structure problem (Stack, Queue, simple structures). Test basic operations: push/pop, enqueue/dequeue, insert/delete.\nInclude edge cases: empty structure, single element.\nSchema as algorithm_io.`,
            unit_api: `\nProduce unit-test-like cases.\nReturn ONLY valid JSON, no markdown/code fences.\nSchema:\n{\n  "testCases": [\n    {\n      "name": string,\n      "description": string,\n      "pre": string,\n      "input": string,\n      "expectedOutput": string\n    }\n  ],\n  "suggestions": string\n}`,
            default: `\nBased on the assignment requirements, suggest 5-10 test cases that would help verify the solution.\nReturn ONLY valid JSON, no markdown/code fences.\nSchema as algorithm_io.`
        };

        const styleBlock = styles[effectiveStyle] || styles.default;

        const prompt = `${baseIntro}

Assignment Context:
${contextText}${examplesText}${testCasesText}

Detected Style: ${detectedStyle}
Effective Style: ${effectiveStyle}
${styleBlock}
${hints ? `\nConstraints/Hints (as plain text for guidance): ${JSON.stringify(hints)}` : ''}

General guidance:\n- PRIORITY: If "Available Test Cases Reference" is provided above, use those test cases as the PRIMARY source and generate test cases that match those descriptions\n- Use the input/output examples as reference for format and style\n- Generate test cases that students can use to verify their code works correctly\n- Cover edge cases, boundary conditions, normal and error cases\n- Focus on expected outputs (what the program should produce)\n- Keep fields as simple text\n- No extra commentary, return the JSON object only\n- IMPORTANT: For 'name' field, use short, clear test case names (e.g., "Add doctor", "Update doctor", "Delete doctor", "Search doctor")\n- IMPORTANT: For 'description' field, write detailed English descriptions based on the actual assignment context:\n  * Describe what the test case validates in the context of this specific assignment\n  * Use terminology and concepts from the assignment (e.g., if it's about employees, use "Add employee", "Update employee", etc.)\n  * Be specific about the scenario being tested (success case, error case, edge case, etc.)\n  * Examples: "Add new employee successfully", "Update employee with invalid ID", "Delete non-existent employee", "Search employee by empty criteria"\n- Make descriptions specific and clear about what the test case validates in the context of this assignment\n- ORDER test cases logically: basic operations first (Add, Create), then modifications (Update, Edit), then queries (Search, View), then deletions (Delete, Remove), finally edge cases and error handling\n- For management systems: Start with "Add [entity]", then "Update [entity]", then "Search [entity]", then "Delete [entity]", then error cases like "Add [entity] with existing code", "Update non-existent [entity]", etc.\n- If test cases from assignment PDF are provided, prioritize generating test cases that cover those specific scenarios mentioned in the assignment`;

        // Call Gemini API for test case generation
        const geminiApiKey = getGeminiApiKey();
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

        // Validate schema depending on style
        if (effectiveStyle === 'terminal_menu') {
            const ok = Array.isArray(parsed?.testCases) && parsed.testCases.every(tc =>
                tc && typeof tc.name === 'string' && typeof tc.description === 'string' && Array.isArray(tc.steps) && typeof tc.expectedOutput === 'string'
            );
            if (!ok) {
                debugLog('Test case validation failed (terminal_menu)', { parsed });
                return res.status(502).json({ success: false, error: 'Invalid test case schema (terminal_menu)', rawResponse: responseText });
            }
        } else if (effectiveStyle === 'unit_api') {
            const ok = Array.isArray(parsed?.testCases) && parsed.testCases.every(tc =>
                tc && typeof tc.name === 'string' && typeof tc.description === 'string' && typeof tc.pre === 'string' && typeof tc.input === 'string' && typeof tc.expectedOutput === 'string'
            );
            if (!ok) {
                debugLog('Test case validation failed (unit_api)', { parsed });
                return res.status(502).json({ success: false, error: 'Invalid test case schema (unit_api)', rawResponse: responseText });
            }
        } else {
        if (!validateTestCases(parsed)) {
            debugLog('Test case validation failed', { parsed });
                return res.status(502).json({ success: false, error: 'Invalid test case schema', rawResponse: responseText });
            }
        }

        debugLog('Test case validation successful', { testCasesCount: parsed.testCases?.length || 0 });
        
        // Format expectedOutput for frontend display (replace \n with <br>)
        const formattedTestCases = (parsed.testCases || []).map(tc => ({
            ...tc,
            expectedOutputFormatted: tc.expectedOutput ? tc.expectedOutput.replace(/\n/g, '<br>') : tc.expectedOutput
        }));
        
        res.json({
            success: true,
            assignmentId: assignmentId,
            detectedStyle: detectedStyle,
            effectiveStyle: effectiveStyle,
            testCases: formattedTestCases,
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
        if (!testCase.name || typeof testCase.name !== 'string') return false;
        if (!testCase.input || typeof testCase.input !== 'string') return false;
        if (!testCase.expectedOutput || typeof testCase.expectedOutput !== 'string') return false;
        if (!testCase.description || typeof testCase.description !== 'string') return false;
    }
    
    return true;
}

// Extract test cases from RAG context (from assignment PDF)
function extractTestCasesFromContext(contextText) {
    if (!contextText) return [];
    
    const testCases = [];
    const lines = contextText.split('\n');
    
    // Look for test case patterns
    let inTestCasesSection = false;
    
    for (let i = 0; i < lines.length; i++) {
        const line = lines[i].trim();
        
        // Detect test cases section
        if (line.toLowerCase().includes('test case') && line.toLowerCase().includes('description')) {
            inTestCasesSection = true;
            continue;
        }
        
        // Stop if we hit another major section
        if (inTestCasesSection && (line.toLowerCase().includes('requirement') || 
                                  line.toLowerCase().includes('specification') ||
                                  line.toLowerCase().includes('implementation') ||
                                  line.toLowerCase().includes('note'))) {
            break;
        }
        
        // Parse test case lines
        if (inTestCasesSection) {
            // Pattern: ● TC1: Successfully add a new fruit
            const tcMatch = line.match(/●\s*TC\d+:\s*(.+)/i);
            if (tcMatch) {
                const description = tcMatch[1].trim();
                const tcNumber = line.match(/TC(\d+)/i)?.[1] || '';
                
                testCases.push({
                    name: `TC${tcNumber}`,
                    description: description,
                    source: 'assignment_pdf'
                });
            }
            
            // Alternative pattern: TC1: Successfully add a new fruit
            const altMatch = line.match(/TC\d+:\s*(.+)/i);
            if (altMatch && !line.includes('●')) {
                const description = altMatch[1].trim();
                const tcNumber = line.match(/TC(\d+)/i)?.[1] || '';
                
                testCases.push({
                    name: `TC${tcNumber}`,
                    description: description,
                    source: 'assignment_pdf'
                });
            }
        }
    }
    
    debugLog('Extracted test cases from context', { 
        totalLines: lines.length, 
        testCasesFound: testCases.length,
        testCases: testCases.map(tc => `${tc.name}: ${tc.description}`)
    });
    
    return testCases;
}

// Auto-detect assignment style from RAG context - Simplified for Java school assignments
function detectAssignmentStyle(contextText) {
    const text = contextText.toLowerCase();
    
    // Score-based detection system for common Java school assignments
    const scores = {
        terminal_menu: 0,      // Quản lý nhân viên, fruitshop, etc.
        sorting_array: 0,      // Bubble sort, selection sort, etc.
        algorithm_io: 0,       // Fibonacci, date comparison, etc.
        data_structure: 0,     // Stack, Queue, simple structures
        string_ops: 0          // String manipulation
    };
    
    // Terminal Menu & Management (most common in Java courses)
    const menuPatterns = ['menu', 'option', 'display', 'console', 'interactive'];
    const crudPatterns = ['add', 'update', 'delete', 'create', 'modify', 'remove'];
    const managementPatterns = ['management', 'employee', 'worker', 'student', 'customer', 'product', 'fruit', 'shop', 'store'];
    const dataStoragePatterns = ['hashmap', 'arraylist', 'linkedlist', 'array'];
    
    if (menuPatterns.some(p => text.includes(p))) scores.terminal_menu += 2;
    if (crudPatterns.filter(p => text.includes(p)).length >= 2) scores.terminal_menu += 3;
    if (managementPatterns.some(p => text.includes(p))) scores.terminal_menu += 3;
    if (dataStoragePatterns.some(p => text.includes(p))) scores.terminal_menu += 1;
    
    // Sorting Algorithms (very common)
    const sortingPatterns = ['sort', 'bubble', 'insertion', 'selection', 'quick', 'merge'];
    const orderPatterns = ['ascending', 'descending', 'order', 'arrange'];
    
    if (sortingPatterns.some(p => text.includes(p))) scores.sorting_array += 4;
    if (orderPatterns.some(p => text.includes(p))) scores.sorting_array += 2;
    
         // Basic Algorithms (Fibonacci, date comparison, matrix, BMI, arithmetic, etc.)
     const algorithmPatterns = ['fibonacci', 'factorial', 'gcd', 'lcm', 'prime', 'palindrome'];
     const datePatterns = ['date', 'time', 'compare', 'day', 'month', 'year'];
     const mathPatterns = ['calculate', 'compute', 'sum', 'average', 'maximum', 'minimum'];
     const matrixPatterns = ['matrix', 'array', '2d', 'two dimensional', 'row', 'column'];
     const bmiPatterns = ['bmi', 'body mass index', 'weight', 'height', 'kg', 'm'];
     const arithmeticPatterns = ['add', 'subtract', 'multiply', 'divide', 'plus', 'minus', 'times', 'division', 'addition', 'subtraction', 'multiplication'];
     
     if (algorithmPatterns.some(p => text.includes(p))) scores.algorithm_io += 3;
     if (datePatterns.some(p => text.includes(p))) scores.algorithm_io += 3;
     if (mathPatterns.some(p => text.includes(p))) scores.algorithm_io += 2;
     if (matrixPatterns.some(p => text.includes(p))) scores.algorithm_io += 3;
     if (bmiPatterns.some(p => text.includes(p))) scores.algorithm_io += 3;
     if (arithmeticPatterns.some(p => text.includes(p))) scores.algorithm_io += 2;
    
    // Simple Data Structures
    const dsPatterns = ['stack', 'queue', 'linked list', 'binary tree'];
    if (dsPatterns.some(p => text.includes(p))) scores.data_structure += 3;
    
    // String Operations
    const stringPatterns = ['string', 'reverse', 'palindrome', 'count', 'character'];
    if (stringPatterns.some(p => text.includes(p))) scores.string_ops += 2;
    
    // Find the highest scoring style
    const maxScore = Math.max(...Object.values(scores));
    const detectedStyles = Object.entries(scores)
        .filter(([style, score]) => score === maxScore && score > 0)
        .map(([style]) => style);
    
    // Return the most likely style
    if (detectedStyles.length > 0 && maxScore >= 2) {
        return detectedStyles[0];
    }
    
    // Default fallback
    if (text.includes('implement') || text.includes('create') || text.includes('build')) {
        return 'algorithm_io';
    }
    
    return 'default';
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
