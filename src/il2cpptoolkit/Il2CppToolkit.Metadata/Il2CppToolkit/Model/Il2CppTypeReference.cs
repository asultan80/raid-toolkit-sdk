namespace Il2CppToolkit.Model;

public class Il2CppTypeReference : ITypeReference
{
	public string Name { get; }

	public Il2CppType CppType { get; }

	public TypeDescriptor TypeContext { get; }

	public Il2CppTypeReference(string typeName, Il2CppType cppType, TypeDescriptor typeContext)
	{
		Name = typeName;
		CppType = cppType;
		TypeContext = typeContext;
	}
}
