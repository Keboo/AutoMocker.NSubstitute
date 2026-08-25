using System.Net;
using System.Net.Http;
using NSubstitute.AutoMock.Http;

namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// A resolver that provides <see cref="HttpClient" /> instances backed by a substituted
/// <see cref="HttpMessageHandlerWrapper" />.
/// </summary>
public class HttpClientResolver : SimpleTypeResolver<HttpClient>
{
    /// <inheritdoc />
    protected override HttpClient GetValue(SubstituteResolutionContext context)
    {
        var handler = context.AutoMocker.GetSubstitute<HttpMessageHandlerWrapper>();
        return new HttpClient(handler, disposeHandler: false);
    }
}

/// <summary>
/// A resolver that provides <see cref="HttpMessageHandlerWrapper" /> substitutes as partial
/// substitutes (equivalent to <c>Substitute.ForPartsOf</c>), regardless of the AutoMocker's
/// global <see cref="AutoMocker.CallBase" /> setting. This is required so that the wrapper's
/// concrete <c>SendAsync</c> override runs and delegates to the substituted
/// <see cref="HttpMessageHandlerWrapper.SendAsyncPublic" /> member.
/// </summary>
public class HttpMessageHandlerWrapperResolver : SimpleTypeResolver<HttpMessageHandlerWrapper>
{
    /// <inheritdoc />
    protected override HttpMessageHandlerWrapper GetValue(SubstituteResolutionContext context)
    {
        var handler = (HttpMessageHandlerWrapper)(context.AutoMocker.CreateSubstitute(typeof(HttpMessageHandlerWrapper), callBase: true, context.ObjectGraphContext)
            ?? throw new InvalidOperationException($"Unable to create a substitute for {typeof(HttpMessageHandlerWrapper).FullName}"));

        // Provide a default "loose" response for any unconfigured request so that tests
        // that don't care about the HTTP response don't need to set one up explicitly.
        // Setups added afterwards (e.g. via SetupHttpGet) take precedence for matching calls
        // because NSubstitute prefers the most specific/most recently configured matcher.
        handler
            .SendAsyncPublic(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            }));

        return handler;
    }
}
