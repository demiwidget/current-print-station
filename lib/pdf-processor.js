// PDF Processing Module for Current-RMS Print Server Extension
// This module handles PDF parsing, text extraction, and page splitting

class PDFProcessor {
    constructor() {
        this.pdfDoc = null;
        this.pages = [];
    }

    // Process PDF from ArrayBuffer
    async processPDF(pdfArrayBuffer) {
        try {
            console.log('Processing PDF, size:', pdfArrayBuffer.byteLength);
            
            // For a real implementation, we would use PDF.js here
            // Since we can't easily include PDF.js in this demo, we'll simulate the processing
            
            // Simulate PDF processing delay
            await new Promise(resolve => setTimeout(resolve, 1000));
            
            // Simulate extracting pages from PDF
            const simulatedPages = await this.simulatePDFExtraction(pdfArrayBuffer);
            
            this.pages = simulatedPages;
            
            return {
                success: true,
                pages: this.pages,
                totalPages: this.pages.length
            };
            
        } catch (error) {
            console.error('Error processing PDF:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Simulate PDF extraction (in real implementation, this would use PDF.js)
    async simulatePDFExtraction(pdfArrayBuffer) {
        // This is a simulation. In a real implementation, you would:
        // 1. Use PDF.js to load the PDF
        // 2. Extract text from each page
        // 3. Render each page as an image
        // 4. Return structured data
        
        const simulatedPages = [
            {
                pageNumber: 1,
                text: `FLIGHTCASE LABEL
                Flight Case ID: FC001
                Barcode: 123456789
                Contents: LED Lights x12
                Weight: 25kg
                Dimensions: 60x40x30cm
                Job: Wedding Setup - Smith
                Date: 2025-09-01`,
                imageDataUrl: await this.generatePageImage('FC001', '123456789'),
                barcodes: ['123456789']
            },
            {
                pageNumber: 2,
                text: `FLIGHTCASE LABEL
                Flight Case ID: FC002
                Barcode: 987654321
                Contents: Audio Mixer
                Weight: 15kg
                Dimensions: 50x35x25cm
                Job: Wedding Setup - Smith
                Date: 2025-09-01`,
                imageDataUrl: await this.generatePageImage('FC002', '987654321'),
                barcodes: ['987654321']
            },
            {
                pageNumber: 3,
                text: `FLIGHTCASE LABEL
                Flight Case ID: FC003
                Barcode: 456789123
                Contents: Microphones x6
                Weight: 8kg
                Dimensions: 40x30x20cm
                Job: Wedding Setup - Smith
                Date: 2025-09-01`,
                imageDataUrl: await this.generatePageImage('FC003', '456789123'),
                barcodes: ['456789123']
            }
        ];
        
        return simulatedPages;
    }

    // Generate a simulated page image (in real implementation, this would be rendered from PDF)
    async generatePageImage(flightcaseId, barcode) {
        // Create a canvas to generate a sample label image
        const canvas = document.createElement('canvas');
        canvas.width = 400;
        canvas.height = 300;
        const ctx = canvas.getContext('2d');
        
        // Background
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Border
        ctx.strokeStyle = '#000000';
        ctx.lineWidth = 2;
        ctx.strokeRect(10, 10, canvas.width - 20, canvas.height - 20);
        
        // Title
        ctx.fillStyle = '#000000';
        ctx.font = 'bold 24px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('FLIGHTCASE LABEL', canvas.width / 2, 50);
        
        // Flight Case ID
        ctx.font = '18px Arial';
        ctx.fillText(`Flight Case: ${flightcaseId}`, canvas.width / 2, 90);
        
        // Barcode representation (simplified)
        ctx.font = '16px monospace';
        ctx.fillText(`Barcode: ${barcode}`, canvas.width / 2, 130);
        
        // Barcode bars (simplified representation)
        ctx.fillStyle = '#000000';
        for (let i = 0; i < 20; i++) {
            const x = 50 + (i * 15);
            const height = Math.random() * 40 + 20;
            ctx.fillRect(x, 150, 8, height);
        }
        
        // Additional info
        ctx.font = '14px Arial';
        ctx.fillText('Contents: Equipment', canvas.width / 2, 220);
        ctx.fillText('Weight: Variable', canvas.width / 2, 240);
        ctx.fillText('Date: 2025-09-01', canvas.width / 2, 260);
        
        // Convert canvas to data URL
        return canvas.toDataURL('image/png');
    }

    // Find page by barcode
    findPageByBarcode(barcode) {
        console.log('Searching for barcode:', barcode);
        
        for (const page of this.pages) {
            // Direct barcode match
            if (page.barcodes && page.barcodes.includes(barcode)) {
                console.log('Found exact barcode match on page:', page.pageNumber);
                return page;
            }
            
            // Text search (case insensitive)
            if (page.text && page.text.toLowerCase().includes(barcode.toLowerCase())) {
                console.log('Found barcode in text on page:', page.pageNumber);
                return page;
            }
            
            // Partial match (remove spaces and special characters)
            const normalizedBarcode = barcode.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
            const normalizedText = page.text.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
            
            if (normalizedText.includes(normalizedBarcode)) {
                console.log('Found normalized barcode match on page:', page.pageNumber);
                return page;
            }
        }
        
        console.log('Barcode not found in any page');
        return null;
    }

    // Get page by number
    getPage(pageNumber) {
        return this.pages.find(page => page.pageNumber === pageNumber);
    }

    // Get all pages
    getAllPages() {
        return this.pages;
    }

    // Extract all barcodes from PDF
    extractAllBarcodes() {
        const allBarcodes = [];
        
        for (const page of this.pages) {
            if (page.barcodes) {
                allBarcodes.push(...page.barcodes);
            }
            
            // Also try to extract barcodes from text using regex
            const barcodeRegex = /\b\d{8,}\b/g; // Simple regex for numeric barcodes
            const textBarcodes = page.text.match(barcodeRegex) || [];
            allBarcodes.push(...textBarcodes);
        }
        
        // Remove duplicates
        return [...new Set(allBarcodes)];
    }

    // Split PDF into individual pages (returns data URLs)
    async splitPDFPages() {
        const pageImages = [];
        
        for (const page of this.pages) {
            pageImages.push({
                pageNumber: page.pageNumber,
                imageDataUrl: page.imageDataUrl,
                barcodes: page.barcodes
            });
        }
        
        return pageImages;
    }

    // Convert page to printable format
    async convertPageToPrintable(pageNumber) {
        const page = this.getPage(pageNumber);
        if (!page) {
            throw new Error(`Page ${pageNumber} not found`);
        }
        
        // Create a printable HTML document
        const printableHTML = `
            <!DOCTYPE html>
            <html>
            <head>
                <title>Flightcase Label - Page ${pageNumber}</title>
                <style>
                    body {
                        margin: 0;
                        padding: 20px;
                        text-align: center;
                        font-family: Arial, sans-serif;
                    }
                    .label-container {
                        max-width: 100%;
                        margin: 0 auto;
                    }
                    .label-image {
                        max-width: 100%;
                        height: auto;
                        border: 1px solid #ccc;
                    }
                    .label-info {
                        margin-top: 10px;
                        font-size: 12px;
                        color: #666;
                    }
                    @media print {
                        body { margin: 0; padding: 0; }
                        .label-info { display: none; }
                        .label-image { width: 100%; height: auto; }
                    }
                </style>
            </head>
            <body>
                <div class="label-container">
                    <img src="${page.imageDataUrl}" alt="Flightcase Label" class="label-image">
                    <div class="label-info">
                        Page ${pageNumber} - Barcodes: ${page.barcodes ? page.barcodes.join(', ') : 'None'}
                    </div>
                </div>
                <script>
                    window.onload = function() {
                        // Auto-print after a short delay
                        setTimeout(function() {
                            window.print();
                        }, 500);
                    };
                </script>
            </body>
            </html>
        `;
        
        return printableHTML;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PDFProcessor;
} else if (typeof window !== 'undefined') {
    window.PDFProcessor = PDFProcessor;
}

