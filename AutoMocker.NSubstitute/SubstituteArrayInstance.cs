using System.Diagnostics;

namespace NSubstitute.AutoMock;

[DebuggerDisplay("Array: {_type.Name,nq}[] (Count = {_instances.Count})")]
internal sealed class SubstituteArrayInstance(Type type) : IInstance
{
    private readonly Type _type = type;
    private readonly List<IInstance> _instances = [];

    public object Value
    {
        get
        {
            int i = 0;
            Array array = Array.CreateInstance(_type, _instances.Count);
            foreach (IInstance instance in _instances)
                array.SetValue(instance.Value, i++);
            return array;
        }
    }

    public bool IsSubstitute { get { return _instances.Any(m => m.IsSubstitute); } }

    public void Add(IInstance instance)
    {
        _instances.Add(instance);
    }
}
