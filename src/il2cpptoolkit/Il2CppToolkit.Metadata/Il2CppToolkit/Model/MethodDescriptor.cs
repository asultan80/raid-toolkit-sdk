using System.Collections.Generic;

namespace Il2CppToolkit.Model;

public class MethodDescriptor
{
	public string DisambiguatedName;

	public readonly string Name;

	public readonly List<ITypeReference> DeclaringTypeArgs = new List<ITypeReference>();

	public MethodDescriptor(string name)
	{
		Name = name;
	}
}
