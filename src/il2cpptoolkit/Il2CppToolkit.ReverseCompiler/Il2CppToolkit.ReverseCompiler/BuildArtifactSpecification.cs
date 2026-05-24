namespace Il2CppToolkit.ReverseCompiler;

public class BuildArtifactSpecification<T> : ITypedSpecification<T>, IStateSpecification
{
	public string Name { get; }

	public object DefaultValue { get; }

	public BuildArtifactSpecification(string name)
	{
		Name = name;
	}
}
