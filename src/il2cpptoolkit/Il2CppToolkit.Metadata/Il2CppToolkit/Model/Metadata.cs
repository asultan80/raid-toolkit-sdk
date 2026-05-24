using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppToolkit.Model;

public sealed class Metadata : BinaryStream
{
	public Il2CppGlobalMetadataHeader header;

	public Il2CppImageDefinition[] imageDefs;

	public Il2CppAssemblyDefinition[] assemblyDefs;

	public Il2CppTypeDefinition[] typeDefs;

	public Il2CppMethodDefinition[] methodDefs;

	public Il2CppParameterDefinition[] parameterDefs;

	public Il2CppFieldDefinition[] fieldDefs;

	private readonly Dictionary<int, Il2CppFieldDefaultValue> fieldDefaultValuesDic;

	private readonly Dictionary<int, Il2CppParameterDefaultValue> parameterDefaultValuesDic;

	public Il2CppPropertyDefinition[] propertyDefs;

	public Il2CppCustomAttributeTypeRange[] attributeTypeRanges;

	public Il2CppCustomAttributeDataRange[] attributeDataRanges;

	private readonly Dictionary<Il2CppImageDefinition, Dictionary<uint, int>> attributeTypeRangesDic;

	public Il2CppStringLiteral[] stringLiterals;

	private readonly Il2CppMetadataUsageList[] metadataUsageLists;

	private readonly Il2CppMetadataUsagePair[] metadataUsagePairs;

	public int[] attributeTypes;

	public int[] interfaceIndices;

	public Dictionary<Il2CppMetadataUsage, SortedDictionary<uint, uint>> metadataUsageDic;

	public long metadataUsagesCount;

	public int[] nestedTypeIndices;

	public Il2CppEventDefinition[] eventDefs;

	public Il2CppGenericContainer[] genericContainers;

	public Il2CppFieldRef[] fieldRefs;

	public Il2CppGenericParameter[] genericParameters;

	public int[] constraintIndices;

	public uint[] vtableMethods;

	public Il2CppRGCTXDefinition[] rgctxEntries;

	private readonly Dictionary<uint, string> stringCache = new Dictionary<uint, string>();

	public Metadata(Stream stream)
		: base(stream)
	{
		if (ReadUInt32() != 4205910959u)
		{
			throw new InvalidDataException("ERROR: Metadata file supplied is not valid metadata file.");
		}
		int num = ReadInt32();
		if (num < 0 || num > 1000)
		{
			throw new InvalidDataException("ERROR: Metadata file supplied is not valid metadata file.");
		}
		if (num < 16 || num > 31)
		{
			throw new NotSupportedException($"ERROR: Metadata file supplied is not a supported version[{num}].");
		}
		Version = num;
		header = ReadClass<Il2CppGlobalMetadataHeader>(0uL);
		if (num == 24)
		{
			if (header.stringLiteralOffset == 264)
			{
				Version = 24.2;
				header = ReadClass<Il2CppGlobalMetadataHeader>(0uL);
			}
			else
			{
				imageDefs = ReadMetadataClassArray<Il2CppImageDefinition>(header.imagesOffset, header.imagesSize);
				if (imageDefs.Any((Il2CppImageDefinition x) => x.token != 1))
				{
					Version = 24.1;
				}
			}
		}
		imageDefs = ReadMetadataClassArray<Il2CppImageDefinition>(header.imagesOffset, header.imagesSize);
		if (Version == 24.2 && header.assembliesSize / 68 < imageDefs.Length)
		{
			Version = 24.4;
		}
		bool flag = false;
		if (Version == 24.1 && header.assembliesSize / 64 == imageDefs.Length)
		{
			flag = true;
		}
		if (flag)
		{
			Version = 24.4;
		}
		assemblyDefs = ReadMetadataClassArray<Il2CppAssemblyDefinition>(header.assembliesOffset, header.assembliesSize);
		if (flag)
		{
			Version = 24.1;
		}
		typeDefs = ReadMetadataClassArray<Il2CppTypeDefinition>(header.typeDefinitionsOffset, header.typeDefinitionsSize);
		methodDefs = ReadMetadataClassArray<Il2CppMethodDefinition>(header.methodsOffset, header.methodsSize);
		parameterDefs = ReadMetadataClassArray<Il2CppParameterDefinition>(header.parametersOffset, header.parametersSize);
		fieldDefs = ReadMetadataClassArray<Il2CppFieldDefinition>(header.fieldsOffset, header.fieldsSize);
		Il2CppFieldDefaultValue[] source = ReadMetadataClassArray<Il2CppFieldDefaultValue>(header.fieldDefaultValuesOffset, header.fieldDefaultValuesSize);
		Il2CppParameterDefaultValue[] source2 = ReadMetadataClassArray<Il2CppParameterDefaultValue>(header.parameterDefaultValuesOffset, header.parameterDefaultValuesSize);
		fieldDefaultValuesDic = source.ToDictionary((Il2CppFieldDefaultValue x) => x.fieldIndex);
		parameterDefaultValuesDic = source2.ToDictionary((Il2CppParameterDefaultValue x) => x.parameterIndex);
		propertyDefs = ReadMetadataClassArray<Il2CppPropertyDefinition>(header.propertiesOffset, header.propertiesSize);
		interfaceIndices = ReadClassArray<int>(header.interfacesOffset, header.interfacesSize / 4);
		nestedTypeIndices = ReadClassArray<int>(header.nestedTypesOffset, header.nestedTypesSize / 4);
		eventDefs = ReadMetadataClassArray<Il2CppEventDefinition>(header.eventsOffset, header.eventsSize);
		genericContainers = ReadMetadataClassArray<Il2CppGenericContainer>(header.genericContainersOffset, header.genericContainersSize);
		genericParameters = ReadMetadataClassArray<Il2CppGenericParameter>(header.genericParametersOffset, header.genericParametersSize);
		constraintIndices = ReadClassArray<int>(header.genericParameterConstraintsOffset, header.genericParameterConstraintsSize / 4);
		vtableMethods = ReadClassArray<uint>(header.vtableMethodsOffset, header.vtableMethodsSize / 4);
		stringLiterals = ReadMetadataClassArray<Il2CppStringLiteral>(header.stringLiteralOffset, header.stringLiteralSize);
		if (Version > 16.0)
		{
			fieldRefs = ReadMetadataClassArray<Il2CppFieldRef>(header.fieldRefsOffset, header.fieldRefsSize);
			if (Version < 27.0)
			{
				metadataUsageLists = ReadMetadataClassArray<Il2CppMetadataUsageList>(header.metadataUsageListsOffset, header.metadataUsageListsCount);
				metadataUsagePairs = ReadMetadataClassArray<Il2CppMetadataUsagePair>(header.metadataUsagePairsOffset, header.metadataUsagePairsCount);
				ProcessingMetadataUsage();
			}
		}
		if (Version > 20.0 && Version < 29.0)
		{
			attributeTypeRanges = ReadMetadataClassArray<Il2CppCustomAttributeTypeRange>(header.attributesInfoOffset, header.attributesInfoCount);
			attributeTypes = ReadClassArray<int>(header.attributeTypesOffset, header.attributeTypesCount / 4);
		}
		if (Version >= 29.0)
		{
			attributeDataRanges = ReadMetadataClassArray<Il2CppCustomAttributeDataRange>(header.attributeDataRangeOffset, header.attributeDataRangeSize);
		}
		if (Version > 24.0)
		{
			attributeTypeRangesDic = new Dictionary<Il2CppImageDefinition, Dictionary<uint, int>>();
			Il2CppImageDefinition[] array = imageDefs;
			foreach (Il2CppImageDefinition il2CppImageDefinition in array)
			{
				Dictionary<uint, int> dictionary = new Dictionary<uint, int>();
				attributeTypeRangesDic[il2CppImageDefinition] = dictionary;
				long num2 = il2CppImageDefinition.customAttributeStart + il2CppImageDefinition.customAttributeCount;
				for (int j = il2CppImageDefinition.customAttributeStart; j < num2; j++)
				{
					if (Version >= 29.0)
					{
						dictionary.Add(attributeDataRanges[j].token, j);
					}
					else
					{
						dictionary.Add(attributeTypeRanges[j].token, j);
					}
				}
			}
		}
		if (Version <= 24.1)
		{
			rgctxEntries = ReadMetadataClassArray<Il2CppRGCTXDefinition>(header.rgctxEntriesOffset, header.rgctxEntriesCount);
		}
	}

	private T[] ReadMetadataClassArray<T>(uint addr, int count) where T : new()
	{
		return ReadClassArray<T>(addr, count / SizeOf(typeof(T)));
	}

	public bool GetFieldDefaultValueFromIndex(int index, out Il2CppFieldDefaultValue value)
	{
		return fieldDefaultValuesDic.TryGetValue(index, out value);
	}

	public bool GetParameterDefaultValueFromIndex(int index, out Il2CppParameterDefaultValue value)
	{
		return parameterDefaultValuesDic.TryGetValue(index, out value);
	}

	public uint GetDefaultValueFromIndex(int index)
	{
		return (uint)(header.fieldAndParameterDefaultValueDataOffset + index);
	}

	public string GetStringFromIndex(uint index)
	{
		if (!stringCache.TryGetValue(index, out var value))
		{
			value = ReadStringToNull(header.stringOffset + index);
			stringCache.Add(index, value);
		}
		return value;
	}

	public int GetCustomAttributeIndex(Il2CppImageDefinition imageDef, int customAttributeIndex, uint token)
	{
		if (Version > 24.0)
		{
			if (attributeTypeRangesDic[imageDef].TryGetValue(token, out var value))
			{
				return value;
			}
			return -1;
		}
		return customAttributeIndex;
	}

	public string GetStringLiteralFromIndex(uint index)
	{
		Il2CppStringLiteral il2CppStringLiteral = stringLiterals[index];
		base.Position = (uint)(header.stringLiteralDataOffset + il2CppStringLiteral.dataIndex);
		return Encoding.UTF8.GetString(ReadBytes((int)il2CppStringLiteral.length));
	}

	private void ProcessingMetadataUsage()
	{
		metadataUsageDic = new Dictionary<Il2CppMetadataUsage, SortedDictionary<uint, uint>>();
		for (uint num = 1u; num <= 6; num++)
		{
			metadataUsageDic[(Il2CppMetadataUsage)num] = new SortedDictionary<uint, uint>();
		}
		Il2CppMetadataUsageList[] array = metadataUsageLists;
		foreach (Il2CppMetadataUsageList il2CppMetadataUsageList in array)
		{
			for (int j = 0; j < il2CppMetadataUsageList.count; j++)
			{
				long num2 = il2CppMetadataUsageList.start + j;
				if (num2 < metadataUsagePairs.Length)
				{
					Il2CppMetadataUsagePair il2CppMetadataUsagePair = metadataUsagePairs[num2];
					uint encodedIndexType = GetEncodedIndexType(il2CppMetadataUsagePair.encodedSourceIndex);
					uint decodedMethodIndex = GetDecodedMethodIndex(il2CppMetadataUsagePair.encodedSourceIndex);
					metadataUsageDic[(Il2CppMetadataUsage)encodedIndexType][il2CppMetadataUsagePair.destinationIndex] = decodedMethodIndex;
				}
			}
		}
		metadataUsagesCount = metadataUsageDic.Max((KeyValuePair<Il2CppMetadataUsage, SortedDictionary<uint, uint>> x) => x.Value.Select((KeyValuePair<uint, uint> y) => y.Key).DefaultIfEmpty().Max()) + 1;
	}

	public static uint GetEncodedIndexType(uint index)
	{
		return (index & 0xE0000000u) >> 29;
	}

	public uint GetDecodedMethodIndex(uint index)
	{
		if (Version >= 27.0)
		{
			return (index & 0x1FFFFFFE) >> 1;
		}
		return index & 0x1FFFFFFFu;
	}

	public int SizeOf(Type type)
	{
		int num = 0;
		FieldInfo[] fields = type.GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			VersionAttribute versionAttribute = (VersionAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(VersionAttribute));
			if (versionAttribute == null || (!(Version < versionAttribute.Min) && !(Version > versionAttribute.Max)))
			{
				Type fieldType = fieldInfo.FieldType;
				if (fieldType.IsPrimitive)
				{
					num += GetPrimitiveTypeSize(fieldType.Name);
				}
				else if (fieldType.IsEnum)
				{
					Type fieldType2 = fieldType.GetField("value__").FieldType;
					num += GetPrimitiveTypeSize(fieldType2.Name);
				}
				else if (fieldType.IsArray)
				{
					ArrayLengthAttribute customAttribute = fieldInfo.GetCustomAttribute<ArrayLengthAttribute>();
					num += customAttribute.Length;
				}
				else
				{
					num += SizeOf(fieldType);
				}
			}
		}
		return num;
		static int GetPrimitiveTypeSize(string name)
		{
			switch (name)
			{
			case "Int32":
			case "UInt32":
				return 4;
			case "Int16":
			case "UInt16":
				return 2;
			default:
				return 0;
			}
		}
	}
}
