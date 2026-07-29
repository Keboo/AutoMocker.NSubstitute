namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// A resolver that can provide a <see cref="CancellationToken"/>
/// </summary>
public class CancellationTokenResolver : ISubstituteResolver
{
    /// <inheritdoc />
    public void Resolve(SubstituteResolutionContext context)
    {
        if (context.RequestType == typeof(CancellationToken))
        {
            if (context.AutoMocker.ResolvedObjects?.TryGetValue(typeof(CancellationTokenSource), out object? ctsObject) == true &&
                ctsObject is CancellationTokenSource cts)
            {
                context.Value = cts.Token;
            }
            else
            {
                context.Value = CancellationToken.None;
            }
        }
    }
}
