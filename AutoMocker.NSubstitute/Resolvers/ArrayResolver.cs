namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// Provides a means to create arrays.
/// </summary>
public class ArrayResolver : ISubstituteResolver
{
    /// <inheritdoc />
    public void Resolve(SubstituteResolutionContext context)
    {
        if (context.RequestType.IsArray && context.RequestType != typeof(string))
        {
            Type elmType = context.RequestType.GetElementType() ?? throw new InvalidOperationException($"Could not determine element type for '{context.RequestType}'");
            SubstituteArrayInstance arrayInstance = new(elmType);
            if (context.AutoMocker.TryGet(elmType, context.ObjectGraphContext, out IInstance? instance, out bool noCache))
            {
                arrayInstance.Add(instance);
                context.NoCache = noCache;
            }
            context.Value = arrayInstance;
        }
    }
}
