using System.Text;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

using VerifyCS = AutoMocker.NSubstitute.Generators.Tests.CSharpSourceGeneratorVerifier<AutoMocker.NSubstitute.Generators.HttpClientFactoryResolverSourceGenerator>;

namespace AutoMocker.NSubstitute.Generators.Tests;

[TestClass]
public class HttpClientFactoryGeneratorTests
{
    private const string ExpectedHttpClientFactoryGeneratedFile =
        """
        #nullable enable
        using System;
        using System.Collections.Concurrent;
        using System.Net.Http;
        using Microsoft.Extensions.Http;
        using NSubstitute.AutoMock.Http;
        using NSubstitute.AutoMock.Resolvers;

        namespace NSubstitute.AutoMock;

        public static class AutoMockerHttpClientFactoryExtensions
        {
            public static AutoMocker WithHttpClientFactory(this AutoMocker mocker)
            {
                if (mocker is null)
                    throw new ArgumentNullException(nameof(mocker));

                mocker.InsertResolverAfter<CacheResolver>(new HttpClientFactoryResolver());
                return mocker;
            }

            private sealed class HttpClientFactoryResolver : ISubstituteResolver
            {
                public void Resolve(SubstituteResolutionContext context)
                {
                    if (context.RequestType == typeof(IHttpClientFactory))
                        context.Value = new TestableHttpClientFactory(context.AutoMocker);
                }
            }

            private sealed class TestableHttpClientFactory(AutoMocker autoMocker) : IHttpClientFactory
            {
                private readonly ConcurrentDictionary<string, HttpClient> _clients = new(StringComparer.Ordinal);

                public HttpClient CreateClient(string name)
                    => _clients.GetOrAdd(name ?? string.Empty, _ =>
                        new HttpClient(autoMocker.GetSubstitute<HttpMessageHandlerWrapper>(), disposeHandler: false));
            }
        }
        """;

    [TestMethod]
    public async Task WhenHttpAssemblyIsNotReferenced_NoGenerationOccurs()
    {
        await new VerifyCS.Test
        {
            TestCode = "",
            ReferenceHttp = false
        }.RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task WhenHttpAssemblyIsReferenced_ExtensionMethodIsGenerated()
    {
        await new VerifyCS.Test
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            ReferenceHttp = true,
            TestCode = "",
            TestState =
            {
                GeneratedSources =
                {
                    GetSourceFile(ExpectedHttpClientFactoryGeneratedFile, "AutoMockerHttpClientFactoryExtensions.g.cs")
                }
            }
        }.RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task WhenGeneratorIsDisabled_NoGenerationOccurs()
    {
        var test = new VerifyCS.Test
        {
            ReferenceHttp = true,
            TestCode = ""
        };
        test.SetGlobalOption("build_property.EnableAutoMockerNSubstituteHttpClientFactoryGenerator", "false");
        await test.RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task WhenGeneratorIsExplicitlyEnabled_ExtensionMethodIsGenerated()
    {
        var test = new VerifyCS.Test
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            ReferenceHttp = true,
            TestCode = "",
            TestState =
            {
                GeneratedSources =
                {
                    GetSourceFile(ExpectedHttpClientFactoryGeneratedFile, "AutoMockerHttpClientFactoryExtensions.g.cs")
                }
            }
        };
        test.SetGlobalOption("build_property.EnableAutoMockerNSubstituteHttpClientFactoryGenerator", "true");
        await test.RunAsync(TestContext.CancellationToken);
    }

    private static (string FileName, SourceText SourceText) GetSourceFile(string content, string fileName)
    {
        return (Path.Combine("AutoMocker.NSubstitute.Generators", "AutoMocker.NSubstitute.Generators.HttpClientFactoryResolverSourceGenerator", fileName), SourceText.From(content, Encoding.UTF8, SourceHashAlgorithm.Sha256));
    }

    public TestContext TestContext { get; set; } = null!;
}
