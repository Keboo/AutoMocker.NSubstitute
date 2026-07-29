namespace NSubstitute.AutoMock.Tests.Util;

public interface IService1
{
    string GetValue();
}

public interface IService2
{
    IService1? Other { get; set; }
    string Name { get; }
}

public class Service2 : IService2
{
    public IService1? Other { get; set; }
    public string Name => nameof(Service2);
}

public class WithService
{
    public WithService(IService2 service)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public IService2 Service { get; }
}

public class WithServiceArray
{
    public WithServiceArray(IService1[] services)
    {
        Services = services;
    }

    public IService1[] Services { get; }
}

public class WithServiceEnumerable
{
    public WithServiceEnumerable(IEnumerable<IService1> services)
    {
        Services = services;
    }

    public IEnumerable<IService1> Services { get; }
}

public class WithServiceFunc
{
    public WithServiceFunc(Func<IService1> factory)
    {
        Factory = factory;
    }

    public Func<IService1> Factory { get; }
}

public class WithServiceLazy
{
    public WithServiceLazy(Lazy<IService1> service)
    {
        Service = service;
    }

    public Lazy<IService1> Service { get; }
}

public class WithCancellationToken
{
    public WithCancellationToken(CancellationToken token)
    {
        Token = token;
    }

    public CancellationToken Token { get; }
}

public class WithAutoMocker
{
    public WithAutoMocker(AutoMocker mocker)
    {
        Mocker = mocker;
    }

    public AutoMocker Mocker { get; }
}

public class WithPrivateConstructor
{
    private WithPrivateConstructor(IService2 service)
    {
        Service = service;
    }

    public IService2? Service { get; }
}

public abstract class AbstractWithImplementation
{
    public abstract string AbstractValue { get; }

    public virtual string VirtualValue => "base";

    public string CombinedValue => $"{AbstractValue}-{VirtualValue}";
}

public class VirtualService : IService1
{
    public virtual string GetValue() => "real";

    string IService1.GetValue() => GetValue();
}

public interface IDisposableService : IDisposable
{
}

public class DisposableTracker : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public class SelfReferencing
{
    public SelfReferencing(SelfReferencing self)
    {
    }
}
