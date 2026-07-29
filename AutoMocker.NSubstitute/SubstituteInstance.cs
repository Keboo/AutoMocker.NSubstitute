using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NSubstitute.AutoMock;

[DebuggerDisplay("Substitute: {Value.GetType().Name,nq}")]
internal sealed class SubstituteInstance(object substitute) : IInstance
{
    [NotNull]
    public object? Value { get; } = substitute ?? throw new ArgumentNullException(nameof(substitute));
    public bool IsSubstitute => true;
}
