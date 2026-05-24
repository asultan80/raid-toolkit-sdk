using System;
using System.Linq;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime;

public static class Il2CppTypeName
{
	public static ClassId GetKlass(Type type)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		ClassId val = new ClassId
		{
			Name = GetTypeName(type, includeNamespace: false),
			Namespaze = type.Namespace,
			IsValueType = type.IsValueType,
			IsNullable = (type.BaseType == typeof(Nullable<>))
		};
		Type type2 = type;
		ClassId val2 = val;
		while ((type2 = type2.DeclaringType) != null)
		{
			ClassId val3 = (val2.DeclaringType = GetKlass(type2));
			val2 = val3;
		}
		return val;
	}

	private static string GetTypeScope(Type type, bool includeNamespace)
	{
		if (type.DeclaringType != null)
		{
			return GetTypeName(type.DeclaringType, includeNamespace) + ".";
		}
		if (!includeNamespace || string.IsNullOrEmpty(type.Namespace))
		{
			return "";
		}
		return type.Namespace + ".";
	}

	public static string GetTypeName(Type type, bool includeNamespace = true)
	{
		string typeScope = GetTypeScope(type, includeNamespace);
		typeScope += type.Name;
		if (type.IsConstructedGenericType)
		{
			typeScope = typeScope.Substring(0, typeScope.Length - 2);
			typeScope += "<";
			typeScope += string.Join(",", type.GenericTypeArguments.Select((Type arg) => GetTypeName(arg)));
			typeScope += ">";
		}
		return typeScope;
	}

	public static string GetTypeName(ClassId type)
	{
		string text = string.Empty;
		if (type.DeclaringType != null)
		{
			text = GetTypeName(type.DeclaringType);
			text += "+";
		}
		if (!string.IsNullOrEmpty(type.Namespaze))
		{
			text += type.Namespaze;
			text += ".";
		}
		return text + type.Name;
	}
}
public static class Il2CppTypeName<TClass>
{
	public static ClassId klass = Il2CppTypeName.GetKlass(typeof(TClass));
}
