namespace Il2CppToolkit.ReverseCompiler;

public interface IStateSpecificationValue
{
	IStateSpecification Specification { get; }

	object Value { get; }
}
