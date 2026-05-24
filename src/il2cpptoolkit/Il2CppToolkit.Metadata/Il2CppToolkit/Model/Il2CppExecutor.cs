using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Il2CppToolkit.Model;

public class Il2CppExecutor
{
	public Metadata metadata;

	public Il2Cpp il2Cpp;

	private static readonly Dictionary<int, string> TypeString = new Dictionary<int, string>
	{
		{ 1, "void" },
		{ 2, "bool" },
		{ 3, "char" },
		{ 4, "sbyte" },
		{ 5, "byte" },
		{ 6, "short" },
		{ 7, "ushort" },
		{ 8, "int" },
		{ 9, "uint" },
		{ 10, "long" },
		{ 11, "ulong" },
		{ 12, "float" },
		{ 13, "double" },
		{ 14, "string" },
		{ 22, "TypedReference" },
		{ 24, "IntPtr" },
		{ 25, "UIntPtr" },
		{ 28, "object" }
	};

	public ulong[] customAttributeGenerators;

	public Il2CppExecutor(Metadata metadata, Il2Cpp il2Cpp)
	{
		this.metadata = metadata;
		this.il2Cpp = il2Cpp;
		if (il2Cpp.Version >= 27.0 && il2Cpp.Version < 29.0)
		{
			customAttributeGenerators = new ulong[metadata.imageDefs.Sum((Il2CppImageDefinition x) => x.customAttributeCount)];
			Il2CppImageDefinition[] imageDefs = metadata.imageDefs;
			foreach (Il2CppImageDefinition il2CppImageDefinition in imageDefs)
			{
				string stringFromIndex = metadata.GetStringFromIndex(il2CppImageDefinition.nameIndex);
				Il2CppCodeGenModule il2CppCodeGenModule = il2Cpp.CodeGenModules[stringFromIndex];
				if (il2CppImageDefinition.customAttributeCount != 0)
				{
					il2Cpp.ReadClassArray<ulong>(il2Cpp.MapVATR(il2CppCodeGenModule.customAttributeCacheGenerator), il2CppImageDefinition.customAttributeCount).CopyTo(customAttributeGenerators, il2CppImageDefinition.customAttributeStart);
				}
			}
		}
		else if (il2Cpp.Version < 27.0)
		{
			customAttributeGenerators = il2Cpp.CustomAttributeGenerators;
		}
	}

	public string GetTypeName(Il2CppType il2CppType, bool addNamespace, bool is_nested)
	{
		switch (il2CppType.type)
		{
		case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
		{
			Il2CppArrayType il2CppArrayType = il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
			Il2CppType il2CppType2 = il2Cpp.GetIl2CppType(il2CppArrayType.etype);
			return GetTypeName(il2CppType2, addNamespace, is_nested: false) + "[" + new string(',', il2CppArrayType.rank - 1) + "]";
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
		{
			Il2CppType il2CppType3 = il2Cpp.GetIl2CppType(il2CppType.data.type);
			return GetTypeName(il2CppType3, addNamespace, is_nested: false) + "[]";
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
		{
			Il2CppType il2CppType4 = il2Cpp.GetIl2CppType(il2CppType.data.type);
			return GetTypeName(il2CppType4, addNamespace, is_nested: false) + "*";
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
		case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
		{
			Il2CppGenericParameter genericParameteFromIl2CppType = GetGenericParameteFromIl2CppType(il2CppType);
			return metadata.GetStringFromIndex(genericParameteFromIl2CppType.nameIndex);
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
		case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
		case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
		{
			string text = string.Empty;
			Il2CppGenericClass il2CppGenericClass = null;
			Il2CppTypeDefinition il2CppTypeDefinition;
			if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
			{
				il2CppGenericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				il2CppTypeDefinition = GetGenericClassTypeDefinition(il2CppGenericClass);
			}
			else
			{
				il2CppTypeDefinition = GetTypeDefinitionFromIl2CppType(il2CppType);
			}
			if (il2CppTypeDefinition.declaringTypeIndex != -1)
			{
				text += GetTypeName(il2Cpp.Types[il2CppTypeDefinition.declaringTypeIndex], addNamespace, is_nested: true);
				text += ".";
			}
			else if (addNamespace)
			{
				string stringFromIndex = metadata.GetStringFromIndex(il2CppTypeDefinition.namespaceIndex);
				if (stringFromIndex != "")
				{
					text = text + stringFromIndex + ".";
				}
			}
			string stringFromIndex2 = metadata.GetStringFromIndex(il2CppTypeDefinition.nameIndex);
			int num = stringFromIndex2.IndexOf("`");
			text = ((num == -1) ? (text + stringFromIndex2) : (text + stringFromIndex2.Substring(0, num)));
			if (is_nested)
			{
				return text;
			}
			if (il2CppGenericClass != null)
			{
				Il2CppGenericInst genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(il2CppGenericClass.context.class_inst);
				text += GetGenericInstParams(genericInst);
			}
			else if (il2CppTypeDefinition.genericContainerIndex >= 0)
			{
				Il2CppGenericContainer genericContainer = metadata.genericContainers[il2CppTypeDefinition.genericContainerIndex];
				text += GetGenericContainerParams(genericContainer);
			}
			return text;
		}
		default:
			return TypeString[(int)il2CppType.type];
		}
	}

	public string GetTypeDefName(Il2CppTypeDefinition typeDef, bool addNamespace, bool genericParameter)
	{
		string text = string.Empty;
		if (typeDef.declaringTypeIndex != -1)
		{
			text = GetTypeName(il2Cpp.Types[typeDef.declaringTypeIndex], addNamespace, is_nested: true) + ".";
		}
		else if (addNamespace)
		{
			string stringFromIndex = metadata.GetStringFromIndex(typeDef.namespaceIndex);
			if (stringFromIndex != "")
			{
				text = stringFromIndex + ".";
			}
		}
		string text2 = metadata.GetStringFromIndex(typeDef.nameIndex);
		if (typeDef.genericContainerIndex >= 0)
		{
			int num = text2.IndexOf("`");
			if (num != -1)
			{
				text2 = text2.Substring(0, num);
			}
			if (genericParameter)
			{
				Il2CppGenericContainer genericContainer = metadata.genericContainers[typeDef.genericContainerIndex];
				text2 += GetGenericContainerParams(genericContainer);
			}
		}
		return text + text2;
	}

	public string GetGenericInstParams(Il2CppGenericInst genericInst)
	{
		List<string> list = new List<string>();
		ulong[] array = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
		for (int i = 0; i < genericInst.type_argc; i++)
		{
			Il2CppType il2CppType = il2Cpp.GetIl2CppType(array[i]);
			list.Add(GetTypeName(il2CppType, addNamespace: false, is_nested: false));
		}
		return "<" + string.Join(", ", list) + ">";
	}

	public string GetGenericContainerParams(Il2CppGenericContainer genericContainer)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < genericContainer.type_argc; i++)
		{
			int num = genericContainer.genericParameterStart + i;
			Il2CppGenericParameter il2CppGenericParameter = metadata.genericParameters[num];
			list.Add(metadata.GetStringFromIndex(il2CppGenericParameter.nameIndex));
		}
		return "<" + string.Join(", ", list) + ">";
	}

	public (string, string) GetMethodSpecName(Il2CppMethodSpec methodSpec, bool addNamespace = false)
	{
		Il2CppMethodDefinition il2CppMethodDefinition = metadata.methodDefs[methodSpec.methodDefinitionIndex];
		Il2CppTypeDefinition typeDef = metadata.typeDefs[il2CppMethodDefinition.declaringType];
		string text = GetTypeDefName(typeDef, addNamespace, genericParameter: false);
		if (methodSpec.classIndexIndex != -1)
		{
			Il2CppGenericInst genericInst = il2Cpp.GenericInsts[methodSpec.classIndexIndex];
			text += GetGenericInstParams(genericInst);
		}
		string text2 = metadata.GetStringFromIndex(il2CppMethodDefinition.nameIndex);
		if (methodSpec.methodIndexIndex != -1)
		{
			Il2CppGenericInst genericInst2 = il2Cpp.GenericInsts[methodSpec.methodIndexIndex];
			text2 += GetGenericInstParams(genericInst2);
		}
		return (text, text2);
	}

	public Il2CppGenericContext GetMethodSpecGenericContext(Il2CppMethodSpec methodSpec)
	{
		ulong class_inst = 0uL;
		ulong method_inst = 0uL;
		if (methodSpec.classIndexIndex != -1)
		{
			class_inst = il2Cpp.GenericInstPointers[methodSpec.classIndexIndex];
		}
		if (methodSpec.methodIndexIndex != -1)
		{
			method_inst = il2Cpp.GenericInstPointers[methodSpec.methodIndexIndex];
		}
		return new Il2CppGenericContext
		{
			class_inst = class_inst,
			method_inst = method_inst
		};
	}

	public Il2CppRGCTXDefinition[] GetRGCTXDefinition(string imageName, Il2CppTypeDefinition typeDef)
	{
		Il2CppRGCTXDefinition[] value = null;
		if (il2Cpp.Version >= 24.2)
		{
			il2Cpp.RGCTXDictionary[imageName].TryGetValue(typeDef.token, out value);
		}
		else if (typeDef.rgctxCount > 0)
		{
			value = new Il2CppRGCTXDefinition[typeDef.rgctxCount];
			Array.Copy(metadata.rgctxEntries, typeDef.rgctxStartIndex, value, 0, typeDef.rgctxCount);
		}
		return value;
	}

	public Il2CppRGCTXDefinition[] GetRGCTXDefinition(string imageName, Il2CppMethodDefinition methodDef)
	{
		Il2CppRGCTXDefinition[] value = null;
		if (il2Cpp.Version >= 24.2)
		{
			il2Cpp.RGCTXDictionary[imageName].TryGetValue(methodDef.token, out value);
		}
		else if (methodDef.rgctxCount > 0)
		{
			value = new Il2CppRGCTXDefinition[methodDef.rgctxCount];
			Array.Copy(metadata.rgctxEntries, methodDef.rgctxStartIndex, value, 0, methodDef.rgctxCount);
		}
		return value;
	}

	public Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass)
	{
		if (il2Cpp.Version >= 27.0)
		{
			Il2CppType il2CppType = il2Cpp.GetIl2CppType(genericClass.type);
			if (il2CppType == null)
			{
				return null;
			}
			return GetTypeDefinitionFromIl2CppType(il2CppType);
		}
		if (genericClass.typeDefinitionIndex == uint.MaxValue || genericClass.typeDefinitionIndex == -1)
		{
			return null;
		}
		return metadata.typeDefs[genericClass.typeDefinitionIndex];
	}

	public Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType)
	{
		if (il2Cpp.Version >= 27.0 && il2Cpp.IsDumped)
		{
			ulong num = (il2CppType.data.typeHandle - metadata.ImageBase - metadata.header.typeDefinitionsOffset) / (ulong)metadata.SizeOf(typeof(Il2CppTypeDefinition));
			return metadata.typeDefs[num];
		}
		return metadata.typeDefs[il2CppType.data.klassIndex];
	}

	public Il2CppGenericParameter GetGenericParameteFromIl2CppType(Il2CppType il2CppType)
	{
		if (il2Cpp.Version >= 27.0 && il2Cpp.IsDumped)
		{
			ulong num = (il2CppType.data.genericParameterHandle - metadata.ImageBase - metadata.header.genericParametersOffset) / (ulong)metadata.SizeOf(typeof(Il2CppGenericParameter));
			return metadata.genericParameters[num];
		}
		return metadata.genericParameters[il2CppType.data.genericParameterIndex];
	}

	public SectionHelper GetSectionHelper()
	{
		return il2Cpp.GetSectionHelper(metadata.methodDefs.Count((Il2CppMethodDefinition x) => x.methodIndex >= 0), metadata.typeDefs.Length, metadata.imageDefs.Length);
	}

	public bool TryGetDefaultValue(int typeIndex, int dataIndex, out object value)
	{
		uint defaultValueFromIndex = metadata.GetDefaultValueFromIndex(dataIndex);
		Il2CppType il2CppType = il2Cpp.Types[typeIndex];
		metadata.Position = defaultValueFromIndex;
		if (GetConstantValueFromBlob(il2CppType.type, metadata.Reader, out var value2))
		{
			value = value2.Value;
			return true;
		}
		value = defaultValueFromIndex;
		return false;
	}

	public bool GetConstantValueFromBlob(Il2CppTypeEnum type, BinaryReader reader, out BlobValue value)
	{
		value = new BlobValue
		{
			il2CppTypeEnum = type
		};
		switch (type)
		{
		case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
			value.Value = reader.ReadBoolean();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U1:
			value.Value = reader.ReadByte();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I1:
			value.Value = reader.ReadSByte();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
			value.Value = BitConverter.ToChar(reader.ReadBytes(2), 0);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U2:
			value.Value = reader.ReadUInt16();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I2:
			value.Value = reader.ReadInt16();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U4:
			if (il2Cpp.Version >= 29.0)
			{
				value.Value = reader.ReadCompressedUInt32();
			}
			else
			{
				value.Value = reader.ReadUInt32();
			}
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I4:
			if (il2Cpp.Version >= 29.0)
			{
				value.Value = reader.ReadCompressedInt32();
			}
			else
			{
				value.Value = reader.ReadInt32();
			}
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U8:
			value.Value = reader.ReadUInt64();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I8:
			value.Value = reader.ReadInt64();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_R4:
			value.Value = reader.ReadSingle();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_R8:
			value.Value = reader.ReadDouble();
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
			if (il2Cpp.Version >= 29.0)
			{
				int num3 = reader.ReadCompressedInt32();
				if (num3 == -1)
				{
					value.Value = null;
				}
				else
				{
					value.Value = Encoding.UTF8.GetString(reader.ReadBytes(num3));
				}
			}
			else
			{
				int num3 = reader.ReadInt32();
				value.Value = reader.ReadString(num3);
			}
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
		{
			int num2 = reader.ReadCompressedInt32();
			if (num2 == -1)
			{
				value.Value = null;
			}
			else
			{
				BlobValue[] array = new BlobValue[num2];
				Il2CppType enumType;
				Il2CppTypeEnum il2CppTypeEnum = ReadEncodedTypeEnum(reader, out enumType);
				byte b = reader.ReadByte();
				for (int i = 0; i < num2; i++)
				{
					Il2CppTypeEnum il2CppTypeEnum2 = il2CppTypeEnum;
					if (b == 1)
					{
						il2CppTypeEnum2 = ReadEncodedTypeEnum(reader, out enumType);
					}
					GetConstantValueFromBlob(il2CppTypeEnum2, reader, out var value2);
					value2.il2CppTypeEnum = il2CppTypeEnum2;
					value2.EnumType = enumType;
					array[i] = value2;
				}
				value.Value = array;
			}
			return true;
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_IL2CPP_TYPE_INDEX:
		{
			int num = reader.ReadCompressedInt32();
			if (num == -1)
			{
				value.Value = null;
			}
			else
			{
				value.Value = il2Cpp.Types[num];
			}
			return true;
		}
		default:
			value = null;
			return false;
		}
	}

	public Il2CppTypeEnum ReadEncodedTypeEnum(BinaryReader reader, out Il2CppType enumType)
	{
		enumType = null;
		Il2CppTypeEnum il2CppTypeEnum = (Il2CppTypeEnum)reader.ReadByte();
		if (il2CppTypeEnum == Il2CppTypeEnum.IL2CPP_TYPE_ENUM)
		{
			int num = reader.ReadCompressedInt32();
			enumType = il2Cpp.Types[num];
			Il2CppTypeDefinition typeDefinitionFromIl2CppType = GetTypeDefinitionFromIl2CppType(enumType);
			il2CppTypeEnum = il2Cpp.Types[typeDefinitionFromIl2CppType.elementTypeIndex].type;
		}
		return il2CppTypeEnum;
	}
}
