// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccess.Services;

/// <summary>
/// HueApi deserializes every answer of the bridge as JSON, even HTML error pages, and then only reports
/// "'&lt;' is an invalid start of a value". This handler remembers what the bridge really answered.
/// </summary>
public sealed class HueResponseDiagnosticsHandler : DelegatingHandler
{
    private const int MaxSnippetLength = 200;

    private volatile string? lastNonJsonResponse;

    public HueResponseDiagnosticsHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    /// <summary>
    /// Describes the last answer that was an error or not JSON, or null if the last answer was fine.
    /// </summary>
    public string? LastNonJsonResponse => lastNonJsonResponse;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        try
        {
            await response.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
            string body = (await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
            bool isJson = body.StartsWith('{') || body.StartsWith('[');
            if (response.IsSuccessStatusCode && isJson)
            {
                lastNonJsonResponse = null;
            }
            else if (!isJson)
            {
                string snippet = body.Length > MaxSnippetLength ? body[..MaxSnippetLength] : body;
                snippet = snippet.ReplaceLineEndings(" ");
                lastNonJsonResponse = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} for {request.Method} {request.RequestUri?.AbsolutePath}, "
                                      + $"content type '{response.Content.Headers.ContentType?.MediaType}', body: '{snippet}'";
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Diagnostics must never break the request.
        }
        return response;
    }
}
