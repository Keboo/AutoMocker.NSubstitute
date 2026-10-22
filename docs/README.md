# AutoMocker.NSubstitute documentation

| Reference | APIs |
| --- | --- |
| [AutoMocker](AutoMocker.md) | Construction, registration, retrieval, partial substitutes, combining services, disposal |
| [Resolvers](Resolvers.md) | Resolver pipeline, built-in resolvers, resolution contexts |
| [Diagnostics and constructor tests](Diagnostics.md) | `ObjectCreationException`, `ConstructorTestsAttribute`, `TestGenerationBehavior` |
| [HTTP](HttpClient.md) | `HttpMessageHandlerExtensions`, `HttpResponseExtensions`, `HttpMessageHandlerWrapper` |
| [Generated extensions](GeneratedExtensions.md) | Conditional extension signatures, grouped keyed-service overloads, telemetry accessors |
| [Source generators](SourceGenerators.md) | Conditional options, logging, time, telemetry, keyed services, metrics, HTTP factory extensions |
| [Compiler integration](CompilerIntegration.md) | Public Roslyn generator, analyzer, diagnostic, and model APIs |

Overloads are grouped by method. Signatures omit `public` and extension receivers where noted. Examples use application-specific types such as `IStore` and `Store`; replace these with your own types.

Start with `using NSubstitute.AutoMock;` and `using NSubstitute;`. HTTP extensions also require `using NSubstitute.AutoMock.Http;`.
