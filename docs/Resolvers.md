# Resolvers

Namespace: `NSubstitute.AutoMock.Resolvers`. [Documentation index](README.md).

## Pipeline

`AutoMocker.Resolvers` is an ordered `IList<ISubstituteResolver>`. The first resolver that assigns `context.Value` wins, even if it assigns null. Leave `Value` untouched to decline a request.

```csharp
AutoMocker InsertResolverBefore<TResolver>(ISubstituteResolver resolver)
    where TResolver : ISubstituteResolver
AutoMocker InsertResolverAfter<TResolver>(ISubstituteResolver resolver)
    where TResolver : ISubstituteResolver
```

Both return the container and insert relative to the first matching resolver. A missing target throws `InvalidOperationException`. Put custom resolvers after `CacheResolver` to preserve explicit registrations.

```csharp
using NSubstitute.AutoMock;
using NSubstitute.AutoMock.Resolvers;

var mocker = new AutoMocker();
mocker.InsertResolverBefore<SubstituteResolver>(new TestIdResolver());
Guid id = mocker.Get<Guid>();

public sealed class TestIdResolver : ISubstituteResolver
{
    public void Resolve(SubstituteResolutionContext context)
    {
        if (context.RequestType != typeof(Guid))
            return;

        context.Value = Guid.NewGuid();
        context.NoCache = true;
    }
}
```

Resolver exceptions are recorded in `ObjectGraphContext.DiagnosticMessages` and resolution continues. Inspect diagnostics when troubleshooting unsuccessful construction.

## ISubstituteResolver.Resolve

```csharp
void Resolve(SubstituteResolutionContext context)
```

Implement this interface to supply custom values, as above. Built-in implementations expose the same method.

## SubstituteResolutionContext

```csharp
SubstituteResolutionContext(
    AutoMocker autoMocker, Type requestType, ObjectGraphContext objectGraphContext)
void Deconstruct(out AutoMocker autoMocker, out Type type, out object? value)
```

All constructor arguments must be non-null.

| Property | Access | Meaning |
| --- | --- | --- |
| `AutoMocker` | get | Container servicing this request |
| `RequestType` | get | Requested service type |
| `ObjectGraphContext` | get | Shared construction state and target parameter |
| `Value` | get/set | Resolved object; assigning null also completes resolution |
| `NoCache` | get/set | Skip caching this result; defaults to `false` |

```csharp
var context = new SubstituteResolutionContext(
    mocker, typeof(Guid), new ObjectGraphContext(enablePrivate: false));
new TestIdResolver().Resolve(context);
var (container, requestedType, value) = context;
```

## Built-in resolvers

Every concrete resolver has a public parameterless constructor except `SubstituteResolver(bool callBase)`. Each implements `Resolve(SubstituteResolutionContext)` (directly or through `SimpleTypeResolver<T>`).

Default order:

| Resolver | Resolution |
| --- | --- |
| `CacheResolver` | Previously registered or cached value |
| `CallbackResolver` | Factory registered with `Use` or `AddCallback` |
| `SelfResolver` | The current `AutoMocker`; skips explicit substitute creation |
| `ArrayResolver` | Array containing a resolved element, or empty if the element cannot resolve |
| `AutoMockerDisposableResolver` | Internal cleanup handle returned by `AsDisposable()` |
| `EnumerableResolver` | `IEnumerable<T>` containing one resolved `T` |
| `LazyResolver` | `Lazy<T>` that calls the container on first value access |
| `FuncResolver` | `Func<..., TResult>` that calls `Get<TResult>()` on invocation; input arguments are ignored |
| `CancellationTokenResolver` | Token from a cached `CancellationTokenSource`, otherwise `CancellationToken.None` |
| `HttpMessageHandlerWrapperResolver` | Partial handler substitute with an HTTP 200 fallback, independent of `CallBase` |
| `HttpClientResolver` | `HttpClient` using that handler without owning it |
| `SubstituteResolver` | NSubstitute object using the supplied `callBase`; does not resolve `string` |

`InstanceResolver()` is opt-in: it constructs real objects with resolved constructor dependencies. Insert it before `SubstituteResolver` when real instances should take precedence.

```csharp
var mocker = new AutoMocker();
var callbacks = mocker.Resolvers.OfType<CallbackResolver>().Single();
callbacks.AddCallback<string>(_ => "test");
string text = mocker.Get<string>();

using var source = new CancellationTokenSource();
mocker.Use(source);
CancellationToken token = mocker.Get<CancellationToken>();

var realObjects = new AutoMocker();
realObjects.InsertResolverBefore<SubstituteResolver>(new InstanceResolver());
Store store = realObjects.Get<Store>();

var partialResolver = new SubstituteResolver(callBase: true);
```

### CallbackResolver.AddCallback

```csharp
void AddCallback<TService>(Func<AutoMocker, TService?> callback)
```

Registers or replaces a factory for the requested type. The normal pipeline caches its result, so register before first resolution. Prefer `AutoMocker.Use` unless directly customizing this resolver.

### SimpleTypeResolver&lt;T&gt;

An abstract base for resolvers centered on one type.

```csharp
public virtual void Resolve(SubstituteResolutionContext context)
protected abstract T GetValue(SubstituteResolutionContext context)
protected bool IncludeBaseTypes { get; set; } // default: true
protected bool IncludeInterfaces { get; set; } // default: true
```

`Resolve` matches `T` exactly, implemented interfaces when `IncludeInterfaces` is true, and requests satisfying `typeof(T).IsAssignableFrom(context.RequestType)` when `IncludeBaseTypes` is true. Disable both flags for exact matching.

```csharp
public sealed class ConnectionStringResolver : SimpleTypeResolver<string>
{
    public ConnectionStringResolver()
    {
        IncludeBaseTypes = false;
        IncludeInterfaces = false;
    }

    protected override string GetValue(SubstituteResolutionContext context)
        => "test-connection";
}
```

`SelfResolver`, `HttpClientResolver`, and `HttpMessageHandlerWrapperResolver` override `GetValue`; `SelfResolver` also overrides `Resolve`.
