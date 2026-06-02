// Background script for Current-RMS Print Server Extension
console.log('Current-RMS Print Server Extension background script loaded');

// Import PDF.js with proper error handling
try {
    importScripts('/lib/pdf.min.js');
    console.log('PDF.js loaded successfully');
    
    // Configure PDF.js worker
    if (typeof pdfjsLib !== 'undefined') {
        pdfjsLib.GlobalWorkerOptions.workerSrc = chrome.runtime.getURL('lib/pdf.worker.min.js');
        console.log('PDF.js worker configured');
    } else {
        console.error('PDF.js not available');
    }
} catch (error) {
    console.error('Error loading PDF.js:', error);
}

// Listen for messages from content script and popup
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    console.log('Background script received message:', request.action);
    
    if (request.action === 'processPdfForBarcode') {
        handlePdfProcessing(request.pdfData, request.barcode)
            .then(result => {
                console.log('PDF processing result:', result);
                sendResponse(result);
            })
            .catch(error => {
                console.error('Error processing PDF:', error);
                sendResponse({ success: false, error: error.message });
            });
        return true; // Keep message channel open for async response
    }
});

// Process PDF and search for barcode
async function handlePdfProcessing(pdfData, barcode) {
    try {
        console.log('Processing PDF for barcode:', barcode);
        console.log('PDF data type:', typeof pdfData);
        console.log('PDF data length:', pdfData ? pdfData.length : 'null');
        
        // Check if PDF.js is available
        if (typeof pdfjsLib === 'undefined') {
            throw new Error('PDF.js not loaded. Please reload the extension.');
        }
        
        // Convert data URL to array buffer if needed
        let arrayBuffer;
        if (typeof pdfData === 'string' && pdfData.startsWith('data:')) {
            console.log('Converting data URL to array buffer');
            try {
                // Extract base64 data from data URL
                const base64Data = pdfData.split(',')[1];
                if (!base64Data) {
                    throw new Error('Invalid data URL format');
                }
                
                // Convert base64 to binary string
                const binaryString = atob(base64Data);
                
                // Convert binary string to array buffer
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }
                arrayBuffer = bytes.buffer;
            } catch (error) {
                throw new Error(`Failed to convert data URL: ${error.message}`);
            }
        } else if (pdfData instanceof ArrayBuffer) {
            arrayBuffer = pdfData;
        } else if (typeof pdfData === 'string') {
            // Handle base64 string directly
            console.log('Converting base64 string to array buffer');
            try {
                const binaryString = atob(pdfData);
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }
                arrayBuffer = bytes.buffer;
            } catch (error) {
                throw new Error(`Failed to convert base64: ${error.message}`);
            }
        } else {
            throw new Error('Invalid PDF data format');
        }
        
        console.log('Array buffer size:', arrayBuffer.byteLength);
        
        // Load PDF document
        console.log('Loading PDF document...');
        const loadingTask = pdfjsLib.getDocument({ data: arrayBuffer });
        const pdfDoc = await loadingTask.promise;
        
        console.log(`PDF loaded successfully. Pages: ${pdfDoc.numPages}`);
        
        // Search for barcode across all pages
        let foundPage = null;
        for (let pageNum = 1; pageNum <= pdfDoc.numPages; pageNum++) {
            console.log(`Searching page ${pageNum} for barcode ${barcode}`);
            
            try {
                const page = await pdfDoc.getPage(pageNum);
                const textContent = await page.getTextContent();
                const text = textContent.items.map(item => item.str).join(' ');
                
                console.log(`Page ${pageNum} text length:`, text.length);
                
                if (barcodeMatchesText(text, barcode)) {
                    console.log(`Found barcode ${barcode} on page ${pageNum}`);
                    foundPage = pageNum;
                    break;
                }
            } catch (pageError) {
                console.error(`Error processing page ${pageNum}:`, pageError);
            }
        }
        
        if (foundPage) {
            // Render the found page to canvas
            console.log(`Rendering page ${foundPage}...`);
            const page = await pdfDoc.getPage(foundPage);
            const viewport = page.getViewport({ scale: 1.5 });
            
            // Create offscreen canvas
            const canvas = new OffscreenCanvas(viewport.width, viewport.height);
            const context = canvas.getContext('2d');
            
            // Render page
            const renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            
            await page.render(renderContext).promise;
            console.log('Page rendered successfully');
            
            // Convert to blob and then to data URL
            const blob = await canvas.convertToBlob({ type: 'image/png' });
            const dataUrl = await blobToDataUrl(blob);
            
            // Clean up
            pdfDoc.destroy();
            
            return {
                success: true,
                pageNumber: foundPage,
                pageData: dataUrl,
                width: viewport.width,
                height: viewport.height,
                barcode: barcode
            };
        } else {
            // Clean up
            pdfDoc.destroy();
            
            return {
                success: false,
                error: `Barcode ${barcode} not found in PDF (searched ${pdfDoc.numPages} pages)`
            };
        }
        
    } catch (error) {
        console.error('Error in PDF processing:', error);
        return {
            success: false,
            error: `PDF processing failed: ${error.message}`
        };
    }
}

function normalizeBarcodeText(value) {
    return String(value || '').replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
}

function barcodeMatchesText(text, barcode) {
    const rawText = String(text || '');
    const target = String(barcode || '').trim();

    if (!target) {
        return false;
    }

    return rawText.includes(target) || normalizeBarcodeText(rawText).includes(normalizeBarcodeText(target));
}

// Convert blob to data URL
function blobToDataUrl(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}
