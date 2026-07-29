namespace NSubstitute.AutoMock;

internal interface IInstance
{
    object? Value { get; }
    bool IsSubstitute { get; }
}
