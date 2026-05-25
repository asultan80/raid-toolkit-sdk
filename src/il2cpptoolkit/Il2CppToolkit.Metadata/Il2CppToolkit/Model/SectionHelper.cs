using System;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppToolkit.Model;

public class SectionHelper
{
	private List<SearchSection> exec;

	private List<SearchSection> data;

	private List<SearchSection> bss;

	private readonly Il2Cpp il2Cpp;

	private readonly int methodCount;

	private readonly int typeDefinitionsCount;

	private readonly long metadataUsagesCount;

	private readonly int imageCount;

	private bool pointerInExec;

	private static readonly byte[] featureBytes = new byte[13]
	{
		109, 115, 99, 111, 114, 108, 105, 98, 46, 100,
		108, 108, 0
	};

	public List<SearchSection> Exec => exec;

	public List<SearchSection> Data => data;

	public List<SearchSection> Bss => bss;

	public SectionHelper(Il2Cpp il2Cpp, int methodCount, int typeDefinitionsCount, long metadataUsagesCount, int imageCount)
	{
		this.il2Cpp = il2Cpp;
		this.methodCount = methodCount;
		this.typeDefinitionsCount = typeDefinitionsCount;
		this.metadataUsagesCount = metadataUsagesCount;
		this.imageCount = imageCount;
	}

	public void SetSection(SearchSectionType type, Elf32_Phdr[] sections)
	{
		List<SearchSection> list = new List<SearchSection>();
		foreach (Elf32_Phdr elf32_Phdr in sections)
		{
			if (elf32_Phdr != null)
			{
				list.Add(new SearchSection
				{
					offset = elf32_Phdr.p_offset,
					offsetEnd = elf32_Phdr.p_offset + elf32_Phdr.p_filesz,
					address = elf32_Phdr.p_vaddr,
					addressEnd = elf32_Phdr.p_vaddr + elf32_Phdr.p_memsz
				});
			}
		}
		SetSection(type, list);
	}

	public void SetSection(SearchSectionType type, Elf64_Phdr[] sections)
	{
		List<SearchSection> list = new List<SearchSection>();
		foreach (Elf64_Phdr elf64_Phdr in sections)
		{
			if (elf64_Phdr != null)
			{
				list.Add(new SearchSection
				{
					offset = elf64_Phdr.p_offset,
					offsetEnd = elf64_Phdr.p_offset + elf64_Phdr.p_filesz,
					address = elf64_Phdr.p_vaddr,
					addressEnd = elf64_Phdr.p_vaddr + elf64_Phdr.p_memsz
				});
			}
		}
		SetSection(type, list);
	}

	public void SetSection(SearchSectionType type, MachoSection[] sections)
	{
		List<SearchSection> list = new List<SearchSection>();
		foreach (MachoSection machoSection in sections)
		{
			if (machoSection != null)
			{
				list.Add(new SearchSection
				{
					offset = machoSection.offset,
					offsetEnd = machoSection.offset + machoSection.size,
					address = machoSection.addr,
					addressEnd = machoSection.addr + machoSection.size
				});
			}
		}
		SetSection(type, list);
	}

	public void SetSection(SearchSectionType type, MachoSection64Bit[] sections)
	{
		List<SearchSection> list = new List<SearchSection>();
		foreach (MachoSection64Bit machoSection64Bit in sections)
		{
			if (machoSection64Bit != null)
			{
				list.Add(new SearchSection
				{
					offset = machoSection64Bit.offset,
					offsetEnd = machoSection64Bit.offset + machoSection64Bit.size,
					address = machoSection64Bit.addr,
					addressEnd = machoSection64Bit.addr + machoSection64Bit.size
				});
			}
		}
		SetSection(type, list);
	}

	public void SetSection(SearchSectionType type, ulong imageBase, SectionHeader[] sections)
	{
		List<SearchSection> list = new List<SearchSection>();
		foreach (SectionHeader sectionHeader in sections)
		{
			if (sectionHeader != null)
			{
				list.Add(new SearchSection
				{
					offset = sectionHeader.PointerToRawData,
					offsetEnd = sectionHeader.PointerToRawData + sectionHeader.SizeOfRawData,
					address = sectionHeader.VirtualAddress + imageBase,
					addressEnd = sectionHeader.VirtualAddress + sectionHeader.VirtualSize + imageBase
				});
			}
		}
		SetSection(type, list);
	}

	public void SetSection(SearchSectionType type, params NSOSegmentHeader[] sections)
	{
		List<SearchSection> list = new List<SearchSection>();
		foreach (NSOSegmentHeader nSOSegmentHeader in sections)
		{
			if (nSOSegmentHeader != null)
			{
				list.Add(new SearchSection
				{
					offset = nSOSegmentHeader.FileOffset,
					offsetEnd = nSOSegmentHeader.FileOffset + nSOSegmentHeader.DecompressedSize,
					address = nSOSegmentHeader.MemoryOffset,
					addressEnd = nSOSegmentHeader.MemoryOffset + nSOSegmentHeader.DecompressedSize
				});
			}
		}
		SetSection(type, list);
	}

	public void SetSection(SearchSectionType type, params SearchSection[] secs)
	{
		SetSection(type, secs.ToList());
	}

	private void SetSection(SearchSectionType type, List<SearchSection> secs)
	{
		switch (type)
		{
		case SearchSectionType.Exec:
			exec = secs;
			break;
		case SearchSectionType.Data:
			data = secs;
			break;
		case SearchSectionType.Bss:
			bss = secs;
			break;
		}
	}

	public ulong FindCodeRegistration()
	{
		if (il2Cpp.Version >= 24.2)
		{
			ulong num;
			if (il2Cpp is ElfBase)
			{
				num = FindCodeRegistrationExec();
				if (num == 0L)
				{
					num = FindCodeRegistrationData();
				}
				else
				{
					pointerInExec = true;
				}
			}
			else
			{
				num = FindCodeRegistrationData();
				if (num == 0L)
				{
					num = FindCodeRegistrationExec();
					pointerInExec = true;
				}
			}
			return num;
		}
		return FindCodeRegistrationOld();
	}

	public ulong FindMetadataRegistration()
	{
		if (il2Cpp.Version < 19.0)
		{
			return 0uL;
		}
		if (il2Cpp.Version >= 27.0)
		{
			return FindMetadataRegistrationV21();
		}
		return FindMetadataRegistrationOld();
	}

	private ulong FindCodeRegistrationOld()
	{
		foreach (SearchSection datum in data)
		{
			il2Cpp.Position = datum.offset;
			while (il2Cpp.Position < datum.offsetEnd)
			{
				ulong position = il2Cpp.Position;
				if (il2Cpp.ReadIntPtr() == methodCount)
				{
					try
					{
						ulong num = il2Cpp.MapVATR(il2Cpp.ReadUIntPtr());
						if (CheckPointerRangeDataRa(num))
						{
							ulong[] pointers = il2Cpp.ReadClassArray<ulong>(num, methodCount);
							if (CheckPointerRangeExecVa(pointers))
							{
								return position - datum.offset + datum.address;
							}
						}
					}
					catch
					{
					}
				}
				il2Cpp.Position = position + il2Cpp.PointerSize;
			}
		}
		return 0uL;
	}

	private ulong FindMetadataRegistrationOld()
	{
		foreach (SearchSection datum in data)
		{
			il2Cpp.Position = datum.offset;
			ulong num = Math.Min(datum.offsetEnd, il2Cpp.Length) - il2Cpp.PointerSize;
			while (il2Cpp.Position < num)
			{
				ulong position = il2Cpp.Position;
				if (il2Cpp.ReadIntPtr() == typeDefinitionsCount)
				{
					try
					{
						il2Cpp.Position += il2Cpp.PointerSize * 2;
						ulong num2 = il2Cpp.MapVATR(il2Cpp.ReadUIntPtr());
						if (CheckPointerRangeDataRa(num2))
						{
							ulong[] pointers = il2Cpp.ReadClassArray<ulong>(num2, metadataUsagesCount);
							if (CheckPointerRangeBssVa(pointers))
							{
								return position - il2Cpp.PointerSize * 12 - datum.offset + datum.address;
							}
						}
					}
					catch
					{
					}
				}
				il2Cpp.Position = position + il2Cpp.PointerSize;
			}
		}
		return 0uL;
	}

	private ulong FindMetadataRegistrationV21()
	{
		foreach (SearchSection datum in data)
		{
			il2Cpp.Position = datum.offset;
			ulong num = Math.Min(datum.offsetEnd, il2Cpp.Length) - il2Cpp.PointerSize;
			while (il2Cpp.Position < num)
			{
				ulong position = il2Cpp.Position;
				if (il2Cpp.ReadIntPtr() == typeDefinitionsCount)
				{
					il2Cpp.Position += il2Cpp.PointerSize;
					if (il2Cpp.ReadIntPtr() == typeDefinitionsCount)
					{
						try
						{
							ulong num2 = il2Cpp.MapVATR(il2Cpp.ReadUIntPtr());
							if (CheckPointerRangeDataRa(num2))
							{
								ulong[] pointers = il2Cpp.ReadClassArray<ulong>(num2, typeDefinitionsCount);
								if ((!pointerInExec) ? CheckPointerRangeDataVa(pointers) : CheckPointerRangeExecVa(pointers))
								{
									return position - il2Cpp.PointerSize * 10 - datum.offset + datum.address;
								}
							}
						}
						catch
						{
						}
					}
				}
				il2Cpp.Position = position + il2Cpp.PointerSize;
			}
		}
		return 0uL;
	}

	private bool CheckPointerRangeDataRa(ulong pointer)
	{
		return data.Any((SearchSection x) => pointer >= x.offset && pointer <= x.offsetEnd);
	}

	private bool CheckPointerRangeExecVa(ulong[] pointers)
	{
		return pointers.All((ulong x) => exec.Any((SearchSection y) => x >= y.address && x <= y.addressEnd));
	}

	private bool CheckPointerRangeDataVa(ulong[] pointers)
	{
		return pointers.All((ulong x) => data.Any((SearchSection y) => x >= y.address && x <= y.addressEnd));
	}

	private bool CheckPointerRangeBssVa(ulong[] pointers)
	{
		return pointers.All((ulong x) => bss.Any((SearchSection y) => x >= y.address && x <= y.addressEnd));
	}

	private ulong FindCodeRegistrationData()
	{
		return FindCodeRegistration2019(data);
	}

	private ulong FindCodeRegistrationExec()
	{
		return FindCodeRegistration2019(exec);
	}

	private ulong FindCodeRegistration2019(List<SearchSection> secs)
	{
		foreach (SearchSection sec in secs)
		{
			il2Cpp.Position = sec.offset;
			foreach (int item in il2Cpp.ReadBytes((int)(sec.offsetEnd - sec.offset)).Search(featureBytes))
			{
				ulong addr = (ulong)item + sec.address;
				foreach (ulong item2 in FindReference(addr))
				{
					foreach (ulong item3 in FindReference(item2))
					{
						if (il2Cpp.Version >= 27.0)
						{
							for (int num = imageCount - 1; num >= 0; num--)
							{
								foreach (ulong item4 in FindReference(item3 - (ulong)((long)num * (long)il2Cpp.PointerSize)))
								{
									il2Cpp.Position = il2Cpp.MapVATR(item4 - il2Cpp.PointerSize);
									if (il2Cpp.ReadIntPtr() == imageCount)
									{
										if (il2Cpp.Version >= 29.1)
										{
											return item4 - il2Cpp.PointerSize * 16;
										}
										if (il2Cpp.Version >= 29.0)
										{
											return item4 - il2Cpp.PointerSize * 14;
										}
										return item4 - il2Cpp.PointerSize * 13;
									}
								}
							}
							continue;
						}
						for (int i = 0; i < imageCount; i++)
						{
							using IEnumerator<ulong> enumerator5 = FindReference(item3 - (ulong)((long)i * (long)il2Cpp.PointerSize)).GetEnumerator();
							if (enumerator5.MoveNext())
							{
								return enumerator5.Current - il2Cpp.PointerSize * 13;
							}
						}
					}
				}
			}
		}
		return 0uL;
	}

	private IEnumerable<ulong> FindReference(ulong addr)
	{
		foreach (SearchSection dataSec in data)
		{
			ulong position = dataSec.offset;
			for (ulong end = Math.Min(dataSec.offsetEnd, il2Cpp.Length) - il2Cpp.PointerSize; position < end; position += il2Cpp.PointerSize)
			{
				il2Cpp.Position = position;
				if (il2Cpp.ReadUIntPtr() == addr)
				{
					yield return position - dataSec.offset + dataSec.address;
				}
			}
		}
	}
}
