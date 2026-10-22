# AutoMocker

Namespace: `NSubstitute.AutoMock`. [Documentation index](README.md).

## Constructors and properties

```csharp
AutoMocker()
AutoMocker(bool callBase)
```

`callBase` defaults to `false`. When `true`, class dependencies are partial substitutes: unconfigured virtual members run their real implementation. Interfaces remain substitutes.

| Property | Type | Meaning |
| --- | --- | --- |
| `CallBase` | `bool` | Constructor setting; read-only |
| `Resolvers` | `IList<ISubstituteResolver>` | Mutable, ordered [resolution pipeline](Resolvers.md) |
| `ResolvedObjects` | `IReadOnlyDictionary<Type, object?>` | Snapshot of cached service instances, including substitutes and explicitly registered nulls |

```csharp
var mocker = new AutoMocker(callBase: false);
var store = mocker.GetSubstitute<IStore>();
bool callsBase = mocker.CallBase;
object? cached = mocker.ResolvedObjects[typeof(IStore)];
```

## CreateInstance

```csharp
T CreateInstance<T>() where T : class
T CreateInstance<T>(bool enablePrivate) where T : class
object CreateInstance(Type type)
object CreateInstance(Type type, bool enablePrivate)
```

Constructs a real object using the longest resolvable constructor. Dependencies come from registrations or resolvers. Each call creates a new root object; dependencies are normally cached. `enablePrivate` defaults to `false`; `true` includes non-public constructors, but does not bypass proxy accessibility restrictions.

Throws `ArgumentNullException` for a null type, or [ObjectCreationException](Diagnostics.md#objectcreationexception) when no constructor can be used. Exceptions thrown by the constructor propagate.

```csharp
var service = mocker.CreateInstance<OrderService>();
var another = (OrderService)mocker.CreateInstance(typeof(OrderService));
var internalService = mocker.CreateInstance<InternalService>(enablePrivate: true);
```

## Get

```csharp
TService Get<TService>()
TService Get<TService>(bool enablePrivate)
object Get(Type serviceType)
object Get(Type serviceType, bool enablePrivate)
```

Returns the registered or cached value, otherwise resolves and caches one. Unlike `CreateInstance`, an unregistered class normally resolves to a substitute, not a real instance. `enablePrivate` defaults to `false`. Registered nulls are returned as null (or `default(TService)` by generic overloads).

Throws `ArgumentNullException` for a null type or `ArgumentException` if resolution fails.

```csharp
IStore store = mocker.Get<IStore>();
object sameStore = mocker.Get(typeof(IStore));
CancellationToken token = mocker.Get<CancellationToken>();
```

### IServiceProvider.GetService

`AutoMocker` explicitly implements `IServiceProvider.GetService(Type)`. It uses the same pipeline and cache, returning null when resolution fails rather than throwing the unresolved-service `ArgumentException`.

```csharp
IServiceProvider provider = mocker;
var store = (IStore?)provider.GetService(typeof(IStore));
```

## GetSubstitute

```csharp
TService GetSubstitute<TService>() where TService : class
TService GetSubstitute<TService>(bool enablePrivate) where TService : class
object GetSubstitute(Type serviceType)
object GetSubstitute(Type serviceType, bool enablePrivate)
```

Returns or creates the cached NSubstitute object itself, not a wrapper. `enablePrivate` defaults to `false`. Throws `ArgumentException` if the service is not a substitute and `ArgumentNullException` for a null type.

```csharp
var store = mocker.GetSubstitute<IStore>();
store.Find(42).Returns("item");
var service = mocker.CreateInstance<OrderService>();
// After exercising service:
store.Received(1).Find(42);
```

## WithSubstitute

```csharp
T WithSubstitute<T>(bool enablePrivate = false, bool? callBase = null)
    where T : class
object WithSubstitute(
    Type serviceType, bool enablePrivate = false, bool? callBase = null)
```

Creates a fresh substitute, registers it under the requested service type, and
returns the substitute itself for configuration. Supports interfaces and
substitutable classes; class constructor dependencies are resolved from the
container. Each call replaces an existing registration, discarding that
substitute's configuration and call history from the container. Use
`GetSubstitute` to reuse the current substitute instead.

`enablePrivate` defaults to `false`; `true` includes non-public constructors but
does not bypass proxy accessibility restrictions. `callBase` defaults to the
container's `CallBase` setting. When true, class targets are partial substitutes
whose unconfigured virtual members run the real implementation; interface
targets remain ordinary substitutes. Non-virtual members cannot be substituted.

Throws `ArgumentNullException` for a null runtime type or
[ObjectCreationException](Diagnostics.md#objectcreationexception) when the
substitute cannot be created.

```csharp
using NSubstitute.Extensions;

var store = mocker.WithSubstitute<IStore>();
store.Find(42).Returns("item");
IStore sameStore = mocker.Get<IStore>();

var clock = (IClock)mocker.WithSubstitute(typeof(IClock));

// Configure() avoids running the real virtual member during setup.
var worker = mocker.WithSubstitute<Worker>(callBase: true);
worker.Configure().Load().Returns("test");
```

## Use

All overloads return the same `AutoMocker` for chaining.

```csharp
AutoMocker Use<TService>(TService? service)
AutoMocker Use(Type type, object? service)
AutoMocker Use<TService>(Func<AutoMocker, TService> factory) where TService : class
AutoMocker Use<TService>(Func<TService> factory) where TService : class
AutoMocker Use<TService, TImplementation>()
    where TService : class
    where TImplementation : class, TService
```

Instance overloads register immediately, including null, replacing an existing different value. Registering the same instance twice under the same type throws `InvalidOperationException`; incompatible values throw `ArgumentException`.

Factory overloads run on first resolution and cache the result. Existing cached values take precedence. `Use<TService, TImplementation>()` defers to `Get<TImplementation>()`, so the implementation is normally substituted. Use `With` to construct a real implementation.

```csharp
mocker.Use<IStore>(new Store())
    .Use<string>("test");
mocker.Use(typeof(int), 42);
mocker.Use<IClock>(() => new Clock());
mocker.Use<IOrderService>(m => new OrderService(m.Get<IStore>()));
mocker.Use<IWorker, Worker>();
```

## With

```csharp
TImplementation With<TImplementation>() where TImplementation : class
TImplementation With<TService, TImplementation>()
    where TImplementation : class, TService
object With(Type implementationType)
object With(Type serviceType, Type implementationType)
```

Constructs a real implementation immediately, registers it, and returns it. One-type overloads register under the implementation type; two-type overloads register only under the service type.

```csharp
Store store = mocker.With<IStore, Store>();
OrderService service = mocker.With<OrderService>();
object clock = mocker.With(typeof(IClock), typeof(Clock));
```

## CreateSelfSubstitute

```csharp
T CreateSelfSubstitute<T>() where T : class
T CreateSelfSubstitute<T>(bool enablePrivate) where T : class
```

Creates a partial substitute with constructor dependencies resolved by the container. Real implementations run unless configured otherwise. The root is not registered. `enablePrivate` defaults to `false`.

Use NSubstitute's `Configure()` before configuring a virtual member to avoid running its real implementation during setup. Non-virtual members cannot be substituted.

```csharp
using NSubstitute.Extensions;

var worker = mocker.CreateSelfSubstitute<Worker>();
worker.Configure().Load().Returns("test");
```

## WithSelfSubstitute

```csharp
T WithSelfSubstitute<T>(bool enablePrivate = false) where T : class
TService WithSelfSubstitute<TService, TImplementation>(bool enablePrivate = false)
    where TImplementation : class, TService
    where TService : class
object WithSelfSubstitute(Type implementationType, bool enablePrivate = false)
object WithSelfSubstitute(
    Type serviceType, Type implementationType, bool enablePrivate = false)
```

Creates and returns a partial substitute like `CreateSelfSubstitute`, but also registers it. Two-type overloads register the same object under both types. An incompatible implementation throws `ArgumentException`.

```csharp
IWorker worker = mocker.WithSelfSubstitute<IWorker, Worker>();
var sameWorker = mocker.GetSubstitute<Worker>();
```

## Combine

```csharp
void Combine(Type type, params Type[] forwardTo)
void Combine<TService, TAsWellAs1>()
// Generic overloads accept 2 through 11 type arguments.
```

Creates one substitute implementing every supplied type and caches it under each type. At most one type may be a class. This replaces previously cached substitutes for these types, losing their setups and call history; combine before configuring dependencies. Failure to create the substitute throws `ObjectCreationException`.

```csharp
mocker.Combine<IReader, IWriter>();
var reader = mocker.GetSubstitute<IReader>();
var writer = mocker.GetSubstitute<IWriter>();
bool same = ReferenceEquals(reader, writer); // true

// Equivalent runtime-type form, on a separate container:
var runtimeMocker = new AutoMocker();
runtimeMocker.Combine(typeof(IReader), typeof(IWriter));
```

## AsDisposable

```csharp
IDisposable AsDisposable()
```

Returns a cleanup handle for cached values implementing `IDisposable`. Dispose it after exercising the test. It does not clean up uncached roots from `CreateInstance`, nor perform asynchronous disposal.

```csharp
var mocker = new AutoMocker();
using var cleanup = mocker.AsDisposable();
mocker.Use<Stream>(new MemoryStream());
var service = mocker.CreateInstance<OrderService>();
```

## IsSubstitute

```csharp
static bool IsSubstitute(object? value)
```

Returns whether an object is an NSubstitute substitute, including substituted delegates; returns `false` for null.

```csharp
bool substituted = AutoMocker.IsSubstitute(mocker.Get<IStore>()); // true
bool ordinary = AutoMocker.IsSubstitute(new Store()); // false
```

## InsertResolverBefore / InsertResolverAfter

See [Resolvers](Resolvers.md#pipeline) for both signatures, ordering, and a complete custom resolver example.
