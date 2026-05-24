using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Il2CppToolkit.Model;

public sealed class Macho64 : Il2Cpp
{
	private static readonly byte[] FeatureBytes1 = new byte[4] { 2, 0, 128, 210 };

	private static readonly byte[] FeatureBytes2 = new byte[4] { 3, 0, 128, 82 };

	private readonly List<MachoSection64Bit> sections = new List<MachoSection64Bit>();

	private readonly ulong vmaddr;

	public Macho64(Stream stream)
		: base(stream)
	{
		base.Position += 16uL;
		uint num = ReadUInt32();
		base.Position += 12uL;
		for (int i = 0; i < num; i++)
		{
			ulong position = base.Position;
			uint num2 = ReadUInt32();
			uint num3 = ReadUInt32();
			switch (num2)
			{
			case 25u:
			{
				if (Encoding.UTF8.GetString(ReadBytes(16)).TrimEnd('\0') == "__TEXT")
				{
					vmaddr = ReadUInt64();
				}
				else
				{
					base.Position += 8uL;
				}
				base.Position += 32uL;
				uint num4 = ReadUInt32();
				base.Position += 4uL;
				for (int j = 0; j < num4; j++)
				{
					MachoSection64Bit machoSection64Bit = new MachoSection64Bit();
					sections.Add(machoSection64Bit);
					machoSection64Bit.sectname = Encoding.UTF8.GetString(ReadBytes(16)).TrimEnd('\0');
					base.Position += 16uL;
					machoSection64Bit.addr = ReadUInt64();
					machoSection64Bit.size = ReadUInt64();
					machoSection64Bit.offset = ReadUInt32();
					base.Position += 12uL;
					machoSection64Bit.flags = ReadUInt32();
					base.Position += 12uL;
				}
				break;
			}
			case 44u:
				base.Position += 8uL;
				if (ReadUInt32() != 0)
				{
					Console.WriteLine("ERROR: This Mach-O executable is encrypted and cannot be processed.");
				}
				break;
			}
			base.Position = position + num3;
		}
	}

	public override ulong MapVATR(ulong addr)
	{
		MachoSection64Bit machoSection64Bit = sections.First((MachoSection64Bit x) => addr >= x.addr && addr <= x.addr + x.size);
		if (machoSection64Bit.sectname == "__bss")
		{
			throw new Exception();
		}
		return addr - machoSection64Bit.addr + machoSection64Bit.offset;
	}

	public override ulong MapRTVA(ulong addr)
	{
		MachoSection64Bit machoSection64Bit = sections.FirstOrDefault((MachoSection64Bit x) => addr >= x.offset && addr <= x.offset + x.size);
		if (machoSection64Bit == null)
		{
			return 0uL;
		}
		if (machoSection64Bit.sectname == "__bss")
		{
			throw new Exception();
		}
		return addr - machoSection64Bit.offset + machoSection64Bit.addr;
	}

	public override bool Search()
	{
		ulong num = 0uL;
		ulong num2 = 0uL;
		if (Version < 23.0)
		{
			MachoSection64Bit machoSection64Bit = sections.First((MachoSection64Bit x) => x.sectname == "__mod_init_func");
			ulong[] array = ReadClassArray<ulong>(machoSection64Bit.offset, machoSection64Bit.size / 8);
			foreach (ulong num3 in array)
			{
				if (num3 == 0)
				{
					continue;
				}
				bool flag = false;
				ulong num4 = 0uL;
				base.Position = MapVATR(num3);
				byte[] second = ReadBytes(4);
				if (FeatureBytes1.SequenceEqual(second))
				{
					second = ReadBytes(4);
					if (FeatureBytes2.SequenceEqual(second))
					{
						base.Position += 8uL;
						byte[] inst = ReadBytes(4);
						if (ArmUtils.IsAdr(inst))
						{
							num4 = ArmUtils.DecodeAdr(num3 + 16, inst);
							flag = true;
						}
					}
				}
				else
				{
					base.Position += 12uL;
					second = ReadBytes(4);
					if (FeatureBytes2.SequenceEqual(second))
					{
						second = ReadBytes(4);
						if (FeatureBytes1.SequenceEqual(second))
						{
							base.Position -= 16uL;
							byte[] inst2 = ReadBytes(4);
							if (ArmUtils.IsAdr(inst2))
							{
								num4 = ArmUtils.DecodeAdr(num3 + 8, inst2);
								flag = true;
							}
						}
					}
				}
				if (flag)
				{
					ulong num6 = (base.Position = MapVATR(num4));
					num = ArmUtils.DecodeAdrp(num4, ReadBytes(4));
					num += ArmUtils.DecodeAdd(ReadBytes(4));
					base.Position = num6 + 8;
					num2 = ArmUtils.DecodeAdrp(num4 + 8, ReadBytes(4));
					num2 += ArmUtils.DecodeAdd(ReadBytes(4));
				}
			}
		}
		if (Version == 23.0)
		{
			MachoSection64Bit machoSection64Bit2 = sections.First((MachoSection64Bit x) => x.sectname == "__mod_init_func");
			ulong[] array = ReadClassArray<ulong>(machoSection64Bit2.offset, machoSection64Bit2.size / 8);
			foreach (ulong num7 in array)
			{
				if (num7 == 0)
				{
					continue;
				}
				base.Position = MapVATR(num7) + 16;
				byte[] second2 = ReadBytes(4);
				if (FeatureBytes1.SequenceEqual(second2))
				{
					second2 = ReadBytes(4);
					if (FeatureBytes2.SequenceEqual(second2))
					{
						base.Position -= 16uL;
						ulong num8 = ArmUtils.DecodeAdr(num7 + 8, ReadBytes(4));
						ulong num10 = (base.Position = MapVATR(num8));
						num = ArmUtils.DecodeAdrp(num8, ReadBytes(4));
						num += ArmUtils.DecodeAdd(ReadBytes(4));
						base.Position = num10 + 8;
						num2 = ArmUtils.DecodeAdrp(num8 + 8, ReadBytes(4));
						num2 += ArmUtils.DecodeAdd(ReadBytes(4));
					}
				}
			}
		}
		if (Version >= 24.0)
		{
			MachoSection64Bit machoSection64Bit3 = sections.First((MachoSection64Bit x) => x.sectname == "__mod_init_func");
			ulong[] array = ReadClassArray<ulong>(machoSection64Bit3.offset, machoSection64Bit3.size / 8);
			foreach (ulong num11 in array)
			{
				if (num11 == 0)
				{
					continue;
				}
				base.Position = MapVATR(num11) + 16;
				byte[] second3 = ReadBytes(4);
				if (FeatureBytes2.SequenceEqual(second3))
				{
					second3 = ReadBytes(4);
					if (FeatureBytes1.SequenceEqual(second3))
					{
						base.Position -= 16uL;
						ulong num12 = ArmUtils.DecodeAdr(num11 + 8, ReadBytes(4));
						ulong num14 = (base.Position = MapVATR(num12));
						num = ArmUtils.DecodeAdrp(num12, ReadBytes(4));
						num += ArmUtils.DecodeAdd(ReadBytes(4));
						base.Position = num14 + 8;
						num2 = ArmUtils.DecodeAdrp(num12 + 8, ReadBytes(4));
						num2 += ArmUtils.DecodeAdd(ReadBytes(4));
					}
				}
			}
		}
		if (num != 0L && num2 != 0L)
		{
			Console.WriteLine("CodeRegistration : {0:x}", num);
			Console.WriteLine("MetadataRegistration : {0:x}", num2);
			Init(num, num2);
			return true;
		}
		return false;
	}

	public override bool PlusSearch(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		SectionHelper sectionHelper = GetSectionHelper(methodCount, typeDefinitionsCount, imageCount);
		ulong codeRegistration = sectionHelper.FindCodeRegistration();
		ulong metadataRegistration = sectionHelper.FindMetadataRegistration();
		return AutoPlusInit(codeRegistration, metadataRegistration);
	}

	public override bool SymbolSearch()
	{
		return false;
	}

	public override ulong GetRVA(ulong pointer)
	{
		return pointer - vmaddr;
	}

	public override SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		MachoSection64Bit[] array = sections.Where((MachoSection64Bit x) => x.sectname == "__const" || x.sectname == "__cstring" || x.sectname == "__data").ToArray();
		MachoSection64Bit[] array2 = sections.Where((MachoSection64Bit x) => x.flags == 2147484672u).ToArray();
		MachoSection64Bit[] array3 = sections.Where((MachoSection64Bit x) => x.flags == 1).ToArray();
		SectionHelper sectionHelper = new SectionHelper(this, methodCount, typeDefinitionsCount, m_metadataUsagesCount, imageCount);
		sectionHelper.SetSection(SearchSectionType.Exec, array2);
		sectionHelper.SetSection(SearchSectionType.Data, array);
		sectionHelper.SetSection(SearchSectionType.Bss, array3);
		return sectionHelper;
	}

	public override bool CheckDump()
	{
		return false;
	}

	public override ulong ReadUIntPtr()
	{
		ulong num = ReadUInt64();
		if (num > vmaddr + uint.MaxValue)
		{
			ulong addr = base.Position;
			MachoSection64Bit machoSection64Bit = sections.First((MachoSection64Bit x) => addr >= x.offset && addr <= x.offset + x.size);
			if (machoSection64Bit.sectname == "__const" || machoSection64Bit.sectname == "__data")
			{
				num = ((num - vmaddr) & 0xFFFFFFFFu) + vmaddr;
			}
		}
		return num;
	}
}
