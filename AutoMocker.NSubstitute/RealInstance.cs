using System.Diagnostics;

namespace NSubstitute.AutoMock;

[DebuggerDisplay("Real: {Value?.GetType().Name ?? \"null\",nq}")]
internal sealed class RealInstance(object? value) : IInstance
{
    public object? Value { get; } = value;
    public bool IsSubstitute => false;
}
