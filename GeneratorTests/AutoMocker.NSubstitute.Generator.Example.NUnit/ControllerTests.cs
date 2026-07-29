namespace NSubstitute.AutoMock.Generator.Example.NUnit;

[ConstructorTests(TargetType = typeof(Controller))]
public partial class ControllerTests
{
    partial void AutoMockerTestSetup(NSubstitute.AutoMock.AutoMocker mocker, string testName)
    {
        mocker.Use<string>("");
    }
}
