namespace Il2CppToolkit.Model;

public class TypeDescriptorReference : ITypeReference
{
	public string Name { get; }

	public TypeDescriptor Descriptor { get; }

	public TypeDescriptorReference(TypeDescriptor descriptor)
	{
		Name = descriptor.Name;
		Descriptor = descriptor;
	}
}
