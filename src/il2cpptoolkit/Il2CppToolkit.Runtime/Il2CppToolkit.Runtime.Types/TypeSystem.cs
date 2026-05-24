using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Runtime.Types;

public static class TypeSystem
{
	private static readonly Dictionary<Type, Type> NativeFactoryMapping;

	private static readonly Dictionary<Type, ITypeFactory> NativeFactoryInstances;

	private static readonly Dictionary<string, Type> NativeMapping;

	public static readonly Dictionary<Type, int> TypeSizes;

	public static bool TryGetSubstituteType(string typeName, out Type mappedType, Type declaringType = null)
	{
		if (NativeMapping.TryGetValue(typeName, out mappedType))
		{
			return true;
		}
		Type type = ((declaringType != null) ? declaringType.GetNestedType(typeName) : Type.GetType(typeName, throwOnError: false));
		if (type != null && (NativeFactoryMapping.TryGetValue(type, out var _) || type.IsInterface))
		{
			mappedType = type;
			return true;
		}
		if (typeName.StartsWith("System."))
		{
			mappedType = null;
			return true;
		}
		return false;
	}

	public static bool TryGetTypeFactory(Type type, out ITypeFactory typeFactory)
	{
		if (!NativeFactoryMapping.TryGetValue(type, out var value) && (!type.IsConstructedGenericType || !NativeFactoryMapping.TryGetValue(type.GetGenericTypeDefinition(), out value)))
		{
			typeFactory = null;
			return false;
		}
		if (NativeFactoryInstances.TryGetValue(type, out typeFactory))
		{
			return true;
		}
		ErrorHandler.VerifyElseThrow<RuntimeError>(value.IsAssignableTo(typeof(ITypeFactory)), RuntimeError.TypeFactoryImplementationMissing, "Class marked with [TypeFactoryAttribute] must extend ITypeFactory: '" + value.FullName + "'");
		if (type.IsGenericType)
		{
			ErrorHandler.VerifyElseThrow<RuntimeError>(value.IsGenericTypeDefinition, RuntimeError.GenericFactoryRequired, "A generic type must have a generic factory");
			value = value.MakeGenericType(type.GenericTypeArguments);
		}
		typeFactory = Activator.CreateInstance(value) as ITypeFactory;
		NativeFactoryInstances.TryAdd(type, typeFactory);
		return true;
	}

	static TypeSystem()
	{
		NativeFactoryMapping = new Dictionary<Type, Type>();
		NativeFactoryInstances = new Dictionary<Type, ITypeFactory>();
		NativeMapping = new Dictionary<string, Type>();
		TypeSizes = new Dictionary<Type, int>
		{
			{
				typeof(void),
				0
			},
			{
				typeof(bool),
				1
			},
			{
				typeof(char),
				2
			},
			{
				typeof(sbyte),
				1
			},
			{
				typeof(byte),
				1
			},
			{
				typeof(short),
				2
			},
			{
				typeof(ushort),
				2
			},
			{
				typeof(int),
				4
			},
			{
				typeof(uint),
				4
			},
			{
				typeof(long),
				8
			},
			{
				typeof(ulong),
				8
			},
			{
				typeof(float),
				4
			},
			{
				typeof(double),
				8
			},
			{
				typeof(string),
				8
			},
			{
				typeof(IntPtr),
				8
			},
			{
				typeof(UIntPtr),
				8
			},
			{
				typeof(object),
				8
			}
		};
		NativeMapping.Add(typeof(ValueType).FullName, typeof(ValueType));
		foreach (var (type, value) in GetTypesWithMappingAttribute(typeof(TypeSystem).Assembly))
		{
			NativeMapping.Add(type.FullName, value);
		}
		foreach (var (typeFactoryAttribute, value2) in GetTypesWithFactoryAttribute(typeof(TypeSystem).Assembly))
		{
			NativeFactoryMapping.Add(typeFactoryAttribute.Type, value2);
		}
	}

	private static IEnumerable<(Type, Type)> GetTypesWithMappingAttribute(Assembly assembly)
	{
		Type[] types = assembly.GetTypes();
		foreach (Type type in types)
		{
			TypeMappingAttribute customAttribute = type.GetCustomAttribute<TypeMappingAttribute>(inherit: true);
			if (customAttribute != null)
			{
				yield return (customAttribute.Type, type);
			}
		}
	}

	private static IEnumerable<(TypeFactoryAttribute, Type)> GetTypesWithFactoryAttribute(Assembly assembly)
	{
		Type[] types = assembly.GetTypes();
		foreach (Type type in types)
		{
			TypeFactoryAttribute customAttribute = type.GetCustomAttribute<TypeFactoryAttribute>(inherit: true);
			if (customAttribute != null)
			{
				yield return (customAttribute, type);
			}
		}
	}
}
