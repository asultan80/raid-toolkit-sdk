using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppToolkit.Model;

public sealed class Elf : ElfBase
{
	private Elf32_Ehdr elfHeader;

	private Elf32_Phdr[] programSegment;

	private Elf32_Dyn[] dynamicSection;

	private Elf32_Sym[] symbolTable;

	private Elf32_Shdr[] sectionTable;

	private Elf32_Phdr pt_dynamic;

	private static readonly string ARMFeatureBytes = "? 0x10 ? 0xE7 ? 0x00 ? 0xE0 ? 0x20 ? 0xE0";

	private static readonly string X86FeatureBytes = "? 0x10 ? 0xE7 ? 0x00 ? 0xE0 ? 0x20 ? 0xE0";

	public Elf(Stream stream)
		: base(stream)
	{
		Is32Bit = true;
		Load();
	}

	protected override void Load()
	{
		elfHeader = ReadClass<Elf32_Ehdr>(0uL);
		programSegment = ReadClassArray<Elf32_Phdr>(elfHeader.e_phoff, elfHeader.e_phnum);
		if (IsDumped)
		{
			FixedProgramSegment();
		}
		pt_dynamic = programSegment.First((Elf32_Phdr x) => x.p_type == 2);
		dynamicSection = ReadClassArray<Elf32_Dyn>(pt_dynamic.p_offset, pt_dynamic.p_filesz / 8);
		if (IsDumped)
		{
			FixedDynamicSection();
		}
		ReadSymbol();
		if (!IsDumped)
		{
			RelocationProcessing();
			if (CheckProtection())
			{
				Console.WriteLine("ERROR: This file may be protected.");
			}
		}
	}

	protected override bool CheckSection()
	{
		try
		{
			List<string> list = new List<string>();
			sectionTable = ReadClassArray<Elf32_Shdr>(elfHeader.e_shoff, elfHeader.e_shnum);
			uint sh_offset = sectionTable[elfHeader.e_shstrndx].sh_offset;
			Elf32_Shdr[] array = sectionTable;
			foreach (Elf32_Shdr elf32_Shdr in array)
			{
				list.Add(ReadStringToNull(sh_offset + elf32_Shdr.sh_name));
			}
			if (!list.Contains(".text"))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public override ulong MapVATR(ulong addr)
	{
		Elf32_Phdr elf32_Phdr = programSegment.First((Elf32_Phdr x) => addr >= x.p_vaddr && addr <= x.p_vaddr + x.p_memsz);
		return addr - elf32_Phdr.p_vaddr + elf32_Phdr.p_offset;
	}

	public override ulong MapRTVA(ulong addr)
	{
		Elf32_Phdr elf32_Phdr = programSegment.FirstOrDefault((Elf32_Phdr x) => addr >= x.p_offset && addr <= x.p_offset + x.p_filesz);
		if (elf32_Phdr == null)
		{
			return 0uL;
		}
		return addr - elf32_Phdr.p_offset + elf32_Phdr.p_vaddr;
	}

	public override bool Search()
	{
		uint d_un = dynamicSection.First((Elf32_Dyn x) => x.d_tag == 3).d_un;
		Elf32_Phdr[] array = programSegment.Where((Elf32_Phdr x) => x.p_type == 1 && (x.p_flags & 1) == 1).ToArray();
		List<int> list = new List<int>();
		string stringPattern = ((elfHeader.e_machine == 40) ? ARMFeatureBytes : X86FeatureBytes);
		Elf32_Phdr[] array2 = array;
		foreach (Elf32_Phdr elf32_Phdr in array2)
		{
			base.Position = elf32_Phdr.p_offset;
			byte[] array3 = ReadBytes((int)elf32_Phdr.p_filesz);
			foreach (int item in array3.Search(stringPattern))
			{
				if (array3[item + 2].HexToBin()[3] == '1')
				{
					list.Add(item);
				}
			}
		}
		if (list.Count == 1)
		{
			uint num = 0u;
			uint num2 = 0u;
			uint num3 = (uint)list[0];
			if (Version < 24.0)
			{
				if (elfHeader.e_machine == 40)
				{
					base.Position = num3 + 20;
					num = ReadUInt32() + d_un;
					base.Position = num3 + 24;
					uint num4 = ReadUInt32() + d_un;
					base.Position = MapVATR(num4);
					num2 = ReadUInt32();
				}
			}
			else if (Version >= 24.0 && elfHeader.e_machine == 40)
			{
				base.Position = num3 + 20;
				num = ReadUInt32() + num3 + 12 + (uint)(int)ImageBase;
				base.Position = num3 + 16;
				uint num5 = ReadUInt32() + num3 + 8;
				base.Position = MapVATR(num5 + ImageBase);
				num2 = ReadUInt32();
			}
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
		uint num = 0u;
		uint num2 = 0u;
		ulong num3 = MapVATR(dynamicSection.First((Elf32_Dyn x) => x.d_tag == 5).d_un);
		Elf32_Sym[] array = symbolTable;
		foreach (Elf32_Sym elf32_Sym in array)
		{
			string text = ReadStringToNull(num3 + elf32_Sym.st_name);
			if (!(text == "g_CodeRegistration"))
			{
				if (text == "g_MetadataRegistration")
				{
					num2 = elf32_Sym.st_value;
				}
			}
			else
			{
				num = elf32_Sym.st_value;
			}
		}
		if (num != 0 && num2 != 0)
		{
			Console.WriteLine("Detected Symbol !");
			Console.WriteLine("CodeRegistration : {0:x}", num);
			Console.WriteLine("MetadataRegistration : {0:x}", num2);
			Init(num, num2);
			return true;
		}
		Console.WriteLine("ERROR: No symbol is detected");
		return false;
	}

	private void ReadSymbol()
	{
		try
		{
			uint num = 0u;
			Elf32_Dyn elf32_Dyn = dynamicSection.FirstOrDefault((Elf32_Dyn x) => x.d_tag == 4);
			if (elf32_Dyn != null)
			{
				ulong position = MapVATR(elf32_Dyn.d_un);
				base.Position = position;
				ReadUInt32();
				num = ReadUInt32();
			}
			else
			{
				elf32_Dyn = dynamicSection.First((Elf32_Dyn x) => x.d_tag == 1879047925);
				ulong num3 = (base.Position = MapVATR(elf32_Dyn.d_un));
				uint num4 = ReadUInt32();
				uint num5 = ReadUInt32();
				uint num6 = ReadUInt32();
				ReadUInt32();
				ulong num7 = num3 + 16 + 4 * num6;
				uint num8 = ReadClassArray<uint>(num7, num4).Max();
				if (num8 < num5)
				{
					num = num5;
				}
				else
				{
					ulong num9 = num7 + 4 * num4;
					base.Position = num9 + (num8 - num5) * 4;
					uint num10;
					do
					{
						num10 = ReadUInt32();
						num8++;
					}
					while ((num10 & 1) == 0);
					num = num8;
				}
			}
			ulong addr = MapVATR(dynamicSection.First((Elf32_Dyn x) => x.d_tag == 6).d_un);
			symbolTable = ReadClassArray<Elf32_Sym>(addr, num);
		}
		catch
		{
		}
	}

	private void RelocationProcessing()
	{
		Console.WriteLine("Applying relocations...");
		try
		{
			ulong addr = MapVATR(dynamicSection.First((Elf32_Dyn x) => x.d_tag == 17).d_un);
			uint d_un = dynamicSection.First((Elf32_Dyn x) => x.d_tag == 18).d_un;
			Elf32_Rel[] array = ReadClassArray<Elf32_Rel>(addr, d_un / 8);
			bool flag = elfHeader.e_machine == 3;
			Elf32_Rel[] array2 = array;
			foreach (Elf32_Rel elf32_Rel in array2)
			{
				uint num = elf32_Rel.r_info & 0xFF;
				uint num2 = elf32_Rel.r_info >> 8;
				uint num3 = num;
				if (num3 != 1)
				{
					if (num3 != 2 || flag)
					{
						continue;
					}
				}
				else if (!flag)
				{
					continue;
				}
				Elf32_Sym elf32_Sym = symbolTable[num2];
				base.Position = MapVATR(elf32_Rel.r_offset);
				Write(elf32_Sym.st_value);
			}
		}
		catch
		{
		}
	}

	private bool CheckProtection()
	{
		try
		{
			if (dynamicSection.Any((Elf32_Dyn x) => x.d_tag == 12))
			{
				Console.WriteLine("WARNING: find .init_proc");
				return true;
			}
			ulong num = MapVATR(dynamicSection.First((Elf32_Dyn x) => x.d_tag == 5).d_un);
			Elf32_Sym[] array = symbolTable;
			foreach (Elf32_Sym elf32_Sym in array)
			{
				if (ReadStringToNull(num + elf32_Sym.st_name) == "JNI_OnLoad")
				{
					Console.WriteLine("WARNING: find JNI_OnLoad");
					return true;
				}
			}
			if (sectionTable != null && sectionTable.Any((Elf32_Shdr x) => x.sh_type == 2147483648u))
			{
				Console.WriteLine("WARNING: find SHT_LOUSER section");
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public override ulong GetRVA(ulong pointer)
	{
		if (IsDumped)
		{
			return pointer - ImageBase;
		}
		return pointer;
	}

	private void FixedProgramSegment()
	{
		for (uint num = 0u; num < programSegment.Length; num++)
		{
			base.Position = elfHeader.e_phoff + num * 32 + 4;
			Elf32_Phdr elf32_Phdr = programSegment[num];
			elf32_Phdr.p_offset = elf32_Phdr.p_vaddr;
			Write(elf32_Phdr.p_offset);
			elf32_Phdr.p_vaddr += (uint)(int)ImageBase;
			Write(elf32_Phdr.p_vaddr);
			base.Position += 4uL;
			elf32_Phdr.p_filesz = elf32_Phdr.p_memsz;
			Write(elf32_Phdr.p_filesz);
		}
	}

	private void FixedDynamicSection()
	{
		for (uint num = 0u; num < dynamicSection.Length; num++)
		{
			base.Position = pt_dynamic.p_offset + num * 8 + 4;
			Elf32_Dyn elf32_Dyn = dynamicSection[num];
			switch (elf32_Dyn.d_tag)
			{
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 12:
			case 13:
			case 17:
			case 23:
			case 25:
			case 26:
				elf32_Dyn.d_un += (uint)(int)ImageBase;
				Write(elf32_Dyn.d_un);
				break;
			}
		}
	}

	public override SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		List<Elf32_Phdr> list = new List<Elf32_Phdr>();
		List<Elf32_Phdr> list2 = new List<Elf32_Phdr>();
		Elf32_Phdr[] array = programSegment;
		foreach (Elf32_Phdr elf32_Phdr in array)
		{
			if ((long)elf32_Phdr.p_memsz != 0L)
			{
				switch (elf32_Phdr.p_flags)
				{
				case 1u:
				case 3u:
				case 5u:
				case 7u:
					list2.Add(elf32_Phdr);
					break;
				case 2u:
				case 4u:
				case 6u:
					list.Add(elf32_Phdr);
					break;
				}
			}
		}
		Elf32_Phdr[] sections = list.ToArray();
		Elf32_Phdr[] sections2 = list2.ToArray();
		SectionHelper sectionHelper = new SectionHelper(this, methodCount, typeDefinitionsCount, m_metadataUsagesCount, imageCount);
		sectionHelper.SetSection(SearchSectionType.Exec, sections2);
		sectionHelper.SetSection(SearchSectionType.Data, sections);
		sectionHelper.SetSection(SearchSectionType.Bss, sections);
		return sectionHelper;
	}
}
