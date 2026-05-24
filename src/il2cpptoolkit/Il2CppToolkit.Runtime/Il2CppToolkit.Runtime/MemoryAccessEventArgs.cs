using System;

namespace Il2CppToolkit.Runtime;

public class MemoryAccessEventArgs : EventArgs
{
	public ulong Address { get; }

	public Type Type { get; }

	public MemoryAccessEventArgs(Type type, ulong address)
	{
		Address = address;
		Type = type;
	}
}
