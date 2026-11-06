using System.Net;
using Integration.DevKit.RESTApiMgmt.Settings;
using Integration.DevKit.RESTApiMgmt.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Moq;

namespace Integration.DevKit.RESTApiMgmt.Tests;

public class RetryHandlerTests
{
    private static (HttpClient client, FakeHttpMessageHandler fake) CreateClient(params HttpStatusCode[] statuses)
    {
        var settings = Options.Create(new ApiManagerSettings { MaxRetries = 3, RetryBaseDelay_Milliseconds = 1 });
        var manager = new ApiManager(settings, new Mock<IHttpClientFactory>().Object);

        int call = 0;
        var fake = new FakeHttpMessageHandler
        {
            Responder = _ => new HttpResponseMessage(statuses[Math.Min(call++, statuses.Length - 1)])
        };

        var client = new HttpClient(new RetryHandler(manager) { InnerHandler = fake });
        return (client, fake);
    }

    [Fact]
    public async Task Get_RetriesTransientFailures_UntilSuccess()
    {
        var (client, fake) = CreateClient(HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway, HttpStatusCode.OK);

        var response = await client.GetAsync("https://example.com/x");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, fake.CallCount);
    }

    [Fact]
    public async Task Get_StopsAfterMaxRetries()
    {
        var (client, fake) = CreateClient(HttpStatusCode.InternalServerError);

        var response = await client.GetAsync("https://example.com/x");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(4, fake.CallCount);
    }

    [Fact]
    public async Task Get_DoesNotRetryNonTransientFailure()
    {
        var (client, fake) = CreateClient(HttpStatusCode.NotFound);

        await client.GetAsync("https://example.com/x");

        Assert.Equal(1, fake.CallCount);
    }

    [Fact]
    public async Task Post_IsNeverRetried()
    {
        var (client, fake) = CreateClient(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

        var response = await client.PostAsync("https://example.com/x", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, fake.CallCount);
    }
}
