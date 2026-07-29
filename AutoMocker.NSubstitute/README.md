# AutoMocker.NSubstitute

An auto-mocking container for [NSubstitute](https://nsubstitute.github.io/) — the NSubstitute flavor of [Moq.AutoMocker](https://github.com/moq/Moq.AutoMocker).

Construct the class under test and let the container automatically generate substitutes for any dependencies you have not explicitly supplied.

## Installation

```
dotnet add package AutoMocker.NSubstitute
```

The library types live in the `NSubstitute.AutoMock` namespace.

## Usage

```csharp
var mocker = new AutoMocker();

// Creates CarFactory with substitutes for all constructor dependencies
CarFactory carFactory = mocker.CreateInstance<CarFactory>();

// Configure a dependency using regular NSubstitute syntax
mocker.GetSubstitute<IEngineProvider>()
    .GetEngine()
    .Returns(new Engine());

// Provide explicit instances when needed
mocker.Use<IColorPicker>(new RedColorPicker());

Car car = carFactory.Create();

// Verify using regular NSubstitute syntax
mocker.GetSubstitute<IEngineProvider>().Received(1).GetEngine();
```

## Features

- `CreateInstance<T>()` — constructs `T`, resolving constructor arguments from the container or generating substitutes.
- `GetSubstitute<T>()` — retrieves (or creates) the substitute used for a service, ready to be configured with `Returns`, `Received`, etc.
- `Use(...)` / `With(...)` — register explicit instances, factories, or implementation types.
- `CreateSelfSubstitute<T>()` / `WithSelfSubstitute<T>()` — partial substitutes (`Substitute.ForPartsOf`) so you can test a class while substituting selected virtual members.
- `Combine(...)` — one substitute instance registered under multiple service types.
- Built-in resolution of `IEnumerable<T>`, arrays, `Lazy<T>`, `Func<T>`, and `CancellationToken` dependencies.
- `AsDisposable()` — dispose all disposable instances tracked by the container.
- Implements `IServiceProvider`.

## Differences from Moq.AutoMock

NSubstitute has no `Mock<T>` wrapper, behaviors, or setup expressions, so this package intentionally omits the Moq-specific APIs
(`Setup`, `Verify`, `MockBehavior`, `DefaultValue`, etc.). Configure and verify substitutes directly using standard NSubstitute syntax
on the objects returned from `GetSubstitute<T>()`.
