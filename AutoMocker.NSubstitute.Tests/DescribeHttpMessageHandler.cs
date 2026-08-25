using System.Net;
using System.Net.Http;
using NSubstitute.AutoMock.Http;
using NSubstitute.AutoMock.Tests.Util;

namespace NSubstitute.AutoMock.Tests;

[TestClass]
public class DescribeHttpMessageHandler
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task HttpClient_CanSetupResponsesDirectlyOnSubstitute()
    {
        var mocker = new AutoMocker();

        mocker.GetSubstitute<HttpMessageHandlerWrapper>()
            .SendAsyncPublic(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Hello, World!")
            }));

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.GetAsync("https://example.com/api/test");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.AreEqual("Hello, World!", content);
    }

    [TestMethod]
    public async Task HttpClient_CanVerifySpecificRequestsWereMadeDirectlyOnSubstitute()
    {
        var mocker = new AutoMocker();

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        await service.GetAsync("https://example.com/api/users");
        await service.PostAsync("https://example.com/api/users", "{}");

        var handler = mocker.GetSubstitute<HttpMessageHandlerWrapper>();
        await handler.Received(1).SendAsyncPublic(
            Arg.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
            Arg.Any<CancellationToken>());

        await handler.Received(1).SendAsyncPublic(
            Arg.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task HttpClient_CreateClient_UsesTheSubstituteHandler()
    {
        var mocker = new AutoMocker();

        var handler = mocker.GetSubstitute<HttpMessageHandlerWrapper>();
        handler.SendAsyncPublic(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        using var httpClient = handler.CreateClient();

        var response = await httpClient.GetAsync("https://example.com/api/test", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
