using System.Net.Http;
using NSubstitute.AutoMock.Http;

namespace NSubstitute.AutoMock;

/// <summary>
/// Provides extension methods for configuring and verifying calls made through an
/// <see cref="HttpMessageHandlerWrapper" /> substitute using idiomatic NSubstitute syntax.
/// </summary>
/// <remarks>
/// Because NSubstitute has no equivalent of Moq's <c>.Protected()</c> API, the protected
/// <see cref="HttpMessageHandler.SendAsync(HttpRequestMessage, CancellationToken)" /> method
/// cannot be configured directly. Instead, these helpers work against the public
/// <see cref="HttpMessageHandlerWrapper.SendAsyncPublic(HttpRequestMessage, CancellationToken)" />
/// member, which the wrapper class delegates to internally.
/// </remarks>
public static partial class HttpMessageHandlerExtensions
{
    /// <summary>
    /// Creates a new <see cref="HttpClient" /> backed by this handler substitute.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    public static HttpClient CreateClient(this HttpMessageHandlerWrapper handler)
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

        return new HttpClient(handler, disposeHandler: false);
    }

    /// <summary>
    /// Configures a call for a GET request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    public static Task<HttpResponseMessage> SetupHttpGet(this AutoMocker mocker, string? requestUri = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpGet(requestUri);
    }

    /// <summary>
    /// Configures a call for a GET request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpGet(this AutoMocker mocker, Func<HttpRequestMessage, bool> match)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpGet(match);
    }

    /// <summary>
    /// Configures a call for a GET request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    public static Task<HttpResponseMessage> SetupHttpGet(this HttpMessageHandlerWrapper handler, string? requestUri = null)
        => handler.SetupHttpGet(r => MatchesRequestUri(r.RequestUri, requestUri));

    /// <summary>
    /// Configures a call for a GET request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpGet(this HttpMessageHandlerWrapper handler, Func<HttpRequestMessage, bool> match)
        => SetupHttp(handler, HttpMethod.Get, match);

    /// <summary>
    /// Configures a call for a POST request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="content">Optional request content to match.</param>
    public static Task<HttpResponseMessage> SetupHttpPost(this AutoMocker mocker, string? requestUri = null, string? content = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpPost(requestUri, content);
    }

    /// <summary>
    /// Configures a call for a POST request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpPost(this AutoMocker mocker, Func<HttpRequestMessage, bool> match)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpPost(match);
    }

    /// <summary>
    /// Configures a call for a POST request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="content">Optional request content to match.</param>
    public static Task<HttpResponseMessage> SetupHttpPost(this HttpMessageHandlerWrapper handler, string? requestUri = null, string? content = null)
        => handler.SetupHttpPost(r => MatchesRequestUri(r.RequestUri, requestUri) && ContentEquals(r.Content, content));

    /// <summary>
    /// Configures a call for a POST request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpPost(this HttpMessageHandlerWrapper handler, Func<HttpRequestMessage, bool> match)
        => SetupHttp(handler, HttpMethod.Post, match);

    /// <summary>
    /// Configures a call for a PUT request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="content">Optional request content to match.</param>
    public static Task<HttpResponseMessage> SetupHttpPut(this AutoMocker mocker, string? requestUri = null, string? content = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpPut(requestUri, content);
    }

    /// <summary>
    /// Configures a call for a PUT request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpPut(this AutoMocker mocker, Func<HttpRequestMessage, bool> match)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpPut(match);
    }

    /// <summary>
    /// Configures a call for a PUT request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="content">Optional request content to match.</param>
    public static Task<HttpResponseMessage> SetupHttpPut(this HttpMessageHandlerWrapper handler, string? requestUri = null, string? content = null)
        => handler.SetupHttpPut(r => MatchesRequestUri(r.RequestUri, requestUri) && ContentEquals(r.Content, content));

    /// <summary>
    /// Configures a call for a PUT request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpPut(this HttpMessageHandlerWrapper handler, Func<HttpRequestMessage, bool> match)
        => SetupHttp(handler, HttpMethod.Put, match);

    /// <summary>
    /// Configures a call for a DELETE request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    public static Task<HttpResponseMessage> SetupHttpDelete(this AutoMocker mocker, string? requestUri = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpDelete(requestUri);
    }

    /// <summary>
    /// Configures a call for a DELETE request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpDelete(this AutoMocker mocker, Func<HttpRequestMessage, bool> match)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpDelete(match);
    }

    /// <summary>
    /// Configures a call for a DELETE request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    public static Task<HttpResponseMessage> SetupHttpDelete(this HttpMessageHandlerWrapper handler, string? requestUri = null)
        => handler.SetupHttpDelete(r => MatchesRequestUri(r.RequestUri, requestUri));

    /// <summary>
    /// Configures a call for a DELETE request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpDelete(this HttpMessageHandlerWrapper handler, Func<HttpRequestMessage, bool> match)
        => SetupHttp(handler, HttpMethod.Delete, match);

    /// <summary>
    /// Configures a call for a HEAD request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    public static Task<HttpResponseMessage> SetupHttpHead(this AutoMocker mocker, string? requestUri = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpHead(requestUri);
    }

    /// <summary>
    /// Configures a call for a HEAD request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpHead(this AutoMocker mocker, Func<HttpRequestMessage, bool> match)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttpHead(match);
    }

    /// <summary>
    /// Configures a call for a HEAD request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    public static Task<HttpResponseMessage> SetupHttpHead(this HttpMessageHandlerWrapper handler, string? requestUri = null)
        => handler.SetupHttpHead(r => MatchesRequestUri(r.RequestUri, requestUri));

    /// <summary>
    /// Configures a call for a HEAD request. Chain with <c>Returns</c>/<c>ReturnsHttpResponse</c> to
    /// specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttpHead(this HttpMessageHandlerWrapper handler, Func<HttpRequestMessage, bool> match)
        => SetupHttp(handler, HttpMethod.Head, match);

    /// <summary>
    /// Configures a call matching any HTTP method for the given predicate. Chain with
    /// <c>Returns</c>/<c>ReturnsHttpResponse</c> to specify the response.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttp(this AutoMocker mocker, Func<HttpRequestMessage, bool> match)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        return mocker.GetSubstitute<HttpMessageHandlerWrapper>().SetupHttp(match);
    }

    /// <summary>
    /// Configures a call matching any HTTP method for the given predicate. Chain with
    /// <c>Returns</c>/<c>ReturnsHttpResponse</c> to specify the response.
    /// </summary>
    /// <param name="handler">The <see cref="HttpMessageHandlerWrapper" /> substitute.</param>
    /// <param name="match">The predicate used to match the <see cref="HttpRequestMessage" />.</param>
    public static Task<HttpResponseMessage> SetupHttp(this HttpMessageHandlerWrapper handler, Func<HttpRequestMessage, bool> match)
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));
        if (match is null)
            throw new ArgumentNullException(nameof(match));

        return handler.SendAsyncPublic(Arg.Is<HttpRequestMessage>(r => match(r)), Arg.Any<CancellationToken>());
    }

    private static Task<HttpResponseMessage> SetupHttp(HttpMessageHandlerWrapper handler, HttpMethod method, Func<HttpRequestMessage, bool> match)
        => SetupHttp(handler, r => r.Method == method && match(r));

    /// <summary>
    /// Verifies that a GET request matching the given URI was made.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="requiredNumberOfCalls">The number of times the request is expected to have been made. Defaults to at least once.</param>
    public static void VerifyHttpGet(this AutoMocker mocker, string? requestUri = null, int? requiredNumberOfCalls = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        mocker.GetSubstitute<HttpMessageHandlerWrapper>().VerifyHttp(HttpMethod.Get, r => MatchesRequestUri(r.RequestUri, requestUri), requiredNumberOfCalls);
    }

    /// <summary>
    /// Verifies that a POST request matching the given URI and content was made.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="content">Optional request content to match.</param>
    /// <param name="requiredNumberOfCalls">The number of times the request is expected to have been made. Defaults to at least once.</param>
    public static void VerifyHttpPost(this AutoMocker mocker, string? requestUri = null, string? content = null, int? requiredNumberOfCalls = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        mocker.GetSubstitute<HttpMessageHandlerWrapper>().VerifyHttp(HttpMethod.Post,
            r => MatchesRequestUri(r.RequestUri, requestUri) && ContentEquals(r.Content, content), requiredNumberOfCalls);
    }

    /// <summary>
    /// Verifies that a PUT request matching the given URI and content was made.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="content">Optional request content to match.</param>
    /// <param name="requiredNumberOfCalls">The number of times the request is expected to have been made. Defaults to at least once.</param>
    public static void VerifyHttpPut(this AutoMocker mocker, string? requestUri = null, string? content = null, int? requiredNumberOfCalls = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        mocker.GetSubstitute<HttpMessageHandlerWrapper>().VerifyHttp(HttpMethod.Put,
            r => MatchesRequestUri(r.RequestUri, requestUri) && ContentEquals(r.Content, content), requiredNumberOfCalls);
    }

    /// <summary>
    /// Verifies that a DELETE request matching the given URI was made.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="requiredNumberOfCalls">The number of times the request is expected to have been made. Defaults to at least once.</param>
    public static void VerifyHttpDelete(this AutoMocker mocker, string? requestUri = null, int? requiredNumberOfCalls = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        mocker.GetSubstitute<HttpMessageHandlerWrapper>().VerifyHttp(HttpMethod.Delete, r => MatchesRequestUri(r.RequestUri, requestUri), requiredNumberOfCalls);
    }

    /// <summary>
    /// Verifies that a HEAD request matching the given URI was made.
    /// </summary>
    /// <param name="mocker">The <see cref="AutoMocker" /> instance.</param>
    /// <param name="requestUri">A substring to match against the request URI.</param>
    /// <param name="requiredNumberOfCalls">The number of times the request is expected to have been made. Defaults to at least once.</param>
    public static void VerifyHttpHead(this AutoMocker mocker, string? requestUri = null, int? requiredNumberOfCalls = null)
    {
        if (mocker is null)
            throw new ArgumentNullException(nameof(mocker));
        mocker.GetSubstitute<HttpMessageHandlerWrapper>().VerifyHttp(HttpMethod.Head, r => MatchesRequestUri(r.RequestUri, requestUri), requiredNumberOfCalls);
    }

    private static void VerifyHttp(this HttpMessageHandlerWrapper handler, HttpMethod method, Func<HttpRequestMessage, bool> match, int? requiredNumberOfCalls)
    {
        var received = requiredNumberOfCalls is { } n ? handler.Received(n) : handler.Received();
        received.SendAsyncPublic(Arg.Is<HttpRequestMessage>(r => r.Method == method && match(r)), Arg.Any<CancellationToken>());
    }

    private static bool MatchesRequestUri(Uri? requestUri, string? match)
        => match is null || (requestUri is not null && requestUri.AbsoluteUri.Contains(match));

    private static bool ContentEquals(HttpContent? content, string? match)
    {
        if (match is null)
            return true;

        return content is not null && content.ReadAsStringAsync().GetAwaiter().GetResult() == match;
    }
}
