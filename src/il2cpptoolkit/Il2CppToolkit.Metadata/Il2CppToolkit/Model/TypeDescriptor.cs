using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Common;

namespace Il2CppToolkit.Model;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class TypeDescriptor
{
	private readonly string m_name;

	public readonly Il2CppImageDefinition ImageDef;

	public readonly Il2CppTypeDefinition TypeDef;

	public readonly Il2CppGenericClass GenericClass;

	public readonly uint GenericTypeIndex;

	public readonly List<ITypeReference> Implements = new List<ITypeReference>();

	public readonly List<TypeDescriptor> NestedTypes = new List<TypeDescriptor>();

	public TypeDescriptor DeclaringParent;

	public TypeDescriptor GenericParent;

	public ITypeReference[] GenericTypeParams;

	public ITypeReference Base;

	public TypeAttributes Attributes;

	public uint SizeInBytes;

	public string[] GenericParameterNames = Array.Empty<string>();

	public readonly List<FieldDescriptor> Fields = new List<FieldDescriptor>();

	public readonly List<PropertyDescriptor> Properties = new List<PropertyDescriptor>();

	public readonly List<MethodDescriptor> Methods = new List<MethodDescriptor>();

	public TypeInfoAddress TypeInfo;

	public string Tag => Utilities.GetTypeTag((long)TypeDef.nameIndex, (long)TypeDef.namespaceIndex, (long)TypeDef.token);

	public string Name
	{
		get
		{
			if (GenericParameterNames.Length == 0)
			{
				return m_name;
			}
			return $"{m_name}`{GenericParameterNames.Length}";
		}
	}

	public string FullName
	{
		get
		{
			if (DeclaringParent != null)
			{
				return DeclaringParent.FullName + "+" + m_name.Split('.').Last();
			}
			return Name;
		}
	}

	public bool IsStatic
	{
		get
		{
			if (((TypeAttributes)TypeDef.flags).HasFlag(TypeAttributes.Abstract))
			{
				return ((TypeAttributes)TypeDef.flags).HasFlag(TypeAttributes.Sealed);
			}
			return false;
		}
	}

	private string DebuggerDisplay => string.Join(" : ", Name, Base?.Name).TrimEnd(' ', ':');

	public TypeDescriptor(string name, Il2CppTypeDefinition typeDef, Il2CppImageDefinition imageDef)
	{
		m_name = name;
		TypeDef = typeDef;
		ImageDef = imageDef;
		Attributes = Helpers.GetTypeAttributes(typeDef);
	}

	public TypeDescriptor(string name, Il2CppTypeDefinition typeDef, Il2CppImageDefinition imageDef, Il2CppGenericClass genericClass, uint genericTypeIndex)
	{
		m_name = name;
		TypeDef = typeDef;
		ImageDef = imageDef;
		GenericClass = genericClass;
		GenericTypeIndex = genericTypeIndex;
		Attributes = Helpers.GetTypeAttributes(typeDef);
	}
}
