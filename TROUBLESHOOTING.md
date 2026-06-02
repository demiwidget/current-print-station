# Troubleshooting Guide - Current-RMS Print Server Extension

## PDF Download Issues

### "Download failed" Error

If you're experiencing PDF download failures, follow these steps:

#### 1. Check Browser Console
1. Open the extension popup
2. Right-click and select "Inspect" or press F12
3. Go to the Console tab
4. Try downloading the PDF again
5. Look for detailed error messages and authentication attempts

#### 2. Verify Configuration
- **Subdomain**: Make sure it's just the subdomain part (e.g., "yourcompany" not "yourcompany.current-rms.com")
- **API Key**: Ensure it's the correct API key from your Current-RMS settings

#### 3. Test Connection First
Always use the "Test Connection" button in settings to verify your credentials work before trying to download PDFs.

#### 4. Check URL Format
The extension expects to be on a Current-RMS opportunity page with a URL like:
```
https://yourcompany.current-rms.com/opportunities/12345
```

#### 5. Authentication Methods Tried
The extension automatically tries multiple authentication methods:
1. **X-SUBDOMAIN/X-AUTH-TOKEN headers** (Current-RMS API style)
2. **Authorization Bearer** (Standard OAuth style)
3. **Basic Authentication** (Username:Password style)
4. **Session-based** (Browser cookies only)

#### 6. Common Error Codes
- **401 Unauthorized**: Check your API key
- **403 Forbidden**: Your account may not have permission to access this document
- **404 Not Found**: The configured document ID may not exist for this opportunity
- **CORS Error**: Browser security restriction - try refreshing the page

#### 7. Document ID Issues
The extension uses the Document ID saved in settings for flightcase labels. If that ID doesn't exist in your Current-RMS system:
1. Go to the opportunity page in Current-RMS
2. Look for the "Print" or "Documents" section
3. Find the flightcase label document
4. Note the document ID from the URL when you click it
5. Update the Document ID in the extension settings

## Browser Compatibility

### Chrome
- Minimum version: 88+
- Enable "Developer mode" in chrome://extensions/

### Edge
- Minimum version: 88+
- Enable "Developer mode" in edge://extensions/

## Network Issues

### Corporate Firewalls
If you're behind a corporate firewall:
1. Ensure *.current-rms.com is whitelisted
2. Check if CORS requests are blocked
3. Try using the extension from a different network

### VPN Issues
Some VPNs may interfere with the extension. Try disabling your VPN temporarily.

## Getting Help

### Debug Information
When reporting issues, please include:
1. Browser console output
2. Your Current-RMS subdomain (without sensitive info)
3. The exact error message
4. The opportunity URL you're trying to use

### Console Commands
You can run these in the browser console for additional debugging:

```javascript
// Check if extension is loaded
chrome.runtime.getManifest()

// Check stored configuration (API key will be hidden)
chrome.storage.local.get(['subdomain', 'apiKey']).then(console.log)

// Test URL pattern matching
window.location.href.match(/\/opportunities\/(\d+)/)
```

## Known Limitations

1. **Document ID**: Must be configured correctly for your Current-RMS system
2. **Single Document Type**: Only supports flightcase labels
3. **Authentication**: May not work with all Current-RMS authentication setups
4. **CORS**: Subject to browser security restrictions

## Contact Support

If none of these solutions work, please provide:
- Browser console output
- Extension version number
- Current-RMS subdomain
- Specific error messages
- Steps to reproduce the issue
