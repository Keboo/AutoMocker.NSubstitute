using System.Reflection;

namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// A resolver that resolves IEnumerable&lt;T&gt; requested types.
/// </summary>
public class EnumerableResolver : ISubstituteResolver
{
    /// <summary>
    /// Resolves IEnumerable&lt;T&gt; types.
    /// </summary>
    /// <param name="context">The resolution context.</param>
    public void Resolve(SubstituteResolutionContext context)
    {
        var (am, serviceType, _) = context;

        if (!serviceType.GetTypeInfo().IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            return;

        var elementType = serviceType.GetGenericArguments().Single();
        var array = Array.CreateInstance(elementType, 1);

        array.SetValue(am.Get(elementType), 0);
        context.Value = array;
    }
}
