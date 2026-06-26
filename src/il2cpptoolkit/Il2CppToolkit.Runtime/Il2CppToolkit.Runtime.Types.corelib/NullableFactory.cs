using System;
using System.Runtime.CompilerServices;
using Il2CppToolkit.Runtime;

namespace Il2CppToolkit.Runtime.Types.corelib;

[TypeFactory(typeof(Nullable<>))]
public class NullableFactory<T> : ITypeFactory where T : struct
{
	public object ReadValue(IMemorySource source, ulong address)
	{
		// Read hasValue bool at offset 0 directly — avoids requiring GetTypeInfo(Nullable<T>)
		byte hasValueByte = source.ReadMemory(address, 1).Span[0];
		if (hasValueByte == 0) return null;
		// value field is aligned after hasValue: align to T's natural size
		int tSize = Unsafe.SizeOf<T>();
		int valueOffset = tSize >= 8 ? 8 : tSize >= 4 ? 4 : tSize >= 2 ? 2 : 1;
		T value2 = (T)source.ReadValue(typeof(T), address + (ulong)valueOffset, 1);
		return new T?(value2);
	}

	public void WriteValue(IMemorySource source, ulong address, object value)
	{
		throw new NotImplementedException();
	}
}
