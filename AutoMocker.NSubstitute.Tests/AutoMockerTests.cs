using NSubstitute.AutoMock.Tests.Util;

namespace NSubstitute.AutoMock.Tests;

[TestClass]
public class AutoMockerTests
{
    [TestMethod]
    public void CreateInstance_WithInterfaceDependency_GeneratesSubstitute()
    {
        AutoMocker mocker = new();

        WithService instance = mocker.CreateInstance<WithService>();

        Assert.IsNotNull(instance.Service);
        Assert.IsTrue(AutoMocker.IsSubstitute(instance.Service));
    }

    [TestMethod]
    public void CreateInstance_SubstituteDependency_CanBeConfiguredAndVerified()
    {
        AutoMocker mocker = new();
        WithService instance = mocker.CreateInstance<WithService>();

        IService2 substitute = mocker.GetSubstitute<IService2>();
        substitute.Name.Returns("configured");

        Assert.AreSame(instance.Service, substitute);
        Assert.AreEqual("configured", instance.Service.Name);
        _ = substitute.Received(1).Name;
    }

    [TestMethod]
    public void GetSubstitute_CreatesAndCachesSubstitute()
    {
        AutoMocker mocker = new();

        IService1 first = mocker.GetSubstitute<IService1>();
        IService1 second = mocker.GetSubstitute<IService1>();

        Assert.AreSame(first, second);
        Assert.IsTrue(AutoMocker.IsSubstitute(first));
    }

    [TestMethod]
    public void GetSubstitute_WithRegisteredRealInstance_Throws()
    {
        AutoMocker mocker = new();
        mocker.Use<IService2>(new Service2());

        Assert.ThrowsExactly<ArgumentException>(() => mocker.GetSubstitute<IService2>());
    }

    [TestMethod]
    public void Use_RegisteredInstance_IsUsedForConstruction()
    {
        AutoMocker mocker = new();
        Service2 service = new();
        mocker.Use<IService2>(service);

        WithService instance = mocker.CreateInstance<WithService>();

        Assert.AreSame(service, instance.Service);
    }

    [TestMethod]
    public void Use_RegisteredSubstitute_IsRetrievableAsSubstitute()
    {
        AutoMocker mocker = new();
        IService2 substitute = Substitute.For<IService2>();
        mocker.Use(substitute);

        Assert.AreSame(substitute, mocker.GetSubstitute<IService2>());
    }

    [TestMethod]
    public void Use_WithNonMatchingType_Throws()
    {
        AutoMocker mocker = new();

        Assert.ThrowsExactly<ArgumentException>(() => mocker.Use(typeof(IService1), new Service2()));
    }

    [TestMethod]
    public void Use_Factory_IsInvokedLazilyAndCached()
    {
        AutoMocker mocker = new();
        int invocationCount = 0;
        mocker.Use<IService2>(() =>
        {
            invocationCount++;
            return new Service2();
        });

        Assert.AreEqual(0, invocationCount);
        IService2 first = mocker.Get<IService2>();
        IService2 second = mocker.Get<IService2>();

        Assert.AreEqual(1, invocationCount);
        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void Use_ServiceImplementationPair_ResolvesImplementation()
    {
        AutoMocker mocker = new();
        mocker.Use<IService2, Service2>();

        IService2 service = mocker.Get<IService2>();

        Assert.IsInstanceOfType<Service2>(service);
    }

    [TestMethod]
    public void With_CreatesAndRegistersImplementation()
    {
        AutoMocker mocker = new();

        Service2 implementation = mocker.With<IService2, Service2>();

        Assert.AreSame(implementation, mocker.Get<IService2>());
        WithService instance = mocker.CreateInstance<WithService>();
        Assert.AreSame(implementation, instance.Service);
    }

    [TestMethod]
    public void Get_ReturnsCachedInstanceAcrossCalls()
    {
        AutoMocker mocker = new();

        IService1 first = mocker.Get<IService1>();
        IService1 second = mocker.Get<IService1>();

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void Get_WithString_Throws()
    {
        AutoMocker mocker = new();

        Assert.ThrowsExactly<ArgumentException>(() => mocker.Get<string>());
    }

    [TestMethod]
    public void CreateInstance_WithArrayDependency_ResolvesArray()
    {
        AutoMocker mocker = new();

        WithServiceArray instance = mocker.CreateInstance<WithServiceArray>();

        Assert.AreEqual(1, instance.Services.Length);
        Assert.IsTrue(AutoMocker.IsSubstitute(instance.Services[0]));
    }

    [TestMethod]
    public void CreateInstance_WithEnumerableDependency_ResolvesEnumerable()
    {
        AutoMocker mocker = new();

        WithServiceEnumerable instance = mocker.CreateInstance<WithServiceEnumerable>();

        Assert.AreEqual(1, instance.Services.Count());
    }

    [TestMethod]
    public void CreateInstance_WithFuncDependency_ResolvesFunc()
    {
        AutoMocker mocker = new();

        WithServiceFunc instance = mocker.CreateInstance<WithServiceFunc>();
        IService1 service = instance.Factory();

        Assert.AreSame(mocker.GetSubstitute<IService1>(), service);
    }

    [TestMethod]
    public void CreateInstance_WithLazyDependency_ResolvesLazy()
    {
        AutoMocker mocker = new();

        WithServiceLazy instance = mocker.CreateInstance<WithServiceLazy>();

        Assert.IsFalse(instance.Service.IsValueCreated);
        Assert.AreSame(mocker.GetSubstitute<IService1>(), instance.Service.Value);
    }

    [TestMethod]
    public void CreateInstance_WithCancellationToken_ResolvesNone()
    {
        AutoMocker mocker = new();

        WithCancellationToken instance = mocker.CreateInstance<WithCancellationToken>();

        Assert.AreEqual(CancellationToken.None, instance.Token);
    }

    [TestMethod]
    public void CreateInstance_WithRegisteredCancellationTokenSource_ResolvesItsToken()
    {
        AutoMocker mocker = new();
        using CancellationTokenSource cts = new();
        mocker.Use(cts);

        WithCancellationToken instance = mocker.CreateInstance<WithCancellationToken>();

        Assert.AreEqual(cts.Token, instance.Token);
    }

    [TestMethod]
    public void CreateInstance_WithAutoMockerDependency_ResolvesSelf()
    {
        AutoMocker mocker = new();

        WithAutoMocker instance = mocker.CreateInstance<WithAutoMocker>();

        Assert.AreSame(mocker, instance.Mocker);
    }

    [TestMethod]
    public void CreateInstance_WithPrivateConstructor_RequiresEnablePrivate()
    {
        AutoMocker mocker = new();

        Assert.ThrowsExactly<ObjectCreationException>(() => mocker.CreateInstance<WithPrivateConstructor>());

        WithPrivateConstructor instance = mocker.CreateInstance<WithPrivateConstructor>(enablePrivate: true);
        Assert.IsNotNull(instance.Service);
    }

    [TestMethod]
    public void CreateInstance_WithSelfReferencingConstructor_ThrowsObjectCreationException()
    {
        AutoMocker mocker = new();

        ObjectCreationException ex = Assert.ThrowsExactly<ObjectCreationException>(() => mocker.CreateInstance<SelfReferencing>());
        Assert.IsNotEmpty(ex.DiagnosticMessages);
    }

    [TestMethod]
    public void CreateSelfSubstitute_AbstractClass_InvokesBaseImplementation()
    {
        AutoMocker mocker = new();

        AbstractWithImplementation instance = mocker.CreateSelfSubstitute<AbstractWithImplementation>();
        instance.AbstractValue.Returns("abstract");

        Assert.AreEqual("abstract-base", instance.CombinedValue);
    }

    [TestMethod]
    public void WithSelfSubstitute_RegistersServiceAndImplementation()
    {
        AutoMocker mocker = new();

        IService1 substitute = mocker.WithSelfSubstitute<IService1, VirtualService>();

        Assert.AreSame(substitute, mocker.Get<IService1>());
        Assert.AreSame(substitute, mocker.Get<VirtualService>());
        Assert.AreEqual("real", substitute.GetValue());
    }

    [TestMethod]
    public void Combine_RegistersSingleSubstituteForMultipleTypes()
    {
        AutoMocker mocker = new();

        mocker.Combine(typeof(IService1), typeof(IDisposableService));

        IService1 service1 = mocker.GetSubstitute<IService1>();
        IDisposableService disposable = mocker.GetSubstitute<IDisposableService>();

        Assert.AreSame((object)service1, disposable);
    }

    [TestMethod]
    public void AsDisposable_DisposesTrackedDisposables()
    {
        AutoMocker mocker = new();
        DisposableTracker tracker = new();
        mocker.Use(tracker);

        using (mocker.AsDisposable())
        {
        }

        Assert.IsTrue(tracker.IsDisposed);
    }

    [TestMethod]
    public void ServiceProvider_GetService_ResolvesService()
    {
        AutoMocker mocker = new();
        IServiceProvider provider = mocker;

        object? service = provider.GetService(typeof(IService1));

        Assert.IsNotNull(service);
        Assert.IsTrue(AutoMocker.IsSubstitute(service));
    }

    [TestMethod]
    public void ResolvedObjects_ContainsResolvedServices()
    {
        AutoMocker mocker = new();
        IService1 substitute = mocker.GetSubstitute<IService1>();

        Assert.IsTrue(mocker.ResolvedObjects.TryGetValue(typeof(IService1), out object? resolved));
        Assert.AreSame(substitute, (IService1?)resolved);
    }

    [TestMethod]
    public void CallBase_ClassSubstitutes_InvokeBaseImplementation()
    {
        AutoMocker mocker = new(callBase: true);

        VirtualService substitute = mocker.GetSubstitute<VirtualService>();

        Assert.AreEqual("real", substitute.GetValue());
    }

    [TestMethod]
    public void DefaultBehavior_ClassSubstitutes_ReturnDefaultValues()
    {
        AutoMocker mocker = new();

        VirtualService substitute = mocker.GetSubstitute<VirtualService>();

        Assert.IsTrue(string.IsNullOrEmpty(substitute.GetValue()));
    }

    [TestMethod]
    public void IsSubstitute_DetectsSubstitutesAndRealObjects()
    {
        Assert.IsTrue(AutoMocker.IsSubstitute(Substitute.For<IService1>()));
        Assert.IsFalse(AutoMocker.IsSubstitute(new Service2()));
        Assert.IsFalse(AutoMocker.IsSubstitute(null));
    }

    [TestMethod]
    public void Use_SameInstanceTwice_Throws()
    {
        AutoMocker mocker = new();
        Service2 service = new();
        mocker.Use<IService2>(service);

        Assert.ThrowsExactly<InvalidOperationException>(() => mocker.Use<IService2>(service));
    }
}
