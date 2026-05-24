namespace Il2CppToolkit.Model;

public class Il2CppMetadataRegistration
{
	public long genericClassesCount;

	public ulong genericClasses;

	public long genericInstsCount;

	public ulong genericInsts;

	public long genericMethodTableCount;

	public ulong genericMethodTable;

	public long typesCount;

	public ulong types;

	public long methodSpecsCount;

	public ulong methodSpecs;

	[Version(Max = 16.0)]
	public long methodReferencesCount;

	[Version(Max = 16.0)]
	public ulong methodReferences;

	public long fieldOffsetsCount;

	public ulong fieldOffsets;

	public long typeDefinitionsSizesCount;

	public ulong typeDefinitionsSizes;

	[Version(Min = 19.0)]
	public ulong metadataUsagesCount;

	[Version(Min = 19.0)]
	public ulong metadataUsages;
}
