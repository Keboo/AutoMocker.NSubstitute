using System.Text;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AutoMocker.NSubstitute.Generators.Tests.CSharpSourceGeneratorVerifier<AutoMocker.NSubstitute.Generators.UnitTestSourceGenerator>;

namespace AutoMocker.NSubstitute.Generators.Tests;

[TestClass]
public class GeneratorsTests
{
    [TestMethod]
    public async Task Generation_WithProjectThatDoesNotReferenceAutoMocker_ProducesDiagnosticWarning()
    {
        var code = """
            // Empty file
            """;
        var expectedResult =
            DiagnosticResult.CompilerWarning(Diagnostics.MustReferenceAutoMock.DiagnosticId);
        await new VerifyCS.Test
        {
            TestCode = code,
            ReferenceAutoMocker = false,
            ExpectedDiagnostics =
            {
               expectedResult
            }
        }.RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Generation_WithDecoratedNonPartialClass_ProducesDiagnosticError()
    {
        var code = """

            using NSubstitute.AutoMock;

            namespace TestNamespace;

            [ConstructorTests(TargetType = typeof(Controller))]
            public class ControllerTests
            {
                
            }

            public class Controller { }

            """;
        var expectedResult =
            DiagnosticResult.CompilerError(Diagnostics.TestClassesMustBePartial.DiagnosticId)
                        .WithSpan(6, 1, 10, 2)
                        .WithArguments("TestNamespace.ControllerTests");
        await new VerifyCS.Test
        {
            TestCode = code,
            ExpectedDiagnostics =
            {
                expectedResult
            }
        }.RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Generation_WithNoTargetTypeSpecified_ProducesDiagnosticError()
    {
        var code = """

            using NSubstitute.AutoMock;

            namespace TestNamespace;

            [ConstructorTests]
            public class ControllerTests
            {
                
            }

            public class Controller { }

            """;
        var expectedResult =
            DiagnosticResult.CompilerError(Diagnostics.MustSpecifyTargetType.DiagnosticId)
                        .WithSpan(6, 2, 6, 18)
                        .WithArguments("TestNamespace.ControllerTests");
        await new VerifyCS.Test
        {
            TestCode = code,
            ExpectedDiagnostics =
            {
                expectedResult
            }
        }.RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    [Description("Issue 142")]
    public async Task Generation_WithGenericParameter_RemovesInvalidCharactersFromTestsName()
    {
        var code = """
            using NSubstitute.AutoMock;

            namespace TestNamespace;

            [ConstructorTests(typeof(Controller))]
            public partial class ControllerTests
            {
    
            }

            public class Controller
            {
                public Controller(ILogger<Controller> logger) { }
            }

            public interface ILogger<Controller> { }
            """;
        string expected = """
            namespace TestNamespace
            {
                partial class ControllerTests
                {
                    partial void AutoMockerTestSetup(NSubstitute.AutoMock.AutoMocker mocker, string testName);

                    partial void ControllerConstructor_WithNullILoggerController_ThrowsArgumentNullExceptionSetup(NSubstitute.AutoMock.AutoMocker mocker);

                    public void ControllerConstructor_WithNullILoggerController_ThrowsArgumentNullException()
                    {
                        NSubstitute.AutoMock.AutoMocker mocker = new NSubstitute.AutoMock.AutoMocker();
                        AutoMockerTestSetup(mocker, "ControllerConstructor_WithNullILoggerController_ThrowsArgumentNullException");
                        ControllerConstructor_WithNullILoggerController_ThrowsArgumentNullExceptionSetup(mocker);
                        using(System.IDisposable __mockerDisposable = mocker.AsDisposable())
                        {
                        }
                    }

                }
            }

            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            TestState =
            {
                GeneratedSources =
                {
                    GetSourceFile(expected, "TestNamespace.ControllerTests.g.cs")
                }
            }

        }.RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Generation_WithValueTypeParameter_DoesNotGenerateTest()
    {
        var code = """
            using NSubstitute.AutoMock;
            using System.Threading;

            namespace TestNamespace;

            [ConstructorTests(typeof(Controller))]
            public partial class ControllerTests
            {
                
            }

            public class Controller
            {
                public Controller(CancellationToken token) { }
            }

            """;
        string expected = """
            namespace TestNamespace
            {
                partial class ControllerTests
                {
                    partial void AutoMockerTestSetup(NSubstitute.AutoMock.AutoMocker mocker, string testName);

                }
            }

            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            TestState =
            {
                GeneratedSources =
                {
                    GetSourceFile(expected, "TestNamespace.ControllerTests.g.cs")
                }
            }

        }.RunAsync(TestContext.CancellationToken);
    }

    private static (string FileName, SourceText SourceText) GetSourceFile(string content, string fileName)
    {
        return (Path.Combine("AutoMocker.NSubstitute.Generators", "AutoMocker.NSubstitute.Generators.UnitTestSourceGenerator", fileName), SourceText.From(content, Encoding.UTF8, SourceHashAlgorithm.Sha256));
    }

    public TestContext TestContext { get; set; }
}
