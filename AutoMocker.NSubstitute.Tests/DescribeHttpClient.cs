using System.Net;
using System.Net.Http;
using NSubstitute.AutoMock.Http;
using NSubstitute.AutoMock.Tests.Util;

namespace NSubstitute.AutoMock.Tests;

[TestClass]
public class DescribeHttpClient
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task HttpClient_CanSetupDifferentResponsesForDifferentUrls()
    {
        var mocker = new AutoMocker();

        mocker.SetupHttpGet("/users")
            .ReturnsHttpResponse(HttpStatusCode.OK, """{"users": []}""");

        mocker.SetupHttpGet("/products")
            .ReturnsHttpResponse(HttpStatusCode.OK, """{"products": []}""");

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var usersResponse = await service.GetAsync("https://example.com/api/users");
        var productsResponse = await service.GetAsync("https://example.com/api/products");

        var usersContent = await usersResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var productsContent = await productsResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual("""{"users": []}""", usersContent);
        Assert.AreEqual("""{"products": []}""", productsContent);
    }

    [TestMethod]
    public async Task HttpClient_UnconfiguredRequestsReturnDefaultOkResponse()
    {
        var mocker = new AutoMocker();

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.GetAsync("https://example.com/api/test");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HttpClient_CanVerifyHttpGetRequestsWereMade()
    {
        var mocker = new AutoMocker();

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        await service.GetAsync("https://example.com/api/test");

        mocker.VerifyHttpGet("https://example.com/api/test", 1);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpGetByUrl()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpGet("/people")
            .ReturnsHttpResponse(HttpStatusCode.OK, content);

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.GetAsync("https://example.com/people");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpGetByExpression()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpGet(r => r.RequestUri!.AbsoluteUri.EndsWith("/people"))
            .ReturnsHttpResponse(HttpStatusCode.OK, content);

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.GetAsync("https://example.com/people");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_SetupHttpGetDoesNotMatchOtherVerbs()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpGet("/people")
            .ReturnsHttpResponse(HttpStatusCode.OK, content);
        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        // Falls back to the default 200 OK, not the GET-specific setup.
        var response = await service.PostAsync("https://example.com/people", "data");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreNotEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpGetErrorResponses()
    {
        var mocker = new AutoMocker();

        mocker.SetupHttpGet()
            .ReturnsHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.GetAsync("https://example.com/api/test");

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpGetByteArrayResponse()
    {
        var mocker = new AutoMocker();
        var expectedBytes = "Hello"u8.ToArray();

        mocker.SetupHttpGet()
            .ReturnsResponse(HttpStatusCode.OK, expectedBytes, "application/octet-stream");

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.GetAsync("https://example.com/api/binary");
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        CollectionAssert.AreEqual(expectedBytes, content);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpGetResponseWithCustomHeaders()
    {
        var mocker = new AutoMocker();

        mocker.SetupHttpGet()
            .ReturnsHttpResponse(HttpStatusCode.OK, "Response with headers", configure: response => response.Headers.Add("X-Custom-Header", "CustomValue"));

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.GetAsync("https://example.com/api/test");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.Contains("X-Custom-Header"));
        Assert.AreEqual("CustomValue", response.Headers.GetValues("X-Custom-Header").First());
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpPostByUrl()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpPost("/people", "data")
            .ReturnsHttpResponse(HttpStatusCode.OK, content);

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.PostAsync("https://example.com/people", "data");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpPostByExpression()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpPost(r => r.RequestUri!.AbsoluteUri.EndsWith("/people"))
            .ReturnsHttpResponse(HttpStatusCode.OK, content);

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.PostAsync("https://example.com/people", "data");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_CanVerifyHttpPostRequestsWereMade()
    {
        var mocker = new AutoMocker();

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        await service.PostAsync("https://example.com/api/test", "Some content");

        mocker.VerifyHttpPost("https://example.com/api/test", "Some content", 1);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpPostErrorResponses()
    {
        var mocker = new AutoMocker();

        mocker.SetupHttpPost()
            .ReturnsHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.PostAsync("https://example.com/api/test", "Stuff");

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpPutByUrl()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpPut("/people", "data")
            .ReturnsHttpResponse(HttpStatusCode.OK, content);

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.PutAsync("https://example.com/people", "data");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_CanVerifyHttpPutRequestsWereMade()
    {
        var mocker = new AutoMocker();

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        await service.PutAsync("https://example.com/api/test", "Some content");

        mocker.VerifyHttpPut("https://example.com/api/test", "Some content", 1);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpDeleteByUrl()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpDelete("/people")
            .ReturnsHttpResponse(HttpStatusCode.OK, content);

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.DeleteAsync("https://example.com/people");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_CanVerifyHttpDeleteRequestsWereMade()
    {
        var mocker = new AutoMocker();

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        await service.DeleteAsync("https://example.com/api/test");

        mocker.VerifyHttpDelete("https://example.com/api/test", requiredNumberOfCalls: 1);
    }

    [TestMethod]
    public async Task HttpClient_CanSetupHttpHeadByUrl()
    {
        var mocker = new AutoMocker();
        string content = """[{name: "test"}]""";
        mocker.SetupHttpHead("/people")
            .ReturnsHttpResponse(HttpStatusCode.OK, content);

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        var response = await service.HeadAsync("https://example.com/people");
        var receivedContent = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(content, receivedContent);
    }

    [TestMethod]
    public async Task HttpClient_CanVerifyHttpHeadRequestsWereMade()
    {
        var mocker = new AutoMocker();

        var service = mocker.CreateInstance<ServiceWithHttpClient>();

        await service.HeadAsync("https://example.com/api/test");

        mocker.VerifyHttpHead("https://example.com/api/test", requiredNumberOfCalls: 1);
    }
}
