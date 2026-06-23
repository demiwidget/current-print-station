# Current-RMS Print Station Prototype

This is a Windows desktop prototype for the print-station flow:

1. Scan or type a case/asset number.
2. Look up the active Current-RMS opportunity containing that asset number.
3. Prepare and download the label PDF through the Current-RMS API.
4. Find the PDF page containing the scanned value.
5. Preview it.
6. Scan the same case again to print, or untick that option and use the Print button.

If a manually typed container label, for example `2 x stands`, appears on more than one current job, the app shows a job picker so the operator can choose the correct job before previewing/printing.

When the app has to check or refresh multiple job PDFs, the kiosk screen shows progress so the operator can see how many candidate PDFs have been checked.

If a label is not found in the cached/current PDFs, the app now asks before doing the slower full refresh and PDF re-download pass.

## Fastest Way To Run

From the repository folder, double-click:

```text
Run Print Station.cmd
```

That uses `dotnet run`, so the PC needs the .NET SDK/runtime installed.

## Make A Standalone Windows EXE

From the repository folder, double-click:

```text
Publish Print Station.cmd
```

The standalone build will be created in a timestamped folder here:

```text
windows-print-station\bin\Release\net9.0-windows\win-x64\publish-YYYYMMDD-HHMMSS\CurrentRmsPrintStation.exe
```

Run `CurrentRmsPrintStation.exe` from that folder on the print-station PC.

## App Updates

The app now uses Velopack for normal installed-app updates. Once releases are packaged with Velopack and uploaded to the GitHub Releases page for:

```text
https://github.com/demiwidget/current-print-station
```

the print station checks for updates on startup. If a release is available, the kiosk screen shows an update notice. The settings page also has a `Check Updates` button.

Velopack applies delta packages first where possible, so small changes patch into the installed app instead of replacing the whole folder. If no delta is available, Velopack can fall back to the full package. Running from `dotnet run` or a raw publish folder is still useful for testing, but the updater is only active after the app has been installed from a Velopack release.

To make an install/update release, double-click:

```text
Package Print Station Update.cmd
```

That creates release files in:

```text
Releases
```

Upload those files to a GitHub Release tagged with the same app version, for example `v2.7.0`. The first print-station PC install should use the generated setup file. After that, the app can find later GitHub Releases and patch itself from the update notice.

## First Setup

- Enter the Current-RMS subdomain, without `.current-rms.com`.
- Enter the API key.
- Enter the label document layout ID.
- Pick the Windows printer.
- If the API PDF is missing the embedded logo, tick `Add logo overlay to preview and print`, choose the logo image, then adjust `Logo X %`, `Logo Y %`, and `Logo width %` until the preview is right.
- `Logo overlay` can be set to `Numeric only`, `Always`, or `Off`. `Numeric only` is useful when manual container labels already include the Current-RMS logo.
- Select an `Inside label printer` for the Dymo/small label printer. After a case label is previewed, use `Print Inside Labels` to print the contents rows as bold individual labels.
- Inside labels print one label per contents row, keeping the quantity in the text, for example `3x Adapter 15amp -> 16amp`.
- Set `Inside width mm`, `Inside height mm`, and `Inside labels landscape` to match the Dymo stock. Each inside label is sent as its own print job to force one physical label per contents row.
- Select a `Production label printer` for production/client/job labels. After a case label is previewed, set `Prod qty` and use `Print Production Label` to print labels formatted as production, italic client, and bold job number.
- Set `Production width mm`, `Production height mm`, and `Production labels landscape` to match the production label stock. Use `Production left mm` and `Production top mm` to nudge the text on the physical label if the printer driver offsets it.
- On the kiosk screen, leave `Print when the same case is scanned again` ticked for two-scan printing. Untick it to preview first and print with the button.
- Set `Flightcase quantity` before using `Print Flightcase Label` if you need more than one main case label. Use `Stillage Print (2)` as a shortcut for printing exactly two main labels.
- To open settings from the kiosk screen, click the `Label Print Station` title five times.
- Leave `Find active opportunity from scanned case` ticked for live use.
- Set `Current view ID` to the prep/current jobs view, for example `1000067`.
- Keep `Days ahead` at `7` to only scan PDFs for jobs going out soon.
- Keep `Job cache mins` at `5` and `PDF cache mins` at `30` for faster repeated scans. Set either to `0` to disable that cache while testing.
- Set `Auto-download mins` above `0` to refresh the current job list and pre-download candidate PDFs in the background. Leave it at `0` to only download on scan.
- Keep `Lookup filters` as `needing_prep,prepared,orders+not_cancelled` to prefer jobs still being prepared.
- Set `Required tag` if your prep jobs have a specific Current-RMS tag.
- Leave `Preview before printing` unticked for silent printing.
- Leave `Local PDF` blank for live API lookup.

For testing, you can either enter a fallback Opportunity ID or untick `Find active opportunity from scanned case` and choose a saved PDF.

## Silent Printing Notes

The app does not use the browser print dialog. It renders the matching label page and sends it directly to the selected Windows printer through the Windows print spooler.

Windows or the printer driver can still show hardware/error prompts, for example if the printer is offline, out of labels, or the driver requires manual confirmation.

## Current Prototype Lookup

The live lookup uses Current-RMS's documented opportunity search predicate for an opportunity containing a specific asset number:

```text
q[opportunity_items_opportunity_item_assets_stock_level_asset_number_eq]
```

If your barcode is stored somewhere else in Current-RMS, the lookup method in `CurrentRmsClient.cs` is the place to adjust the query.

The primary lookup now uses `Current view ID` as the candidate set and searches each candidate label PDF for the scanned barcode. This is useful when Current-RMS asset searches return historical jobs or do not expose allocation status in the API response.

After one label has been found, the app checks that same cached job PDF first on the next scan. This is the fastest path when one prep station is working through cases from a single job.

If the barcode is not found in any PDF from that view, the app automatically refreshes the current view PDFs once and checks again before it falls back to the older asset-number lookup.
