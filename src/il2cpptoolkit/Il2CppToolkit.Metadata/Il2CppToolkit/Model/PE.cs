using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppToolkit.Model;

public sealed class PE : Il2Cpp
{
	private readonly SectionHeader[] sections;

	public PE(Stream stream)
		: base(stream)
	{
		DosHeader dosHeader = ReadClass<DosHeader>();
		if (dosHeader.Magic != 23117)
		{
			throw new InvalidDataException("ERROR: Invalid PE file");
		}
		base.Position = dosHeader.Lfanew;
		if (ReadUInt32() != 17744)
		{
			throw new InvalidDataException("ERROR: Invalid PE file");
		}
		FileHeader fileHeader = ReadClass<FileHeader>();
		ulong position = base.Position;
		ushort num = ReadUInt16();
		base.Position -= 2uL;
		switch (num)
		{
		case 267:
		{
			Is32Bit = true;
			OptionalHeader optionalHeader2 = ReadClass<OptionalHeader>();
			ImageBase = optionalHeader2.ImageBase;
			break;
		}
		case 523:
		{
			OptionalHeader64 optionalHeader = ReadClass<OptionalHeader64>();
			ImageBase = optionalHeader.ImageBase;
			break;
		}
		default:
			throw new NotSupportedException($"Invalid Optional header magic {num}");
		}
		base.Position = position + fileHeader.SizeOfOptionalHeader;
		sections = ReadClassArray<SectionHeader>(fileHeader.NumberOfSections);
	}

	public void LoadFromMemory(ulong addr)
	{
		ImageBase = addr;
		SectionHeader[] array = sections;
		foreach (SectionHeader obj in array)
		{
			obj.PointerToRawData = obj.VirtualAddress;
			obj.SizeOfRawData = obj.VirtualSize;
		}
	}

	public override ulong MapVATR(ulong absAddr)
	{
		ulong addr = absAddr - ImageBase;
		SectionHeader sectionHeader = sections.FirstOrDefault((SectionHeader x) => addr >= x.VirtualAddress && addr <= x.VirtualAddress + x.VirtualSize);
		if (sectionHeader == null)
		{
			return 0uL;
		}
		return addr - sectionHeader.VirtualAddress + sectionHeader.PointerToRawData;
	}

	public override ulong MapRTVA(ulong addr)
	{
		SectionHeader sectionHeader = sections.FirstOrDefault((SectionHeader x) => addr >= x.PointerToRawData && addr <= x.PointerToRawData + x.SizeOfRawData);
		if (sectionHeader == null)
		{
			return 0uL;
		}
		return addr - sectionHeader.PointerToRawData + sectionHeader.VirtualAddress + ImageBase;
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
		return false;
	}

	public override ulong GetRVA(ulong pointer)
	{
		return pointer - ImageBase;
	}

	public override SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		List<SectionHeader> list = new List<SectionHeader>();
		List<SectionHeader> list2 = new List<SectionHeader>();
		SectionHeader[] array = sections;
		foreach (SectionHeader sectionHeader in array)
		{
			switch (sectionHeader.Characteristics)
			{
			case 1610612768u:
				list.Add(sectionHeader);
				break;
			case 3221225536u:
			case 1073741888u:
				list2.Add(sectionHeader);
				break;
			}
		}
		SectionHelper sectionHelper = new SectionHelper(this, methodCount, typeDefinitionsCount, m_metadataUsagesCount, imageCount);
		SectionHeader[] array2 = list2.ToArray();
		sectionHelper.SetSection(sections: list.ToArray(), type: SearchSectionType.Exec, imageBase: ImageBase);
		sectionHelper.SetSection(SearchSectionType.Data, ImageBase, array2);
		sectionHelper.SetSection(SearchSectionType.Bss, ImageBase, array2);
		return sectionHelper;
	}

	public override bool CheckDump()
	{
		if (Is32Bit)
		{
			return ImageBase != 268435456;
		}
		return ImageBase != 6442450944L;
	}
}
