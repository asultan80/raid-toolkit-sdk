using System;

namespace Il2CppToolkit.Model;

public class DotNetTypeReference : ITypeReference
{
	public string Name { get; }

	public Type Type { get; }

	public DotNetTypeReference(Type type)
	{
		Name = type.FullName;
		Type = type;
	}
}
