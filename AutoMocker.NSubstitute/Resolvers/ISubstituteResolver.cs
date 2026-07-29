namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// Base interface for all substitute resolvers.
/// </summary>
public interface ISubstituteResolver
{
    /// <summary>
    /// Resolve a dependency.
    /// </summary>
    /// <param name="context">The context to be used while resolving the dependency.</param>
    void Resolve(SubstituteResolutionContext context);
}
