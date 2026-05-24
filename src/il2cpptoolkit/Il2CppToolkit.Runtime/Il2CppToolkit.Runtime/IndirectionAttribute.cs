using System;

namespace Il2CppToolkit.Runtime;

[AttributeUsage(AttributeTargets.Field)]
public class IndirectionAttribute : Attribute
{
	public byte Indirection { get; }

	public IndirectionAttribute(byte indirection)
	{
		Indirection = indirection;
	}
}
