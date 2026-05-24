using System;

namespace Il2CppToolkit.Model;

internal static class ArmUtils
{
	public static uint DecodeMov(byte[] asm)
	{
		ushort num = (ushort)(asm[2] + ((asm[3] & 0x70) << 4) + ((asm[1] & 4) << 9) + ((asm[0] & 0xF) << 12));
		return (uint)(((ushort)(asm[6] + ((asm[7] & 0x70) << 4) + ((asm[5] & 4) << 9) + ((asm[4] & 0xF) << 12)) << 16) + num);
	}

	public static ulong DecodeAdr(ulong pc, byte[] inst)
	{
		string text = inst.HexToBin();
		string text2 = text.Substring(8, 19) + text.Substring(1, 2);
		text2 = text2.PadLeft(64, text2[0]);
		return pc + Convert.ToUInt64(text2, 2);
	}

	public static ulong DecodeAdrp(ulong pc, byte[] inst)
	{
		pc &= 0xFFFFFFFFFFFFF000uL;
		string text = inst.HexToBin();
		string text2 = text.Substring(8, 19) + text.Substring(1, 2) + new string('0', 12);
		text2 = text2.PadLeft(64, text2[0]);
		return pc + Convert.ToUInt64(text2, 2);
	}

	public static ulong DecodeAdd(byte[] inst)
	{
		string text = inst.HexToBin();
		ulong num = Convert.ToUInt64(text.Substring(10, 12), 2);
		if (text[9] == '1')
		{
			num <<= 12;
		}
		return num;
	}

	public static bool IsAdr(byte[] inst)
	{
		string text = inst.HexToBin();
		if (text[0] == '0')
		{
			return text.Substring(3, 5) == "10000";
		}
		return false;
	}
}
