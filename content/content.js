// Content script for Current-RMS Print Server Extension
console.log('Current-RMS Print Server Extension content script loaded');

// Listen for messages from popup
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    console.log('Content script received message:', request.action);
    
    if (request.action === 'searchBarcode') {
        handleBarcodeSearch(request.barcode)
            .then(response => sendResponse(response))
            .catch(error => {
                console.error('Error in barcode search:', error);
                sendResponse({ success: false, error: error.message });
            });
        return true; // Keep message channel open for async response
    }
    
    if (request.action === 'checkJobPdf') {
        checkJobPdfAvailability()
            .then(response => sendResponse(response))
            .catch(error => {
                console.error('Error checking job PDF:', error);
                sendResponse({ success: false, error: error.message });
            });
        return true;
    }
    
    if (request.action === 'downloadJobPdf') {
        downloadJobPdf()
            .then(response => sendResponse(response))
            .catch(error => {
                console.error('Error downloading job PDF:', error);
                sendResponse({ success: false, error: error.message });
            });
        return true;
    }
});

function extractOpportunityIdFromUrl(url = window.location.href) {
    const opportunityMatch = url.match(/\/opportunities\/(\d+)/);
    return opportunityMatch ? opportunityMatch[1] : null;
}

function getConfiguredDocumentId(config) {
    if (config && config.documentId) {
        return String(config.documentId).trim();
    }

    return extractDocumentIdFromPage() || '1000167';
}

// Check if PDF is available for the current job
async function checkJobPdfAvailability() {
    try {
        console.log('Checking PDF availability for current job...');
        
        // Extract opportunity ID from URL
        const opportunityId = extractOpportunityIdFromUrl();
        if (!opportunityId) {
            return { success: false, error: 'Not on a job page' };
        }
        
        console.log('Opportunity ID:', opportunityId);
        
        // Get stored configuration
        const config = await chrome.storage.local.get(['subdomain', 'apiKey', 'documentId']);
        if (!config.subdomain || !config.apiKey || !config.documentId) {
            return { success: false, error: 'Extension not configured' };
        }
        
        const documentId = getConfiguredDocumentId(config);
        if (!documentId) {
            return { success: false, error: 'Could not find flightcase label document' };
        }
        
        // Return success without actually downloading the PDF
        return { 
            success: true, 
            opportunityId: opportunityId,
            documentId: documentId,
            pages: 'Unknown' // We don't know page count without downloading
        };
        
    } catch (error) {
        console.error('Error checking PDF availability:', error);
        return { success: false, error: error.message };
    }
}

// Download the PDF for the current job
async function downloadJobPdf() {
    try {
        console.log('Downloading PDF for current job...');
        
        // Extract opportunity ID from URL
        const opportunityId = extractOpportunityIdFromUrl();
        if (!opportunityId) {
            return { success: false, error: 'Not on a job page' };
        }
        
        console.log('Opportunity ID:', opportunityId);
        
        // Get stored configuration
        const config = await chrome.storage.local.get(['subdomain', 'apiKey', 'documentId']);
        if (!config.subdomain || !config.apiKey || !config.documentId) {
            return { success: false, error: 'Extension not configured' };
        }
        
        console.log("Using subdomain:", config.subdomain);
        
        const documentId = getConfiguredDocumentId(config);
        console.log("Using document ID:", documentId);
        
        // Construct the PDF download URL using your exact format
        const pdfUrl = `https://${config.subdomain}.current-rms.com/opportunities/${opportunityId}/print_document.pdf?document_id=${documentId}`;
        console.log("PDF URL:", pdfUrl);
        
        // Download the PDF
        const response = await fetch(pdfUrl, {
            method: 'GET',
            headers: {
                'X-SUBDOMAIN': config.subdomain,
                'X-AUTH-TOKEN': config.apiKey,
                'Accept': 'application/pdf'
            }
        });
        
        console.log('PDF download response status:', response.status);
        console.log('PDF download response headers:', Object.fromEntries(response.headers.entries()));
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('PDF download failed:', response.status, errorText);
            return { 
                success: false, 
                error: `PDF download failed: ${response.status} ${response.statusText} - ${errorText.substring(0, 200)}...` 
            };
        }
        
        const contentType = response.headers.get('content-type');
        console.log('Content-Type:', contentType);
        
        if (!contentType || !contentType.includes('application/pdf')) {
            const responseText = await response.text();
            console.error('Invalid content type:', contentType, 'Response:', responseText.substring(0, 500));
            return { 
                success: false, 
                error: `Invalid content type: ${contentType}. Expected PDF but got: ${responseText.substring(0, 100)}...` 
            };
        }
        
        const arrayBuffer = await response.arrayBuffer();
        console.log('PDF size:', arrayBuffer.byteLength, 'bytes');
        
        if (arrayBuffer.byteLength === 0) {
            return { success: false, error: 'Downloaded PDF is empty' };
        }
        
        // Convert to base64 for storage without overflowing the call stack on large PDFs.
        const dataUrl = await blobToDataUrl(new Blob([arrayBuffer], { type: 'application/pdf' }));
        const base64String = dataUrl.split(',')[1];
        
        console.log('PDF downloaded successfully, base64 length:', base64String.length);
        
        return {
            success: true,
            pdfData: base64String,
            opportunityId: opportunityId,
            documentId: documentId,
            pages: 'Multiple' // We could parse the PDF to get exact count
        };
        
    } catch (error) {
        console.error('Error downloading PDF:', error);
        return { success: false, error: error.message };
    }
}

// Extract job information and handle PDF download
async function handleBarcodeSearch(barcode) {
    try {
        // Extract job/opportunity ID from current URL
        const jobInfo = extractJobInfo();
        if (!jobInfo.success) {
            return { success: false, error: jobInfo.error };
        }

        // Get stored configuration
        const config = await chrome.storage.local.get(['subdomain', 'apiKey', 'documentId']);
        if (!config.subdomain || !config.apiKey || !config.documentId) {
            return { success: false, error: 'Extension not configured. Please check settings.' };
        }

        // Download PDF silently
        const pdfResult = await downloadFlightcasePdf(jobInfo.opportunityId, config);
        if (!pdfResult.success) {
            return { success: false, error: pdfResult.error };
        }

        // Search for barcode in PDF
        const searchResult = await searchBarcodeInPdf(pdfResult.pdfData, barcode);
        if (!searchResult.success) {
            return { success: false, error: 'Barcode not found in PDF' };
        }

        return {
            success: true,
            pdfData: searchResult.pageData,
            pageNumber: searchResult.pageNumber,
            barcode: barcode
        };

    } catch (error) {
        console.error('Error in handleBarcodeSearch:', error);
        return { success: false, error: error.message };
    }
}

// Extract job/opportunity information from current page
function extractJobInfo() {
    try {
        const url = window.location.href;
        
        // Match opportunity ID from URL patterns like:
        // /opportunities/12345
        // /opportunities/12345/edit
        // /opportunities/12345/print_document.pdf
        const opportunityMatch = url.match(/\/opportunities\/(\d+)/);
        
        if (opportunityMatch) {
            return {
                success: true,
                opportunityId: opportunityMatch[1],
                type: 'opportunity'
            };
        }

        // Try to extract from page elements if URL doesn't contain ID
        const breadcrumbs = document.querySelector('.breadcrumb, .breadcrumbs');
        if (breadcrumbs) {
            const links = breadcrumbs.querySelectorAll('a[href*="/opportunities/"]');
            for (const link of links) {
                const match = link.href.match(/\/opportunities\/(\d+)/);
                if (match) {
                    return {
                        success: true,
                        opportunityId: match[1],
                        type: 'opportunity'
                    };
                }
            }
        }

        // Try to find opportunity ID in page data attributes or hidden fields
        const hiddenInputs = document.querySelectorAll('input[type="hidden"]');
        for (const input of hiddenInputs) {
            if (input.name.includes('opportunity') && input.value.match(/^\d+$/)) {
                return {
                    success: true,
                    opportunityId: input.value,
                    type: 'opportunity'
                };
            }
        }

        return {
            success: false,
            error: 'Could not extract opportunity ID from current page. Please ensure you are on a Current-RMS opportunity/job page.'
        };

    } catch (error) {
        console.error('Error extracting job info:', error);
        return {
            success: false,
            error: 'Error extracting job information from page'
        };
    }
}

// Download flightcase PDF using the correct URL format
async function downloadFlightcasePdf(opportunityId, config) {
    try {
        console.log('Downloading PDF for opportunity:', opportunityId);
        
        const documentId = getConfiguredDocumentId(config);
        if (!documentId) {
            return { success: false, error: 'Could not find flightcase label document ID' };
        }
        
        console.log('Using document ID:', documentId);
        
        // Construct the PDF download URL using the format provided by the user
        const pdfUrl = `https://${config.subdomain}.current-rms.com/opportunities/${opportunityId}/print_document.pdf?document_id=${documentId}`;
        
        console.log('Downloading PDF from:', pdfUrl);

        // Download PDF using fetch with authentication
        const response = await fetch(pdfUrl, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${config.apiKey}`,
                'Accept': 'application/pdf',
                'Content-Type': 'application/pdf'
            },
            credentials: 'include'
        });

        console.log('PDF download response status:', response.status);
        console.log('PDF download response headers:', [...response.headers.entries()]);

        if (!response.ok) {
            throw new Error(`Failed to download PDF: ${response.status} ${response.statusText}`);
        }

        // Check if response is actually a PDF
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/pdf')) {
            console.warn('Response may not be a PDF. Content-Type:', contentType);
        }

        // Convert response to blob and then to data URL
        const blob = await response.blob();
        console.log('PDF blob size:', blob.size, 'bytes');
        
        if (blob.size === 0) {
            throw new Error('Downloaded PDF is empty');
        }
        
        const dataUrl = await blobToDataUrl(blob);
        console.log('PDF converted to data URL, length:', dataUrl.length);

        return {
            success: true,
            pdfData: dataUrl,
            documentId: documentId
        };

    } catch (error) {
        console.error('Error downloading PDF:', error);
        return {
            success: false,
            error: `Failed to download PDF: ${error.message}`
        };
    }
}

// Get the document ID for flightcase labels


// Extract document ID from current page
function extractDocumentIdFromPage() {
    try {
        // Look for document ID in URL parameters
        const urlParams = new URLSearchParams(window.location.search);
        const docId = urlParams.get('document_id');
        if (docId) {
            return docId;
        }

        // Look for document ID in page elements
        return extractDocumentIdFromPageElements();

    } catch (error) {
        console.error('Error extracting document ID from page:', error);
        return null;
    }
}

// Extract document ID from page elements
function extractDocumentIdFromPageElements() {
    try {
        console.log('Extracting document ID from page elements...');
        
        // Look for links or forms that contain document_id
        const links = document.querySelectorAll('a[href*="document_id="], a[href*="print_document"]');
        console.log('Found links with document_id:', links.length);
        
        for (const link of links) {
            const match = link.href.match(/document_id=(\d+)/);
            if (match) {
                console.log('Found document ID in link:', match[1]);
                return match[1];
            }
        }

        // Look for hidden inputs with document_id
        const hiddenInputs = document.querySelectorAll('input[name*="document_id"], input[value*="document_id"]');
        console.log('Found hidden inputs with document_id:', hiddenInputs.length);
        
        for (const input of hiddenInputs) {
            const match = input.value.match(/\d+/);
            if (match) {
                console.log('Found document ID in input:', match[0]);
                return match[0];
            }
        }

        // Look for data attributes
        const elements = document.querySelectorAll('[data-document-id], [data-doc-id]');
        console.log('Found elements with data-document-id:', elements.length);
        
        for (const element of elements) {
            const docId = element.dataset.documentId || element.dataset.docId;
            if (docId && docId.match(/^\d+$/)) {
                console.log('Found document ID in data attribute:', docId);
                return docId;
            }
        }

        // Look for the document ID in the current URL if we're on a print_document page
        const currentUrl = window.location.href;
        const urlMatch = currentUrl.match(/document_id=(\d+)/);
        if (urlMatch) {
            console.log('Found document ID in current URL:', urlMatch[1]);
            return urlMatch[1];
        }

        // Look for print buttons or links that might contain the document ID
        const printButtons = document.querySelectorAll('a[href*="print"], button[onclick*="print"], .print-btn, .btn-print');
        console.log('Found print buttons/links:', printButtons.length);
        
        for (const button of printButtons) {
            const href = button.href || button.getAttribute('onclick') || '';
            const match = href.match(/document_id[=:](\d+)/);
            if (match) {
                console.log('Found document ID in print button:', match[1]);
                return match[1];
            }
        }

        // Use the specific document ID from the user's example as fallback
        console.log('Using fallback document ID from user example: 1000167');
        return '1000167'; // Updated to match user's actual document ID

    } catch (error) {
        console.error('Error extracting document ID from page elements:', error);
        return '1000167'; // Fallback to user's document ID
    }
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

// Search for barcode in PDF data
async function searchBarcodeInPdf(pdfData, barcode) {
    try {
        // Send PDF data to background script for processing
        const response = await chrome.runtime.sendMessage({
            action: 'processPdfForBarcode',
            pdfData: pdfData,
            barcode: barcode
        });

        return response;

    } catch (error) {
        console.error('Error searching PDF:', error);
        return {
            success: false,
            error: 'Error processing PDF'
        };
    }
}
