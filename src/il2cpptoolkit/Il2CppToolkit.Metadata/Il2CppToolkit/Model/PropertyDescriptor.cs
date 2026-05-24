using System.Reflection;

namespace Il2CppToolkit.Model;

public class PropertyDescriptor
{
	public readonly string StorageName;

	public readonly string Name;

	public readonly ITypeReference Type;

	public PropertyAttributes Attributes;

	public MethodAttributes GetMethodAttributes;

	public PropertyDescriptor(string name, ITypeReference typeReference, PropertyAttributes attrs, MethodAttributes getMethodAttrs)
	{
		Name = name;
		StorageName = "_" + Name + "_BackingField";
		Type = typeReference;
		Attributes = attrs;
		GetMethodAttributes = getMethodAttrs;
	}
}
