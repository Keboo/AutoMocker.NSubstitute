# NSubstitute usage in AutoMocker.NSubstitute

AutoMocker.NSubstitute creates and registers NSubstitute substitutes for constructor
dependencies. Once a substitute has been retrieved, configure and verify it with the
normal NSubstitute API.

## Native NSubstitute interactions

The following interactions are the same as they are with a substitute created directly
with `Substitute.For<T>()`:

```csharp
var dependency = Substitute.For<IEngineProvider>();
mocker.Use(dependency);

dependency.GetEngine().Returns(new Engine());
_ = dependency.GetEngine();
dependency.Received(1).GetEngine();
dependency.DidNotReceive().Reset();
```

Use NSubstitute argument matchers when a call should match a range of values:

```csharp
dependency.Load(Arg.Any<string>())
    .Returns(new Engine());

dependency.Received().Load(Arg.Is<string>(name => name.StartsWith("test-")));
```

As with NSubstitute itself, if one argument in a call uses an argument matcher, use
matchers for the other arguments in that call as well. `Returns`, `Received`, `DidNotReceive`, callbacks, and call inspection all operate on
the substitute instance directly.

## AutoMocker interactions

`AutoMocker` is the composition layer around NSubstitute; it is not a replacement for
NSubstitute's substitute API.

| AutoMocker call | Purpose | Difference from direct NSubstitute |
| --- | --- | --- |
| `CreateInstance<T>()` | Construct the system under test and resolve its constructor dependencies | Creates substitutes automatically instead of requiring each `Substitute.For<T>()` call |
| `GetOrCreateSubstitute<T>()` | Retrieve the substitute registered for a service, or create it if absent | Makes substitute creation an explicit container operation; it is not a `Mock<T>` wrapper |
| `GetSubstitute<T>()` | Compatibility alias for `GetOrCreateSubstitute<T>()` | Retained for existing callers; prefer the explicit `GetOrCreateSubstitute<T>()` name in new code |
| `Use<T>(value)` | Register a real object or an existing substitute | Replaces the value resolved for that service |
| `With<TService, TImplementation>()` | Create and register a real implementation | Uses the container to construct the implementation |
| `Combine(...)` | Register one substitute for multiple service types | Creates a new multi-type substitute and replaces cached values for those types |

`GetOrCreateSubstitute<T>()` only returns an NSubstitute substitute. If a real
instance was supplied with `Use`, retrieve it with `Get<T>()`
instead. This keeps the distinction between a real dependency and a substitute
explicit:

```csharp
var realClock = new TestClock();
mocker.Use<IClock>(realClock);

Assert.Same(realClock, mocker.Get<IClock>());
// Throws: IClock is registered as a real instance, not a substitute.
// mocker.GetOrCreateSubstitute<IClock>();
```

When an interface or mockable class has not been registered, requesting it with
`GetOrCreateSubstitute<T>()` creates and caches a substitute. This is a container
convenience; it does not change how that substitute handles calls, return values, or
received-call assertions.

## Partial substitutes and `CallBase`

`new AutoMocker(callBase: true)` creates class substitutes as partial substitutes,
equivalent to `Substitute.ForPartsOf<T>()`, when a class dependency is generated.
`CreateSelfSubstitute<T>()` and `WithSelfSubstitute<...>()` always create partial
substitutes. Configure virtual members before exercising the system under test when
the base implementation would have side effects or would otherwise make the test
non-deterministic.

Interfaces do not have base implementations, so `CallBase` has no effect on interface
substitutes.

## HTTP helpers

`SetupHttp*` and `VerifyHttp*` are AutoMocker convenience methods, not NSubstitute
methods. They adapt the protected `HttpMessageHandler.SendAsync` call to the public
`HttpMessageHandlerWrapper.SendAsyncPublic` member and then use ordinary NSubstitute
`Returns` and `Received` interactions underneath.

For complete control, use the wrapper directly:

```csharp
var handler = mocker.GetOrCreateSubstitute<HttpMessageHandlerWrapper>();

handler.SendAsyncPublic(
        Arg.Any<HttpRequestMessage>(),
        Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

await handler.Received(1).SendAsyncPublic(
    Arg.Is<HttpRequestMessage>(request => request.Method == HttpMethod.Get),
    Arg.Any<CancellationToken>());
```

The HTTP resolver supplies an HTTP 200 response for otherwise-unconfigured requests.
Tests that need to prove a request was not configured should verify the request
separately; an unconfigured HTTP call does not fail merely because no setup exists.

## Practical rule

Use `Substitute.For<T>()` and `Use(...)` when you want to follow NSubstitute's explicit
create/configure/register flow. Use `AutoMocker` to assemble the object graph and
`GetOrCreateSubstitute<T>()` when the container should create and cache the substitute.
Configure and assert on the returned substitute using the normal NSubstitute API.
