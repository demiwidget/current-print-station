# Current-RMS Print Server Extension - Changelog

## Version 1.2.0 - Major UI/UX Improvements (Latest)

### 🎉 Major UI/UX Improvements
- **Larger Popup Window**: Increased popup size from 400×600 to 600×800 pixels for better visibility
- **Enhanced Label Preview**: Increased preview canvas from 350×250 to 500×350 pixels for much better label visibility
- **Separate Label Information**: Moved label details (barcode, page number, status) to a dedicated information section outside the preview
- **Auto-Clear Barcode Input**: Barcode input field automatically clears after successful scan and refocuses for next scan
- **Improved Layout**: Better organized sections with cleaner visual hierarchy

### 🔧 Technical Fixes
- **Fixed Printing CSP Issues**: Resolved Content Security Policy violations in print function by removing inline scripts
- **PDF Processing in Popup**: Moved PDF processing from service worker to popup context to avoid PDF.js compatibility issues
- **Better Error Handling**: Enhanced error messages and user feedback throughout the application
- **Cleaner Code**: Removed duplicate and leftover code for better maintainability

### 🖨️ Print Improvements
- **CSP-Compliant Printing**: Print function now works without Content Security Policy violations
- **Better Print Window**: Improved print window creation using DOM manipulation instead of document.write()
- **Enhanced Print Quality**: Better image handling and print layout

### 📱 User Experience
- **Seamless Workflow**: Scan barcode → View large preview → See label info → Print → Auto-clear for next scan
- **Better Visual Feedback**: Improved status messages and loading indicators
- **Responsive Design**: Better layout adaptation for the larger popup size

## Version 1.1.1 - Bug Fix

### 🐛 Bug Fixes

#### Content Script Communication Error
- **Fixed**: "Could not establish connection. Receiving end does not exist" error
- **Solution**: Added automatic content script injection with fallback handling
- **Impact**: PDF download now works reliably even if content script wasn't initially loaded
- **Improvement**: Better error messages to guide users when issues occur

#### Enhanced Error Handling
- **Added**: Specific error messages for different failure scenarios
- **Added**: Automatic retry mechanism for content script injection
- **Added**: User-friendly guidance when content script fails to load

### 🔧 Technical Improvements
- **Improved**: Content script injection reliability
- **Enhanced**: Error message specificity and user guidance
- **Added**: Fallback mechanisms for script loading issues

---

## Version 1.1.0 - Enhanced Features

### 🎉 New Features

#### Enhanced PDF Display
- **High-Quality Preview**: Improved canvas rendering with 2x scale for better quality
- **Zoom Controls**: Added zoom in, zoom out, and fit-to-window controls
- **Better Layout**: Enhanced preview layout with proper aspect ratio handling
- **Visual Feedback**: Added green border highlight for found barcodes
- **Detailed Information**: Enhanced info section showing barcode, page, and status

#### Advanced Print Dialog Integration
- **Print Options Modal**: Added comprehensive print options dialog
- **Paper Size Selection**: Support for A4, Letter, and auto-fit label sizes
- **Orientation Control**: Portrait, landscape, and auto orientation options
- **Quality Settings**: Draft, normal, and high-quality print modes
- **Header Options**: Toggle for including label information header
- **Windows Print Dialog**: Proper integration with system print dialog

#### Version Tracking System
- **Automated Packaging**: Created packaging script with version tracking
- **Versioned ZIP Files**: ZIP files now include version number in filename
- **Consistent Versioning**: Version numbers synchronized across all files

#### Settings Section
- **Collapsible Settings**: Added a gear icon in the header to toggle settings visibility
- **Clean Interface**: Settings are now hidden by default, focusing on the main barcode scanning functionality
- **Auto-hide**: Settings automatically hide after successful configuration save

#### Silent PDF Download
- **Correct Document ID**: Fixed to use document ID 1000167 for flightcase labels
- **Proper URL Format**: Uses the correct Current-RMS URL format: `opportunities/{id}/print_document.pdf?document_id={doc_id}`
- **Automatic Document ID Detection**: Intelligently extracts document IDs from the current page or via API calls
- **Silent Processing**: PDF downloads happen in the background without opening new tabs

#### Real PDF Processing
- **PDF.js Integration**: Added full PDF.js library for proper PDF parsing and rendering
- **Text Extraction**: Real barcode searching within PDF text content
- **Page Rendering**: Actual PDF pages are rendered as images for preview

#### Enhanced Preview
- **In-Popup Display**: PDF pages are displayed directly within the extension popup
- **Canvas Rendering**: High-quality rendering of PDF pages using HTML5 Canvas
- **Responsive Sizing**: Preview automatically scales to fit the popup window

#### Improved Printing
- **Direct Print Dialog**: Print button opens the browser's native print dialog
- **Page-Specific Printing**: Only the selected label page is printed
- **High-Quality Output**: Maintains original PDF quality for printing

### 🔧 Technical Improvements

#### Edge Browser Compatibility
- **CSS Fixes**: Added Edge-specific CSS rules for proper popup display
- **Viewport Meta Tags**: Enhanced HTML meta tags for better Edge rendering
- **Cross-Browser Testing**: Improved compatibility across Chrome and Edge

#### Error Handling
- **Better Error Messages**: More descriptive error messages for troubleshooting
- **Fallback Mechanisms**: Multiple methods for extracting job and document IDs
- **Graceful Degradation**: Extension continues to work even if some features fail

#### Performance Optimizations
- **Efficient PDF Processing**: Optimized PDF loading and rendering
- **Memory Management**: Proper cleanup of PDF resources after processing
- **Async Operations**: Non-blocking operations for better user experience

### 🐛 Bug Fixes

#### Display Issues
- **Fixed Popup Dimensions**: Resolved "thin and long" display issue in Edge
- **Consistent Sizing**: Popup now maintains 400px width across all browsers
- **Responsive Layout**: Better handling of different screen sizes

#### API Integration
- **Correct Authentication**: Fixed API key handling for Current-RMS requests
- **Proper Headers**: Added correct headers for PDF download requests
- **Error Recovery**: Better handling of API failures and network issues

### 📚 Documentation Updates

#### New Documentation
- **Edge Compatibility Guide**: Detailed explanation of Edge-specific fixes
- **Changelog**: This comprehensive changelog documenting all changes
- **Enhanced README**: Updated with new features and usage instructions

#### Existing Documentation
- **Updated Installation Guide**: Reflects new features and requirements
- **Improved Limitations**: More accurate description of current limitations
- **Enhanced Demo Guide**: Updated demonstration guide with new interface

---

## Version 1.0.0 - Initial Release

### 🎉 Initial Features

#### Core Functionality
- **Basic Settings**: Subdomain and API key configuration
- **Barcode Scanning**: Input field for barcode entry
- **PDF Preview**: Basic PDF preview functionality
- **Print Support**: Basic printing capabilities

#### Browser Extension
- **Chrome Extension**: Manifest V3 compatible extension
- **Content Script**: Integration with Current-RMS pages
- **Background Script**: PDF processing in background
- **Popup Interface**: User interface for configuration and scanning

#### Documentation
- **README**: Basic installation and usage instructions
- **Installation Guide**: Step-by-step installation process
- **Limitations**: Known limitations and future improvements

---

## Technical Details

### Dependencies Added in v1.1.0
- **PDF.js v3.11.174**: For PDF parsing and rendering
- **PDF.js Worker**: For background PDF processing
- **Custom PDF Renderer**: Wrapper for PDF.js functionality

### File Structure Changes
```
current-rms-print-server-extension/
├── lib/
│   ├── pdf.min.js (NEW)
│   ├── pdf.worker.min.js (NEW)
│   ├── pdf-renderer.js (NEW)
│   └── pdf-processor.js (EXISTING)
├── popup/
│   ├── popup.html (UPDATED)
│   ├── popup.css (UPDATED)
│   └── popup.js (UPDATED)
├── content/
│   └── content.js (UPDATED)
├── background/
│   └── background.js (UPDATED)
├── manifest.json (UPDATED)
├── CHANGELOG.md (NEW)
└── EDGE_COMPATIBILITY.md (NEW)
```

### Manifest Changes
- Added `lib/*` to web accessible resources
- Added content security policy for PDF.js WASM support
- Updated permissions for better PDF handling

---

## Migration Guide

### From v1.0.0 to v1.1.0

1. **Uninstall Previous Version**: Remove the old extension from Chrome/Edge
2. **Install New Version**: Load the updated extension package
3. **Reconfigure Settings**: Re-enter subdomain and API key (settings are now hidden by default)
4. **Test Functionality**: Verify barcode scanning and PDF preview work correctly

### Breaking Changes
- Settings are now hidden by default (click gear icon to access)
- PDF processing now requires actual PDF files (simulation removed)
- Some internal API changes for better error handling

---

## Future Roadmap

### Planned Features
- **Multiple Document Types**: Support for different label formats
- **Batch Processing**: Process multiple barcodes at once
- **Print Queue**: Queue multiple labels for printing
- **Custom Templates**: Support for custom label templates
- **Offline Mode**: Cache PDFs for offline use

### Performance Improvements
- **Faster PDF Processing**: Optimize PDF.js usage
- **Better Caching**: Cache processed pages for faster access
- **Reduced Memory Usage**: More efficient memory management

---

*For technical support or feature requests, please refer to the README.md file for contact information.*

