using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime.Types.corelib;

[TypeFactory(typeof(string))]
public class StringFactory : ITypeFactory
{
	public object ReadValue(IMemorySource source, ulong address)
	{
		int value = Il2CppTypeInfoLookup<string>.GetValue<int>(new UnknownObject(source, address), "_stringLength", 1);
		if (value <= 0)
		{
			ErrorHandler.Assert(value == 0, "Invalid string length");
			return string.Empty;
		}
		Il2CppTypeInfo typeInfo = Il2CppTypeCache.GetTypeInfo(source.ParentContext, typeof(string), 0uL);
		ReadOnlyMemory<byte> readOnlyMemory = source.ReadMemory(address + ((IEnumerable<Il2CppField>)typeInfo.Fields).First((Il2CppField fld) => fld.Name == "_firstChar").Offset, (ulong)value * 2uL);
		return Encoding.Unicode.GetString(readOnlyMemory.Span);
	}

	public void WriteValue(IMemorySource source, ulong address, object value)
	{
		throw new NotSupportedException("Cannot write directly to string buffer");
	}
}
