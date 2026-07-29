namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// Contains all of the callbacks registered with AutoMocker.
/// </summary>
public class CallbackResolver : ISubstituteResolver
{
    private NonBlocking.ConcurrentDictionary<Type, Func<AutoMocker, object?>> CallbackMap { get; } = new();

    /// <inheritdoc />
    public void Resolve(SubstituteResolutionContext context)
    {
        if (CallbackMap.TryGetValue(context.RequestType, out Func<AutoMocker, object?>? callback))
        {
            context.Value = callback(context.AutoMocker);
        }
    }


    /// <summary>
    /// Adds a callback to the resolver.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="callback">The callback to register.</param>
    public void AddCallback<TService>(Func<AutoMocker, TService?> callback)
    {
        CallbackMap[typeof(TService)] = am => callback(am);
    }
}
