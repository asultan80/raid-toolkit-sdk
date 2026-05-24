using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Il2CppToolkit.Common;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Model;

public class TypeModel : ITypeModelMetadata, ITypeModel
{
	private readonly Loader m_loader;

	private ulong[] m_fieldOffsets;

	private readonly Dictionary<int, TypeDescriptor> m_parentTypeIndexToTypeInstDescriptor = new Dictionary<int, TypeDescriptor>();

	private readonly Dictionary<int, TypeDescriptor> m_typeCache = new Dictionary<int, TypeDescriptor>();

	private readonly Dictionary<Il2CppTypeDefinition, TypeDescriptor> m_cppTypeToDescriptor = new Dictionary<Il2CppTypeDefinition, TypeDescriptor>();

	private readonly List<TypeDescriptor> m_typeDescriptors = new List<TypeDescriptor>();

	private static readonly Dictionary<int, string> TypeString = new Dictionary<int, string>
	{
		{
			1,
			typeof(void).FullName
		},
		{
			2,
			typeof(bool).FullName
		},
		{
			3,
			typeof(char).FullName
		},
		{
			4,
			typeof(sbyte).FullName
		},
		{
			5,
			typeof(byte).FullName
		},
		{
			6,
			typeof(short).FullName
		},
		{
			7,
			typeof(ushort).FullName
		},
		{
			8,
			typeof(int).FullName
		},
		{
			9,
			typeof(uint).FullName
		},
		{
			10,
			typeof(long).FullName
		},
		{
			11,
			typeof(ulong).FullName
		},
		{
			12,
			typeof(float).FullName
		},
		{
			13,
			typeof(double).FullName
		},
		{
			14,
			typeof(string).FullName
		},
		{
			22,
			typeof(IntPtr).FullName
		},
		{
			24,
			typeof(IntPtr).FullName
		},
		{
			25,
			typeof(UIntPtr).FullName
		},
		{
			28,
			typeof(object).FullName
		}
	};

	public Il2Cpp Il2Cpp => m_loader.Il2Cpp;

	public Metadata Metadata => m_loader.Metadata;

	public string ModuleName => m_loader.ModuleName;

	public IReadOnlyList<TypeDescriptor> TypeDescriptors => m_typeDescriptors;

	public TypeModel(Loader loader)
	{
		m_loader = loader;
		Load();
	}

	private void Load()
	{
		LoadFieldOffsets();
		IndexTypeDescriptors();
	}

	private void LoadFieldOffsets()
	{
		if (m_loader.Il2Cpp.FieldOffsetsArePointers)
		{
			m_fieldOffsets = new ulong[m_loader.Metadata.fieldDefs.Length];
			new SortedDictionary<int, ulong>();
			{
				foreach (var item3 in m_loader.Metadata.typeDefs.WithIndexes())
				{
					int item = item3.index;
					Il2CppTypeDefinition item2 = item3.value;
					ulong num = m_loader.Il2Cpp.FieldOffsets[item];
					if (num == 0L)
					{
						continue;
					}
					ulong num2 = m_loader.Il2Cpp.MapVATR(num);
					if (num2 != 0L)
					{
						m_loader.Il2Cpp.Position = num2;
						for (int i = 0; i < item2.field_count; i++)
						{
							m_fieldOffsets[item2.fieldStart + i] = m_loader.Il2Cpp.ReadUInt32();
						}
					}
				}
				return;
			}
		}
		m_fieldOffsets = m_loader.Il2Cpp.FieldOffsets;
	}

	private void IndexTypeDescriptors()
	{
		if (m_typeDescriptors.Count > 0)
		{
			return;
		}
		for (int i = 0; i < m_loader.Metadata.imageDefs.Length; i++)
		{
			Il2CppImageDefinition il2CppImageDefinition = m_loader.Metadata.imageDefs[i];
			long num = il2CppImageDefinition.typeStart + il2CppImageDefinition.typeCount;
			for (int j = il2CppImageDefinition.typeStart; j < num; j++)
			{
				Il2CppTypeDefinition typeDef = m_loader.Metadata.typeDefs[j];
				m_typeDescriptors.Add(MakeTypeDescriptor(typeDef, j, il2CppImageDefinition));
			}
		}
		foreach (TypeDescriptor typeDescriptor in m_typeDescriptors)
		{
			TypeAttributes attributes = typeDescriptor.Attributes;
			if (typeDescriptor.TypeDef.declaringTypeIndex != -1)
			{
				ErrorHandler.Assert((attributes & TypeAttributes.VisibilityMask) > TypeAttributes.Public, "Nested attribute missing");
				Il2CppType il2CppType = m_loader.Il2Cpp.Types[typeDescriptor.TypeDef.declaringTypeIndex];
				typeDescriptor.DeclaringParent = m_typeCache[(int)il2CppType.data.klassIndex];
			}
			else
			{
				ErrorHandler.Assert((attributes & TypeAttributes.VisibilityMask) <= TypeAttributes.Public, "Unexpected nested attribute");
			}
			if (typeDescriptor.TypeDef.nested_type_count > 0)
			{
				foreach (int item5 in m_loader.Metadata.nestedTypeIndices.Range(typeDescriptor.TypeDef.nestedTypesStart, typeDescriptor.TypeDef.nested_type_count))
				{
					typeDescriptor.NestedTypes.Add(m_typeCache[item5]);
				}
			}
			if (typeDescriptor.GenericClass != null)
			{
				ITypeReference typeReference = MakeTypeReferenceFromCppTypeIndex((int)typeDescriptor.GenericTypeIndex, typeDescriptor);
				ErrorHandler.Assert(typeReference.Name != "System.Object", "generic class instance must derive from a generic class definition");
				if (typeReference.Name != "System.Object")
				{
					typeDescriptor.Base = typeReference;
				}
			}
			else
			{
				if (typeDescriptor.TypeDef.genericContainerIndex != -1)
				{
					Il2CppGenericContainer genericContainer = m_loader.Metadata.genericContainers[typeDescriptor.TypeDef.genericContainerIndex];
					typeDescriptor.GenericParameterNames = GetGenericContainerParamNames(genericContainer);
					ErrorHandler.Assert(typeDescriptor.GenericParameterNames.Length != 0, "Generic class must have template arguments");
				}
				if (attributes.HasFlag(TypeAttributes.ClassSemanticsMask))
				{
					typeDescriptor.Base = null;
				}
				else if (typeDescriptor.TypeDef.IsEnum)
				{
					typeDescriptor.Base = new DotNetTypeReference(typeof(Enum));
				}
				else if (typeDescriptor.TypeDef.parentIndex >= 0)
				{
					if (m_parentTypeIndexToTypeInstDescriptor.TryGetValue(typeDescriptor.TypeDef.parentIndex, out var value))
					{
						typeDescriptor.Base = new TypeDescriptorReference(value);
					}
					else
					{
						ITypeReference typeReference2 = MakeTypeReferenceFromCppTypeIndex(typeDescriptor.TypeDef.parentIndex, typeDescriptor);
						if (typeReference2.Name != "System.Object")
						{
							typeDescriptor.Base = typeReference2;
						}
					}
				}
				else
				{
					ErrorHandler.Assert(!typeDescriptor.TypeDef.IsValueType, "Unexpected value type");
				}
				foreach (int item6 in m_loader.Metadata.interfaceIndices.Range(typeDescriptor.TypeDef.interfacesStart, typeDescriptor.TypeDef.interfaces_count))
				{
					typeDescriptor.Implements.Add(MakeTypeReferenceFromCppTypeIndex(item6, typeDescriptor));
				}
			}
			if (typeDescriptor.TypeDef.field_count > 0)
			{
				foreach (var item7 in m_loader.Metadata.fieldDefs.RangeWithIndexes(typeDescriptor.TypeDef.fieldStart, typeDescriptor.TypeDef.field_count))
				{
					int item = item7.index;
					Il2CppFieldDefinition item2 = item7.value;
					Il2CppType il2CppType2 = m_loader.Il2Cpp.Types[item2.typeIndex];
					ITypeReference typeReference3 = MakeTypeReferenceFromCppTypeIndex(item2.typeIndex, typeDescriptor);
					string stringFromIndex = m_loader.Metadata.GetStringFromIndex(item2.nameIndex);
					FieldAttributes attrs = (FieldAttributes)((int)il2CppType2.attrs & -33);
					ulong fieldOffsetFromIndex = GetFieldOffsetFromIndex(typeDescriptor.TypeDef, item);
					FieldDescriptor fieldDescriptor = new FieldDescriptor(stringFromIndex, typeReference3, attrs, fieldOffsetFromIndex);
					if (m_loader.Metadata.GetFieldDefaultValueFromIndex(item, out var value2) && value2.dataIndex != -1 && TryGetDefaultValue(value2, out var value3))
					{
						fieldDescriptor.DefaultValue = value3;
					}
					typeDescriptor.Fields.Add(fieldDescriptor);
				}
			}
			if (typeDescriptor.TypeDef.property_count > 0)
			{
				foreach (int item8 in Enumerable.Range(typeDescriptor.TypeDef.propertyStart, typeDescriptor.TypeDef.property_count))
				{
					Il2CppPropertyDefinition il2CppPropertyDefinition = m_loader.Metadata.propertyDefs[item8];
					if (il2CppPropertyDefinition.get >= 0)
					{
						Il2CppMethodDefinition il2CppMethodDefinition = m_loader.Metadata.methodDefs[typeDescriptor.TypeDef.methodStart + il2CppPropertyDefinition.get];
						Il2CppType il2CppType3 = m_loader.Il2Cpp.Types[il2CppMethodDefinition.returnType];
						MethodAttributes flags = (MethodAttributes)il2CppMethodDefinition.flags;
						string stringFromIndex2 = m_loader.Metadata.GetStringFromIndex(il2CppPropertyDefinition.nameIndex);
						PropertyAttributes attrs2 = (PropertyAttributes)il2CppType3.attrs;
						ITypeReference typeReference4 = MakeTypeReferenceFromCppTypeIndex(il2CppMethodDefinition.returnType, typeDescriptor);
						PropertyDescriptor item3 = new PropertyDescriptor(stringFromIndex2, typeReference4, attrs2, flags);
						typeDescriptor.Properties.Add(item3);
					}
				}
			}
			UniqueName uniqueName = new UniqueName();
			foreach (int item9 in Enumerable.Range(typeDescriptor.TypeDef.methodStart, typeDescriptor.TypeDef.method_count))
			{
				Il2CppMethodDefinition il2CppMethodDefinition2 = m_loader.Metadata.methodDefs[item9];
				string text = uniqueName.Get(m_loader.Metadata.GetStringFromIndex(il2CppMethodDefinition2.nameIndex));
				if (text.StartsWith(".") || !((MethodAttributes)il2CppMethodDefinition2.flags).HasFlag(MethodAttributes.Static))
				{
					continue;
				}
				if (m_loader.Il2Cpp.MethodDefinitionMethodSpecs.TryGetValue(item9, out var value4))
				{
					foreach (Il2CppMethodSpec item10 in value4)
					{
						if (item10.classIndexIndex != -1)
						{
							Il2CppGenericInst il2CppGenericInst = m_loader.Il2Cpp.GenericInsts[item10.classIndexIndex];
							ulong[] array = m_loader.Il2Cpp.MapVATR<ulong>(il2CppGenericInst.type_argv, il2CppGenericInst.type_argc);
							MethodDescriptor methodDescriptor = new MethodDescriptor(text);
							for (int k = 0; k < il2CppGenericInst.type_argc; k++)
							{
								Il2CppType il2CppType4 = m_loader.Il2Cpp.GetIl2CppType(array[k]);
								string typeName = GetTypeName(il2CppType4, addNamespace: true, is_nested: false);
								methodDescriptor.DeclaringTypeArgs.Add(new Il2CppTypeReference(typeName, il2CppType4, typeDescriptor));
							}
							typeDescriptor.Methods.Add(methodDescriptor);
						}
					}
				}
				else
				{
					string stringFromIndex3 = m_loader.Metadata.GetStringFromIndex(typeDescriptor.ImageDef.nameIndex);
					ulong methodPointer = m_loader.Il2Cpp.GetMethodPointer(stringFromIndex3, il2CppMethodDefinition2);
					m_loader.Il2Cpp.GetRVA(methodPointer);
					MethodDescriptor item4 = new MethodDescriptor(text);
					typeDescriptor.Methods.Add(item4);
				}
			}
		}
	}

	public void HandleTypeInfoUsage(uint typeIndex, ulong address)
	{
		if (typeIndex < m_loader.Il2Cpp.Types.Length && m_loader.Il2Cpp.Types[typeIndex].type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
		{
			TypeDescriptor typeDescriptor = MakeGenericTypeInstDescriptor(typeIndex);
			if (!typeDescriptor.Attributes.HasFlag(TypeAttributes.ClassSemanticsMask))
			{
				m_typeDescriptors.Add(typeDescriptor);
			}
		}
	}

	private ITypeReference MakeTypeReferenceFromCppTypeIndex(int typeIndex, TypeDescriptor descriptor)
	{
		Il2CppType il2CppType = m_loader.Il2Cpp.Types[typeIndex];
		return new Il2CppTypeReference(GetTypeName(il2CppType, addNamespace: true, is_nested: false), il2CppType, descriptor);
	}

	private TypeDescriptor MakeTypeDescriptor(Il2CppTypeDefinition typeDef, int typeIndex, Il2CppImageDefinition imageDef)
	{
		TypeDescriptor typeDescriptor = new TypeDescriptor(GetTypeDefName(typeDef), typeDef, imageDef);
		typeDescriptor.SizeInBytes = m_loader.Il2Cpp.TypeDefinitionSizes[typeIndex].instance_size;
		m_typeCache.Add(typeIndex, typeDescriptor);
		m_cppTypeToDescriptor.Add(typeDef, typeDescriptor);
		return typeDescriptor;
	}

	private TypeDescriptor MakeGenericTypeInstDescriptor(uint typeIndex)
	{
		Il2CppType il2CppType = m_loader.Il2Cpp.Types[typeIndex];
		ErrorHandler.Assert(il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST, "Expected GenericInst type");
		string typeName = GetTypeName(il2CppType, addNamespace: true, is_nested: false);
		Il2CppGenericClass genericClass = m_loader.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
		long genericClassTypeDefinitionIndex = GetGenericClassTypeDefinitionIndex(genericClass);
		ErrorHandler.Assert(genericClassTypeDefinitionIndex != -1, "Could not find generic type index");
		Il2CppTypeDefinition typeDef = m_loader.Metadata.typeDefs[genericClassTypeDefinitionIndex];
		TypeDescriptor typeDescriptor = new TypeDescriptor(typeName, typeDef, null, genericClass, typeIndex);
		typeDescriptor.SizeInBytes = m_loader.Il2Cpp.TypeDefinitionSizes[genericClassTypeDefinitionIndex].instance_size;
		m_parentTypeIndexToTypeInstDescriptor[(int)typeIndex] = typeDescriptor;
		return typeDescriptor;
	}

	private string GetTypeDefName(Il2CppTypeDefinition typeDef)
	{
		string text = m_loader.Metadata.GetStringFromIndex(typeDef.nameIndex);
		int num = text.IndexOf("`", StringComparison.Ordinal);
		if (num != -1)
		{
			text = text.Substring(0, num);
		}
		string stringFromIndex = m_loader.Metadata.GetStringFromIndex(typeDef.namespaceIndex);
		if (stringFromIndex != "")
		{
			text = stringFromIndex + "." + text;
		}
		return text;
	}

	public bool TryGetDefaultValueBytes(Il2CppFieldDefaultValue cppDefaultValue, out byte[] value)
	{
		uint defaultValueFromIndex = m_loader.Metadata.GetDefaultValueFromIndex(cppDefaultValue.dataIndex);
		Il2CppType obj = m_loader.Il2Cpp.Types[cppDefaultValue.typeIndex];
		m_loader.Metadata.Position = defaultValueFromIndex;
		switch (obj.type)
		{
		case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
			value = m_loader.Metadata.ReadBytes(1);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U1:
			value = m_loader.Metadata.ReadBytes(1);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I1:
			value = m_loader.Metadata.ReadBytes(1);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
			value = m_loader.Metadata.ReadBytes(2);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U2:
			value = m_loader.Metadata.ReadBytes(2);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I2:
			value = m_loader.Metadata.ReadBytes(2);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U4:
			value = m_loader.Metadata.ReadBytes(4);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I4:
			value = m_loader.Metadata.ReadBytes(4);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_U8:
			value = m_loader.Metadata.ReadBytes(8);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I8:
			value = m_loader.Metadata.ReadBytes(8);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_R4:
			value = m_loader.Metadata.ReadBytes(4);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_R8:
			value = m_loader.Metadata.ReadBytes(8);
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
			value = m_loader.Metadata.ReadBytes(m_loader.Metadata.ReadInt32());
			return true;
		default:
			value = null;
			return false;
		}
	}

	public bool TryGetTypeDescriptor(Il2CppTypeDefinition cppTypeDefinition, [NotNullWhen(true)] out TypeDescriptor? typeDescriptor)
	{
		return m_cppTypeToDescriptor.TryGetValue(cppTypeDefinition, out typeDescriptor);
	}

	public bool TryGetDefaultValue(Il2CppFieldDefaultValue cppDefaultValue, out object value)
	{
		uint defaultValueFromIndex = m_loader.Metadata.GetDefaultValueFromIndex(cppDefaultValue.dataIndex);
		Il2CppType il2CppType = m_loader.Il2Cpp.Types[cppDefaultValue.typeIndex];
		m_loader.Metadata.Position = defaultValueFromIndex;
		if (GetConstantValueFromBlob(il2CppType.type, m_loader.Metadata.Reader, out var value2))
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
			if (m_loader.Il2Cpp.Version >= 29.0)
			{
				value.Value = reader.ReadCompressedUInt32();
			}
			else
			{
				value.Value = reader.ReadUInt32();
			}
			return true;
		case Il2CppTypeEnum.IL2CPP_TYPE_I4:
			if (m_loader.Il2Cpp.Version >= 29.0)
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
			if (m_loader.Il2Cpp.Version >= 29.0)
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
				value.Value = m_loader.Il2Cpp.Types[num];
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
			enumType = m_loader.Il2Cpp.Types[num];
			Il2CppTypeDefinition typeDefinitionFromIl2CppType = GetTypeDefinitionFromIl2CppType(enumType);
			il2CppTypeEnum = m_loader.Il2Cpp.Types[typeDefinitionFromIl2CppType.elementTypeIndex].type;
		}
		return il2CppTypeEnum;
	}

	public ulong GetFieldOffsetFromIndex(Il2CppTypeDefinition typeDefinition, int fieldIndex)
	{
		ulong num = m_fieldOffsets[fieldIndex];
		if (num != 0 && typeDefinition.IsValueType && !((TypeAttributes)typeDefinition.flags).HasFlag(TypeAttributes.Abstract | TypeAttributes.Sealed))
		{
			num = ((!m_loader.Il2Cpp.Is32Bit) ? (num - 16) : (num - 8));
		}
		return num;
	}

	public string GetTypeName(Il2CppType il2CppType, bool addNamespace, bool is_nested)
	{
		switch (il2CppType.type)
		{
		case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
		{
			Il2CppArrayType il2CppArrayType = m_loader.Il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
			Il2CppType il2CppType2 = m_loader.Il2Cpp.GetIl2CppType(il2CppArrayType.etype);
			return GetTypeName(il2CppType2, addNamespace, is_nested: false) + "[" + new string(',', il2CppArrayType.rank - 1) + "]";
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
		{
			Il2CppType il2CppType3 = m_loader.Il2Cpp.GetIl2CppType(il2CppType.data.type);
			return GetTypeName(il2CppType3, addNamespace, is_nested: false) + "[]";
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
		{
			Il2CppType il2CppType4 = m_loader.Il2Cpp.GetIl2CppType(il2CppType.data.type);
			return GetTypeName(il2CppType4, addNamespace, is_nested: false) + "*";
		}
		case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
		case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
		{
			Il2CppGenericParameter genericParameterFromIl2CppType = GetGenericParameterFromIl2CppType(il2CppType);
			return m_loader.Metadata.GetStringFromIndex(genericParameterFromIl2CppType.nameIndex);
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
				il2CppGenericClass = m_loader.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				il2CppTypeDefinition = GetGenericClassTypeDefinition(il2CppGenericClass);
			}
			else
			{
				il2CppTypeDefinition = GetTypeDefinitionFromIl2CppType(il2CppType);
			}
			if (il2CppTypeDefinition.declaringTypeIndex != -1)
			{
				text += GetTypeName(m_loader.Il2Cpp.Types[il2CppTypeDefinition.declaringTypeIndex], addNamespace, is_nested: true);
				text += ".";
			}
			else if (addNamespace)
			{
				string stringFromIndex = m_loader.Metadata.GetStringFromIndex(il2CppTypeDefinition.namespaceIndex);
				if (stringFromIndex != "")
				{
					text = text + stringFromIndex + ".";
				}
			}
			string stringFromIndex2 = m_loader.Metadata.GetStringFromIndex(il2CppTypeDefinition.nameIndex);
			int num = stringFromIndex2.IndexOf("`");
			text = ((num == -1) ? (text + stringFromIndex2) : (text + stringFromIndex2.Substring(0, num)));
			if (is_nested)
			{
				return text;
			}
			if (il2CppGenericClass != null)
			{
				Il2CppGenericInst genericInst = m_loader.Il2Cpp.MapVATR<Il2CppGenericInst>(il2CppGenericClass.context.class_inst);
				text += GetGenericInstParams(genericInst);
			}
			else if (il2CppTypeDefinition.genericContainerIndex >= 0)
			{
				Il2CppGenericContainer genericContainer = m_loader.Metadata.genericContainers[il2CppTypeDefinition.genericContainerIndex];
				text += GetGenericContainerParams(genericContainer);
			}
			return text;
		}
		default:
			return TypeString[(int)il2CppType.type];
		}
	}

	public long GetGenericClassTypeDefinitionIndex(Il2CppGenericClass genericClass)
	{
		if (m_loader.Il2Cpp.Version >= 27.0)
		{
			Il2CppType il2CppType = m_loader.Il2Cpp.GetIl2CppType(genericClass.type);
			return GetTypeDefinitionIndexFromIl2CppType(il2CppType);
		}
		if (genericClass.typeDefinitionIndex == uint.MaxValue || genericClass.typeDefinitionIndex == -1)
		{
			return -1L;
		}
		return genericClass.typeDefinitionIndex;
	}

	public Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass)
	{
		long genericClassTypeDefinitionIndex = GetGenericClassTypeDefinitionIndex(genericClass);
		if (genericClassTypeDefinitionIndex == -1)
		{
			return null;
		}
		return m_loader.Metadata.typeDefs[genericClassTypeDefinitionIndex];
	}

	public Il2CppGenericParameter GetGenericParameterFromIl2CppType(Il2CppType il2CppType)
	{
		if (m_loader.Il2Cpp.Version >= 27.0 && m_loader.Il2Cpp is ElfBase { IsDumped: not false })
		{
			ulong num = (il2CppType.data.genericParameterHandle - m_loader.Metadata.ImageBase - m_loader.Metadata.header.genericParametersOffset) / (ulong)m_loader.Metadata.SizeOf(typeof(Il2CppGenericParameter));
			return m_loader.Metadata.genericParameters[num];
		}
		return m_loader.Metadata.genericParameters[il2CppType.data.genericParameterIndex];
	}

	public long GetTypeDefinitionIndexFromIl2CppType(Il2CppType il2CppType, bool resolveGeneric = true)
	{
		if (m_loader.Il2Cpp.Version >= 27.0 && m_loader.Il2Cpp is ElfBase { IsDumped: not false })
		{
			return (long)((il2CppType.data.typeHandle - m_loader.Metadata.ImageBase - m_loader.Metadata.header.typeDefinitionsOffset) / (ulong)m_loader.Metadata.SizeOf(typeof(Il2CppTypeDefinition)));
		}
		if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST && resolveGeneric)
		{
			Il2CppGenericClass genericClass = m_loader.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
			return GetGenericClassTypeDefinitionIndex(genericClass);
		}
		if (il2CppType.data.klassIndex < m_loader.Metadata.typeDefs.Length)
		{
			return il2CppType.data.klassIndex;
		}
		return -1L;
	}

	public Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType, bool resolveGeneric = true)
	{
		long typeDefinitionIndexFromIl2CppType = GetTypeDefinitionIndexFromIl2CppType(il2CppType, resolveGeneric);
		if (typeDefinitionIndexFromIl2CppType == -1)
		{
			return null;
		}
		return m_loader.Metadata.typeDefs[typeDefinitionIndexFromIl2CppType];
	}

	public string GetGenericInstParams(Il2CppGenericInst genericInst)
	{
		List<string> list = new List<string>();
		ulong[] array = m_loader.Il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
		for (int i = 0; i < genericInst.type_argc; i++)
		{
			Il2CppType il2CppType = m_loader.Il2Cpp.GetIl2CppType(array[i]);
			list.Add(GetTypeName(il2CppType, addNamespace: false, is_nested: false));
		}
		return "<" + string.Join(", ", list) + ">";
	}

	public Il2CppType[] GetGenericInstParamList(Il2CppGenericInst genericInst)
	{
		Il2CppType[] array = new Il2CppType[genericInst.type_argc];
		ulong[] array2 = m_loader.Il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
		for (int i = 0; i < genericInst.type_argc; i++)
		{
			Il2CppType il2CppType = m_loader.Il2Cpp.GetIl2CppType(array2[i]);
			array[i] = il2CppType;
		}
		return array;
	}

	public string[] GetGenericContainerParamNames(Il2CppGenericContainer genericContainer)
	{
		string[] array = new string[genericContainer.type_argc];
		for (int i = 0; i < genericContainer.type_argc; i++)
		{
			int num = genericContainer.genericParameterStart + i;
			Il2CppGenericParameter il2CppGenericParameter = m_loader.Metadata.genericParameters[num];
			array[i] = m_loader.Metadata.GetStringFromIndex(il2CppGenericParameter.nameIndex);
		}
		return array;
	}

	public string GetGenericContainerParams(Il2CppGenericContainer genericContainer)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < genericContainer.type_argc; i++)
		{
			int num = genericContainer.genericParameterStart + i;
			Il2CppGenericParameter il2CppGenericParameter = m_loader.Metadata.genericParameters[num];
			list.Add(m_loader.Metadata.GetStringFromIndex(il2CppGenericParameter.nameIndex));
		}
		return "<" + string.Join(", ", list) + ">";
	}

	public SectionHelper GetSectionHelper()
	{
		return m_loader.Il2Cpp.GetSectionHelper(m_loader.Metadata.methodDefs.Count((Il2CppMethodDefinition x) => x.methodIndex >= 0), m_loader.Metadata.typeDefs.Length, m_loader.Metadata.imageDefs.Length);
	}
}
