using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace NSubstitute.AutoMock.Http;

/// <summary>
/// Provides extension methods for configuring mocked HTTP responses in unit tests using NSubstitute and
/// <see cref="HttpMessageHandlerWrapper" />. These methods enable fluent setup of various response types,
/// including plain text, byte arrays, and streams.
/// </summary>
/// <remarks>
/// These methods work by calling NSubstitute's <c>Returns</c> extension immediately after a call to
/// <c>SetupHttp*</c> (e.g. <see cref="HttpMessageHandlerExtensions.SetupHttpGet(HttpMessageHandlerWrapper, string?)" />),
/// following the pattern <c>substitute.Method(args).Returns(value)</c> that NSubstitute uses to configure the
/// most recently invoked call.
/// </remarks>
public static class HttpResponseExtensions
{
    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        HttpContent? content = null,
        string? mediaType = null,
        Action<HttpResponseMessage>? configure = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = content
        };

        if (content != null && mediaType != null)
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        configure?.Invoke(response);
        return response;
    }

    /// <summary>
    /// Specifies the response to return.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    public static NSubstitute.Core.ConfiguredCall ReturnsHttpResponse(
        this Task<HttpResponseMessage> setup,
        HttpStatusCode statusCode,
        Action<HttpResponseMessage>? configure = null)
    {
        return setup.Returns(Task.FromResult(CreateResponse(statusCode: statusCode, configure: configure)));
    }

    /// <summary>
    /// Specifies the response to return.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="content">The response content.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public static NSubstitute.Core.ConfiguredCall ReturnsHttpResponse(
        this Task<HttpResponseMessage> setup,
        HttpStatusCode statusCode,
        HttpContent content,
        Action<HttpResponseMessage>? configure = null)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        return setup.Returns(Task.FromResult(CreateResponse(statusCode: statusCode, content: content, configure: configure)));
    }

    /// <summary>
    /// Specifies the response to return, as <see cref="StringContent" />.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="content">The response body.</param>
    /// <param name="mediaType">The media type. Defaults to text/plain.</param>
    /// <param name="encoding">The character encoding. Defaults to <see cref="Encoding.UTF8" />.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public static NSubstitute.Core.ConfiguredCall ReturnsHttpResponse(
        this Task<HttpResponseMessage> setup,
        HttpStatusCode statusCode,
        string content,
        string? mediaType = null,
        Encoding? encoding = null,
        Action<HttpResponseMessage>? configure = null)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        return setup.Returns(Task.FromResult(CreateResponse(
            statusCode: statusCode,
            content: new StringContent(content, encoding, mediaType ?? "text/plain"),
            configure: configure)));
    }

    /// <summary>
    /// Specifies the response to return, as <see cref="StringContent" /> with <see cref="HttpStatusCode.OK" />.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="content">The response body.</param>
    /// <param name="mediaType">The media type. Defaults to text/plain.</param>
    /// <param name="encoding">The character encoding. Defaults to <see cref="Encoding.UTF8" />.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public static NSubstitute.Core.ConfiguredCall ReturnsHttpResponse(
        this Task<HttpResponseMessage> setup,
        string content,
        string? mediaType = null,
        Encoding? encoding = null,
        Action<HttpResponseMessage>? configure = null)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        return setup.Returns(Task.FromResult(CreateResponse(
            content: new StringContent(content, encoding, mediaType ?? "text/plain"),
            configure: configure)));
    }

    /// <summary>
    /// Specifies the response to return, as raw byte content.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="content">The response body bytes.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public static NSubstitute.Core.ConfiguredCall ReturnsResponse(
        this Task<HttpResponseMessage> setup,
        HttpStatusCode statusCode,
        byte[] content,
        string? mediaType = null,
        Action<HttpResponseMessage>? configure = null)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        return setup.Returns(Task.FromResult(CreateResponse(
            statusCode: statusCode,
            content: new ByteArrayContent(content),
            mediaType: mediaType,
            configure: configure)));
    }

    /// <summary>
    /// Specifies the response to return, as raw byte content with <see cref="HttpStatusCode.OK" />.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="content">The response body bytes.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public static NSubstitute.Core.ConfiguredCall ReturnsResponse(
        this Task<HttpResponseMessage> setup,
        byte[] content,
        string? mediaType = null,
        Action<HttpResponseMessage>? configure = null)
        => setup.ReturnsResponse(HttpStatusCode.OK, content, mediaType, configure);

    /// <summary>
    /// Specifies the response to return, as stream content.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="content">The response body stream.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    /// <remarks>
    /// If <paramref name="content" /> is seekable, its position is reset to 0 before each request so it can be
    /// reused across multiple requests.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public static NSubstitute.Core.ConfiguredCall ReturnsResponse(
        this Task<HttpResponseMessage> setup,
        HttpStatusCode statusCode,
        Stream content,
        string? mediaType = null,
        Action<HttpResponseMessage>? configure = null)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (content.CanSeek)
        {
            content.Seek(0, SeekOrigin.Begin);
        }

        return setup.Returns(Task.FromResult(CreateResponse(
            statusCode: statusCode,
            content: new StreamContent(content),
            mediaType: mediaType,
            configure: configure)));
    }

    /// <summary>
    /// Specifies the response to return, as stream content with <see cref="HttpStatusCode.OK" />.
    /// </summary>
    /// <param name="setup">The pending call, as returned from a <c>SetupHttp*</c> method.</param>
    /// <param name="content">The response body stream.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configure">An action to configure the response headers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public static NSubstitute.Core.ConfiguredCall ReturnsResponse(
        this Task<HttpResponseMessage> setup,
        Stream content,
        string? mediaType = null,
        Action<HttpResponseMessage>? configure = null)
        => setup.ReturnsResponse(HttpStatusCode.OK, content, mediaType, configure);
}
