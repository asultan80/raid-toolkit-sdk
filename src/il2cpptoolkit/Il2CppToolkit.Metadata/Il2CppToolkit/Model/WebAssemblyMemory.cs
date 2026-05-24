using System.IO;

namespace Il2CppToolkit.Model;

public sealed class WebAssemblyMemory : Il2Cpp
{
	private readonly uint bssStart;

	public WebAssemblyMemory(Stream stream, uint bssStart)
		: base(stream)
	{
		Is32Bit = true;
		this.bssStart = bssStart;
	}

	public override ulong MapVATR(ulong addr)
	{
		return addr;
	}

	public override ulong MapRTVA(ulong addr)
	{
		return addr;
	}

	public override bool PlusSearch(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		SectionHelper sectionHelper = GetSectionHelper(methodCount, typeDefinitionsCount, imageCount);
		ulong codeRegistration = sectionHelper.FindCodeRegistration();
		ulong metadataRegistration = sectionHelper.FindMetadataRegistration();
		return AutoPlusInit(codeRegistration, metadataRegistration);
	}

	public override bool Search()
	{
		return false;
	}

	public override bool SymbolSearch()
	{
		return false;
	}

	public override SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount)
	{
		SearchSection searchSection = new SearchSection
		{
			offset = 0uL,
			offsetEnd = (ulong)methodCount,
			address = 0uL,
			addressEnd = (ulong)methodCount
		};
		SearchSection searchSection2 = new SearchSection
		{
			offset = 1024uL,
			offsetEnd = base.Length,
			address = 1024uL,
			addressEnd = base.Length
		};
		SearchSection searchSection3 = new SearchSection
		{
			offset = bssStart,
			offsetEnd = 9223372036854775807uL,
			address = bssStart,
			addressEnd = 9223372036854775807uL
		};
		SectionHelper sectionHelper = new SectionHelper(this, methodCount, typeDefinitionsCount, m_metadataUsagesCount, imageCount);
		sectionHelper.SetSection(SearchSectionType.Exec, searchSection);
		sectionHelper.SetSection(SearchSectionType.Data, searchSection2);
		sectionHelper.SetSection(SearchSectionType.Bss, searchSection3);
		return sectionHelper;
	}

	public override bool CheckDump()
	{
		return false;
	}
}
