using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CurrentRmsPrintStation;

public sealed class CurrentRmsClient
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> TestConnectionAsync(string subdomain, string apiKey, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.current-rms.com/api/v1/members/1");
        AddAuthHeaders(request, subdomain, apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Current-RMS API test failed: {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForDisplay(body)}");
        }

        return "Current-RMS API connection OK.";
    }

    public async Task<string> DownloadOpportunityPdfAsync(
        string subdomain,
        string apiKey,
        string opportunityId,
        string documentId,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opportunityId))
        {
            throw new InvalidOperationException("Enter an Opportunity ID or choose a local PDF for this prototype.");
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new InvalidOperationException("Enter the Current-RMS document ID for your label layout.");
        }

        var bytes = await TryDownloadOpportunityPdfViaApiAsync(subdomain, apiKey, opportunityId, documentId, log, cancellationToken);

        var cachePath = Path.Combine(
            SettingsStore.CacheDirectory,
            $"opportunity-{CleanFilePart(opportunityId)}-document-{CleanFilePart(documentId)}.pdf");

        await File.WriteAllBytesAsync(cachePath, bytes, cancellationToken);
        return cachePath;
    }

    public async Task<IReadOnlyList<OpportunityLookupResult>> ListLookupViewOpportunitiesAsync(
        string subdomain,
        string apiKey,
        string viewId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(viewId))
        {
            return [];
        }

        var results = new List<OpportunityLookupResult>();
        const int perPage = 25;
        const int maxPagesToScan = 4;

        for (var page = 1; page <= maxPagesToScan; page++)
        {
            var query = new List<KeyValuePair<string, string>>
            {
                new("page", page.ToString()),
                new("per_page", perPage.ToString()),
                new("view_id", viewId.Trim())
            };

            var pageResults = await SearchOpportunitiesAsync(
                subdomain,
                apiKey,
                $"view {viewId}",
                query,
                "",
                cancellationToken);

            results.AddRange(pageResults);
            if (pageResults.Count < perPage)
            {
                break;
            }
        }

        return results
            .GroupBy(opportunity => opportunity.Id)
            .Select(group => group.First())
            .ToList();
    }

    public async Task<ProductionLabelContent> GetProductionLabelContentAsync(
        string subdomain,
        string apiKey,
        string opportunityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opportunityId))
        {
            throw new InvalidOperationException("No Current-RMS opportunity is selected.");
        }

        var url = BuildApiUrl($"/opportunities/{Uri.EscapeDataString(opportunityId.Trim())}", []);
        using var request = BuildJsonAuthRequest(url, subdomain, apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Opportunity detail lookup failed for {opportunityId}: {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForDisplay(json)}");
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("opportunity", out var opportunity) ||
            opportunity.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Current-RMS did not return opportunity details for {opportunityId}.");
        }

        var jobNumber = ReadOptionalString(opportunity, "number");
        var production = ReadOptionalString(opportunity, "subject");
        var client = ReadClientName(opportunity);
        var memberId = ReadOptionalString(opportunity, "member_id");

        if (string.IsNullOrWhiteSpace(client) && !string.IsNullOrWhiteSpace(memberId))
        {
            client = await GetMemberNameAsync(subdomain, apiKey, memberId, cancellationToken);
        }

        return new ProductionLabelContent(production, client, jobNumber);
    }

    public async Task<OpportunityLookupResult?> FindOpportunityForScanAsync(
        string subdomain,
        string apiKey,
        string scanValue,
        OpportunityLookupOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scanValue))
        {
            throw new InvalidOperationException("Scan or type a case number first.");
        }

        var ambiguousMatches = new List<OpportunityLookupResult>();
        foreach (var filterModes in ParseFilterModeGroups(options.LookupFilterModes))
        {
            var assetMatches = await SearchOpportunitiesAsync(
                subdomain,
                apiKey,
                $"asset number ({string.Join("+", filterModes)})",
                BuildOpportunityQuery(filterModes, options.RequiredOpportunityTag, "q[opportunity_items_opportunity_item_assets_stock_level_asset_number_eq]", scanValue),
                scanValue,
                cancellationToken);

            var hydratedAssetMatches = await HydrateOpportunityMatchesAsync(subdomain, apiKey, assetMatches, scanValue, cancellationToken);
            var selected = SelectPreparedOpportunity(hydratedAssetMatches);
            if (selected is not null)
            {
                return selected;
            }

            ambiguousMatches.AddRange(hydratedAssetMatches);
        }

        foreach (var filterModes in ParseFilterModeGroups(options.LookupFilterModes))
        {
            var numberMatches = await SearchOpportunitiesAsync(
                subdomain,
                apiKey,
                $"opportunity number ({string.Join("+", filterModes)})",
                BuildOpportunityQuery(filterModes, options.RequiredOpportunityTag, "q[number_eq]", scanValue),
                scanValue,
                cancellationToken);

            var hydratedNumberMatches = await HydrateOpportunityMatchesAsync(subdomain, apiKey, numberMatches, scanValue, cancellationToken);
            var selected = SelectPreparedOpportunity(hydratedNumberMatches);
            if (selected is not null)
            {
                return selected;
            }

            ambiguousMatches.AddRange(hydratedNumberMatches);
        }

        var uniqueAmbiguousMatches = ambiguousMatches
            .GroupBy(match => match.Id)
            .Select(group => group.First())
            .ToList();
        var uniquePreparedMatches = uniqueAmbiguousMatches
            .Where(HasPreparedMatchingAsset)
            .ToList();

        if (uniquePreparedMatches.Count == 1)
        {
            return uniquePreparedMatches[0];
        }

        if (uniquePreparedMatches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Scan '{scanValue}' is marked Prepared on more than one opportunity. Current-RMS should only have one prepared allocation for this case. Matches: {DescribeMatches(uniquePreparedMatches)}");
        }

        if (uniqueAmbiguousMatches.Count > 0)
        {
            throw new InvalidOperationException(
                $"Scan '{scanValue}' matched opportunities, but none had the scanned case marked Prepared. Matches: {DescribeMatches(uniqueAmbiguousMatches)}");
        }

        return null;
    }

    private async Task<byte[]> TryDownloadOpportunityPdfViaApiAsync(
        string subdomain,
        string apiKey,
        string opportunityId,
        string documentId,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        Exception? apiError = null;

        try
        {
            var prepareUrl = BuildApiUrl(
                $"/opportunities/{Uri.EscapeDataString(opportunityId)}/prepare_document",
                new[] { new KeyValuePair<string, string>("document_id", documentId) });
            using var prepareRequest = BuildJsonAuthRequest(prepareUrl, subdomain, apiKey);
            using var prepareResponse = await _httpClient.SendAsync(prepareRequest, cancellationToken);
            var json = await prepareResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!prepareResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Document prepare failed: {(int)prepareResponse.StatusCode} {prepareResponse.ReasonPhrase}. {TrimForDisplay(json)}");
            }

            var preparedDocumentId = ExtractNestedId(json, "opportunity_document");
            var pdfUrl = $"https://api.current-rms.com/api/v1/opportunity_documents/{Uri.EscapeDataString(preparedDocumentId)}.pdf";
            return await DownloadPdfFromApiAsync(pdfUrl, subdomain, apiKey, cancellationToken);
        }
        catch (Exception ex)
        {
            apiError = ex;
        }

        var browserPdfUrl = $"https://{subdomain}.current-rms.com/opportunities/{opportunityId}/print_document.pdf?document_id={documentId}";
        try
        {
            return await TryDownloadPdfAsync(browserPdfUrl, subdomain, apiKey, cancellationToken);
        }
        catch (Exception webError)
        {
            throw new InvalidOperationException($"{apiError?.Message ?? "API document download failed."} Fallback browser PDF failed: {webError.Message}");
        }
    }

    private async Task<byte[]> DownloadPdfFromApiAsync(string pdfUrl, string subdomain, string apiKey, CancellationToken cancellationToken)
    {
        using var request = BuildHeaderAuthRequest(pdfUrl, subdomain, apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var text = bytes.Length > 0 ? TrimForDisplay(Encoding.UTF8.GetString(bytes)) : "";
            throw new InvalidOperationException($"PDF download failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
        }

        if (!LooksLikePdf(bytes))
        {
            var text = TrimForDisplay(Encoding.UTF8.GetString(bytes));
            throw new InvalidOperationException($"Current-RMS did not return a PDF. Response started with: {text}");
        }

        return bytes;
    }

    private async Task<List<OpportunityLookupResult>> SearchOpportunitiesAsync(
        string subdomain,
        string apiKey,
        string matchSource,
        IEnumerable<KeyValuePair<string, string>> query,
        string scanValue,
        CancellationToken cancellationToken)
    {
        var url = BuildApiUrl("/opportunities", query);
        using var request = BuildJsonAuthRequest(url, subdomain, apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Opportunity lookup failed: {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForDisplay(json)}");
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("opportunities", out var opportunities) ||
            opportunities.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<OpportunityLookupResult>();
        foreach (var opportunity in opportunities.EnumerateArray())
        {
            results.Add(ReadOpportunity(opportunity, matchSource, scanValue));
        }

        return results;
    }

    private async Task<List<OpportunityLookupResult>> HydrateOpportunityMatchesAsync(
        string subdomain,
        string apiKey,
        IEnumerable<OpportunityLookupResult> matches,
        string scanValue,
        CancellationToken cancellationToken)
    {
        var results = new List<OpportunityLookupResult>();
        foreach (var match in matches)
        {
            results.Add(await HydrateOpportunityMatchAsync(subdomain, apiKey, match, scanValue, cancellationToken));
        }

        return results;
    }

    private async Task<OpportunityLookupResult> HydrateOpportunityMatchAsync(
        string subdomain,
        string apiKey,
        OpportunityLookupResult match,
        string scanValue,
        CancellationToken cancellationToken)
    {
        var url = BuildApiUrl(
            $"/opportunities/{Uri.EscapeDataString(match.Id)}",
            new[]
            {
                new KeyValuePair<string, string>("include[]", "item_assets"),
                new KeyValuePair<string, string>("include[]", "opportunity_items")
            });
        using var request = BuildJsonAuthRequest(url, subdomain, apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Opportunity detail lookup failed for {match.Id}: {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForDisplay(json)}");
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("opportunity", out var opportunity) ||
            opportunity.ValueKind != JsonValueKind.Object)
        {
            return match;
        }

        var hydrated = ReadOpportunity(opportunity, match.MatchSource, scanValue);
        var matchingAssets = hydrated.MatchingAssets.Count > 0
            ? hydrated.MatchingAssets
            : match.MatchingAssets;

        return hydrated with
        {
            MatchSource = match.MatchSource,
            MatchingAssets = matchingAssets
        };
    }

    private async Task<string> GetMemberNameAsync(
        string subdomain,
        string apiKey,
        string memberId,
        CancellationToken cancellationToken)
    {
        var url = BuildApiUrl($"/members/{Uri.EscapeDataString(memberId.Trim())}", []);
        using var request = BuildJsonAuthRequest(url, subdomain, apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Member lookup failed for {memberId}: {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForDisplay(json)}");
        }

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("member", out var member) &&
            member.ValueKind == JsonValueKind.Object)
        {
            return ReadClientName(member);
        }

        return ReadClientName(document.RootElement);
    }

    private async Task<byte[]> TryDownloadPdfAsync(string pdfUrl, string subdomain, string apiKey, CancellationToken cancellationToken)
    {
        var attempts = new[]
        {
            BuildHeaderAuthRequest(pdfUrl, subdomain, apiKey),
            BuildQueryAuthRequest($"{pdfUrl}&apikey={Uri.EscapeDataString(apiKey)}&subdomain={Uri.EscapeDataString(subdomain)}")
        };

        Exception? lastError = null;
        foreach (var request in attempts)
        {
            try
            {
                using (request)
                using (var response = await _httpClient.SendAsync(request, cancellationToken))
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        var text = bytes.Length > 0 ? TrimForDisplay(System.Text.Encoding.UTF8.GetString(bytes)) : "";
                        throw new InvalidOperationException($"PDF download failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
                    }

                    if (!LooksLikePdf(bytes))
                    {
                        var text = TrimForDisplay(System.Text.Encoding.UTF8.GetString(bytes));
                        throw new InvalidOperationException($"Current-RMS did not return a PDF. Response started with: {text}");
                    }

                    return bytes;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(lastError?.Message ?? "PDF download failed.");
    }

    private static HttpRequestMessage BuildHeaderAuthRequest(string url, string subdomain, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(request, subdomain, apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        return request;
    }

    private static HttpRequestMessage BuildQueryAuthRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        return request;
    }

    private static HttpRequestMessage BuildJsonAuthRequest(string url, string subdomain, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(request, subdomain, apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static void AddAuthHeaders(HttpRequestMessage request, string subdomain, string apiKey)
    {
        request.Headers.TryAddWithoutValidation("X-SUBDOMAIN", subdomain);
        request.Headers.TryAddWithoutValidation("X-AUTH-TOKEN", apiKey);
    }

    private static bool LooksLikePdf(byte[] bytes)
    {
        return bytes.Length >= 4 &&
               bytes[0] == (byte)'%' &&
               bytes[1] == (byte)'P' &&
               bytes[2] == (byte)'D' &&
               bytes[3] == (byte)'F';
    }

    private static string TrimForDisplay(string value)
    {
        value = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return value.Length <= 220 ? value : value[..220] + "...";
    }

    private static string BuildApiUrl(string path, IEnumerable<KeyValuePair<string, string>> query)
    {
        var queryString = string.Join(
            "&",
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var baseUrl = $"https://api.current-rms.com/api/v1{path}";
        return string.IsNullOrWhiteSpace(queryString) ? baseUrl : $"{baseUrl}?{queryString}";
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildOpportunityQuery(
        IReadOnlyList<string> filterModes,
        string requiredTag,
        string predicate,
        string value)
    {
        foreach (var mode in filterModes)
        {
            yield return new KeyValuePair<string, string>("filtermode[]", mode);
        }

        yield return new KeyValuePair<string, string>("per_page", "25");
        yield return new KeyValuePair<string, string>("include[]", "item_assets");
        yield return new KeyValuePair<string, string>(predicate, value);

        if (!string.IsNullOrWhiteSpace(requiredTag))
        {
            yield return new KeyValuePair<string, string>("q[tags_name_cont]", requiredTag);
        }
    }

    private static IReadOnlyList<IReadOnlyList<string>> ParseFilterModeGroups(string value)
    {
        var groups = (value ?? "")
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(group => group
                .Split(['+', '|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray())
            .Where(group => group.Length > 0)
            .Cast<IReadOnlyList<string>>()
            .ToList();

        return groups.Count > 0
            ? groups
            : [["needing_prep"], ["prepared"], ["orders", "not_cancelled"]];
    }

    private static OpportunityLookupResult ReadOpportunity(JsonElement opportunity, string matchSource, string scanValue)
    {
        return new OpportunityLookupResult(
            ReadRequiredString(opportunity, "id"),
            ReadOptionalString(opportunity, "number"),
            ReadOptionalString(opportunity, "subject"),
            ReadOptionalString(opportunity, "member_id"),
            ReadClientName(opportunity),
            matchSource,
            ReadOptionalString(opportunity, "state_name"),
            ReadOptionalString(opportunity, "status_name"),
            ReadOptionalString(opportunity, "starts_at"),
            ReadOptionalString(opportunity, "ends_at"),
            ReadOptionalString(opportunity, "prep_starts_at"),
            ReadOptionalString(opportunity, "prep_ends_at"),
            FindMatchingAssetStatuses(opportunity, scanValue));
    }

    private static string ReadClientName(JsonElement element)
    {
        var direct = ReadFirstPresent(
            element,
            "member_name",
            "customer_name",
            "client_name",
            "organisation_name",
            "organization_name",
            "company_name",
            "account_name",
            "display_name",
            "full_name",
            "name");

        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        foreach (var nestedName in new[] { "member", "customer", "client", "organisation", "organization", "company", "identity" })
        {
            if (!element.TryGetProperty(nestedName, out var nested) ||
                nested.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var nestedDirect = ReadFirstPresent(
                nested,
                "name",
                "display_name",
                "company_name",
                "organisation_name",
                "organization_name",
                "full_name");
            if (!string.IsNullOrWhiteSpace(nestedDirect))
            {
                return nestedDirect;
            }

            var nestedPerson = JoinName(ReadOptionalString(nested, "first_name"), ReadOptionalString(nested, "last_name"));
            if (!string.IsNullOrWhiteSpace(nestedPerson))
            {
                return nestedPerson;
            }
        }

        return JoinName(ReadOptionalString(element, "first_name"), ReadOptionalString(element, "last_name"));
    }

    private static string JoinName(string firstName, string lastName)
    {
        return $"{firstName} {lastName}".Trim();
    }

    private static OpportunityLookupResult? SelectPreparedOpportunity(IReadOnlyList<OpportunityLookupResult> matches)
    {
        var preparedMatches = matches.Where(HasPreparedMatchingAsset).ToList();
        return preparedMatches.Count == 1 ? preparedMatches[0] : null;
    }

    private static bool HasPreparedMatchingAsset(OpportunityLookupResult match)
    {
        return match.MatchingAssets.Any(IsPreparedAsset);
    }

    private static bool IsPreparedAsset(AssetMatchStatus asset)
    {
        return asset.Status == 15 ||
               asset.StatusName.Contains("prepared", StringComparison.OrdinalIgnoreCase) ||
               asset.StatusName.Contains("prepped", StringComparison.OrdinalIgnoreCase);
    }

    private static List<AssetMatchStatus> FindMatchingAssetStatuses(JsonElement root, string scanValue)
    {
        var results = new List<AssetMatchStatus>();
        Visit(root, scanValue, results);

        return results
            .GroupBy(result => $"{result.AssetNumber}|{result.StatusName}|{result.Container}")
            .Select(group => group.First())
            .ToList();
    }

    private static void Visit(JsonElement element, string scanValue, List<AssetMatchStatus> results)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var directAssetNumber = ReadDirectAssetNumber(element);
            var isMatchingDirectAsset = AssetNumbersEqual(directAssetNumber, scanValue);
            var isMatchingAllocation = string.IsNullOrWhiteSpace(directAssetNumber) &&
                                       LooksLikeOpportunityItemAsset(element) &&
                                       ObjectContainsAssetNumber(element, scanValue);

            if (isMatchingDirectAsset || isMatchingAllocation)
            {
                var assetNumber = string.IsNullOrWhiteSpace(directAssetNumber)
                    ? FindFirstNestedAssetNumber(element, scanValue)
                    : directAssetNumber;
                var statusName = ReadOptionalString(element, "status_name");
                var status = ReadOptionalInt(element, "status");
                var container = ReadOptionalString(element, "container");
                if (!string.IsNullOrWhiteSpace(statusName) || status is not null)
                {
                    results.Add(new AssetMatchStatus(assetNumber, statusName, status, container));
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                Visit(property.Value, scanValue, results);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                Visit(item, scanValue, results);
            }
        }
    }

    private static bool ObjectContainsAssetNumber(JsonElement element, string scanValue)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (IsAssetNumberProperty(property.Name) &&
                property.Value.ValueKind == JsonValueKind.String &&
                AssetNumbersEqual(property.Value.GetString(), scanValue))
            {
                return true;
            }

            if ((property.Value.ValueKind == JsonValueKind.Object || property.Value.ValueKind == JsonValueKind.Array) &&
                ObjectOrArrayContainsAssetNumber(property.Value, scanValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeOpportunityItemAsset(JsonElement element)
    {
        return element.TryGetProperty("opportunity_item_id", out _) ||
               element.TryGetProperty("stock_level_id", out _) ||
               element.TryGetProperty("quantity_allocated", out _) ||
               element.TryGetProperty("quantity_returned", out _) ||
               element.TryGetProperty("stock_level", out _);
    }

    private static string ReadDirectAssetNumber(JsonElement element)
    {
        return ReadFirstPresent(element, "stock_level_asset_number", "asset_number", "stock_level_barcode", "barcode");
    }

    private static string FindFirstNestedAssetNumber(JsonElement element, string scanValue)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (IsAssetNumberProperty(property.Name) &&
                    property.Value.ValueKind == JsonValueKind.String &&
                    AssetNumbersEqual(property.Value.GetString(), scanValue))
                {
                    return property.Value.GetString() ?? "";
                }

                var nested = FindFirstNestedAssetNumber(property.Value, scanValue);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindFirstNestedAssetNumber(item, scanValue);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return "";
    }

    private static bool ObjectOrArrayContainsAssetNumber(JsonElement element, string scanValue)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return ObjectContainsAssetNumber(element, scanValue);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Any(item => ObjectOrArrayContainsAssetNumber(item, scanValue));
        }

        return false;
    }

    private static bool IsAssetNumberProperty(string propertyName)
    {
        return propertyName.Equals("stock_level_asset_number", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("asset_number", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("stock_level_barcode", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("barcode", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AssetNumbersEqual(string? left, string right)
    {
        return NormalizeAssetNumber(left).Equals(NormalizeAssetNumber(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAssetNumber(string? value)
    {
        return new string((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private static string DescribeMatches(IEnumerable<OpportunityLookupResult> matches)
    {
        return string.Join("; ", matches.Take(8).Select(match =>
        {
            var assetStatus = match.MatchingAssets.Count == 0
                ? "asset status unknown"
                : string.Join("/", match.MatchingAssets.Select(asset => string.IsNullOrWhiteSpace(asset.StatusName) ? $"status {asset.Status}" : asset.StatusName));
            return $"{match.Id} {match.DisplayText} [{match.StateName}/{match.StatusName}; {assetStatus}]";
        }));
    }

    private static string ExtractNestedId(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty(propertyName, out var parent) ||
            !parent.TryGetProperty("id", out var id))
        {
            throw new InvalidOperationException($"Current-RMS response did not include {propertyName}.id.");
        }

        return ReadElementAsString(id);
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Current-RMS response did not include opportunity.{propertyName}.");
        }

        return ReadElementAsString(property);
    }

    private static string ReadOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? ReadElementAsString(property) : "";
    }

    private static string ReadFirstPresent(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = ReadOptionalString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
    }

    private static int? ReadOptionalInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        return int.TryParse(ReadElementAsString(property), out number) ? number : null;
    }

    private static string ReadElementAsString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            _ => element.ToString()
        };
    }

    private static string CleanFilePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Where(ch => !invalid.Contains(ch)).ToArray());
    }
}

public sealed record OpportunityLookupOptions(
    string LookupFilterModes,
    string RequiredOpportunityTag,
    bool ExcludeBookedOutOrCheckedIn = true);

public sealed record AssetMatchStatus(string AssetNumber, string StatusName, int? Status, string Container);

public sealed record OpportunityLookupResult(
    string Id,
    string Number,
    string Subject,
    string MemberId,
    string ClientName,
    string MatchSource,
    string StateName,
    string StatusName,
    string StartsAt,
    string EndsAt,
    string PrepStartsAt,
    string PrepEndsAt,
    IReadOnlyList<AssetMatchStatus> MatchingAssets)
{
    public string DisplayText
    {
        get
        {
            var numberPart = string.IsNullOrWhiteSpace(Number) ? "" : $"#{Number} ";
            return $"{numberPart}{Subject}".Trim();
        }
    }
}
