namespace System;

public static class ReadOnlyMemoryExtensions
{
	public static char ToChar(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToChar(memory.Span);
	}

	public static bool ToBoolean(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToBoolean(memory.Span);
	}

	public static double ToDouble(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToDouble(memory.Span);
	}

	public static float ToSingle(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToSingle(memory.Span);
	}

	public static short ToInt16(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToInt16(memory.Span);
	}

	public static int ToInt32(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToInt32(memory.Span);
	}

	public static long ToInt64(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToInt64(memory.Span);
	}

	public static ushort ToUInt16(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToUInt16(memory.Span);
	}

	public static uint ToUInt32(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToUInt32(memory.Span);
	}

	public static ulong ToUInt64(this ReadOnlyMemory<byte> memory)
	{
		return BitConverter.ToUInt64(memory.Span);
	}

	public static IntPtr ToIntPtr(this ReadOnlyMemory<byte> memory)
	{
		return (IntPtr)BitConverter.ToInt64(memory.Span);
	}

	public static UIntPtr ToUIntPtr(this ReadOnlyMemory<byte> memory)
	{
		return (UIntPtr)(ulong)BitConverter.ToInt64(memory.Span);
	}
}
