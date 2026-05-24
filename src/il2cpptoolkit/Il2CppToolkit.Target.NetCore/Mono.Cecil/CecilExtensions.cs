using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil;

public static class CecilExtensions
{
	public static void AddRange<T>(this Collection<T> self, params T[] items)
	{
		self.AddRange((IEnumerable<T>)items);
	}

	public static void AddRange<T>(this Collection<T> self, IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			self.Add(item);
		}
	}

	public static string GetSafeName(this TypeReference self)
	{
		if (self.HasGenericParameters)
		{
			return ((MemberReference)self).Name.Split('`')[0];
		}
		return ((MemberReference)self).Name;
	}

	public static string GetFullSafeName(this TypeReference self)
	{
		return Regex.Replace(Regex.Replace(((MemberReference)self).FullName, "[<(\\[].*[\\])>]", ""), "`\\d*", "").Replace('.', '_');
	}

	public static TypeReference AsGenericThis(this TypeReference self)
	{
		if (!self.HasGenericParameters)
		{
			return self;
		}
		return (TypeReference)(object)self.MakeGenericType((IEnumerable<TypeReference>)self.GenericParameters);
	}

	public static TypeReference AsTypeReference(this TypeDefinition self)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		return new TypeReference(((TypeReference)self).Namespace, ((MemberReference)self).Name, ((MemberReference)self).Module, ((TypeReference)self).Scope)
		{
			IsValueType = ((TypeReference)self).IsValueType
		};
	}

	public static GenericInstanceType MakeGenericType(this TypeReference self, params TypeReference[] arguments)
	{
		return self.MakeGenericType((IEnumerable<TypeReference>)arguments);
	}

	public static GenericInstanceType MakeGenericType(this TypeReference self, IEnumerable<TypeReference> arguments)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		if (self.GenericParameters.Count != arguments.Count())
		{
			throw new ArgumentOutOfRangeException("self");
		}
		GenericInstanceType val = new GenericInstanceType(self);
		foreach (TypeReference argument in arguments)
		{
			val.GenericArguments.Add(argument);
		}
		return val;
	}

	public static MethodReference GetConstructor(this TypeReference typeReference, ModuleDefinition module, params TypeReference[] arguments)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		MethodReference val = new MethodReference(".ctor", module.TypeSystem.Void, typeReference)
		{
			HasThis = true,
			ExplicitThis = false,
			CallingConvention = (MethodCallingConvention)0
		};
		val.Parameters.AddRange(((IEnumerable<TypeReference>)arguments).Select((Func<TypeReference, ParameterDefinition>)((TypeReference arg) => new ParameterDefinition(arg))));
		return val;
	}

	public static GenericInstanceMethod MakeGeneric(this MethodReference self, params TypeReference[] arguments)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		GenericInstanceMethod val = new GenericInstanceMethod(self);
		val.GenericArguments.AddRange(arguments);
		return val;
	}

	public static void EmitByte(this ILProcessor self, byte value)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		switch (value)
		{
		case 0:
			self.Emit(OpCodes.Ldc_I4_0);
			break;
		case 1:
			self.Emit(OpCodes.Ldc_I4_1);
			break;
		case 2:
			self.Emit(OpCodes.Ldc_I4_2);
			break;
		case 3:
			self.Emit(OpCodes.Ldc_I4_3);
			break;
		case 4:
			self.Emit(OpCodes.Ldc_I4_4);
			break;
		case 5:
			self.Emit(OpCodes.Ldc_I4_5);
			break;
		case 6:
			self.Emit(OpCodes.Ldc_I4_6);
			break;
		case 7:
			self.Emit(OpCodes.Ldc_I4_7);
			break;
		case 8:
			self.Emit(OpCodes.Ldc_I4_8);
			break;
		default:
			throw new ArgumentOutOfRangeException("value");
		}
	}

	public static void EmitI4(this ILProcessor self, int value)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		switch (value)
		{
		case 0:
			self.Emit(OpCodes.Ldc_I4_0);
			break;
		case 1:
			self.Emit(OpCodes.Ldc_I4_1);
			break;
		case 2:
			self.Emit(OpCodes.Ldc_I4_2);
			break;
		case 3:
			self.Emit(OpCodes.Ldc_I4_3);
			break;
		case 4:
			self.Emit(OpCodes.Ldc_I4_4);
			break;
		case 5:
			self.Emit(OpCodes.Ldc_I4_5);
			break;
		case 6:
			self.Emit(OpCodes.Ldc_I4_6);
			break;
		case 7:
			self.Emit(OpCodes.Ldc_I4_7);
			break;
		case 8:
			self.Emit(OpCodes.Ldc_I4_8);
			break;
		default:
			self.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
			break;
		}
	}

	public static void EmitArg(this ILProcessor self, int value)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		switch (value)
		{
		case 0:
			self.Emit(OpCodes.Ldarg_0);
			break;
		case 1:
			self.Emit(OpCodes.Ldarg_1);
			break;
		case 2:
			self.Emit(OpCodes.Ldarg_2);
			break;
		case 3:
			self.Emit(OpCodes.Ldarg_3);
			break;
		default:
			self.Emit(OpCodes.Ldarg_S, (byte)value);
			break;
		}
	}
}
