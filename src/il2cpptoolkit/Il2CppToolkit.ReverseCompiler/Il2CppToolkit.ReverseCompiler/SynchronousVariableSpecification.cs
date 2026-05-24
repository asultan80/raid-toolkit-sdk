namespace Il2CppToolkit.ReverseCompiler;

public class SynchronousVariableSpecification<T> : ITypedSpecification<T>, IStateSpecification, ITypedSynchronousState<T>, ISynchronousState
{
	public string Name { get; }

	public object DefaultValue { get; }

	public SynchronousVariableSpecification(string name)
	{
		Name = name;
	}

	public SynchronousVariableSpecification(string name, object defaultValue)
		: this(name)
	{
		DefaultValue = defaultValue;
	}

	public SynchronousVariableSpecificationValue<T> MakeValue(T value)
	{
		return new SynchronousVariableSpecificationValue<T>(this, value);
	}
}
