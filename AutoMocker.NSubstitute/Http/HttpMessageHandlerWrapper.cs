using System.Net.Http;

namespace NSubstitute.AutoMock.Http;

/// <summary>
/// A subclass of <see cref="HttpMessageHandler" /> that exposes the protected <c>SendAsync</c>
/// method as a public abstract member.
/// </summary>
/// <remarks>
/// Unlike Moq, NSubstitute has no mechanism for configuring or verifying protected members.
/// Substituting this wrapper class (e.g. via <see cref="AutoMocker.GetSubstitute{TService}()" />)
/// allows tests to configure <see cref="SendAsyncPublic" /> using standard NSubstitute syntax
/// such as <c>Returns</c> and <c>Received</c>.
/// </remarks>
public abstract class HttpMessageHandlerWrapper : HttpMessageHandler
{
    /// <summary>
    /// Sends an HTTP request. Configure this member with NSubstitute (e.g. <c>Returns</c>)
    /// to control the response produced by the wrapped <see cref="HttpMessageHandler" />.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    public abstract Task<HttpResponseMessage> SendAsyncPublic(HttpRequestMessage request, CancellationToken cancellationToken);

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => SendAsyncPublic(request, cancellationToken);
}
