using System;

namespace Il2CppToolkit.Runtime;

[AttributeUsage(AttributeTargets.Field)]
public class OffsetAttribute : Attribute
{
	public ulong OffsetBytes { get; }

	public OffsetAttribute(ulong offset)
	{
		OffsetBytes = offset;
	}
}
