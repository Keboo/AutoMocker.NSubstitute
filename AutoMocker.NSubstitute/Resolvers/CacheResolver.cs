namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// Provides the cache used by AutoMocker.
/// </summary>
public class CacheResolver : ISubstituteResolver
{
    internal NonBlocking.ConcurrentDictionary<Type, IInstance> TypeMap { get; } = new();

    /// <inheritdoc />
    public void Resolve(SubstituteResolutionContext context)
    {
        if (TypeMap.TryGetValue(context.RequestType, out IInstance instance))
        {
            context.Value = instance;
        }
    }
}
