namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// A resolver that resolves requested types with NSubstitute substitute instances.
/// </summary>
public class SubstituteResolver : ISubstituteResolver
{
    private readonly bool _callBase;

    /// <summary>
    /// Initializes an instance of <c>SubstituteResolver</c>.
    /// </summary>
    /// <param name="callBase">Whether class substitutes should be created as partial substitutes
    /// (<c>Substitute.ForPartsOf</c>) that call the base implementation for members without configured returns.</param>
    public SubstituteResolver(bool callBase)
    {
        _callBase = callBase;
    }

    /// <summary>
    /// Resolves requested types with substitute instances.
    /// </summary>
    /// <param name="context">The resolution context.</param>
    public void Resolve(SubstituteResolutionContext context)
    {
        if (context.RequestType == typeof(string)) return;

        if (context.AutoMocker.CreateSubstitute(
            context.RequestType,
            _callBase,
            context.ObjectGraphContext) is { } substitute)
        {
            context.Value = substitute;
        }
    }
}
