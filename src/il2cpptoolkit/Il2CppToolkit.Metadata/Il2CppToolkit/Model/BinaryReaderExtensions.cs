using System;
using System.IO;
using System.Text;

namespace Il2CppToolkit.Model;

public static class BinaryReaderExtensions
{
	public static string ReadString(this BinaryReader reader, int numChars)
	{
		long position = reader.BaseStream.Position;
		string text = Encoding.UTF8.GetString(reader.ReadBytes(numChars * 4)).Substring(0, numChars);
		reader.BaseStream.Position = position;
		reader.ReadBytes(Encoding.UTF8.GetByteCount(text));
		return text;
	}

	public static uint ReadULeb128(this BinaryReader reader)
	{
		uint num = reader.ReadByte();
		if (num >= 128)
		{
			int num2 = 0;
			num &= 0x7Fu;
			byte b;
			do
			{
				b = reader.ReadByte();
				num2 += 7;
				num |= (uint)((b & 0x7F) << num2);
			}
			while (b >= 128);
		}
		return num;
	}

	public static uint ReadCompressedUInt32(this BinaryReader reader)
	{
		byte b = reader.ReadByte();
		if ((b & 0x80) == 0)
		{
			return b;
		}
		if ((b & 0xC0) == 128)
		{
			uint num = (uint)((b & -129) << 8);
			return num | reader.ReadByte();
		}
		if ((b & 0xE0) == 192)
		{
			uint num = (uint)((b & -193) << 24);
			num |= (uint)(reader.ReadByte() << 16);
			num |= (uint)(reader.ReadByte() << 8);
			return num | reader.ReadByte();
		}
		return b switch
		{
			240 => reader.ReadUInt32(), 
			254 => 4294967294u, 
			byte.MaxValue => uint.MaxValue, 
			_ => throw new Exception("Invalid compressed integer format"), 
		};
	}

	public static int ReadCompressedInt32(this BinaryReader reader)
	{
		uint num = reader.ReadCompressedUInt32();
		if (num == uint.MaxValue)
		{
			return int.MinValue;
		}
		bool num2 = (num & 1) != 0;
		num >>= 1;
		if (num2)
		{
			return (int)(0 - (num + 1));
		}
		return (int)num;
	}
}
