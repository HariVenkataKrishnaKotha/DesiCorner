using System.Net;
using System.Text;

namespace DesiCorner.Tests.Shared;

/// <summary>
/// Terminal HttpMessageHandler that records every outbound request (headers and body)
/// and answers it with a caller-supplied responder.
/// </summary>
public sealed class RecordingHandler : HttpMessageHandler
{
    private readonly Func<RecordedRequest, HttpResponseMessage> _respond;

    public RecordingHandler(Func<RecordedRequest, HttpResponseMessage>? respond = null)
    {
        _respond = respond ?? (_ => new HttpResponseMessage(HttpStatusCode.OK));
    }

    public List<RecordedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        var recorded = new RecordedRequest(request.Method, request.RequestUri!, headers, body);
        lock (Requests) Requests.Add(recorded);

        var response = _respond(recorded);
        response.RequestMessage = request;
        return response;
    }

    // Shared across HttpClientFactory handler rotations, so never torn down by them.
    protected override void Dispose(bool disposing) { }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

public sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string[]> Headers,
    string? Body)
{
    public string? Header(string name) =>
        Headers.TryGetValue(name, out var values) ? string.Join(",", values) : null;
}
