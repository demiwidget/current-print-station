# Current-RMS Print Server Extension

A Chrome extension that integrates with Current-RMS to provide selective printing of flightcase labels based on barcode scanning.

## Features

- **Secure Configuration**: Store your Current-RMS subdomain and API key securely using Chrome's storage API
- **PDF Processing**: Automatically download and process flightcase label PDFs from Current-RMS
- **Barcode Matching**: Scan barcodes to quickly find and display the corresponding label page
- **Print Preview**: Preview the selected label before printing
- **Selective Printing**: Print only the specific page you need, not the entire PDF
- **Download Option**: Download individual label pages as PNG images

## Installation

### Method 1: Developer Mode (Recommended for Testing)

1. **Download the Extension**
   - Download or clone this repository to your local machine
   - Extract the files if downloaded as a ZIP

2. **Open Chrome Extensions Page**
   - Open Google Chrome
   - Navigate to `chrome://extensions/`
   - Or go to Chrome menu → More tools → Extensions

3. **Enable Developer Mode**
   - Toggle the "Developer mode" switch in the top-right corner

4. **Load the Extension**
   - Click "Load unpacked"
   - Select the `current-rms-print-server-extension` folder
   - The extension should now appear in your extensions list

5. **Pin the Extension**
   - Click the puzzle piece icon in the Chrome toolbar
   - Find "Current-RMS Print Server" and click the pin icon

### Method 2: Chrome Web Store (Future)

This extension is currently in development and not yet available on the Chrome Web Store.

## Setup and Configuration

### 1. Configure Current-RMS Connection

1. **Click the Extension Icon**
   - Click the Current-RMS Print Server icon in your Chrome toolbar
   - The popup window will open

2. **Enter Your Credentials**
   - **Subdomain**: Enter your Current-RMS subdomain (without .current-rms.com)
     - Example: If your Current-RMS URL is `mycompany.current-rms.com`, enter `mycompany`
   - **API Key**: Enter your Current-RMS API key
     - You can find this in your Current-RMS account settings

3. **Save Configuration**
   - Click "Save Configuration"
   - Your credentials will be stored securely on your device

4. **Test Connection**
   - Click "Test Connection" to verify your credentials work
   - You should see a success message if everything is configured correctly

### 2. Get Your Current-RMS API Key

1. Log into your Current-RMS account
2. Go to Settings → API
3. Generate a new API key or copy your existing one
4. Make sure the API key has permissions to access job/opportunity data

## Usage

### Basic Workflow

1. **Open a Job in Current-RMS**
   - Navigate to any job/opportunity page in Current-RMS
   - The extension will detect when you're on a job page

2. **Scan a Barcode**
   - Click the extension icon to open the popup
   - Click in the "Scan barcode here..." field
   - Scan a barcode using your barcode scanner
   - Or manually type a barcode and press Enter

3. **View the Preview**
   - The extension will search the flightcase label PDF for your barcode
   - If found, a preview of the matching page will appear
   - You'll see the page number and barcode information

4. **Print or Download**
   - Click "Print Page" to open a print dialog for just that page
   - Click "Download Page" to save the page as a PNG image

### Test Barcodes

For testing purposes, the extension includes these sample barcodes:
- `123456789` - Flight Case FC001
- `987654321` - Flight Case FC002  
- `456789123` - Flight Case FC003

## How It Works

### Technical Overview

1. **Content Script**: Monitors Current-RMS pages and detects when you're viewing a job
2. **Background Script**: Handles PDF downloading, processing, and barcode matching
3. **Popup Interface**: Provides the user interface for configuration and barcode scanning
4. **Secure Storage**: Uses Chrome's storage API to securely store your credentials

### PDF Processing

1. When you scan a barcode, the extension:
   - Communicates with the Current-RMS page to trigger PDF download
   - Downloads the flightcase label PDF using your API credentials
   - Processes the PDF to extract text and images from each page
   - Searches for the scanned barcode across all pages
   - Displays the matching page in the preview window

### Security

- **Local Storage**: All credentials are stored locally on your device using Chrome's secure storage
- **No External Servers**: The extension communicates directly with Current-RMS
- **API Key Protection**: Your API key is never transmitted to third-party servers

## Troubleshooting

### Common Issues

**"Configuration not found" Error**
- Make sure you've entered and saved your subdomain and API key
- Check that your subdomain doesn't include ".current-rms.com"

**"Connection failed" Error**
- Verify your subdomain is correct
- Check that your API key is valid and has the necessary permissions
- Ensure you're connected to the internet

**"No Current-RMS tab found" Error**
- Make sure you have a Current-RMS page open in another tab
- Navigate to a job/opportunity page in Current-RMS
- Refresh the Current-RMS page if needed

**"Barcode not found" Error**
- Verify the barcode exists in the current job's flightcase labels
- Try scanning the barcode again
- Check that the PDF contains the barcode you're looking for

### Debug Mode

To enable debug mode:
1. Open Chrome Developer Tools (F12)
2. Go to the Console tab
3. Look for messages from "Current-RMS Print Server"

## Development

### Project Structure

```
current-rms-print-server-extension/
├── manifest.json              # Extension manifest
├── popup/
│   ├── popup.html            # Popup interface
│   ├── popup.css             # Popup styling
│   └── popup.js              # Popup functionality
├── background/
│   └── background.js         # Background script
├── content/
│   └── content.js            # Content script for Current-RMS pages
├── lib/
│   └── pdf-processor.js      # PDF processing utilities
├── assets/
│   ├── icon16.png           # Extension icons
│   ├── icon32.png
│   ├── icon48.png
│   └── icon128.png
└── README.md                # This file
```

### Building from Source

1. Clone the repository
2. No build process required - the extension uses vanilla JavaScript
3. Load the extension in Chrome developer mode

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Limitations

### Current Limitations

- **PDF.js Integration**: Currently uses simulated PDF processing. A full implementation would require integrating PDF.js library
- **Real-time Sync**: Requires manual refresh if job data changes in Current-RMS
- **Barcode Formats**: Currently supports basic numeric barcodes
- **Print Quality**: Print quality depends on the original PDF resolution

### Future Enhancements

- Integration with real PDF.js library for actual PDF processing
- Support for more barcode formats (QR codes, Code 128, etc.)
- Batch printing of multiple labels
- Integration with label printers
- Offline caching of frequently used labels

## Support

For issues, questions, or feature requests:

1. Check the troubleshooting section above
2. Review the browser console for error messages
3. Ensure you're using the latest version of Chrome
4. Verify your Current-RMS API permissions

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Changelog

### Version 1.0.0 (Current)
- Initial release
- Basic PDF processing and barcode matching
- Secure credential storage
- Print and download functionality
- Current-RMS integration

---

**Note**: This extension is designed specifically for Current-RMS and requires a valid Current-RMS account and API access.
