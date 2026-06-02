// PDF Renderer for Current-RMS Print Server Extension
// This module handles PDF processing, page extraction, and barcode searching

class PDFRenderer {
    constructor() {
        this.pdfDoc = null;
        this.workerSrc = chrome.runtime.getURL('lib/pdf.worker.min.js');
        
        // Configure PDF.js
        if (typeof pdfjsLib !== 'undefined') {
            pdfjsLib.GlobalWorkerOptions.workerSrc = this.workerSrc;
        }
    }

    // Load PDF from data URL or blob
    async loadPDF(pdfData) {
        try {
            let arrayBuffer;
            
            if (typeof pdfData === 'string' && pdfData.startsWith('data:')) {
                // Convert data URL to array buffer
                const response = await fetch(pdfData);
                arrayBuffer = await response.arrayBuffer();
            } else if (pdfData instanceof ArrayBuffer) {
                arrayBuffer = pdfData;
            } else {
                throw new Error('Invalid PDF data format');
            }

            // Load PDF document
            const loadingTask = pdfjsLib.getDocument({ data: arrayBuffer });
            this.pdfDoc = await loadingTask.promise;
            
            console.log(`PDF loaded successfully. Pages: ${this.pdfDoc.numPages}`);
            return {
                success: true,
                numPages: this.pdfDoc.numPages
            };

        } catch (error) {
            console.error('Error loading PDF:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Render a specific page to canvas
    async renderPage(pageNumber, canvas, scale = 1.5) {
        try {
            if (!this.pdfDoc) {
                throw new Error('PDF not loaded');
            }

            if (pageNumber < 1 || pageNumber > this.pdfDoc.numPages) {
                throw new Error(`Invalid page number: ${pageNumber}`);
            }

            const page = await this.pdfDoc.getPage(pageNumber);
            const viewport = page.getViewport({ scale });
            
            // Set canvas dimensions
            canvas.width = viewport.width;
            canvas.height = viewport.height;
            
            const context = canvas.getContext('2d');
            
            // Render page
            const renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            
            await page.render(renderContext).promise;
            
            return {
                success: true,
                width: viewport.width,
                height: viewport.height
            };

        } catch (error) {
            console.error('Error rendering page:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Extract text from a specific page
    async extractPageText(pageNumber) {
        try {
            if (!this.pdfDoc) {
                throw new Error('PDF not loaded');
            }

            const page = await this.pdfDoc.getPage(pageNumber);
            const textContent = await page.getTextContent();
            
            // Combine all text items
            const text = textContent.items.map(item => item.str).join(' ');
            
            return {
                success: true,
                text: text
            };

        } catch (error) {
            console.error('Error extracting text:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Search for barcode across all pages
    async searchBarcode(barcode) {
        try {
            if (!this.pdfDoc) {
                throw new Error('PDF not loaded');
            }

            const results = [];
            
            for (let pageNum = 1; pageNum <= this.pdfDoc.numPages; pageNum++) {
                const textResult = await this.extractPageText(pageNum);
                
                if (textResult.success) {
                    // Search for exact barcode match
                    if (textResult.text.includes(barcode)) {
                        results.push({
                            pageNumber: pageNum,
                            text: textResult.text,
                            found: true
                        });
                    }
                }
            }

            if (results.length > 0) {
                return {
                    success: true,
                    results: results,
                    firstMatch: results[0]
                };
            } else {
                return {
                    success: false,
                    error: 'Barcode not found in PDF'
                };
            }

        } catch (error) {
            console.error('Error searching barcode:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Get page as image data URL
    async getPageAsImage(pageNumber, scale = 1.5) {
        try {
            const canvas = document.createElement('canvas');
            const renderResult = await this.renderPage(pageNumber, canvas, scale);
            
            if (renderResult.success) {
                return {
                    success: true,
                    dataUrl: canvas.toDataURL('image/png'),
                    width: renderResult.width,
                    height: renderResult.height
                };
            } else {
                return renderResult;
            }

        } catch (error) {
            console.error('Error getting page as image:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Split PDF into individual page images
    async splitPdfToImages(scale = 1.5) {
        try {
            if (!this.pdfDoc) {
                throw new Error('PDF not loaded');
            }

            const pages = [];
            
            for (let pageNum = 1; pageNum <= this.pdfDoc.numPages; pageNum++) {
                const imageResult = await this.getPageAsImage(pageNum, scale);
                
                if (imageResult.success) {
                    pages.push({
                        pageNumber: pageNum,
                        dataUrl: imageResult.dataUrl,
                        width: imageResult.width,
                        height: imageResult.height
                    });
                }
            }

            return {
                success: true,
                pages: pages,
                totalPages: this.pdfDoc.numPages
            };

        } catch (error) {
            console.error('Error splitting PDF:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Find and extract specific page with barcode
    async findAndExtractPage(barcode, scale = 1.5) {
        try {
            // First search for the barcode
            const searchResult = await this.searchBarcode(barcode);
            
            if (!searchResult.success) {
                return searchResult;
            }

            // Get the first matching page as image
            const pageNumber = searchResult.firstMatch.pageNumber;
            const imageResult = await this.getPageAsImage(pageNumber, scale);
            
            if (imageResult.success) {
                return {
                    success: true,
                    pageNumber: pageNumber,
                    dataUrl: imageResult.dataUrl,
                    width: imageResult.width,
                    height: imageResult.height,
                    barcode: barcode
                };
            } else {
                return imageResult;
            }

        } catch (error) {
            console.error('Error finding and extracting page:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Clean up resources
    destroy() {
        if (this.pdfDoc) {
            this.pdfDoc.destroy();
            this.pdfDoc = null;
        }
    }
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PDFRenderer;
} else {
    window.PDFRenderer = PDFRenderer;
}

