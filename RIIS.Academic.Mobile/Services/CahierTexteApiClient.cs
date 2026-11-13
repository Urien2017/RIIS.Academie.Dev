using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RIIS.Academic.Mobile.Services;

public sealed class CahierTexteApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task<MobileCaptureUploadResult> UploadPageAsync(
        FileResult file,
        string baseUrl,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var content = new MultipartFormDataContent();
        await using var stream = await file.OpenReadAsync();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", file.FileName);

        using var request = CreateRequest(HttpMethod.Post, baseUrl, "/api/mobile/cahiers-texte/pages/upload", apiKey);
        request.Content = content;
        return await SendAsync<MobileCaptureUploadResult>(request, cancellationToken);
    }

    public async Task<CahierTexteLookups> GetLookupsAsync(
        string baseUrl,
        string apiKey,
        long? anneeAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var path = anneeAcademiqueId is null
            ? "/api/mobile/cahiers-texte/lookups"
            : $"/api/mobile/cahiers-texte/lookups?anneeAcademiqueId={anneeAcademiqueId.Value}";
        using var request = CreateRequest(HttpMethod.Get, baseUrl, path, apiKey);
        return await SendAsync<CahierTexteLookups>(request, cancellationToken);
    }

    public Task<CahierTexteImportResult> ImportAsync(CahierTexteImportRequest payload, string baseUrl, string apiKey, CancellationToken cancellationToken = default)
        => SendJsonAsync<CahierTexteImportResult>(HttpMethod.Post, baseUrl, "/api/mobile/cahiers-texte/import", payload, apiKey, cancellationToken);

    public Task<CahierTexteValidationReport> ValidateAsync(CahierTexteImportRequest payload, string baseUrl, string apiKey, long? cahierTexteId = null, CancellationToken cancellationToken = default)
    {
        var path = cahierTexteId is null
            ? "/api/mobile/cahiers-texte/validate"
            : $"/api/mobile/cahiers-texte/validate?cahierTexteId={cahierTexteId.Value}";
        return SendJsonAsync<CahierTexteValidationReport>(HttpMethod.Post, baseUrl, path, payload, apiKey, cancellationToken);
    }

    public Task<CahierTexteSeancesSaveResult> ConfirmAsync(long cahierTexteId, CahierTexteImportRequest payload, string baseUrl, string apiKey, CancellationToken cancellationToken = default)
        => SendJsonAsync<CahierTexteSeancesSaveResult>(HttpMethod.Post, baseUrl, $"/api/mobile/cahiers-texte/{cahierTexteId}/seances/confirm", payload, apiKey, cancellationToken);

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string baseUrl, string path, object payload, string apiKey, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, baseUrl, path, apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        return await SendAsync<T>(request, cancellationToken);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string baseUrl, string path, string apiKey)
    {
        var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("L'URL de l'API est invalide.");

        var request = new HttpRequestMessage(method, new Uri(baseUri, path));
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Add("X-RIIS-Mobile-Key", apiKey.Trim());
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"L'API a refusé la requête ({(int)response.StatusCode}). {body}");

        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException("La réponse de l'API est vide.");
    }
}
