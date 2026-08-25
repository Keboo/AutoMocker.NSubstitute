# HttpClient Support

AutoMocker.NSubstitute provides built-in support for testing code that depends on `HttpClient`. When your class accepts an `HttpClient` constructor parameter, AutoMocker automatically resolves it with a substituted `HttpMessageHandler` — no manual setup required.

## Features

- **Automatic resolution** of `HttpClient` dependencies via `HttpClientResolver`
- **Verb-specific setup methods** for GET, POST, PUT, DELETE, and HEAD
- **Flexible request matching** by URI substring or a predicate
- **Fluent response builders** for string, byte array, stream, and custom content types
- **Verification helpers** built on NSubstitute's `Received()` to assert that specific HTTP requests were made
- **Custom response configuration** including headers, status codes, and media types
- **Default behavior** returns HTTP 200 OK with empty content for any unconfigured request

## How It Works

NSubstitute has no equivalent of Moq's `.Protected()` API, so it cannot configure or verify the protected `HttpMessageHandler.SendAsync` method directly. To work around this, AutoMocker.NSubstitute substitutes an internal `HttpMessageHandlerWrapper` — a subclass of `HttpMessageHandler` that exposes a public abstract `SendAsyncPublic` method and overrides the protected `SendAsync` to delegate to it. Because the wrapper's real `SendAsync` override must run, AutoMocker always creates this substitute as a partial substitute (`Substitute.ForPartsOf`), regardless of the container's `CallBase` setting.

`HttpClientResolver` intercepts `HttpClient` dependencies. When `CreateInstance<T>()` encounters an `HttpClient` parameter, the resolver:

1. Creates (or retrieves) the `HttpMessageHandlerWrapper` substitute via `GetSubstitute<HttpMessageHandlerWrapper>()`
2. Configures a default response of HTTP 200 OK with empty content for any request
3. Wraps the handler in a new `HttpClient` instance

This means you can immediately create and test classes that use `HttpClient` without any explicit setup. The extension methods on `AutoMocker` and `HttpMessageHandlerWrapper` then let you customize request matching and response behavior using standard NSubstitute syntax.

## Usage

### Basic Setup

Create an `AutoMocker`, set up an HTTP response, and test your service:

```csharp
[TestMethod]
public async Task GetUsers_ReturnsUserList()
{
    // Arrange
    AutoMocker mocker = new();
    mocker.SetupHttpGet("/users")
        .ReturnsHttpResponse(HttpStatusCode.OK, """{"users": []}""");

    var service = mocker.CreateInstance<UserService>();

    // Act
    var response = await service.GetUsersAsync();

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

Without any setup, requests return HTTP 200 OK with empty content by default.

### Setup by URL

Match requests by a URL substring. The match checks whether the request URI contains the specified string:

```csharp
mocker.SetupHttpGet("/users")
    .ReturnsHttpResponse(HttpStatusCode.OK, """{"users": []}""");

mocker.SetupHttpPost("/orders", "order data")
    .ReturnsHttpResponse(HttpStatusCode.Created, """{"id": 1}""");

mocker.SetupHttpPut("/users/1", "updated data")
    .ReturnsHttpResponse(HttpStatusCode.OK, """{"updated": true}""");

mocker.SetupHttpDelete("/users/1")
    .ReturnsHttpResponse(HttpStatusCode.NoContent);

mocker.SetupHttpHead("/health")
    .ReturnsHttpResponse(HttpStatusCode.OK);
```

### Setup by Predicate

Use a `Func<HttpRequestMessage, bool>` for more complex request matching:

```csharp
mocker.SetupHttpGet(r => r.RequestUri!.AbsoluteUri.EndsWith("/people"))
    .ReturnsHttpResponse(HttpStatusCode.OK, """[{"name": "Alice"}]""");

mocker.SetupHttpPost(r => r.RequestUri!.AbsoluteUri.Contains("/api/"))
    .ReturnsHttpResponse(HttpStatusCode.Accepted);
```

### Multiple URLs with Different Responses

```csharp
mocker.SetupHttpGet("/users")
    .ReturnsHttpResponse(HttpStatusCode.OK, """{"users": []}""");

mocker.SetupHttpGet("/products")
    .ReturnsHttpResponse(HttpStatusCode.OK, """{"products": []}""");

var service = mocker.CreateInstance<CatalogService>();

var usersResponse = await service.GetUsersAsync();    // matches /users
var productsResponse = await service.GetProductsAsync(); // matches /products
```

### Error Responses

```csharp
mocker.SetupHttpGet()
    .ReturnsHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

mocker.SetupHttpPost()
    .ReturnsHttpResponse(HttpStatusCode.BadRequest, """{"error": "Invalid input"}""");
```

### Custom Headers

Use the `configure` callback to modify response headers or other properties:

```csharp
mocker.SetupHttpGet()
    .ReturnsHttpResponse(HttpStatusCode.OK, "Response body", configure: response =>
    {
        response.Headers.Add("X-Custom-Header", "CustomValue");
        response.Headers.Add("X-Request-Id", "abc-123");
    });
```

### Binary Content

Return byte array responses with a specified media type:

```csharp
var pdfBytes = File.ReadAllBytes("sample.pdf");

mocker.SetupHttpGet("/documents/1")
    .ReturnsResponse(HttpStatusCode.OK, pdfBytes, "application/pdf");
```

### Stream Content

Return stream-based responses. Seekable streams are automatically reset to position 0 before use:

```csharp
using var stream = new MemoryStream(Encoding.UTF8.GetBytes("streamed content"));

mocker.SetupHttpGet("/download")
    .ReturnsResponse(HttpStatusCode.OK, stream, "application/octet-stream");
```

### Verb Isolation

Each setup method only matches its specific HTTP method. A `SetupHttpGet` will not match POST requests, and vice versa — unmatched requests fall back to the default HTTP 200 OK response (or another matching setup, if configured):

```csharp
mocker.SetupHttpGet("/people")
    .ReturnsHttpResponse(HttpStatusCode.OK, "[]");

var service = mocker.CreateInstance<PeopleService>();

// This POST does not match the GET-only setup above.
var response = await service.CreatePersonAsync("Alice");
```

## Verification

Verify that specific HTTP requests were made during the test, using an NSubstitute-style expected call count:

```csharp
var service = mocker.CreateInstance<NotificationService>();

await service.SendNotificationAsync("Hello");

// Verify a GET was made to a specific URL
mocker.VerifyHttpGet("https://example.com/api/status", requiredNumberOfCalls: 1);

// Verify a POST with specific content
mocker.VerifyHttpPost("https://example.com/api/notify", "Hello", requiredNumberOfCalls: 1);

// Verify PUT, DELETE, HEAD
mocker.VerifyHttpPut("https://example.com/api/config", "new value", requiredNumberOfCalls: 1);
mocker.VerifyHttpDelete("https://example.com/api/cache", requiredNumberOfCalls: 1);
mocker.VerifyHttpHead("https://example.com/api/health", requiredNumberOfCalls: 1);
```

## Lower-Level Access

For advanced scenarios, retrieve the `HttpMessageHandlerWrapper` substitute directly and use plain NSubstitute syntax:

```csharp
var handler = mocker.GetSubstitute<HttpMessageHandlerWrapper>();

// Setup directly
handler.SendAsyncPublic(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("Hello, World!")
    }));

// Create an HttpClient from the substitute directly
HttpClient client = handler.CreateClient();

// Verify directly
await handler.Received(1).SendAsyncPublic(
    Arg.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
    Arg.Any<CancellationToken>());
```

## API Reference

### Setup Methods

All setup methods are available as extension methods on both `AutoMocker` and `HttpMessageHandlerWrapper`. Each returns a `Task<HttpResponseMessage>` representing the pending call, ready to be chained with `Returns`/`ReturnsHttpResponse`/`ReturnsResponse`.

| Method | Description |
|--------|-------------|
| `SetupHttpGet(string?)` | Setup GET requests, optionally matching a URL substring |
| `SetupHttpGet(Func<HttpRequestMessage, bool>)` | Setup GET requests matching a predicate |
| `SetupHttpPost(string?, string?)` | Setup POST requests with optional URL and content matching |
| `SetupHttpPost(Func<HttpRequestMessage, bool>)` | Setup POST requests matching a predicate |
| `SetupHttpPut(string?, string?)` | Setup PUT requests with optional URL and content matching |
| `SetupHttpPut(Func<HttpRequestMessage, bool>)` | Setup PUT requests matching a predicate |
| `SetupHttpDelete(string?)` | Setup DELETE requests, optionally matching a URL substring |
| `SetupHttpDelete(Func<HttpRequestMessage, bool>)` | Setup DELETE requests matching a predicate |
| `SetupHttpHead(string?)` | Setup HEAD requests, optionally matching a URL substring |
| `SetupHttpHead(Func<HttpRequestMessage, bool>)` | Setup HEAD requests matching a predicate |
| `SetupHttp(Func<HttpRequestMessage, bool>)` | Setup a call matching any HTTP method for the given predicate |

### Response Methods

| Method | Description |
|--------|-------------|
| `ReturnsHttpResponse(HttpStatusCode)` | Return a response with the given status code |
| `ReturnsHttpResponse(HttpStatusCode, string)` | Return a string response with status code |
| `ReturnsHttpResponse(HttpStatusCode, HttpContent)` | Return custom content with status code |
| `ReturnsHttpResponse(string)` | Return a string response with HTTP 200 OK |
| `ReturnsResponse(HttpStatusCode, byte[], string?)` | Return byte array content with media type |
| `ReturnsResponse(HttpStatusCode, Stream, string?)` | Return stream content with media type |
| `ReturnsResponse(byte[], string?)` | Return byte array content with HTTP 200 OK |
| `ReturnsResponse(Stream, string?)` | Return stream content with HTTP 200 OK |

All response methods accept an optional `Action<HttpResponseMessage>? configure` parameter for customizing headers and other response properties.

### Verification Methods

| Method | Target |
|--------|--------|
| `VerifyHttpGet(string?, int?)` | Verify GET requests to a URL |
| `VerifyHttpPost(string?, string?, int?)` | Verify POST requests with optional content |
| `VerifyHttpPut(string?, string?, int?)` | Verify PUT requests with optional content |
| `VerifyHttpDelete(string?, int?)` | Verify DELETE requests to a URL |
| `VerifyHttpHead(string?, int?)` | Verify HEAD requests to a URL |

`requiredNumberOfCalls` defaults to at least once (NSubstitute's `Received()` with no count) when omitted.

### Utility Methods

| Method | Description |
|--------|-------------|
| `CreateClient()` | Create an `HttpClient` backed by an `HttpMessageHandlerWrapper` substitute |

## Best Practices

- **Use verb-specific methods** (`SetupHttpGet`, `SetupHttpPost`, etc.) instead of the generic `SetupHttp` for clearer test intent and automatic HTTP method filtering
- **Match by URL substring** for simple cases (`"/users"`) and **predicates** for complex matching logic
- **Verify requests** to confirm your code sends the right HTTP method, URL, and content
- **Leverage the `configure` callback** to test code that inspects response headers
