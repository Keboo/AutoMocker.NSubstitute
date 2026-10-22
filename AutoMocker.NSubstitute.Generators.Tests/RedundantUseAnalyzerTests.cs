using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoMocker.NSubstitute.Generators.Tests;

[TestClass]
public class RedundantUseAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> RunAsync(string body)
    {
        string source = $$"""
            using System;
            using NSubstitute;
            using NSubstitute.AutoMock;
            using SubstituteAlias = NSubstitute.Substitute;

            public interface IService { int GetValue(); }
            public class Service : IService
            {
                public Service() { }
                public Service(int value) { }
                public virtual int GetValue() => 0;
            }
            public abstract class AbstractService { public abstract int GetValue(); }
            public class Lookalike
            {
                public void Use(object value) { }
                public IService GetSubstitute<T>() => Substitute.For<IService>();
                public static IService For<T>() => Substitute.For<IService>();
            }
            public class Tests
            {
                private readonly NSubstitute.AutoMock.AutoMocker _mocker = new();
                private NSubstitute.AutoMock.AutoMocker Mocker => new();
                private NSubstitute.AutoMock.AutoMocker GetMocker() => new();
                private void Configure(object value) { }
                public void Run(NSubstitute.AutoMock.AutoMocker parameter)
                {
                    var mocker = new NSubstitute.AutoMock.AutoMocker();
                    var other = new NSubstitute.AutoMock.AutoMocker();
                    {{body}}
                }
            }
            """;
        string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var references = trusted.Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        Compilation compilation = CSharpCompilation.Create("Test",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Assert.IsEmpty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray());

        var diagnostics = await compilation.WithAnalyzers([new RedundantUseAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        Assert.IsTrue(diagnostics.All(d => d.Id == Diagnostics.RedundantUse.DiagnosticId),
            string.Join(Environment.NewLine, diagnostics));
        return diagnostics;
    }

    [TestMethod]
    [DataRow("mocker.Use(Substitute.For<IService>());")]
    [DataRow("mocker.Use<IService>(Substitute.For<IService>());")]
    [DataRow("mocker.Use(typeof(IService), Substitute.For<IService>());")]
    [DataRow("mocker.Use(service: Substitute.For<IService>(), type: typeof(IService));")]
    [DataRow("mocker.Use(SubstituteAlias.For<IService>());")]
    [DataRow("var service = Substitute.For<IService>(); mocker.Use(service);")]
    public async Task UnconfiguredSubstitute_Reports(string body)
    {
        var diagnostics = await RunAsync(body);
        Assert.HasCount(1, diagnostics);
        Assert.AreEqual(DiagnosticSeverity.Info, diagnostics[0].Severity);
        StringAssert.Contains(diagnostics[0].GetMessage(), "WithSubstitute<T>()");
        Assert.AreEqual(body.Contains("Use<IService>", StringComparison.Ordinal) ? "Use<IService>" : "Use",
            diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan));
    }

    [TestMethod]
    [DataRow("mocker.Use(mocker.GetSubstitute<IService>());")]
    [DataRow("mocker.Use(mocker.GetSubstitute<IService>(true));")]
    [DataRow("mocker.Use(typeof(IService), mocker.GetSubstitute(typeof(IService)));")]
    [DataRow("mocker.Use(service: mocker.GetSubstitute(typeof(IService), true), type: typeof(IService));")]
    [DataRow("var service = mocker.GetSubstitute<IService>(); mocker.Use(service);")]
    [DataRow("var service = mocker.GetSubstitute<IService>(); service.GetValue().Returns(1); mocker.Use(service);")]
    [DataRow("_mocker.Use(this._mocker.GetSubstitute<IService>());")]
    [DataRow("var service = _mocker.GetSubstitute<IService>(); _mocker.Use(service);")]
    [DataRow("parameter.Use(parameter.GetSubstitute<IService>());")]
    [DataRow("mocker.Use(((mocker.GetSubstitute<IService>())));")]
    [DataRow("var same = mocker.Use(mocker.GetSubstitute<IService>());")]
    [DataRow("mocker.Use(mocker.GetSubstitute<IService>()).GetSubstitute<IDisposable>();")]
    public async Task RetainedSubstitute_Reports(string body)
    {
        var diagnostics = await RunAsync(body);
        Assert.HasCount(1, diagnostics);
        StringAssert.Contains(diagnostics[0].GetMessage(), "already retains");
        Assert.IsFalse(diagnostics[0].GetMessage().Contains("WithSubstitute", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("mocker.Use(Substitute.For<Service>());")]
    [DataRow("mocker.Use<Service>(Substitute.For<Service>());")]
    [DataRow("mocker.Use(typeof(Service), Substitute.For<Service>());")]
    [DataRow("var service = Substitute.For<Service>(); mocker.Use(service);")]
    [DataRow("mocker.Use(Substitute.For<AbstractService>());")]
    [DataRow("mocker.Use(mocker.GetSubstitute<Service>());")]
    [DataRow("mocker.Use(typeof(Service), mocker.GetSubstitute(typeof(Service)));")]
    [DataRow("var service = mocker.GetSubstitute<Service>(); mocker.Use(service);")]
    [DataRow("mocker.Use(mocker.GetSubstitute<AbstractService>());")]
    public async Task ClassTarget_DoesNotReport(string body)
    {
        Assert.IsEmpty(await RunAsync(body));
    }

    [TestMethod]
    [DataRow("mocker.Use(Substitute.For<Service>(42));")]
    [DataRow("mocker.Use(Substitute.ForPartsOf<Service>(42));")]
    [DataRow("mocker.Use(Substitute.For<IService, IDisposable>());")]
    [DataRow("var service = Substitute.For<IService>(); service.GetValue().Returns(1); mocker.Use(service);")]
    [DataRow("var service = Substitute.For<IService>(); mocker.Use(service); service.GetValue().Returns(1);")]
    [DataRow("var service = Substitute.For<IService>(); mocker.Use(service); service.Received().GetValue();")]
    [DataRow("var service = Substitute.For<IService>(); Configure(service); mocker.Use(service);")]
    [DataRow("var service = Substitute.For<IService>(); var alias = service; mocker.Use(service);")]
    [DataRow("var service = mocker.GetSubstitute<IService>(); service = Substitute.For<IService>(); mocker.Use(service);")]
    [DataRow("mocker.Use(other.GetSubstitute<IService>());")]
    [DataRow("var service = other.GetSubstitute<IService>(); mocker.Use(service);")]
    [DataRow("var service = mocker.GetSubstitute<IService>(); mocker = other; mocker.Use(service);")]
    [DataRow("var service = mocker.GetSubstitute<IService>(); mocker.Use<IService>(new Service(1)); mocker.Use(service);")]
    [DataRow("mocker.Use<object>(mocker.GetSubstitute<IService>());")]
    [DataRow("mocker.Use(typeof(object), mocker.GetSubstitute(typeof(IService)));")]
    [DataRow("mocker.Use<object>(Substitute.For<IService>());")]
    [DataRow("mocker.Use(new Service(1));")]
    [DataRow("mocker.WithSubstitute<IService>();")]
    [DataRow("mocker.WithSubstitute(typeof(IService));")]
    [DataRow("mocker.Use<IService>(() => new Service(1));")]
    [DataRow("new Lookalike().Use(Substitute.For<IService>());")]
    [DataRow("mocker.Use(new Lookalike().GetSubstitute<IService>());")]
    [DataRow("mocker.Use(Lookalike.For<IService>());")]
    [DataRow("Mocker.Use(Mocker.GetSubstitute<IService>());")]
    [DataRow("GetMocker().Use(GetMocker().GetSubstitute<IService>());")]
    [DataRow("IService service = null!; mocker.Use(service);")]
    [DataRow("var service = Substitute.For<IService>(); Action configure = () => service.GetValue().Returns(1); mocker.Use(service);")]
    public async Task IntentionalOrUnknownRegistration_DoesNotReport(string body)
    {
        Assert.IsEmpty(await RunAsync(body));
    }
}
