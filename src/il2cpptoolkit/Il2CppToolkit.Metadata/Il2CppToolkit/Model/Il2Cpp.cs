using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppToolkit.Model;

public abstract class Il2Cpp : BinaryStream
{
	private Il2CppMetadataRegistration m_metadataRegistration;

	private Il2CppCodeRegistration m_codeRegistration;

	private Il2CppGenericMethodFunctionsDefinitions[] m_genericMethodTable;

	public Il2CppTypeDefinitionSizes[] TypeDefinitionSizes;

	public ulong[] MethodPointers;

	public ulong[] GenericMethodPointers;

	public ulong[] InvokerPointers;

	public ulong[] CustomAttributeGenerators;

	public ulong[] ReversePInvokeWrappers;

	public ulong[] UnresolvedVirtualCallPointers;

	public ulong[] FieldOffsets;

	public Il2CppType[] Types;

	private Dictionary<ulong, Il2CppType> m_typeDic = new Dictionary<ulong, Il2CppType>();

	public ulong[] MetadataUsages;

	public ulong[] GenericInstPointers;

	public Il2CppGenericInst[] GenericInsts;

	public Il2CppMethodSpec[] MethodSpecs;

	public Dictionary<int, List<Il2CppMethodSpec>> MethodDefinitionMethodSpecs = new Dictionary<int, List<Il2CppMethodSpec>>();

	public Dictionary<Il2CppMethodSpec, ulong> MethodSpecGenericMethodPointers = new Dictionary<Il2CppMethodSpec, ulong>();

	public bool FieldOffsetsArePointers;

	protected long m_metadataUsagesCount;

	public Dictionary<string, Il2CppCodeGenModule> CodeGenModules;

	public Dictionary<string, ulong[]> CodeGenModuleMethodPointers;

	public Dictionary<string, Dictionary<uint, Il2CppRGCTXDefinition[]>> RGCTXDictionary;

	public bool IsDumped;

	public abstract ulong MapVATR(ulong addr);

	public abstract ulong MapRTVA(ulong addr);

	public abstract bool Search();

	public abstract bool PlusSearch(int methodCount, int typeDefinitionsCount, int imageCount);

	public abstract bool SymbolSearch();

	public abstract SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount);

	public abstract bool CheckDump();

	protected Il2Cpp(Stream stream)
		: base(stream)
	{
	}

	public void SetProperties(double version, long metadataUsagesCount)
	{
		Version = version;
		m_metadataUsagesCount = metadataUsagesCount;
	}

	protected bool AutoPlusInit(ulong codeRegistration, ulong metadataRegistration)
	{
		if (codeRegistration != 0L)
		{
			uint num = ((this is WebAssemblyMemory) ? 217088u : 327680u);
			if (Version >= 24.2)
			{
				m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
				double version = Version;
				if (version != 29.0)
				{
					if (version != 27.0)
					{
						if (version != 24.4)
						{
							if (version == 24.2 && m_codeRegistration.interopDataCount == 0L)
							{
								Version = 24.3;
								codeRegistration -= base.PointerSize * 2;
								Console.WriteLine($"Change il2cpp version to: {Version}");
							}
						}
						else
						{
							codeRegistration -= base.PointerSize * 2;
							if (m_codeRegistration.reversePInvokeWrapperCount > num)
							{
								Version = 24.5;
								codeRegistration -= base.PointerSize;
								Console.WriteLine($"Change il2cpp version to: {Version}");
							}
						}
					}
					else if (m_codeRegistration.reversePInvokeWrapperCount > num)
					{
						Version = 27.1;
						codeRegistration -= base.PointerSize;
						Console.WriteLine($"Change il2cpp version to: {Version}");
					}
				}
				else if (m_codeRegistration.genericMethodPointersCount > num)
				{
					Version = 29.1;
					codeRegistration -= base.PointerSize * 2;
					Console.WriteLine($"Change il2cpp version to: {Version}");
				}
			}
		}
		Console.WriteLine("CodeRegistration : {0:x}", codeRegistration);
		Console.WriteLine("MetadataRegistration : {0:x}", metadataRegistration);
		if (codeRegistration != 0L && metadataRegistration != 0L)
		{
			Init(codeRegistration, metadataRegistration);
			return true;
		}
		return false;
	}

	public virtual void Init(ulong codeRegistration, ulong metadataRegistration)
	{
		m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
		uint limit = ((this is WebAssemblyMemory) ? 217088u : 327680u);
		if (Version == 27.0 && m_codeRegistration.invokerPointersCount > limit)
		{
			Version = 27.1;
			Console.WriteLine($"Change il2cpp version to: {Version}");
			m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
		}
		if (Version == 27.1)
		{
			ulong[] array = MapVATR<ulong>(m_codeRegistration.codeGenModules, m_codeRegistration.codeGenModulesCount);
			foreach (ulong addr in array)
			{
				Il2CppCodeGenModule il2CppCodeGenModule = MapVATR<Il2CppCodeGenModule>(addr);
				if (il2CppCodeGenModule.rgctxsCount > 0)
				{
					if (MapVATR<Il2CppRGCTXDefinition>(il2CppCodeGenModule.rgctxs, il2CppCodeGenModule.rgctxsCount).All((Il2CppRGCTXDefinition x) => x.data.rgctxDataDummy > limit))
					{
						Version = 27.2;
						Console.WriteLine($"Change il2cpp version to: {Version}");
					}
					break;
				}
			}
		}
		if (Version == 24.4 && m_codeRegistration.invokerPointersCount > limit)
		{
			Version = 24.5;
			Console.WriteLine($"Change il2cpp version to: {Version}");
			m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
		}
		if (Version == 24.2 && m_codeRegistration.codeGenModules == 0L)
		{
			Version = 24.3;
			Console.WriteLine($"Change il2cpp version to: {Version}");
			m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
		}
		m_metadataRegistration = MapVATR<Il2CppMetadataRegistration>(metadataRegistration);
		GenericMethodPointers = MapVATR<ulong>(m_codeRegistration.genericMethodPointers, m_codeRegistration.genericMethodPointersCount);
		InvokerPointers = MapVATR<ulong>(m_codeRegistration.invokerPointers, m_codeRegistration.invokerPointersCount);
		if (Version < 27.0)
		{
			CustomAttributeGenerators = MapVATR<ulong>(m_codeRegistration.customAttributeGenerators, m_codeRegistration.customAttributeCount);
		}
		if (Version > 16.0 && Version < 27.0)
		{
			MetadataUsages = MapVATR<ulong>(m_metadataRegistration.metadataUsages, m_metadataUsagesCount);
		}
		if (Version >= 22.0)
		{
			if (m_codeRegistration.reversePInvokeWrapperCount != 0L)
			{
				ReversePInvokeWrappers = MapVATR<ulong>(m_codeRegistration.reversePInvokeWrappers, m_codeRegistration.reversePInvokeWrapperCount);
			}
			if (m_codeRegistration.unresolvedVirtualCallCount != 0L)
			{
				UnresolvedVirtualCallPointers = MapVATR<ulong>(m_codeRegistration.unresolvedVirtualCallPointers, m_codeRegistration.unresolvedVirtualCallCount);
			}
		}
		GenericInstPointers = MapVATR<ulong>(m_metadataRegistration.genericInsts, m_metadataRegistration.genericInstsCount);
		GenericInsts = Array.ConvertAll(GenericInstPointers, MapVATR<Il2CppGenericInst>);
		FieldOffsetsArePointers = Version > 21.0;
		if (Version == 21.0)
		{
			uint[] array2 = MapVATR<uint>(m_metadataRegistration.fieldOffsets, 6L);
			FieldOffsetsArePointers = array2[0] == 0 && array2[1] == 0 && array2[2] == 0 && array2[3] == 0 && array2[4] == 0 && array2[5] != 0;
		}
		if (FieldOffsetsArePointers)
		{
			FieldOffsets = MapVATR<ulong>(m_metadataRegistration.fieldOffsets, m_metadataRegistration.fieldOffsetsCount);
		}
		else
		{
			FieldOffsets = Array.ConvertAll(MapVATR<uint>(m_metadataRegistration.fieldOffsets, m_metadataRegistration.fieldOffsetsCount), (Converter<uint, ulong>)((uint x) => x));
		}
		ulong[] array3 = MapVATR<ulong>(m_metadataRegistration.typeDefinitionsSizes, m_metadataRegistration.typeDefinitionsSizesCount);
		TypeDefinitionSizes = new Il2CppTypeDefinitionSizes[m_metadataRegistration.typeDefinitionsSizesCount];
		for (int j = 0; j < array3.Length; j++)
		{
			if (array3[j] != 0L)
			{
				base.Position = MapVATR(array3[j]);
				TypeDefinitionSizes[j] = ReadClass<Il2CppTypeDefinitionSizes>();
			}
		}
		ulong[] array4 = MapVATR<ulong>(m_metadataRegistration.types, m_metadataRegistration.typesCount);
		Types = new Il2CppType[m_metadataRegistration.typesCount];
		for (int k = 0; k < m_metadataRegistration.typesCount; k++)
		{
			Types[k] = MapVATR<Il2CppType>(array4[k]);
			Types[k].Init(Version);
			m_typeDic.Add(array4[k], Types[k]);
		}
		if (Version >= 24.2)
		{
			ulong[] array5 = MapVATR<ulong>(m_codeRegistration.codeGenModules, m_codeRegistration.codeGenModulesCount);
			CodeGenModules = new Dictionary<string, Il2CppCodeGenModule>(array5.Length, StringComparer.Ordinal);
			CodeGenModuleMethodPointers = new Dictionary<string, ulong[]>(array5.Length, StringComparer.Ordinal);
			RGCTXDictionary = new Dictionary<string, Dictionary<uint, Il2CppRGCTXDefinition[]>>(array5.Length, StringComparer.Ordinal);
			ulong[] array = array5;
			foreach (ulong addr2 in array)
			{
				Il2CppCodeGenModule il2CppCodeGenModule2 = MapVATR<Il2CppCodeGenModule>(addr2);
				string key = ReadStringToNull(MapVATR(il2CppCodeGenModule2.moduleName));
				CodeGenModules.Add(key, il2CppCodeGenModule2);
				ulong[] value;
				try
				{
					value = MapVATR<ulong>(il2CppCodeGenModule2.methodPointers, il2CppCodeGenModule2.methodPointerCount);
				}
				catch
				{
					value = new ulong[il2CppCodeGenModule2.methodPointerCount];
				}
				CodeGenModuleMethodPointers.Add(key, value);
				Dictionary<uint, Il2CppRGCTXDefinition[]> dictionary = new Dictionary<uint, Il2CppRGCTXDefinition[]>();
				RGCTXDictionary.Add(key, dictionary);
				if (il2CppCodeGenModule2.rgctxsCount > 0)
				{
					Il2CppRGCTXDefinition[] sourceArray = MapVATR<Il2CppRGCTXDefinition>(il2CppCodeGenModule2.rgctxs, il2CppCodeGenModule2.rgctxsCount);
					Il2CppTokenRangePair[] array6 = MapVATR<Il2CppTokenRangePair>(il2CppCodeGenModule2.rgctxRanges, il2CppCodeGenModule2.rgctxRangesCount);
					foreach (Il2CppTokenRangePair il2CppTokenRangePair in array6)
					{
						Il2CppRGCTXDefinition[] array7 = new Il2CppRGCTXDefinition[il2CppTokenRangePair.range.length];
						Array.Copy(sourceArray, il2CppTokenRangePair.range.start, array7, 0, il2CppTokenRangePair.range.length);
						dictionary.Add(il2CppTokenRangePair.token, array7);
					}
				}
			}
		}
		else
		{
			MethodPointers = MapVATR<ulong>(m_codeRegistration.methodPointers, m_codeRegistration.methodPointersCount);
		}
		m_genericMethodTable = MapVATR<Il2CppGenericMethodFunctionsDefinitions>(m_metadataRegistration.genericMethodTable, m_metadataRegistration.genericMethodTableCount);
		MethodSpecs = MapVATR<Il2CppMethodSpec>(m_metadataRegistration.methodSpecs, m_metadataRegistration.methodSpecsCount);
		Il2CppGenericMethodFunctionsDefinitions[] genericMethodTable = m_genericMethodTable;
		foreach (Il2CppGenericMethodFunctionsDefinitions il2CppGenericMethodFunctionsDefinitions in genericMethodTable)
		{
			Il2CppMethodSpec il2CppMethodSpec = MethodSpecs[il2CppGenericMethodFunctionsDefinitions.genericMethodIndex];
			int methodDefinitionIndex = il2CppMethodSpec.methodDefinitionIndex;
			if (!MethodDefinitionMethodSpecs.TryGetValue(methodDefinitionIndex, out var value2))
			{
				value2 = new List<Il2CppMethodSpec>();
				MethodDefinitionMethodSpecs.Add(methodDefinitionIndex, value2);
			}
			value2.Add(il2CppMethodSpec);
			MethodSpecGenericMethodPointers.Add(il2CppMethodSpec, GenericMethodPointers[il2CppGenericMethodFunctionsDefinitions.indices.methodIndex]);
		}
	}

	public T MapVATR<T>(ulong addr) where T : new()
	{
		return ReadClass<T>(MapVATR(addr));
	}

	public T[] MapVATR<T>(ulong addr, ulong count) where T : new()
	{
		return ReadClassArray<T>(MapVATR(addr), count);
	}

	public T[] MapVATR<T>(ulong addr, long count) where T : new()
	{
		return ReadClassArray<T>(MapVATR(addr), count);
	}

	public int GetFieldOffsetFromIndex(int typeIndex, int fieldIndexInType, int fieldIndex, bool isValueType, bool isStatic)
	{
		try
		{
			int num = -1;
			if (FieldOffsetsArePointers)
			{
				ulong num2 = FieldOffsets[typeIndex];
				if (num2 != 0)
				{
					base.Position = MapVATR(num2) + (ulong)(4L * (long)fieldIndexInType);
					num = ReadInt32();
				}
			}
			else
			{
				num = (int)FieldOffsets[fieldIndex];
			}
			if (num > 0 && isValueType && !isStatic)
			{
				num = ((!Is32Bit) ? (num - 16) : (num - 8));
			}
			return num;
		}
		catch
		{
			return -1;
		}
	}

	public Il2CppType GetIl2CppType(ulong pointer)
	{
		if (!m_typeDic.TryGetValue(pointer, out var value))
		{
			return null;
		}
		return value;
	}

	public ulong GetMethodPointer(string imageName, Il2CppMethodDefinition methodDef)
	{
		if (Version >= 24.2)
		{
			uint token = methodDef.token;
			ulong[] array = CodeGenModuleMethodPointers[imageName];
			uint num = token & 0xFFFFFFu;
			return array[num - 1];
		}
		int methodIndex = methodDef.methodIndex;
		if (methodIndex >= 0)
		{
			return MethodPointers[methodIndex];
		}
		return 0uL;
	}

	public virtual ulong GetRVA(ulong pointer)
	{
		return pointer;
	}
}
