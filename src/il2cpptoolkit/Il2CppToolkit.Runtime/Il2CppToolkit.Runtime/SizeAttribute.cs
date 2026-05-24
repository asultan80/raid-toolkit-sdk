using System;

namespace Il2CppToolkit.Runtime;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SizeAttribute : Attribute
{
	public uint Size { get; }

	public SizeAttribute(uint size)
	{
		Size = size;
	}
}
