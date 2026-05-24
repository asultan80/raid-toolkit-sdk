namespace Il2CppToolkit.ReverseCompiler;

public class SynchronousVariableSpecificationValue<T> : IStateSpecificationValue
{
	public IStateSpecification Specification { get; }

	public object Value { get; }

	public object DefaultValue => Specification.DefaultValue;

	public SynchronousVariableSpecificationValue(SynchronousVariableSpecification<T> specification, T value)
	{
		Specification = specification;
		Value = value;
	}
}
