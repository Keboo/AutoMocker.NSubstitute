using System.Reflection;
using NSubstitute.AutoMock.Extensions;

namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// A resolver that resolves Lazy&lt;T&gt; requested types.
/// </summary>
public class LazyResolver : ISubstituteResolver
{
    /// <summary>
    /// Resolves Lazy&lt;T&gt; types.
    /// </summary>
    /// <param name="context">The resolution context.</param>
    public void Resolve(SubstituteResolutionContext context)
    {
        var (am, serviceType, _) = context;

        if (!serviceType.GetTypeInfo().IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(Lazy<>))
            return;

        var returnType = serviceType.GetGenericArguments().Single();
        if (am.TryCompileGetter(typeof(Func<>).MakeGenericType(returnType), out var @delegate))
        {
            var lazyType = typeof(Lazy<>).MakeGenericType(returnType);
            context.Value = Activator.CreateInstance(lazyType, @delegate);
        }
    }
}
