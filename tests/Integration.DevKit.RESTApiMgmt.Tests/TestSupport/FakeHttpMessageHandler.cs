using System.Collections.Concurrent;
using System.Net;

namespace Integration.DevKit.RESTApiMgmt.Tests.TestSupport;

/// <summary>
/// An <see cref="HttpMessageHandler"/> test double that never hits the network. Each call is
/// answered by <see cref="Responder"/>, and every request that passes through is recorded so
/// tests can assert on call count and request contents.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; }
        = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    public int CallCount => Requests.Count;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        return Task.FromResult(Responder(request));
    }

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}
