# HTTP

`HttpMessageHandlerExtensions`: `NSubstitute.AutoMock`. `HttpResponseExtensions` and `HttpMessageHandlerWrapper`: `NSubstitute.AutoMock.Http`. [Documentation index](README.md).

`AutoMocker` resolves `HttpClient` with a partial `HttpMessageHandlerWrapper` substitute. Unconfigured requests return HTTP 200 with empty content, regardless of `AutoMocker.CallBase`.

```csharp
using System.Net;
using System.Net.Http;
using NSubstitute;
using NSubstitute.AutoMock;
using NSubstitute.AutoMock.Http;

var mocker = new AutoMocker();
mocker.SetupHttpGet("/users")
    .ReturnsHttpResponse(HttpStatusCode.OK, "[]", mediaType: "application/json");

using var response = await mocker.Get<HttpClient>()
    .GetAsync("https://example.test/users");
mocker.VerifyHttpGet("/users", requiredNumberOfCalls: 1);
```

## HttpMessageHandlerExtensions: setup

Each method below has overloads on **both** `AutoMocker` and `HttpMessageHandlerWrapper`. All return `Task<HttpResponseMessage>`, the pending substitute call; immediately chain `ReturnsHttpResponse`, `ReturnsResponse`, or NSubstitute's `Returns`.

Receivers are omitted:

| Method | Overloads |
| --- | --- |
| `SetupHttpGet` | `(string? requestUri = null)`; `(Func<HttpRequestMessage, bool> match)` |
| `SetupHttpPost` | `(string? requestUri = null, string? content = null)`; `(Func<HttpRequestMessage, bool> match)` |
| `SetupHttpPut` | `(string? requestUri = null, string? content = null)`; `(Func<HttpRequestMessage, bool> match)` |
| `SetupHttpPatch` | `(string? requestUri = null, string? content = null)`; `(Func<HttpRequestMessage, bool> match)` |
| `SetupHttpDelete` | `(string? requestUri = null)`; `(Func<HttpRequestMessage, bool> match)` |
| `SetupHttpHead` | `(string? requestUri = null)`; `(Func<HttpRequestMessage, bool> match)` |
| `SetupHttp` | `(Func<HttpRequestMessage, bool> match)` |

Verb helpers restrict the HTTP method. `SetupHttp` matches any method satisfying the predicate. URI matching is a case-sensitive substring of the absolute URI; body matching is exact text. Null URI/content filters match anything.

```csharp
mocker.SetupHttpPost("/orders", "order data")
    .ReturnsHttpResponse(HttpStatusCode.Created);
mocker.SetupHttpPut("/orders/1", "replacement")
    .ReturnsHttpResponse(HttpStatusCode.OK);
mocker.SetupHttpPatch("/orders/1", "change")
    .ReturnsHttpResponse(HttpStatusCode.OK);
mocker.SetupHttpDelete("/orders/1")
    .ReturnsHttpResponse(HttpStatusCode.NoContent);
mocker.SetupHttpHead("/health")
    .ReturnsHttpResponse(HttpStatusCode.OK);

var handler = mocker.GetSubstitute<HttpMessageHandlerWrapper>();
handler.SetupHttpGet(r => r.RequestUri!.AbsolutePath == "/users")
    .ReturnsHttpResponse("[]");
handler.SetupHttp(r => r.Method == HttpMethod.Options)
    .ReturnsHttpResponse(HttpStatusCode.NoContent);
```

## HttpResponseExtensions: response builders

All overloads extend `Task<HttpResponseMessage>` and return `NSubstitute.Core.ConfiguredCall`. Receivers and return types are omitted below. Every `configure` callback can change headers or other response properties.

### ReturnsHttpResponse

```csharp
ReturnsHttpResponse(
    HttpStatusCode statusCode, Action<HttpResponseMessage>? configure = null)
ReturnsHttpResponse(
    HttpStatusCode statusCode, HttpContent content,
    Action<HttpResponseMessage>? configure = null)
ReturnsHttpResponse(
    HttpStatusCode statusCode, string content,
    string? mediaType = null, Encoding? encoding = null,
    Action<HttpResponseMessage>? configure = null)
ReturnsHttpResponse(
    string content, string? mediaType = null, Encoding? encoding = null,
    Action<HttpResponseMessage>? configure = null)
```

The status-free overload uses HTTP 200. String content defaults to UTF-8 and `text/plain`.

```csharp
mocker.SetupHttpGet("/users")
    .ReturnsHttpResponse(HttpStatusCode.OK, "[]", "application/json",
        configure: response => response.Headers.Add("X-Test", "true"));
mocker.SetupHttpGet("/custom")
    .ReturnsHttpResponse(HttpStatusCode.OK, new StringContent("custom"));
```

### ReturnsResponse

```csharp
ReturnsResponse(
    HttpStatusCode statusCode, byte[] content, string? mediaType = null,
    Action<HttpResponseMessage>? configure = null)
ReturnsResponse(
    byte[] content, string? mediaType = null,
    Action<HttpResponseMessage>? configure = null)
ReturnsResponse(
    HttpStatusCode statusCode, Stream content, string? mediaType = null,
    Action<HttpResponseMessage>? configure = null)
ReturnsResponse(
    Stream content, string? mediaType = null,
    Action<HttpResponseMessage>? configure = null)
```

Status-free overloads use HTTP 200. Seekable streams are reset to position zero **at setup time**. Builders reuse a configured response/content object; use NSubstitute `Returns` with a factory when each call needs a fresh response. Non-null content is required by every content overload.

```csharp
mocker.SetupHttpGet("/bytes")
    .ReturnsResponse(new byte[] { 1, 2, 3 }, "application/octet-stream");
mocker.SetupHttpGet("/stream")
    .ReturnsResponse(HttpStatusCode.OK, new MemoryStream(new byte[] { 4, 5 }));

mocker.SetupHttpGet("/fresh").Returns(_ =>
    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("fresh per call")
    }));
```

## HttpMessageHandlerExtensions: verification

Verb-specific methods extend **AutoMocker only**:

```csharp
void VerifyHttpGet(string? requestUri = null, int? requiredNumberOfCalls = null)
void VerifyHttpPost(
    string? requestUri = null, string? content = null, int? requiredNumberOfCalls = null)
void VerifyHttpPut(
    string? requestUri = null, string? content = null, int? requiredNumberOfCalls = null)
void VerifyHttpPatch(
    string? requestUri = null, string? content = null, int? requiredNumberOfCalls = null)
void VerifyHttpDelete(string? requestUri = null, int? requiredNumberOfCalls = null)
void VerifyHttpHead(string? requestUri = null, int? requiredNumberOfCalls = null)
```

The lower-level helper extends **HttpMessageHandlerWrapper only**:

```csharp
void VerifyHttp(
    HttpMethod method, Func<HttpRequestMessage, bool> match,
    int? requiredNumberOfCalls = null)
```

Matching follows setup rules. A null count means at least once; a supplied count is exact, including zero for no calls. Failed checks throw NSubstitute's received-call exception.

```csharp
mocker.VerifyHttpPost("/orders", "order data", requiredNumberOfCalls: 1);
mocker.VerifyHttpPut("/orders/1", "replacement", requiredNumberOfCalls: 1);
mocker.VerifyHttpPatch("/orders/1", "change", requiredNumberOfCalls: 1);
mocker.VerifyHttpDelete("/orders/1", requiredNumberOfCalls: 0);
mocker.VerifyHttpHead("/health", requiredNumberOfCalls: 1);
handler.VerifyHttp(HttpMethod.Get,
    r => r.RequestUri!.AbsolutePath == "/users", requiredNumberOfCalls: 1);
```

## HttpMessageHandlerWrapper and CreateClient

`HttpMessageHandlerWrapper` is an abstract `HttpMessageHandler` with a protected parameterless constructor. Its protected `SendAsync` override delegates to:

```csharp
public abstract Task<HttpResponseMessage> SendAsyncPublic(
    HttpRequestMessage request, CancellationToken cancellationToken);
```

`HttpMessageHandlerExtensions.CreateClient(this HttpMessageHandlerWrapper handler)` returns an `HttpClient` without transferring handler ownership. Dispose the client separately.

```csharp
var handler = mocker.GetSubstitute<HttpMessageHandlerWrapper>();
using HttpClient client = handler.CreateClient();

await handler.Received(1).SendAsyncPublic(
    Arg.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
    Arg.Any<CancellationToken>());
```

For named clients, see [WithHttpClientFactory](SourceGenerators/HttpClientFactoryExtensionGenerator.md). The generated factory caches one client per name; named clients share the testable handler.
