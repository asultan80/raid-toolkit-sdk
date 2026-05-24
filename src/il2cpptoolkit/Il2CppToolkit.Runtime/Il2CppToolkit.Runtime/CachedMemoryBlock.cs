using System;

namespace Il2CppToolkit.Runtime;

public class CachedMemoryBlock : IMemorySource
{
	private readonly ReadOnlyMemory<byte> Data;

	private readonly ulong Address;

	private readonly ulong Size;

	public IMemorySource Parent { get; }

	public Il2CsRuntimeContext ParentContext
	{
		get
		{
			IMemorySource parent = Parent;
			while (!(parent is Il2CsRuntimeContext))
			{
				parent = parent.Parent;
			}
			return parent as Il2CsRuntimeContext;
		}
	}

	public CachedMemoryBlock(IMemorySource parent, ulong address, byte[] data)
	{
		Parent = parent;
		Data = new ReadOnlyMemory<byte>(data);
		Address = address;
		Size = (ulong)data.Length;
	}

	public bool Contains(ulong address, ulong size)
	{
		if (address >= Address + Size)
		{
			return false;
		}
		if (address + size > Address + Size)
		{
			return false;
		}
		if (address < Address)
		{
			return false;
		}
		return true;
	}

	public ReadOnlyMemory<byte> ReadMemory(ulong address, ulong size)
	{
		if (!Contains(address, size))
		{
			return Parent.ReadMemory(address, size);
		}
		return Data.Slice((int)(address - Address), (int)size);
	}
}
