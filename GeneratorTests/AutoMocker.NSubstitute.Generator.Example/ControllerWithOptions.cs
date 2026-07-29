using Microsoft.Extensions.Options;

namespace NSubstitute.AutoMock.Generator.Example;
public class ControllerWithOptions
{
    public IOptions<TestsOptions> Options { get; }

    public ControllerWithOptions(IOptions<TestsOptions> options)
    {
        Options = options;
    }
}
