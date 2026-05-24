using System;

namespace Il2CppToolkit.Runtime.Types;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public class TypeFactoryAttribute : Attribute
{
	public Type Type { get; private set; }

	public TypeFactoryAttribute(Type type)
	{
		Type = type;
	}
}
