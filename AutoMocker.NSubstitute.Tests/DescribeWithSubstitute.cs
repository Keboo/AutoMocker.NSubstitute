using NSubstitute.AutoMock.Tests.Util;

namespace NSubstitute.AutoMock.Tests;

[TestClass]
public class DescribeWithSubstitute
{
    [TestMethod]
    public void It_registers_a_fresh_configurable_substitute()
    {
        AutoMocker mocker = new();
        IService2 original = mocker.GetSubstitute<IService2>();

        IService2 substitute = mocker.WithSubstitute<IService2>();
        substitute.Name.Returns("configured");

        Assert.AreNotSame(original, substitute);
        Assert.AreSame(substitute, mocker.GetSubstitute<IService2>());
        Assert.AreSame(substitute, mocker.Get<IService2>());
        WithService instance = mocker.CreateInstance<WithService>();
        Assert.AreSame(substitute, instance.Service);
        Assert.AreEqual("configured", instance.Service.Name);
        _ = substitute.Received(1).Name;
    }

    [TestMethod]
    public void It_registers_a_fresh_configurable_substitute_non_generic()
    {
        AutoMocker mocker = new();
        IService2 original = mocker.GetSubstitute<IService2>();

        IService2 substitute = (IService2)mocker.WithSubstitute(typeof(IService2));
        substitute.Name.Returns("configured");

        Assert.AreNotSame(original, substitute);
        Assert.AreSame(substitute, mocker.GetSubstitute<IService2>());
        Assert.AreSame(substitute, mocker.Get<IService2>());
        WithService instance = mocker.CreateInstance<WithService>();
        Assert.AreSame(substitute, instance.Service);
        Assert.AreEqual("configured", instance.Service.Name);
        _ = substitute.Received(1).Name;
    }

    [TestMethod]
    public void It_replaces_real_instances_and_creates_a_new_substitute_on_each_call()
    {
        AutoMocker mocker = new();
        mocker.Use<IService2>(new Service2());

        IService2 first = mocker.WithSubstitute<IService2>();
        IService2 second = mocker.WithSubstitute<IService2>();

        Assert.AreNotSame(first, second);
        Assert.AreSame(second, mocker.GetSubstitute<IService2>());
    }

    [TestMethod]
    public void It_resolves_class_constructor_dependencies()
    {
        AutoMocker mocker = new();
        IService2 dependency = mocker.Get<IService2>();

        WithService substitute = mocker.WithSubstitute<WithService>();

        Assert.AreSame(dependency, substitute.Service);
        Assert.AreSame(substitute, mocker.Get<WithService>());
        Assert.AreSame(substitute, mocker.GetSubstitute<WithService>());
    }

    [TestMethod]
    public void It_resolves_class_constructor_dependencies_non_generic()
    {
        AutoMocker mocker = new();
        IService2 dependency = mocker.Get<IService2>();

        WithService substitute = (WithService)mocker.WithSubstitute(typeof(WithService));

        Assert.AreSame(dependency, substitute.Service);
        Assert.AreSame(substitute, mocker.Get<WithService>());
        Assert.AreSame(substitute, mocker.GetSubstitute<WithService>());
    }

    [TestMethod]
    public void It_inherits_and_can_override_call_base()
    {
        AutoMocker mocker = new(callBase: true);

        VirtualService inherited = mocker.WithSubstitute<VirtualService>();
        Assert.AreEqual("real", inherited.GetValue());

        VirtualService overridden = mocker.WithSubstitute<VirtualService>(callBase: false);
        Assert.AreEqual(string.Empty, overridden.GetValue());
    }

    [TestMethod]
    public void It_inherits_and_can_override_call_base_non_generic()
    {
        AutoMocker mocker = new(callBase: true);

        VirtualService inherited = (VirtualService)mocker.WithSubstitute(typeof(VirtualService));
        Assert.AreEqual("real", inherited.GetValue());

        VirtualService overridden = (VirtualService)mocker.WithSubstitute(typeof(VirtualService), callBase: false);
        Assert.AreEqual(string.Empty, overridden.GetValue());
    }

    [TestMethod]
    public void It_validates_runtime_types()
    {
        AutoMocker mocker = new();

        Assert.ThrowsExactly<ArgumentNullException>(() => mocker.WithSubstitute(null!));
        Assert.ThrowsExactly<ObjectCreationException>(() => mocker.WithSubstitute(typeof(string)));
    }
}
