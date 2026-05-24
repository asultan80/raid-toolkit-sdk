using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppToolkit.Model;

public sealed class NSO : Il2Cpp
{
	private readonly NSOHeader header;

	private readonly bool isTextCompressed;

	private readonly bool isRoDataCompressed;

	private readonly bool isDataCompressed;

	private readonly List<NSOSegmentHeader> segments = new List<NSOSegmentHeader>();

	private Elf64_Sym[] symbolTable;

	private readonly List<Elf64_Dyn> dynamicSection = new List<Elf64_Dyn>();

	private bool IsCompressed
	{
		get
		{
			if (!isTextCompressed && !isRoDataCompressed)
			{
				return isDataCompressed;
			}
			return true;
		}
	}

	public NSO(Stream stream)
		: base(stream)
	{
		header = new NSOHeader
		{
			Magic = ReadUInt32(),
			Version = ReadUInt32(),
			Reserved = ReadUInt32(),
			Flags = ReadUInt32()
		};
		isTextCompressed = (header.Flags & 1) != 0;
		isRoDataCompressed = (header.Flags & 2) != 0;
		isDataCompressed = (header.Flags & 4) != 0;
		header.TextSegment = new NSOSegmentHeader
		{
			FileOffset = ReadUInt32(),
			MemoryOffset = ReadUInt32(),
			DecompressedSize = ReadUInt32()
		};
		segments.Add(header.TextSegment);
		header.ModuleOffset = ReadUInt32();
		header.RoDataSegment = new NSOSegmentHeader
		{
			FileOffset = ReadUInt32(),
			MemoryOffset = ReadUInt32(),
			DecompressedSize = ReadUInt32()
		};
		segments.Add(header.RoDataSegment);
		header.ModuleFileSize = ReadUInt32();
		header.DataSegment = new NSOSegmentHeader
		{
			FileOffset = ReadUInt32(),
			MemoryOffset = ReadUInt32(),
			DecompressedSize = ReadUInt32()
		};
		segments.Add(header.DataSegment);
		header.BssSize = ReadUInt32();
		header.DigestBuildID = ReadBytes(32);
		header.TextCompressedSize = ReadUInt32();
		header.RoDataCompressedSize = ReadUInt32();
		header.DataCompressedSize = ReadUInt32();
		header.Padding = ReadBytes(28);
		header.APIInfo = new NSORelativeExtent
		{
			RegionRoDataOffset = ReadUInt32(),
			RegionSize = ReadUInt32()
		};
		header.DynStr = new NSORelativeExtent
		{
			RegionRoDataOffset = ReadUInt32(),
			RegionSize = ReadUInt32()
		};
		header.DynSym = new NSORelativeExtent
		{
			RegionRoDataOffset = ReadUInt32(),
			RegionSize = ReadUInt32()
		};
		header.TextHash = ReadBytes(32);
		header.RoDataHash = ReadBytes(32);
		header.DataHash = ReadBytes(32);
		if (IsCompressed)
		{
			return;
		}
		base.Position = header.TextSegment.FileOffset + 4;
		uint num = ReadUInt32();
		base.Position = header.TextSegment.FileOffset + num + 4;
		uint num2 = ReadUInt32() + num;
		uint num3 = ReadUInt32();
		uint num4 = ReadUInt32();
		header.BssSegment = new NSOSegmentHeader
		{
			FileOffset = num3,
			MemoryOffset = num3,
			DecompressedSize = num4 - num3
		};
		uint num5 = (header.DataSegment.MemoryOffset + header.DataSegment.DecompressedSize - num2) / 16;
		base.Position = MapVATR(num2);
		for (int i = 0; i < num5; i++)
		{
			Elf64_Dyn elf64_Dyn = ReadClass<Elf64_Dyn>();
			if (elf64_Dyn.d_tag == 0L)
			{
				break;
			}
			dynamicSection.Add(elf64_Dyn);
		}
		ReadSymbol();
		RelocationProcessing();
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
				ulong num = elf64_Rela.r_info & 0xFFFFFFFFu;
				ulong num2 = elf64_Rela.r_info >> 32;
				switch (num)
				{
				case 257uL:
				{
					Elf64_Sym elf64_Sym = symbolTable[num2];
					base.Position = MapVATR(elf64_Rela.r_offset);
					Write(elf64_Sym.st_value + elf64_Rela.r_addend);
					break;
				}
				case 1027uL:
					base.Position = MapVATR(elf64_Rela.r_offset);
					Write(elf64_Rela.r_addend);
					break;
				}
			}
		}
		catch
		{
		}
	}

	public override ulong MapVATR(ulong addr)
	{
		NSOSegmentHeader nSOSegmentHeader = segments.First((NSOSegmentHeader x) => addr >= x.MemoryOffset && addr <= x.MemoryOffset + x.DecompressedSize);
		return addr - nSOSegmentHeader.MemoryOffset + nSOSegmentHeader.FileOffset;
	}

	public override ulong MapRTVA(ulong addr)
	{
		NSOSegmentHeader nSOSegmentHeader = segments.FirstOrDefault((NSOSegmentHeader x) => addr >= x.FileOffset && addr <= x.FileOffset + x.DecompressedSize);
		if (nSOSegmentHeader == null)
		{
			return 0uL;
		}
		return addr - nSOSegmentHeader.FileOffset + nSOSegmentHeader.MemoryOffset;
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

	public NSO UnCompress()
	{
		if (isTextCompressed || isRoDataCompressed || isDataCompressed)
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(header.Magic);
			binaryWriter.Write(header.Version);
			binaryWriter.Write(header.Reserved);
			binaryWriter.Write(0);
			binaryWriter.Write(header.TextSegment.FileOffset);
			binaryWriter.Write(header.TextSegment.MemoryOffset);
			binaryWriter.Write(header.TextSegment.DecompressedSize);
			binaryWriter.Write(header.ModuleOffset);
			uint num = header.TextSegment.FileOffset + header.TextSegment.DecompressedSize;
			binaryWriter.Write(num);
			binaryWriter.Write(header.RoDataSegment.MemoryOffset);
			binaryWriter.Write(header.RoDataSegment.DecompressedSize);
			binaryWriter.Write(header.ModuleFileSize);
			binaryWriter.Write(num + header.RoDataSegment.DecompressedSize);
			binaryWriter.Write(header.DataSegment.MemoryOffset);
			binaryWriter.Write(header.DataSegment.DecompressedSize);
			binaryWriter.Write(header.BssSize);
			binaryWriter.Write(header.DigestBuildID);
			binaryWriter.Write(header.TextCompressedSize);
			binaryWriter.Write(header.RoDataCompressedSize);
			binaryWriter.Write(header.DataCompressedSize);
			binaryWriter.Write(header.Padding);
			binaryWriter.Write(header.APIInfo.RegionRoDataOffset);
			binaryWriter.Write(header.APIInfo.RegionSize);
			binaryWriter.Write(header.DynStr.RegionRoDataOffset);
			binaryWriter.Write(header.DynStr.RegionSize);
			binaryWriter.Write(header.DynSym.RegionRoDataOffset);
			binaryWriter.Write(header.DynSym.RegionSize);
			binaryWriter.Write(header.TextHash);
			binaryWriter.Write(header.RoDataHash);
			binaryWriter.Write(header.DataHash);
			binaryWriter.BaseStream.Position = header.TextSegment.FileOffset;
			base.Position = header.TextSegment.FileOffset;
			byte[] buffer = ReadBytes((int)header.TextCompressedSize);
			if (isTextCompressed)
			{
				byte[] array = new byte[header.TextSegment.DecompressedSize];
				using (Lz4DecoderStream lz4DecoderStream = new Lz4DecoderStream(new MemoryStream(buffer)))
				{
					lz4DecoderStream.Read(array, 0, array.Length);
				}
				binaryWriter.Write(array);
			}
			else
			{
				binaryWriter.Write(buffer);
			}
			byte[] buffer2 = ReadBytes((int)header.RoDataCompressedSize);
			if (isRoDataCompressed)
			{
				byte[] array2 = new byte[header.RoDataSegment.DecompressedSize];
				using (Lz4DecoderStream lz4DecoderStream2 = new Lz4DecoderStream(new MemoryStream(buffer2)))
				{
					lz4DecoderStream2.Read(array2, 0, array2.Length);
				}
				binaryWriter.Write(array2);
			}
			else
			{
				binaryWriter.Write(buffer2);
			}
			byte[] buffer3 = ReadBytes((int)header.DataCompressedSize);
			if (isDataCompressed)
			{
				byte[] array3 = new byte[header.DataSegment.DecompressedSize];
				using (Lz4DecoderStream lz4DecoderStream3 = new Lz4DecoderStream(new MemoryStream(buffer3)))
				{
					lz4DecoderStream3.Read(array3, 0, array3.Length);
				}
				binaryWriter.Write(array3);
			}
			else
			{
				binaryWriter.Write(buffer3);
			}
			binaryWriter.Flush();
			memoryStream.Position = 0L;
			return new NSO(memoryStream);
		}
		return this;
	}

	public override SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		SectionHelper sectionHelper = new SectionHelper(this, methodCount, typeDefinitionsCount, m_metadataUsagesCount, imageCount);
		sectionHelper.SetSection(SearchSectionType.Exec, header.TextSegment);
		sectionHelper.SetSection(SearchSectionType.Data, header.DataSegment, header.RoDataSegment);
		sectionHelper.SetSection(SearchSectionType.Bss, header.BssSegment);
		return sectionHelper;
	}

	public override bool CheckDump()
	{
		return false;
	}
}
