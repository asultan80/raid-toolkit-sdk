using System;
using System.Text;

namespace Il2CppToolkit.Model;

internal static class HexExtensions
{
	public static string HexToBin(this byte b)
	{
		return Convert.ToString(b, 2).PadLeft(8, '0');
	}

	public static string HexToBin(this byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder(bytes.Length * 8);
		foreach (byte b in bytes)
		{
			stringBuilder.Insert(0, b.HexToBin());
		}
		return stringBuilder.ToString();
	}
}
