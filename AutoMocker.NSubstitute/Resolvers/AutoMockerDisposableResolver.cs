namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// A resolver that resolves instances for <see cref="IAutoMockerDisposable"/>.
/// </summary>
public class AutoMockerDisposableResolver : ISubstituteResolver
{
    /// <summary>
    /// Resolve the <see cref="IAutoMockerDisposable"/> if one has not been found.
    /// </summary>
    /// <param name="context"></param>
    public void Resolve(SubstituteResolutionContext context)
    {
        if (context.RequestType.IsAssignableFrom(typeof(IAutoMockerDisposable)))
        {
            context.Value = new AutoMockerDisposable(context.AutoMocker);
        }
    }
}
