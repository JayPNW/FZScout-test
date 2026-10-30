using System.Net;

namespace ResourceHunterBonusScout.Tests.Site;

/// <summary>Stands in for the network: answers every request with one canned response and remembers what was sent.</summary>
sealed class StubHttpHandler : HttpMessageHandler
{
    readonly HttpStatusCode _status;
    readonly string _body;
    readonly Exception? _exception;

    public StubHttpHandler(HttpStatusCode status, string body)
    {
        this._status = status;
        this._body = body;
    }

    public StubHttpHandler(Exception exception)
        : this(HttpStatusCode.OK, string.Empty)
    {
        this._exception = exception;
    }

    public List<SentRequest> Sent { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = string.Empty;
        if (request.Content is not null)
        {
            content = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
        Sent.Add(new SentRequest(request.Method, request.RequestUri!, headers, content));

        if (this._exception is not null)
        {
            throw this._exception;
        }

        return new HttpResponseMessage(this._status) { Content = new StringContent(this._body) };
    }

    public sealed record SentRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string Body);
}
