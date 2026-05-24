using System;

namespace Il2CppToolkit.Runtime.Types.corelib;

[TypeFactory(typeof(Nullable<>))]
public class NullableFactory<T> : ITypeFactory where T : struct
{
	public object ReadValue(IMemorySource source, ulong address)
	{
		UnknownObject obj = new UnknownObject(source, address);
		bool value = Il2CppTypeInfoLookup<T?>.GetValue<bool>(obj, "hasValue", 1);
		T value2 = (value ? Il2CppTypeInfoLookup<T?>.GetValue<T>(obj, "value", 1) : default(T));
		return value ? new T?(value2) : null;
	}

	public void WriteValue(IMemorySource source, ulong address, object value)
	{
		throw new NotImplementedException();
	}
}
