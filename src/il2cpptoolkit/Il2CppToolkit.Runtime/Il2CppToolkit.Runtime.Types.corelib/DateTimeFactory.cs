using System;

namespace Il2CppToolkit.Runtime.Types.corelib;

[TypeFactory(typeof(DateTime))]
public class DateTimeFactory : ITypeFactory
{
	public object ReadValue(IMemorySource source, ulong address)
	{
		return DateTime.FromBinary(Il2CppTypeInfoLookup<DateTime>.GetValue<long>(new UnknownObject(source, address), "_dateData", 1)).ToLocalTime();
	}

	public void WriteValue(IMemorySource source, ulong address, object value)
	{
		throw new NotImplementedException();
	}
}
