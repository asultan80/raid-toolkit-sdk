using System;

namespace Il2CppToolkit.Runtime;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
public class AddressAttribute : Attribute
{
	public ulong Address { get; }

	public string RelativeToModule { get; }

	public AddressAttribute(ulong address, string relativeToModule)
	{
		Address = address;
		RelativeToModule = relativeToModule;
	}
}
