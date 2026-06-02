// DOM elements
const settingsToggle = document.getElementById("settingsToggle");
const settingsSection = document.getElementById("settingsSection");
const scannerSection = document.getElementById("scannerSection");
const configForm = document.getElementById("configForm");
const subdomainInput = document.getElementById("subdomain");
const apiKeyInput = document.getElementById("apiKey");
const documentIdInput = document.getElementById("documentId");
const saveBtn = document.getElementById("saveBtn");
const testBtn = document.getElementById("testBtn");
const statusDiv = document.getElementById("statusDiv");
const barcodeInput = document.getElementById("barcodeInput");
const searchBtn = document.getElementById("searchBtn");
const printBtn = document.getElementById("printBtn");
const downloadBtn = document.getElementById("downloadBtn");
const downloadPageBtn = document.getElementById("downloadBtn"); // Alias for compatibility
const previewContainer = document.getElementById("previewContainer");
const previewActions = document.getElementById("previewActions");
const statusMessages = document.getElementById("statusMessages");
const downloadPdfBtn = document.getElementById("downloadPdfBtn");
const refreshPdfBtn = document.getElementById("refreshPdfBtn");
const pdfStatus = document.getElementById("pdfStatus");
const pdfStatusText = document.getElementById("pdfStatusText");
const autoRefreshCheckbox = document.getElementById("autoRefreshCheckbox");
const autoPrintCheckbox = document.getElementById("autoPrintCheckbox");
const pdfPreview = document.getElementById("previewContainer"); // Use previewContainer as pdfPreview

// New multi-print DOM elements
const singlePrintModeRadio = document.getElementById("singlePrintMode");
const multiPrintModeRadio = document.getElementById("multiPrintMode");
const singlePrintSection = document.getElementById("singlePrintSection");
const multiPrintSection = document.getElementById("multiPrintSection");
const multiBarcodeInput = document.getElementById("multiBarcodeInput");
const processMultiBarcodesBtn = document.getElementById("processMultiBarcodesBtn");
const clearMultiBarcodeInputBtn = document.getElementById("clearMultiBarcodeInputBtn");
const pageNumbersOutput = document.getElementById("pageNumbersOutput");
const copyPageNumbersBtn = document.getElementById("copyPageNumbersBtn");

// State management
let currentPdfData = null;  // Preview image data
let currentPageNumber = null;
let currentBarcode = null;
let currentOriginalPdfData = null;  // Original PDF data for high-quality printing
let currentPdfDocument = null;  // PDF document reference
let currentJobPdf = null; // Store the full PDF for the current job
let autoRefreshInterval = null;
let refreshIntervalSeconds = 30;
let scannedPageNumbers = []; // Array to store page numbers for multi-print
let multiPrintResults = []; // Matched barcode/page pairs for the latest multi-print run
let barcodeSearchTimer = null;

// Utility function to set loading state on buttons
function setLoading(button, isLoading) {
    if (!button) {
        return;
    }

    if (isLoading) {
        button.disabled = true;
        button.classList.add("loading");
    } else {
        button.disabled = false;
        button.classList.remove("loading");
    }
}

function hasChromeStorage() {
    return typeof chrome !== "undefined" && chrome.storage && chrome.storage.local;
}

async function getStoredConfig(keys) {
    if (!hasChromeStorage()) {
        return {};
    }

    return chrome.storage.local.get(keys);
}

async function setStoredConfig(values) {
    if (!hasChromeStorage()) {
        throw new Error("Chrome extension storage is unavailable.");
    }

    return chrome.storage.local.set(values);
}

// Status message display
function showStatus(message, type) {
    statusMessages.textContent = message;
    statusMessages.className = `status-message ${type}`;
    statusMessages.style.display = "block";
    setTimeout(() => {
        statusMessages.style.display = "none";
    }, 5000);
}

function updatePdfStatus(message, type) {
    pdfStatusText.textContent = message;
    const statusClasses = {
        success: "pdf-loaded",
        error: "pdf-error",
        loading: "pdf-loading",
        default: ""
    };
    const statusClass = statusClasses[type] || "";
    pdfStatus.className = ["status-indicator", statusClass].filter(Boolean).join(" ");
}

// PDF Preview handling
async function displayPdfPreview(pageData, pageNumber, barcode, originalPdfData, pdfDocument) {
    previewContainer.innerHTML = ""; // Clear previous preview
    const img = document.createElement("img");
    img.src = pageData;
    img.alt = `Page ${pageNumber} preview`;
    img.style.maxWidth = "100%";
    img.style.height = "auto";

    previewContainer.appendChild(img);
    previewContainer.classList.add("has-content");
    previewActions.classList.remove("hidden");
    previewActions.style.display = "flex";

    currentPdfData = pageData;
    currentPageNumber = pageNumber;
    currentBarcode = barcode;
    currentOriginalPdfData = originalPdfData || currentJobPdf;
    currentPdfDocument = pdfDocument || null;
}

function hidePdfPreview() {
    previewContainer.innerHTML = "";
    previewContainer.classList.remove("has-content");
    previewActions.classList.add("hidden");
    previewActions.style.display = "none";
    currentPdfData = null;
    currentPageNumber = null;
    currentBarcode = null;
    currentOriginalPdfData = null;
    currentPdfDocument = null;
}

function resetScanFormAfterPrint() {
    barcodeInput.value = "";
    multiBarcodeInput.value = "";
    pageNumbersOutput.value = "";
    scannedPageNumbers = [];
    multiPrintResults = [];
    hidePdfPreview();
    focusBarcodeInput();
}

// Event Listeners
document.addEventListener("DOMContentLoaded", async () => {
    await loadSavedConfig();
    setupEventListeners();

    // Check if configuration exists, if not show settings
    const config = await getStoredConfig(["subdomain", "apiKey", "documentId", "autoPrintAfterScan"]);
    autoPrintCheckbox.checked = config.autoPrintAfterScan !== false;

    if (!config.subdomain || !config.apiKey || !config.documentId) {
        showSettings();
    } else {
        hideSettings();
        focusBarcodeInput();

        // Only show PDF status, don't auto-download
        updatePdfStatus("Ready - Click refresh to load PDF", "default");
    }

    // Initialize print mode display
    updatePrintModeDisplay();
});

function setupEventListeners() {
    settingsToggle.addEventListener("click", toggleSettings);
    configForm.addEventListener("submit", handleSaveConfig);
    testBtn.addEventListener("click", handleTestConnection);
    printBtn.addEventListener("click", handlePrint);
    downloadPageBtn.addEventListener("click", handleDownloadPage);
    downloadPdfBtn.addEventListener("click", handleDownloadPdf);
    refreshPdfBtn.addEventListener("click", handleRefreshPdf);
    autoRefreshCheckbox.addEventListener("change", handleAutoRefreshToggle);
    autoPrintCheckbox.addEventListener("change", handleAutoPrintToggle);

    // Mode toggle event listeners
    singlePrintModeRadio.addEventListener("change", updatePrintModeDisplay);
    multiPrintModeRadio.addEventListener("change", updatePrintModeDisplay);

    // Single print section event listeners
    searchBtn.addEventListener("click", () => handleSearch({ autoPrint: autoPrintCheckbox.checked }));
    barcodeInput.addEventListener("input", (e) => {
        const value = e.target.value.trim();
        if (barcodeSearchTimer) {
            clearTimeout(barcodeSearchTimer);
            barcodeSearchTimer = null;
        }

        if (value.length >= 8) { // Assuming minimum barcode length
            barcodeSearchTimer = setTimeout(() => {
                if (barcodeInput.value === value) {
                    handleSearch({ autoPrint: autoPrintCheckbox.checked });
                }
            }, 500); // Small delay to allow for complete scan
        }
    });
    barcodeInput.addEventListener("keypress", (e) => {
        if (e.key === "Enter") {
            if (barcodeSearchTimer) {
                clearTimeout(barcodeSearchTimer);
                barcodeSearchTimer = null;
            }

            handleSearch({ autoPrint: autoPrintCheckbox.checked });
        }
    });

    // Multi print section event listeners
    processMultiBarcodesBtn.addEventListener("click", handleProcessMultiBarcodes);
    clearMultiBarcodeInputBtn.addEventListener("click", handleClearMultiBarcodeInput);
    copyPageNumbersBtn.addEventListener("click", handleCopyPageNumbers);
}

// Print mode display logic
function updatePrintModeDisplay() {
    if (singlePrintModeRadio.checked) {
        singlePrintSection.classList.remove("hidden");
        multiPrintSection.classList.add("hidden");
        barcodeInput.focus();
    } else {
        singlePrintSection.classList.add("hidden");
        multiPrintSection.classList.remove("hidden");
        multiBarcodeInput.focus();
    }
}

// Settings management
function toggleSettings() {
    if (settingsSection.classList.contains("hidden")) {
        showSettings();
    } else {
        hideSettings();
    }
}

function showSettings() {
    settingsSection.classList.remove("hidden");
    scannerSection.style.display = "none";
}

function hideSettings() {
    settingsSection.classList.add("hidden");
    scannerSection.style.display = "block";
    focusBarcodeInput();
}

function focusBarcodeInput() {
    setTimeout(() => {
        barcodeInput.focus();
    }, 100);
}

// Configuration management
async function loadSavedConfig() {
    try {
        const config = await getStoredConfig(["subdomain", "apiKey", "documentId"]);
        if (config.subdomain) {
            subdomainInput.value = config.subdomain;
        }
        if (config.apiKey) {
            apiKeyInput.value = config.apiKey;
        }
        if (config.documentId) {
            documentIdInput.value = config.documentId;
        }
    } catch (error) {
        console.error("Error loading config:", error);
    }
}

async function handleSaveConfig(e) {
    e.preventDefault();

    const subdomain = subdomainInput.value.trim();
    const apiKey = apiKeyInput.value.trim();
    const documentId = documentIdInput.value.trim();

    if (!subdomain || !apiKey || !documentId) {
        showStatus("Please fill in all fields", "error");
        return;
    }

    // Validate document ID is a number
    if (isNaN(documentId) || parseInt(documentId) <= 0) {
        showStatus("Document ID must be a valid positive number", "error");
        return;
    }

    setLoading(saveBtn, true);

    try {
        await setStoredConfig({
            subdomain,
            apiKey,
            documentId: parseInt(documentId),
            autoPrintAfterScan: autoPrintCheckbox.checked
        });
        showStatus("Configuration saved successfully!", "success");

        // Automatically test connection after saving
        console.log("Testing connection after saving...");
        await testConnectionAfterSave(subdomain, apiKey);

        // Don't auto-close settings - let user manually close
        console.log("Settings saved. User can manually close settings panel.");
    } catch (error) {
        console.error("Error saving config:", error);
        showStatus("Error saving configuration", "error");
    } finally {
        setLoading(saveBtn, false);
    }
}

async function handleTestConnection() {
    const subdomain = subdomainInput.value.trim();
    const apiKey = apiKeyInput.value.trim();

    if (!subdomain || !apiKey) {
        showStatus("Please fill in all fields", "error");
        return;
    }

    setLoading(testBtn, true);

    try {
        const response = await fetch(`https://api.current-rms.com/api/v1/members/1`, {
            method: "GET",
            headers: {
                "X-SUBDOMAIN": subdomain,
                "X-AUTH-TOKEN": apiKey,
                "Accept": "application/json"
            }
        });

        if (response.ok) {
            const data = await response.json();
            showStatus(`Connection successful! Welcome, ${data.member.name}.`, "success");
        } else {
            const errorText = await response.text();
            showStatus(`Connection failed: ${response.status} ${response.statusText} - ${errorText.substring(0, 100)}...`, "error");
        }
    } catch (error) {
        console.error("Connection test error:", error);
        showStatus(`Network error: ${error.message}`, "error");
    } finally {
        setLoading(testBtn, false);
    }
}

async function testConnectionAfterSave(subdomain, apiKey) {
    try {
        const response = await fetch(`https://api.current-rms.com/api/v1/members/1`, {
            method: "GET",
            headers: {
                "X-SUBDOMAIN": subdomain,
                "X-AUTH-TOKEN": apiKey,
                "Accept": "application/json"
            }
        });

        if (response.ok) {
            const data = await response.json();
            showStatus(`API Connection Test: Successful! Welcome, ${data.member.name}.`, "success");
        } else {
            const errorText = await response.text();
            showStatus(`API Connection Test: Failed! ${response.status} ${response.statusText} - ${errorText.substring(0, 100)}...`, "error");
        }
    } catch (error) {
        console.error("API Connection Test Error:", error);
        showStatus(`API Connection Test: Network error: ${error.message}`, "error");
    }
}

// PDF Management Functions
async function checkAndLoadJobPdf() {
    try {
        // Get current tab to check if we're on a job page
        const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

        if (!tab.url.includes("current-rms.com") || !tab.url.includes("opportunities/")) {
            updatePdfStatus("Not on a job page", "default");
            return;
        }

        // Automatically download PDF when on a job page
        await handleDownloadPdf(true); // Pass true for silent mode

        // Start auto-refresh if enabled
        if (autoRefreshCheckbox.checked) {
            startAutoRefresh();
        }
    } catch (error) {
        console.error("Error checking job PDF:", error);
        updatePdfStatus("Error checking PDF", "error");
    }
}

async function handleDownloadPdf(silent = false) {
    try {
        // Get current tab
        const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

        if (!tab.url.includes("current-rms.com") || !tab.url.includes("opportunities/")) {
            if (!silent) showStatus("Please navigate to a Current-RMS job page first", "error");
            return;
        }

        if (!silent) setLoading(downloadPdfBtn, true);
        setLoading(refreshPdfBtn, true);
        updatePdfStatus("Downloading PDF...", "loading");

        // Extract opportunity ID from URL
        const opportunityMatch = tab.url.match(/\/opportunities\/(\d+)/);
        if (!opportunityMatch) {
            throw new Error("Could not extract opportunity ID from URL");
        }

        const opportunityId = opportunityMatch[1];
        console.log("Extracted opportunity ID:", opportunityId);

        // Get stored configuration
        const config = await getStoredConfig(["subdomain", "apiKey", "documentId"]);
        if (!config.subdomain || !config.apiKey || !config.documentId) {
            throw new Error("Extension not configured. Please check settings.");
        }

        const documentId = String(config.documentId).trim();

        // Construct the PDF download URL
        const pdfUrl = `https://${config.subdomain}.current-rms.com/opportunities/${opportunityId}/print_document.pdf?document_id=${documentId}`;
        console.log("PDF URL:", pdfUrl);

        // Download the PDF directly with API key authentication methods
        let response;
        let lastError;

        // Method 1: Try with X-SUBDOMAIN and X-AUTH-TOKEN headers (Current-RMS standard)
        try {
            console.log("Attempting PDF download with X-SUBDOMAIN/X-AUTH-TOKEN headers...");
            response = await fetch(pdfUrl, {
                method: "GET",
                headers: {
                    "X-SUBDOMAIN": config.subdomain,
                    "X-AUTH-TOKEN": config.apiKey,
                    "Accept": "application/pdf"
                },
                credentials: "include"
            });

            if (response.ok) {
                console.log("PDF download successful with X-SUBDOMAIN/X-AUTH-TOKEN headers");
            } else {
                throw new Error(`Method 1 failed: ${response.status} ${response.statusText}`);
            }
        } catch (error) {
            console.log("Method 1 failed:", error.message);
            lastError = error;

            // Method 2: Try with Basic Auth using subdomain:apikey
            try {
                console.log("Attempting PDF download with Basic Auth (subdomain:apikey)...");
                const basicAuth = btoa(`${config.subdomain}:${config.apiKey}`);
                response = await fetch(pdfUrl, {
                    method: "GET",
                    headers: {
                        "Authorization": `Basic ${basicAuth}`,
                        "Accept": "application/pdf"
                    },
                    credentials: "include"
                });

                if (response.ok) {
                    console.log("PDF download successful with Basic Auth");
                } else {
                    throw new Error(`Method 2 failed: ${response.status} ${response.statusText}`);
                }
            } catch (error2) {
                console.log("Method 2 failed:", error2.message);
                lastError = error2;

                // Method 3: Try with API key as query parameter
                try {
                    console.log("Attempting PDF download with API key as query parameter...");
                    const urlWithApiKey = `${pdfUrl}&api_key=${encodeURIComponent(config.apiKey)}&subdomain=${encodeURIComponent(config.subdomain)}`;
                    response = await fetch(urlWithApiKey, {
                        method: "GET",
                        headers: {
                            "Accept": "application/pdf"
                        },
                        credentials: "include"
                    });

                    if (response.ok) {
                        console.log("PDF download successful with API key as query parameter");
                    } else {
                        throw new Error(`Method 3 failed: ${response.status} ${response.statusText}`);
                    }
                } catch (error3) {
                    console.log("Method 3 failed:", error3.message);
                    lastError = error3;

                    // Method 4: Try with session credentials only (if user is logged in)
                    try {
                        console.log("Attempting PDF download with session credentials only...");
                        response = await fetch(pdfUrl, {
                            method: "GET",
                            headers: {
                                "Accept": "application/pdf"
                            },
                            credentials: "include"
                        });

                        if (response.ok) {
                            console.log("PDF download successful with session credentials");
                        } else {
                            throw new Error(`Method 4 failed: ${response.status} ${response.statusText}`);
                        }
                    } catch (error4) {
                        console.log("All API key authentication methods failed");
                        throw lastError; // Throw the first error for debugging
                    }
                }
            }
        }

        console.log("PDF download response status:", response.status);
        console.log("PDF download response headers:", Object.fromEntries(response.headers.entries()));

        if (!response.ok) {
            const errorText = await response.text();
            console.log("PDF download error response:", errorText);
            throw new Error(`PDF download failed: ${response.status} ${response.statusText} - ${errorText.substring(0, 200)}`);
        }

        const contentType = response.headers.get("content-type");
        if (!contentType || !contentType.includes("application/pdf")) {
            const responseText = await response.text();
            throw new Error(`Invalid content type: ${contentType}. Expected PDF but got: ${responseText.substring(0, 100)}`);
        }

        const arrayBuffer = await response.arrayBuffer();
        console.log("PDF size:", arrayBuffer.byteLength, "bytes");

        if (arrayBuffer.byteLength === 0) {
            throw new Error("Downloaded PDF is empty");
        }

        // Convert to base64 for storage using a more reliable method
        const uint8Array = new Uint8Array(arrayBuffer);

        // Use the browser's built-in base64 encoding via Blob and FileReader
        const blob = new Blob([uint8Array], { type: "application/pdf" });
        const base64String = await new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                // Remove the data URL prefix to get just the base64 data
                const result = reader.result.split(",")[1];
                resolve(result);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });

        currentJobPdf = base64String; // Store the full PDF for multi-print
        multiPrintResults = [];
        scannedPageNumbers = [];
        pageNumbersOutput.value = "";
        hidePdfPreview();
        updatePdfStatus("PDF loaded successfully", "success");

        if (!silent) showStatus("PDF downloaded and loaded!", "success");

    } catch (error) {
        console.error("Error downloading PDF:", error);
        updatePdfStatus(`PDF download error: ${error.message}`, "error");
        if (!silent) showStatus(`PDF download error: ${error.message}`, "error");
    } finally {
        if (!silent) setLoading(downloadPdfBtn, false);
        setLoading(refreshPdfBtn, false);
    }
}

async function handleRefreshPdf() {
    await handleDownloadPdf();
}

function startAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
    }
    autoRefreshInterval = setInterval(async () => {
        console.log("Auto-refreshing PDF...");
        await handleDownloadPdf(true); // Silent download
    }, refreshIntervalSeconds * 1000);
    showStatus(`Auto-refresh enabled every ${refreshIntervalSeconds} seconds`, "info");
}

function stopAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
        autoRefreshInterval = null;
        showStatus("Auto-refresh disabled", "info");
    }
}

function handleAutoRefreshToggle() {
    if (autoRefreshCheckbox.checked) {
        startAutoRefresh();
    } else {
        stopAutoRefresh();
    }
}

async function handleAutoPrintToggle() {
    try {
        await setStoredConfig({ autoPrintAfterScan: autoPrintCheckbox.checked });
        showStatus(
            autoPrintCheckbox.checked ? "Auto-print after scan enabled" : "Auto-print after scan disabled",
            "info"
        );
    } catch (error) {
        console.error("Error saving auto-print preference:", error);
        showStatus("Could not save auto-print preference", "error");
    }
}

// PDF.js worker setup
// This is required for PDF.js to work in a Chrome Extension context
if (
    typeof pdfjsLib !== "undefined" &&
    typeof chrome !== "undefined" &&
    chrome.runtime &&
    typeof chrome.runtime.getURL === "function"
) {
    pdfjsLib.GlobalWorkerOptions.workerSrc = chrome.runtime.getURL("lib/pdf.worker.min.js");
}

function normalizeBarcodeText(value) {
    return String(value || "").replace(/[^a-zA-Z0-9]/g, "").toLowerCase();
}

function barcodeMatchesText(text, barcode) {
    const rawText = String(text || "");
    const target = String(barcode || "").trim();

    if (!target) {
        return false;
    }

    return rawText.includes(target) || normalizeBarcodeText(rawText).includes(normalizeBarcodeText(target));
}

function base64PdfToArrayBuffer(pdfData) {
    if (typeof pdfData !== "string") {
        throw new Error("Invalid PDF data format");
    }

    const base64Data = pdfData.startsWith("data:") ? pdfData.split(",")[1] : pdfData;
    if (!base64Data) {
        throw new Error("Invalid PDF data format");
    }

    const binaryString = atob(base64Data);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }

    return bytes.buffer;
}

async function loadPdfDocument(pdfData) {
    if (typeof pdfjsLib === "undefined") {
        throw new Error("PDF.js not loaded. Please reload the extension.");
    }

    const loadingTask = pdfjsLib.getDocument({ data: base64PdfToArrayBuffer(pdfData) });
    return loadingTask.promise;
}

async function findBarcodePage(pdfDoc, barcode) {
    for (let pageNumber = 1; pageNumber <= pdfDoc.numPages; pageNumber++) {
        const page = await pdfDoc.getPage(pageNumber);
        const textContent = await page.getTextContent();
        const text = textContent.items.map(item => item.str).join(" ");

        if (barcodeMatchesText(text, barcode)) {
            return {
                success: true,
                pageNumber,
                barcode
            };
        }
    }

    return {
        success: false,
        error: "Barcode not found in PDF"
    };
}

async function renderPdfPageToDataUrl(pdfDoc, pageNumber, scale = 1.5) {
    const page = await pdfDoc.getPage(pageNumber);
    const viewport = page.getViewport({ scale });
    const canvas = document.createElement("canvas");
    const context = canvas.getContext("2d");

    canvas.height = viewport.height;
    canvas.width = viewport.width;

    await page.render({ canvasContext: context, viewport }).promise;
    return canvas.toDataURL("image/png");
}

async function renderPdfPagesToImages(pdfData, pageNumbers, scale = 3) {
    let pdfDoc = null;

    try {
        pdfDoc = await loadPdfDocument(pdfData);
        const uniquePages = [...new Set(pageNumbers.map(Number))].filter(pageNumber => (
            Number.isInteger(pageNumber) && pageNumber >= 1 && pageNumber <= pdfDoc.numPages
        ));

        const images = [];
        for (const pageNumber of uniquePages) {
            const dataUrl = await renderPdfPageToDataUrl(pdfDoc, pageNumber, scale);
            images.push({ pageNumber, dataUrl });
        }

        return images;
    } finally {
        if (pdfDoc) {
            pdfDoc.destroy();
        }
    }
}

// Function to process PDF for barcode (now in popup.js)
async function processPdfForBarcode(pdfData, barcode) {
    let pdfDoc = null;

    try {
        console.log("Processing PDF for barcode:", barcode);

        pdfDoc = await loadPdfDocument(pdfData);
        const match = await findBarcodePage(pdfDoc, barcode);

        if (!match.success) {
            return match;
        }

        console.log(`Barcode ${barcode} found on page ${match.pageNumber}`);
        const pageData = await renderPdfPageToDataUrl(pdfDoc, match.pageNumber, 1.5);

        return {
            success: true,
            pageData,
            pageNumber: match.pageNumber,
            barcode,
            originalPdfData: pdfData
        };
    } catch (error) {
        console.error("Error processing PDF for barcode:", error);
        return { success: false, error: `PDF processing error: ${error.message}` };
    } finally {
        if (pdfDoc) {
            pdfDoc.destroy();
        }
    }
}

async function handleSearch(options = {}) {
    const barcode = barcodeInput.value.trim();
    const autoPrint = options.autoPrint !== undefined ? options.autoPrint : autoPrintCheckbox.checked;

    if (!barcode) {
        showStatus("Please enter a barcode", "error");
        return;
    }

    // If no PDF is loaded, try to download it first
    if (!currentJobPdf) {
        updatePdfStatus("No PDF loaded, downloading...", "loading");
        await handleDownloadPdf(true);

        if (!currentJobPdf) {
            showStatus("Could not load PDF. Please try the Download PDF button.", "error");
            return;
        }
    }

    setLoading(searchBtn, true);

    try {
        const response = await processPdfForBarcode(currentJobPdf, barcode);

        if (response.success) {
            await displayPdfPreview(response.pageData, response.pageNumber, barcode, response.originalPdfData, response.pdfDocument);
            barcodeInput.value = "";
            barcodeInput.focus();

            if (autoPrint) {
                try {
                    const pageImages = await renderPdfPagesToImages(response.originalPdfData || currentJobPdf, [response.pageNumber], 3);
                    openPrintWindow(pageImages, `Current-RMS label - barcode ${barcode} - page ${response.pageNumber}`);
                    showStatus(`Found barcode ${barcode} on page ${response.pageNumber}. Print dialog opened.`, "success");
                } catch (printError) {
                    console.error("Error opening print dialog:", printError);
                    showStatus(`Found barcode ${barcode} on page ${response.pageNumber}, but print dialog was blocked. Click Print Label.`, "error");
                }
            } else {
                showStatus(`Found label for barcode: ${barcode} on page ${response.pageNumber}`, "success");
            }
        } else {
            showStatus(response.error || "Barcode not found in PDF", "error");
            hidePdfPreview();
        }
    } catch (error) {
        console.error("Error searching PDF:", error);
        showStatus(`Search error: ${error.message}`, "error");
        hidePdfPreview();
    } finally {
        setLoading(searchBtn, false);
    }
}

async function handleProcessMultiBarcodes() {
    const barcodesText = multiBarcodeInput.value.trim();
    if (!barcodesText) {
        showStatus("Please enter barcodes in the multi-print input area", "error");
        return;
    }

    // Split by new line or space, then filter out empty strings
    const barcodes = barcodesText.split(/\s+|\n/).filter(b => b.length > 0);
    if (barcodes.length === 0) {
        showStatus("No valid barcodes found in the input", "error");
        return;
    }

    if (!currentJobPdf) {
        updatePdfStatus("No PDF loaded, downloading...", "loading");
        await handleDownloadPdf(true);
        if (!currentJobPdf) {
            showStatus("Could not load PDF. Please try the Download PDF button.", "error");
            return;
        }
    }

    setLoading(processMultiBarcodesBtn, true);
    scannedPageNumbers = []; // Clear previous multi-print results
    multiPrintResults = [];
    pageNumbersOutput.value = "";
    hidePdfPreview();

    try {
        const foundPages = new Set();

        for (const barcode of barcodes) {
            const response = await processPdfForBarcode(currentJobPdf, barcode);
            if (response.success) {
                foundPages.add(response.pageNumber);
                multiPrintResults.push(response);
            } else {
                console.warn(`Barcode ${barcode} not found.`);
            }
        }

        scannedPageNumbers = Array.from(foundPages).sort((a, b) => a - b);
        pageNumbersOutput.value = scannedPageNumbers.join(",");

        if (multiPrintResults.length > 0) {
            showStatus(`Processed ${barcodes.length} barcodes. Found ${scannedPageNumbers.length} unique labels.`, "success");

            const firstPage = scannedPageNumbers[0];
            const firstResult = multiPrintResults.find(result => result.pageNumber === firstPage) || multiPrintResults[0];
            if (firstResult) {
                await displayPdfPreview(
                    firstResult.pageData,
                    firstResult.pageNumber,
                    firstResult.barcode,
                    firstResult.originalPdfData,
                    firstResult.pdfDocument
                );
            }
        } else {
            showStatus("No labels found for the provided barcodes.", "error");
        }
    } catch (error) {
        console.error("Error processing multiple barcodes:", error);
        showStatus(`Multi-print error: ${error.message}`, "error");
    } finally {
        setLoading(processMultiBarcodesBtn, false);
    }
}

function handleClearMultiBarcodeInput() {
    multiBarcodeInput.value = "";
    pageNumbersOutput.value = "";
    scannedPageNumbers = [];
    multiPrintResults = [];
    hidePdfPreview();
    showStatus("Multi-print input cleared.", "info");
}

function handleCopyPageNumbers() {
    if (pageNumbersOutput.value) {
        navigator.clipboard.writeText(pageNumbersOutput.value).then(() => {
            showStatus("Page numbers copied to clipboard!", "success");
        }).catch(err => {
            console.error("Error copying to clipboard:", err);
            showStatus("Failed to copy page numbers.", "error");
        });
    } else {
        showStatus("No page numbers to copy.", "info");
    }
}

async function handlePrint() {
    if (multiPrintModeRadio.checked) {
        if (scannedPageNumbers.length === 0) {
            showStatus("No page numbers generated for multi-print. Please process barcodes first.", "error");
            return;
        }

        if (!currentJobPdf) {
            showStatus("No PDF loaded for multi-print.", "error");
            return;
        }

        setLoading(printBtn, true);
        try {
            const pageImages = await renderPdfPagesToImages(currentJobPdf, scannedPageNumbers, 3);
            openPrintWindow(pageImages, `Current-RMS labels - pages ${scannedPageNumbers.join(",")}`);
            showStatus(`Print dialog opened for pages: ${scannedPageNumbers.join(",")}`, "success");
        } catch (error) {
            console.error("Error printing PDF (multi-print):", error);
            showStatus(`Print error (multi-print): ${error.message}`, "error");
        } finally {
            setLoading(printBtn, false);
        }
    } else { // Single print mode
        if (!currentOriginalPdfData || !currentPageNumber) {
            showStatus("No label has been scanned and processed yet for single print.", "error");
            return;
        }

        setLoading(printBtn, true);
        try {
            const pageImages = await renderPdfPagesToImages(currentOriginalPdfData, [currentPageNumber], 3);
            openPrintWindow(pageImages, `Current-RMS label - page ${currentPageNumber}`);
            showStatus(`Print dialog opened for page ${currentPageNumber}`, "success");
        } catch (error) {
            console.error("Error printing PDF (single-print):", error);
            showStatus(`Print error (single-print): ${error.message}`, "error");
        } finally {
            setLoading(printBtn, false);
        }
    }
}

function openPrintWindow(pageImages, title) {
    if (!pageImages.length) {
        throw new Error("No PDF pages were rendered for printing.");
    }

    const printWindow = window.open("", "_blank", "width=900,height=1100,scrollbars=yes,resizable=yes");
    if (!printWindow) {
        throw new Error("Could not open print window. Please check popup blocker settings.");
    }

    const doc = printWindow.document;
    doc.open();
    doc.write("<!doctype html><html><head><title></title></head><body></body></html>");
    doc.close();
    doc.title = title;

    let printFinished = false;
    const finishPrint = () => {
        if (printFinished) {
            return;
        }

        printFinished = true;
        try {
            resetScanFormAfterPrint();
            showStatus("Printing complete - ready for next scan", "success");
        } catch (error) {
            console.warn("Could not reset popup after printing:", error);
        }

        setTimeout(() => {
            if (!printWindow.closed) {
                printWindow.close();
            }
        }, 250);
    };

    printWindow.addEventListener("afterprint", finishPrint);
    printWindow.onafterprint = finishPrint;

    const style = doc.createElement("style");
    style.textContent = `
        html,
        body {
            margin: 0;
            padding: 0;
            background: #fff;
        }

        .label-page {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            page-break-after: always;
            break-after: page;
        }

        .label-page:last-child {
            page-break-after: auto;
            break-after: auto;
        }

        img {
            display: block;
            max-width: 100%;
            max-height: 100vh;
            width: auto;
            height: auto;
        }

        @media print {
            @page {
                margin: 0;
            }

            .label-page {
                min-height: 100vh;
            }
        }
    `;
    doc.head.appendChild(style);

    const imageLoadPromises = pageImages.map(({ pageNumber, dataUrl }) => {
        const page = doc.createElement("section");
        page.className = "label-page";

        const img = doc.createElement("img");
        img.src = dataUrl;
        img.alt = `Current-RMS label page ${pageNumber}`;

        page.appendChild(img);
        doc.body.appendChild(page);

        if (typeof img.decode === "function") {
            return img.decode().catch(() => undefined);
        }

        return new Promise(resolve => {
            img.onload = resolve;
            img.onerror = resolve;
        });
    });

    Promise.all(imageLoadPromises).then(() => {
        if (printWindow.closed) {
            return;
        }

        printWindow.focus();
        printWindow.print();

        if (printWindow.matchMedia) {
            const mediaQuery = printWindow.matchMedia("print");
            mediaQuery.addEventListener("change", event => {
                if (!event.matches) {
                    finishPrint();
                }
            });
        }
    });
}

// Show print dialog for manual printing of specific page from original PDF
async function showPagePrintDialog(pageNumber, barcode) {
    try {
        console.log(`Showing print dialog for page ${pageNumber}...`);

        // Create a print-optimized HTML page that only shows the target page
        await createPageSpecificPrintWindow(pageNumber, barcode);

    } catch (error) {
        console.error("Error showing page print dialog:", error);
        throw error;
    }
}

// Create a print window that opens the original PDF at the specific page
async function createPageSpecificPrintWindow(pageNumber, barcode) {
    try {
        console.log(`Opening original PDF for page ${pageNumber} printing...`);

        // Convert base64 PDF data back to blob (original PDF)
        const binaryString = atob(currentOriginalPdfData);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        const pdfBlob = new Blob([bytes], { type: "application/pdf" });
        const pdfUrl = URL.createObjectURL(pdfBlob);

        // Open PDF in new window, navigated to the specific page
        const printWindow = window.open(pdfUrl + `#page=${pageNumber}`, "_blank", "width=1200,height=800,scrollbars=yes,resizable=yes");

        if (!printWindow) {
            throw new Error("Could not open print window. Please check popup blocker settings.");
        }

        // Set window title with prominent page number
        printWindow.document.title = `🖨️ PRINT PAGE ${pageNumber} - Barcode: ${barcode} - Current-RMS Labels`;

        // Wait for PDF to load, then handle printing (faster timing)
        setTimeout(() => {
            try {
                if (printWindow.closed) return;

                // Focus the window
                printWindow.focus();

                // Log page number info to console
                console.log(`📄 PRINT PAGE ${pageNumber} - Barcode: ${barcode}`);
                console.log(`🖨️ Set page range to: ${pageNumber}`);
                console.log(`Press Ctrl+P or use browser's print button`);

                // Show page number in window title (non-blocking)
                printWindow.document.title = `🖨️ PRINT PAGE ${pageNumber} - Barcode: ${barcode} - Current-RMS Labels`;

                // Show page number info in the extension popup (non-blocking)
                showStatus(`📄 Print window opened - Set page range to: ${pageNumber}`, "success");

                // Show non-blocking screen overlay with page number
                showPrintPageOverlay(pageNumber, barcode);

                // Monitor print window - close preview when print window closes
                const checkWindowClosed = setInterval(() => {
                    if (printWindow.closed) {
                        clearInterval(checkWindowClosed);
                        // Auto-close preview when print window is closed (indicating printing is done)
                        hidePdfPreview();
                        showStatus("Print window closed - ready for next scan", "info");
                    }
                }, 1000); // Check every second

                // Auto-close PDF window after print dialog interaction
                let printDialogOpened = false;
                const monitorPrintDialog = setInterval(() => {
                    try {
                        if (printWindow.closed) {
                            clearInterval(monitorPrintDialog);
                            return;
                        }

                        // Check if print dialog is open by trying to access window properties
                        // This is a rough detection method
                        const isDialogOpen = !printWindow.document.hasFocus();

                        if (isDialogOpen && !printDialogOpened) {
                            printDialogOpened = true;
                            console.log("Print dialog detected as open");
                        } else if (!isDialogOpen && printDialogOpened) {
                            // Print dialog was open but now closed - assume printing is done
                            console.log("Print dialog closed - auto-closing PDF window in 2 seconds");
                            setTimeout(() => {
                                if (!printWindow.closed) {
                                    printWindow.close();
                                    hidePdfPreview();
                                    showStatus("Printing complete - ready for next scan", "success");
                                }
                            }, 2000); // 2 second delay to ensure printing is complete
                            clearInterval(monitorPrintDialog);
                        }
                    } catch (error) {
                        // Ignore errors from cross-origin access attempts
                    }
                }, 500); // Check every 500ms for more responsive detection

                // Function to trigger print dialog
                const triggerPrint = () => {
                    try {
                        console.log(`Opening print dialog for page ${pageNumber}`);
                        printWindow.print();
                        console.log(`Print dialog opened - user needs to set page range to ${pageNumber}`);
                    } catch (printError) {
                        console.error("Error triggering print:", printError);
                        showStatus(`Print failed - please use Ctrl+P and set page range to ${pageNumber}`, "error");
                    }
                };

                // Trigger print dialog quickly
                setTimeout(() => {
                    console.log("Opening print dialog");
                    triggerPrint();
                }, 300);

                // Add keyboard shortcut for manual print trigger
                printWindow.document.addEventListener("keydown", function(e) {
                    if (e.ctrlKey && e.key === "p") {
                        e.preventDefault();
                        triggerPrint();
                    }
                });

            } catch (error) {
                console.error("Error setting up PDF print window:", error);
            }
        }, 1000); // Reduced from 3000ms to 1000ms for speed

        // Clean up when window is closed
        const checkClosed = setInterval(() => {
            if (printWindow.closed) {
                clearInterval(checkClosed);
                URL.revokeObjectURL(pdfUrl);
            }
        }, 1000);

        // Auto-cleanup after 15 minutes
        setTimeout(() => {
            if (!printWindow.closed) {
                printWindow.close();
            }
            clearInterval(checkClosed);
            URL.revokeObjectURL(pdfUrl);
        }, 900000);

        console.log(`Original PDF opened for page ${pageNumber} printing`);

    } catch (error) {
        console.error("Error creating PDF print window:", error);
        throw error;
    }
}

async function handleDownloadPage() {
    if (!currentPdfData || !currentPageNumber) {
        showStatus("No label to download", "error");
        return;
    }

    try {
        const barcode = currentBarcode || "unknown";
        if (!barcode) {
            showStatus("No barcode specified for download", "error");
            return;
        }

        // Create a temporary anchor element to trigger download
        const a = document.createElement("a");
        a.href = currentPdfData;
        a.download = `label_barcode_${barcode}_page_${currentPageNumber}.png`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        showStatus(`Label for barcode ${barcode} (page ${currentPageNumber}) downloaded.`, "success");
    } catch (error) {
        console.error("Error downloading page:", error);
        showStatus(`Download error: ${error.message}`, "error");
    }
}
