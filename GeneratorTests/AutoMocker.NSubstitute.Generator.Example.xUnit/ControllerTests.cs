namespace NSubstitute.AutoMock.Generator.Example.xUnit3;

//[ConstructorTests(TargetType = typeof(Controller))]
[ConstructorTests(typeof(Controller))]
public partial class ControllerTests
{
    partial void AutoMockerTestSetup(NSubstitute.AutoMock.AutoMocker mocker, string testName)
    {
        mocker.Use<string>("");
    }
}

