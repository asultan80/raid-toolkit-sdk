using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime.Types;

public static class LoadedTypes
{
	private static readonly Dictionary<string, Type> s_nameToType;

	public static Type GetType(string fullName)
	{
		if (!s_nameToType.TryGetValue(fullName, out var value))
		{
			return null;
		}
		return value;
	}

	public static Type GetType(ClassDefinition classDef)
	{
		return GetType(classDef.FullName);
	}

	static LoadedTypes()
	{
		s_nameToType = new Dictionary<string, Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetCustomAttribute<GeneratedAttribute>() == null)
			{
				continue;
			}
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types.Where(t => t != null).ToArray();
			}
			foreach (Type type in types)
			{
				if (type.GetCustomAttribute<GeneratedAttribute>() != null)
				{
					s_nameToType.TryAdd(type.FullName, type);
				}
			}
		}
	}
}
