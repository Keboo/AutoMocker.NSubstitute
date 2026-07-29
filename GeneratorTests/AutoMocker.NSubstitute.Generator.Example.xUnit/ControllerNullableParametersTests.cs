namespace NSubstitute.AutoMock.Generator.Example.xUnit3;

[ConstructorTests(typeof(ControllerWithSomeNullableParameters), Behavior = TestGenerationBehavior.IgnoreNullableParameters)]
public partial class ControllerNullableParametersTests
{
    partial void AutoMockerTestSetup(NSubstitute.AutoMock.AutoMocker mocker, string testName)
    {
        mocker.Use<string>("");
        mocker.Use<int?>(42);
        mocker.Use<int>(24);
    }
}
