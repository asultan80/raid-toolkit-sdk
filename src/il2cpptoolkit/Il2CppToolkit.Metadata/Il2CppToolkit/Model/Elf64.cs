using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppToolkit.Model;

public sealed class Elf64 : ElfBase
{
	private Elf64_Ehdr elfHeader;

	private Elf64_Phdr[] programSegment;

	private Elf64_Dyn[] dynamicSection;

	private Elf64_Sym[] symbolTable;

	private Elf64_Shdr[] sectionTable;

	private Elf64_Phdr pt_dynamic;

	public Elf64(Stream stream)
		: base(stream)
	{
		Load();
	}

	protected override void Load()
	{
		elfHeader = ReadClass<Elf64_Ehdr>(0uL);
		programSegment = ReadClassArray<Elf64_Phdr>(elfHeader.e_phoff, elfHeader.e_phnum);
		if (IsDumped)
		{
			FixedProgramSegment();
		}
		pt_dynamic = programSegment.First((Elf64_Phdr x) => x.p_type == 2);
		dynamicSection = ReadClassArray<Elf64_Dyn>(pt_dynamic.p_offset, pt_dynamic.p_filesz / 16);
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
			sectionTable = ReadClassArray<Elf64_Shdr>(elfHeader.e_shoff, elfHeader.e_shnum);
			ulong sh_offset = sectionTable[elfHeader.e_shstrndx].sh_offset;
			Elf64_Shdr[] array = sectionTable;
			foreach (Elf64_Shdr elf64_Shdr in array)
			{
				list.Add(ReadStringToNull(sh_offset + elf64_Shdr.sh_name));
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
		Elf64_Phdr elf64_Phdr = programSegment.First((Elf64_Phdr x) => addr >= x.p_vaddr && addr <= x.p_vaddr + x.p_memsz);
		return addr - elf64_Phdr.p_vaddr + elf64_Phdr.p_offset;
	}

	public override ulong MapRTVA(ulong addr)
	{
		Elf64_Phdr elf64_Phdr = programSegment.FirstOrDefault((Elf64_Phdr x) => addr >= x.p_offset && addr <= x.p_offset + x.p_filesz);
		if (elf64_Phdr == null)
		{
			return 0uL;
		}
		return addr - elf64_Phdr.p_offset + elf64_Phdr.p_vaddr;
	}

	public override bool Search()
	{
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
		ulong num = 0uL;
		ulong num2 = 0uL;
		ulong num3 = MapVATR(dynamicSection.First((Elf64_Dyn x) => x.d_tag == 5).d_un);
		Elf64_Sym[] array = symbolTable;
		foreach (Elf64_Sym elf64_Sym in array)
		{
			string text = ReadStringToNull(num3 + elf64_Sym.st_name);
			if (!(text == "g_CodeRegistration"))
			{
				if (text == "g_MetadataRegistration")
				{
					num2 = elf64_Sym.st_value;
				}
			}
			else
			{
				num = elf64_Sym.st_value;
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
			Elf64_Dyn elf64_Dyn = dynamicSection.FirstOrDefault((Elf64_Dyn x) => x.d_tag == 4);
			if (elf64_Dyn != null)
			{
				ulong position = MapVATR(elf64_Dyn.d_un);
				base.Position = position;
				ReadUInt32();
				num = ReadUInt32();
			}
			else
			{
				elf64_Dyn = dynamicSection.First((Elf64_Dyn x) => x.d_tag == 1879047925);
				ulong num3 = (base.Position = MapVATR(elf64_Dyn.d_un));
				uint num4 = ReadUInt32();
				uint num5 = ReadUInt32();
				uint num6 = ReadUInt32();
				ReadUInt32();
				ulong num7 = num3 + 16 + 8 * num6;
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
			ulong addr = MapVATR(dynamicSection.First((Elf64_Dyn x) => x.d_tag == 6).d_un);
			symbolTable = ReadClassArray<Elf64_Sym>(addr, num);
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
			ulong addr = MapVATR(dynamicSection.First((Elf64_Dyn x) => x.d_tag == 7).d_un);
			ulong d_un = dynamicSection.First((Elf64_Dyn x) => x.d_tag == 8).d_un;
			Elf64_Rela[] array = ReadClassArray<Elf64_Rela>(addr, d_un / 24);
			foreach (Elf64_Rela elf64_Rela in array)
			{
				ulong num2 = elf64_Rela.r_info & 0xFFFFFFFFu;
				ulong num3 = elf64_Rela.r_info >> 32;
				ushort e_machine = elfHeader.e_machine;
				(ulong, bool) tuple;
				if (num2 <= 8)
				{
					if (num2 != 1)
					{
						if (num2 != 8 || e_machine != 62)
						{
							goto IL_0168;
						}
						tuple = (elf64_Rela.r_addend, true);
					}
					else
					{
						if (e_machine != 62)
						{
							goto IL_0168;
						}
						tuple = (symbolTable[num3].st_value + elf64_Rela.r_addend, true);
					}
				}
				else if (num2 != 257)
				{
					if (num2 != 1027 || e_machine != 183)
					{
						goto IL_0168;
					}
					tuple = (elf64_Rela.r_addend, true);
				}
				else
				{
					if (e_machine != 183)
					{
						goto IL_0168;
					}
					tuple = (symbolTable[num3].st_value + elf64_Rela.r_addend, true);
				}
				goto IL_0172;
				IL_0168:
				tuple = (0uL, false);
				goto IL_0172;
				IL_0172:
				(ulong, bool) tuple2 = tuple;
				if (tuple2.Item2)
				{
					base.Position = MapVATR(elf64_Rela.r_offset);
					Write(tuple2.Item1);
				}
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
			if (dynamicSection.Any((Elf64_Dyn x) => x.d_tag == 12))
			{
				Console.WriteLine("WARNING: find .init_proc");
				return true;
			}
			ulong num = MapVATR(dynamicSection.First((Elf64_Dyn x) => x.d_tag == 5).d_un);
			Elf64_Sym[] array = symbolTable;
			foreach (Elf64_Sym elf64_Sym in array)
			{
				if (ReadStringToNull(num + elf64_Sym.st_name) == "JNI_OnLoad")
				{
					Console.WriteLine("WARNING: find JNI_OnLoad");
					return true;
				}
			}
			if (sectionTable != null && sectionTable.Any((Elf64_Shdr x) => x.sh_type == 2147483648u))
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
			base.Position = elfHeader.e_phoff + num * 56 + 8;
			Elf64_Phdr elf64_Phdr = programSegment[num];
			elf64_Phdr.p_offset = elf64_Phdr.p_vaddr;
			Write(elf64_Phdr.p_offset);
			elf64_Phdr.p_vaddr += ImageBase;
			Write(elf64_Phdr.p_vaddr);
			base.Position += 8uL;
			elf64_Phdr.p_filesz = elf64_Phdr.p_memsz;
			Write(elf64_Phdr.p_filesz);
		}
	}

	private void FixedDynamicSection()
	{
		for (uint num = 0u; num < dynamicSection.Length; num++)
		{
			base.Position = pt_dynamic.p_offset + num * 16 + 8;
			Elf64_Dyn elf64_Dyn = dynamicSection[num];
			long d_tag = elf64_Dyn.d_tag;
			long num2 = d_tag - 3;
			if ((ulong)num2 <= 14uL)
			{
				switch (num2)
				{
				case 0L:
				case 1L:
				case 2L:
				case 3L:
				case 4L:
				case 9L:
				case 10L:
				case 14L:
					goto IL_008f;
				case 5L:
				case 6L:
				case 7L:
				case 8L:
				case 11L:
				case 12L:
				case 13L:
					continue;
				}
			}
			if (d_tag != 23 && (ulong)(d_tag - 25) > 1uL)
			{
				continue;
			}
			goto IL_008f;
			IL_008f:
			elf64_Dyn.d_un += ImageBase;
			Write(elf64_Dyn.d_un);
		}
	}

	public override SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		List<Elf64_Phdr> list = new List<Elf64_Phdr>();
		List<Elf64_Phdr> list2 = new List<Elf64_Phdr>();
		Elf64_Phdr[] array = programSegment;
		foreach (Elf64_Phdr elf64_Phdr in array)
		{
			if (elf64_Phdr.p_memsz != 0L)
			{
				switch (elf64_Phdr.p_flags)
				{
				case 1u:
				case 3u:
				case 5u:
				case 7u:
					list2.Add(elf64_Phdr);
					break;
				case 2u:
				case 4u:
				case 6u:
					list.Add(elf64_Phdr);
					break;
				}
			}
		}
		Elf64_Phdr[] sections = list.ToArray();
		Elf64_Phdr[] sections2 = list2.ToArray();
		SectionHelper sectionHelper = new SectionHelper(this, methodCount, typeDefinitionsCount, m_metadataUsagesCount, imageCount);
		sectionHelper.SetSection(SearchSectionType.Exec, sections2);
		sectionHelper.SetSection(SearchSectionType.Data, sections);
		sectionHelper.SetSection(SearchSectionType.Bss, sections);
		return sectionHelper;
	}
}
