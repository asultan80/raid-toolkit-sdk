namespace Il2CppToolkit.Model;

public class Il2CppGlobalMetadataHeader
{
	public uint sanity;

	public int version;

	public uint stringLiteralOffset;

	public int stringLiteralSize;

	public uint stringLiteralDataOffset;

	public int stringLiteralDataSize;

	public uint stringOffset;

	public int stringSize;

	public uint eventsOffset;

	public int eventsSize;

	public uint propertiesOffset;

	public int propertiesSize;

	public uint methodsOffset;

	public int methodsSize;

	public uint parameterDefaultValuesOffset;

	public int parameterDefaultValuesSize;

	public uint fieldDefaultValuesOffset;

	public int fieldDefaultValuesSize;

	public uint fieldAndParameterDefaultValueDataOffset;

	public int fieldAndParameterDefaultValueDataSize;

	public int fieldMarshaledSizesOffset;

	public int fieldMarshaledSizesSize;

	public uint parametersOffset;

	public int parametersSize;

	public uint fieldsOffset;

	public int fieldsSize;

	public uint genericParametersOffset;

	public int genericParametersSize;

	public uint genericParameterConstraintsOffset;

	public int genericParameterConstraintsSize;

	public uint genericContainersOffset;

	public int genericContainersSize;

	public uint nestedTypesOffset;

	public int nestedTypesSize;

	public uint interfacesOffset;

	public int interfacesSize;

	public uint vtableMethodsOffset;

	public int vtableMethodsSize;

	public int interfaceOffsetsOffset;

	public int interfaceOffsetsSize;

	public uint typeDefinitionsOffset;

	public int typeDefinitionsSize;

	[Version(Max = 24.1)]
	public uint rgctxEntriesOffset;

	[Version(Max = 24.1)]
	public int rgctxEntriesCount;

	public uint imagesOffset;

	public int imagesSize;

	public uint assembliesOffset;

	public int assembliesSize;

	[Version(Min = 19.0, Max = 24.5)]
	public uint metadataUsageListsOffset;

	[Version(Min = 19.0, Max = 24.5)]
	public int metadataUsageListsCount;

	[Version(Min = 19.0, Max = 24.5)]
	public uint metadataUsagePairsOffset;

	[Version(Min = 19.0, Max = 24.5)]
	public int metadataUsagePairsCount;

	[Version(Min = 19.0)]
	public uint fieldRefsOffset;

	[Version(Min = 19.0)]
	public int fieldRefsSize;

	[Version(Min = 20.0)]
	public int referencedAssembliesOffset;

	[Version(Min = 20.0)]
	public int referencedAssembliesSize;

	[Version(Min = 21.0, Max = 27.2)]
	public uint attributesInfoOffset;

	[Version(Min = 21.0, Max = 27.2)]
	public int attributesInfoCount;

	[Version(Min = 21.0, Max = 27.2)]
	public uint attributeTypesOffset;

	[Version(Min = 21.0, Max = 27.2)]
	public int attributeTypesCount;

	[Version(Min = 29.0)]
	public uint attributeDataOffset;

	[Version(Min = 29.0)]
	public int attributeDataSize;

	[Version(Min = 29.0)]
	public uint attributeDataRangeOffset;

	[Version(Min = 29.0)]
	public int attributeDataRangeSize;

	[Version(Min = 22.0)]
	public int unresolvedVirtualCallParameterTypesOffset;

	[Version(Min = 22.0)]
	public int unresolvedVirtualCallParameterTypesSize;

	[Version(Min = 22.0)]
	public int unresolvedVirtualCallParameterRangesOffset;

	[Version(Min = 22.0)]
	public int unresolvedVirtualCallParameterRangesSize;

	[Version(Min = 23.0)]
	public int windowsRuntimeTypeNamesOffset;

	[Version(Min = 23.0)]
	public int windowsRuntimeTypeNamesSize;

	[Version(Min = 27.0)]
	public int windowsRuntimeStringsOffset;

	[Version(Min = 27.0)]
	public int windowsRuntimeStringsSize;

	[Version(Min = 24.0)]
	public int exportedTypeDefinitionsOffset;

	[Version(Min = 24.0)]
	public int exportedTypeDefinitionsSize;
}
