namespace NSubstitute.AutoMock.Generator.Example.TUnit;

public class ControllerWithKeyedServiceTests
{
    [Test]
    public async Task CreateInstance_WithKeyedService_ResolvesService()
    {
        AutoMocker mocker = new();

        IService service = Substitute.For<IService>();
        mocker.Use(service);
        IService service2 = Substitute.For<IService>();
        mocker.WithKeyedService<IService, KeyedServiceWithDependency>("Test");
        mocker.WithKeyedService(service2, "Test2");

        ControllerWithKeyedService controller = mocker.CreateInstance<ControllerWithKeyedService>();

        await Assert.That(controller.Service).IsTypeOf<KeyedServiceWithDependency>();
        var keyedService = (KeyedServiceWithDependency)controller.Service;
        await Assert.That(keyedService.Service).IsEqualTo(service);
        await Assert.That(controller.Service2).IsEqualTo(service2);
    }
}
