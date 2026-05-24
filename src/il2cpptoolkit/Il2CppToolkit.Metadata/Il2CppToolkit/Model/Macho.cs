using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Il2CppToolkit.Model;

public sealed class Macho : Il2Cpp
{
	private static readonly byte[] FeatureBytes1 = new byte[2] { 0, 34 };

	private static readonly byte[] FeatureBytes2 = new byte[4] { 120, 68, 121, 68 };

	private readonly List<MachoSection> sections = new List<MachoSection>();

	private readonly ulong vmaddr;

	public Macho(Stream stream)
		: base(stream)
	{
		Is32Bit = true;
		base.Position += 16uL;
		uint num = ReadUInt32();
		base.Position += 8uL;
		for (int i = 0; i < num; i++)
		{
			ulong position = base.Position;
			uint num2 = ReadUInt32();
			uint num3 = ReadUInt32();
			switch (num2)
			{
			case 1u:
			{
				if (Encoding.UTF8.GetString(ReadBytes(16)).TrimEnd('\0') == "__TEXT")
				{
					vmaddr = ReadUInt32();
				}
				else
				{
					base.Position += 4uL;
				}
				base.Position += 20uL;
				uint num4 = ReadUInt32();
				base.Position += 4uL;
				for (int j = 0; j < num4; j++)
				{
					MachoSection machoSection = new MachoSection();
					sections.Add(machoSection);
					machoSection.sectname = Encoding.UTF8.GetString(ReadBytes(16)).TrimEnd('\0');
					base.Position += 16uL;
					machoSection.addr = ReadUInt32();
					machoSection.size = ReadUInt32();
					machoSection.offset = ReadUInt32();
					base.Position += 12uL;
					machoSection.flags = ReadUInt32();
					base.Position += 8uL;
				}
				break;
			}
			case 33u:
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

	public override void Init(ulong codeRegistration, ulong metadataRegistration)
	{
		base.Init(codeRegistration, metadataRegistration);
		MethodPointers = MethodPointers.Select((ulong x) => x - 1).ToArray();
		CustomAttributeGenerators = CustomAttributeGenerators.Select((ulong x) => x - 1).ToArray();
	}

	public override ulong MapVATR(ulong addr)
	{
		MachoSection machoSection = sections.First((MachoSection x) => addr >= x.addr && addr <= x.addr + x.size);
		return addr - machoSection.addr + machoSection.offset;
	}

	public override ulong MapRTVA(ulong addr)
	{
		MachoSection machoSection = sections.FirstOrDefault((MachoSection x) => addr >= x.offset && addr <= x.offset + x.size);
		if (machoSection == null)
		{
			return 0uL;
		}
		return addr - machoSection.offset + machoSection.addr;
	}

	public override bool Search()
	{
		uint[] array;
		if (Version < 21.0)
		{
			MachoSection machoSection = sections.First((MachoSection x) => x.sectname == "__mod_init_func");
			array = ReadClassArray<uint>(machoSection.offset, machoSection.size / 4);
			foreach (uint num in array)
			{
				if (num == 0)
				{
					continue;
				}
				uint num2 = num - 1;
				base.Position = MapVATR(num2);
				base.Position += 4uL;
				byte[] second = ReadBytes(2);
				if (FeatureBytes1.SequenceEqual(second))
				{
					base.Position += 12uL;
					second = ReadBytes(4);
					if (FeatureBytes2.SequenceEqual(second))
					{
						base.Position = MapVATR(num2) + 10;
						uint num3 = ArmUtils.DecodeMov(ReadBytes(8)) + num2 + 24 - 1;
						ulong num5 = (base.Position = MapVATR(num3));
						uint num6 = ArmUtils.DecodeMov(ReadBytes(8)) + num3 + 16;
						base.Position = MapVATR(num6);
						uint num7 = ReadUInt32();
						base.Position = num5 + 8;
						second = ReadBytes(4);
						base.Position = num5 + 14;
						second = second.Concat(ReadBytes(4)).ToArray();
						uint num8 = ArmUtils.DecodeMov(second) + num3 + 22;
						Console.WriteLine("CodeRegistration : {0:x}", num8);
						Console.WriteLine("MetadataRegistration : {0:x}", num7);
						Init(num8, num7);
						return true;
					}
				}
			}
			return false;
		}
		MachoSection machoSection2 = sections.First((MachoSection x) => x.sectname == "__mod_init_func");
		array = ReadClassArray<uint>(machoSection2.offset, machoSection2.size / 4);
		foreach (uint num9 in array)
		{
			if (num9 == 0)
			{
				continue;
			}
			uint num10 = num9 - 1;
			base.Position = MapVATR(num10);
			base.Position += 4uL;
			byte[] second2 = ReadBytes(2);
			if (FeatureBytes1.SequenceEqual(second2))
			{
				base.Position += 12uL;
				second2 = ReadBytes(4);
				if (FeatureBytes2.SequenceEqual(second2))
				{
					base.Position = MapVATR(num10) + 10;
					uint num11 = ArmUtils.DecodeMov(ReadBytes(8)) + num10 + 24 - 1;
					ulong num13 = (base.Position = MapVATR(num11));
					uint num14 = ArmUtils.DecodeMov(ReadBytes(8)) + num11 + 16;
					base.Position = MapVATR(num14);
					uint num15 = ReadUInt32();
					base.Position = num13 + 8;
					second2 = ReadBytes(4);
					base.Position = num13 + 14;
					second2 = second2.Concat(ReadBytes(4)).ToArray();
					uint num16 = ArmUtils.DecodeMov(second2) + num11 + 26;
					Console.WriteLine("CodeRegistration : {0:x}", num16);
					Console.WriteLine("MetadataRegistration : {0:x}", num15);
					Init(num16, num15);
					return true;
				}
			}
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
		MachoSection[] array = sections.Where((MachoSection x) => x.sectname == "__const").ToArray();
		MachoSection[] array2 = sections.Where((MachoSection x) => x.flags == 2147484672u).ToArray();
		MachoSection[] array3 = sections.Where((MachoSection x) => x.flags == 1).ToArray();
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
}
