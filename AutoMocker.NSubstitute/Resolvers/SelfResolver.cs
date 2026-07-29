namespace NSubstitute.AutoMock.Resolvers;

/// <summary>
/// Resolves calls to retrieve AutoMocker with itself.
/// </summary>
public class SelfResolver : SimpleTypeResolver<AutoMocker>
{
    /// <inheritdoc />
    public override void Resolve(SubstituteResolutionContext context)
    {
        if (context.ObjectGraphContext.IsSubstituteCreation)
        {
            return;
        }
        base.Resolve(context);
    }

    /// <inheritdoc />
    protected override AutoMocker GetValue(SubstituteResolutionContext context)
        => context.AutoMocker;
}
