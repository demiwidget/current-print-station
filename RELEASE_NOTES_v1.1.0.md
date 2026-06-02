# Release Notes - Current-RMS Print Server Extension v1.1.0

## 🚀 What's New

This release brings significant improvements to the Current-RMS Print Server extension, focusing on enhanced PDF display, advanced print options, and better user experience.

## ✅ Issues Fixed

### Critical Fix: Manifest JSON Error
- **Fixed**: Corrected malformed JSON in manifest.json that was causing installation errors
- **Impact**: Extension now installs properly in Chrome and Edge browsers

### Document ID Correction
- **Fixed**: Extension now uses the correct document ID (1000167) for flightcase labels
- **Impact**: PDF downloads now work correctly with the Current-RMS system

## 🎉 Major Enhancements

### Enhanced PDF Display
- **High-Quality Preview**: 2x scale rendering for crisp, clear label previews
- **Zoom Controls**: Interactive zoom in/out and fit-to-window functionality
- **Visual Feedback**: Green border highlights when barcodes are found
- **Better Layout**: Improved aspect ratio handling and responsive design

### Advanced Print Dialog Integration
- **Print Options Modal**: Comprehensive dialog with multiple customization options
- **Paper Size Selection**: Support for A4, Letter, and auto-fit label sizes
- **Orientation Control**: Portrait, landscape, and automatic orientation
- **Quality Settings**: Draft, normal, and high-quality print modes
- **Header Toggle**: Option to include/exclude label information header

### Version Tracking System
- **Automated Packaging**: New packaging script with version tracking
- **Versioned Releases**: ZIP files now include version numbers for better organization
- **Consistent Versioning**: Synchronized version numbers across all extension files

## 🔧 Technical Improvements

### Code Quality
- **Enhanced Error Handling**: Better error messages and user feedback
- **Improved Performance**: Optimized PDF processing and rendering
- **Responsive Design**: Better mobile and small screen support
- **Clean Architecture**: Modular code structure for easier maintenance

### User Experience
- **Intuitive Interface**: Streamlined workflow from barcode scan to print
- **Visual Feedback**: Clear status indicators and loading states
- **Accessibility**: Better keyboard navigation and screen reader support

## 📦 Installation

1. Download the `current-rms-print-server-extension-v1.1.0.zip` file
2. Extract the contents to a folder
3. Open Chrome/Edge and navigate to `chrome://extensions/` or `edge://extensions/`
4. Enable "Developer mode"
5. Click "Load unpacked" and select the extracted folder
6. Configure your Current-RMS subdomain and API key in the extension settings

## 🔄 Upgrade Notes

If you're upgrading from a previous version:
1. Remove the old extension from Chrome/Edge
2. Install the new version following the installation steps above
3. Reconfigure your settings (subdomain and API key)

## 🐛 Known Issues

- None currently identified

## 📞 Support

For issues or questions, please refer to the included documentation:
- `README.md` - General usage instructions
- `INSTALLATION.md` - Detailed installation guide
- `LIMITATIONS.md` - Current limitations and workarounds
- `EDGE_COMPATIBILITY.md` - Edge browser specific notes

## 🙏 Acknowledgments

Thank you for using the Current-RMS Print Server extension. This release represents a significant step forward in functionality and user experience.

---

**Version**: 1.1.0  
**Release Date**: January 2025  
**Compatibility**: Chrome 88+, Edge 88+  
**File Size**: ~15MB

