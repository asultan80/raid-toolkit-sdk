using System;

namespace Il2CppToolkit.Runtime;

public class MemoryAccessErrorEventArgs : MemoryAccessEventArgs
{
	public Exception Exception { get; }

	public MemoryAccessErrorEventArgs(Type type, ulong address, Exception ex)
		: base(type, address)
	{
		Exception = ex;
	}
}
