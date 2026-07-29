using NSubstitute.AutoMock.Extensions;

namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// A resolver that resolves Func&lt;&gt; dependency types
/// </summary>
public class FuncResolver : ISubstituteResolver
{
    /// <summary>
    /// Resolves requested Func&lt;&gt; types.
    /// </summary>
    /// <param name="context">The resolution context.</param>
    public void Resolve(SubstituteResolutionContext context)
    {
        var (am, serviceType, _) = context;

        if (am.TryCompileGetter(serviceType, out var @delegate))
            context.Value = @delegate;
    }
}
