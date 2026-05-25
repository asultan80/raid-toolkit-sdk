using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Grpc.Core;
using Il2CppToolkit.Injection.Client;
using Il2CppToolkit.Runtime.Types;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime;

public class Il2CppTypeInfoLookup<TClass>
{
	public static TValue FromValue<TValue>(IRuntimeObject obj, Value returnValue)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected I4, but got Unknown
		Value.ValueOneofCase valueCase = returnValue.ValueCase;
		return (int)(valueCase - 1) switch
		{
			6 => (TValue)(object)returnValue.Bit,
			0 => (TValue)(object)returnValue.Double,
			1 => (TValue)(object)returnValue.Float,
			2 => (TValue)(object)returnValue.Int32,
			4 => (TValue)(object)returnValue.Int64,
			13 => HydrateObject<TValue>(obj.Source.ParentContext, returnValue.Obj),
			7 => (TValue)(object)returnValue.Str,
			3 => (TValue)(object)returnValue.Uint32,
			5 => (TValue)(object)returnValue.Uint64,
			_ => default(TValue),
		};
	}

	public static Value ValueFrom(object value)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_001c: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Expected O, but got Unknown
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Expected O, but got Unknown
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Expected O, but got Unknown
		//IL_0199: Expected O, but got Unknown
		if (value == null)
		{
			return new Value
			{
				Obj = new Il2CppObject
				{
					Address = 0uL
				}
			};
		}
		if (!(value is NullableArg nullable))
		{
			if (!(value is double @double))
			{
				if (!(value is float @float))
				{
					if (!(value is int @int))
					{
						if (!(value is uint @uint))
						{
							if (!(value is ulong uint2))
							{
								if (!(value is long int2))
								{
									if (!(value is bool bit))
									{
										if (!(value is string str))
										{
											if (value is IRuntimeObject runtimeObject)
											{
												return new Value
												{
													Obj = new Il2CppObject
													{
														Address = runtimeObject.Address
													}
												};
											}
											throw new NotSupportedException("Argument type of '" + value.GetType().FullName + "' is not supported");
										}
										return new Value
										{
											Str = str
										};
									}
									return new Value
									{
										Bit = bit
									};
								}
								return new Value
								{
									Int64 = int2
								};
							}
							return new Value
							{
								Uint64 = uint2
							};
						}
						return new Value
						{
							Uint32 = @uint
						};
					}
					return new Value
					{
						Int32 = @int
					};
				}
				return new Value
				{
					Float = @float
				};
			}
			return new Value
			{
				Double = @double
			};
		}
		return ValueFromNullable(nullable);
	}

	private static Value ValueFromNullable(NullableArg nullable)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected O, but got Unknown
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Expected O, but got Unknown
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_017e: Expected O, but got Unknown
		if (!(nullable is NullableArg<double> nullableArg))
		{
			if (!(nullable is NullableArg<float> nullableArg2))
			{
				if (!(nullable is NullableArg<int> nullableArg3))
				{
					if (!(nullable is NullableArg<uint> nullableArg4))
					{
						if (!(nullable is NullableArg<ulong> nullableArg5))
						{
							if (!(nullable is NullableArg<long> nullableArg6))
							{
								if (nullable is NullableArg<bool> nullableArg7)
								{
									return new Value
									{
										Bit = nullableArg7.TypedValue,
										NullState = (NullableState)(nullable.HasValue ? 1 : 2)
									};
								}
								throw new NotSupportedException("Argument type of '" + nullable.GetType().FullName + "' is not supported");
							}
							return new Value
							{
								Int64 = nullableArg6.TypedValue,
								NullState = (NullableState)(nullable.HasValue ? 1 : 2)
							};
						}
						return new Value
						{
							Uint64 = nullableArg5.TypedValue,
							NullState = (NullableState)(nullable.HasValue ? 1 : 2)
						};
					}
					return new Value
					{
						Uint32 = nullableArg4.TypedValue,
						NullState = (NullableState)(nullable.HasValue ? 1 : 2)
					};
				}
				return new Value
				{
					Int32 = nullableArg3.TypedValue,
					NullState = (NullableState)(nullable.HasValue ? 1 : 2)
				};
			}
			return new Value
			{
				Float = nullableArg2.TypedValue,
				NullState = (NullableState)(nullable.HasValue ? 1 : 2)
			};
		}
		return new Value
		{
			Double = nullableArg.TypedValue,
			NullState = (NullableState)(nullable.HasValue ? 1 : 2)
		};
	}

	public static Value CallMethodCore(Il2CsRuntimeContext context, IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		if (arguments == null)
		{
			throw new ArgumentNullException("arguments");
		}
		CallMethodRequest val = new CallMethodRequest
		{
			MethodName = name,
			Klass = Il2CppTypeName<TClass>.klass
		};
		if (obj != null)
		{
			val.Instance = new Il2CppObject
			{
				Address = obj.Address
			};
		}
		val.Arguments.AddRange(arguments.Select(ValueFrom));
		return context.InjectionClient.Il2Cpp.CallMethod(val, (Metadata)null, DateTime.UtcNow.Add(Il2CppTypeCache.kRpcDeadline), default(CancellationToken)).ReturnValue;
	}

	public static void CallMethod(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
	{
		CallMethodCore(obj.Source.ParentContext, obj, name, arguments);
	}

	public static TValue CallMethod<TValue>(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
	{
		Value val = CallMethodCore(obj.Source.ParentContext, obj, name, arguments);
		if (val == null)
		{
			return default(TValue);
		}
		return FromValue<TValue>(obj, val);
	}

	public static TValue CallStaticMethod<TValue>(IMemorySource source, [CallerMemberName] string name = "", object[] arguments = null)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected I4, but got Unknown
		Value val = CallMethodCore(source.ParentContext, null, name, arguments);
		if (val == null)
		{
			return default(TValue);
		}
		Value.ValueOneofCase valueCase = val.ValueCase;
		return (int)(valueCase - 1) switch
		{
			6 => (TValue)(object)val.Bit,
			0 => (TValue)(object)val.Double,
			1 => (TValue)(object)val.Float,
			2 => (TValue)(object)val.Int32,
			4 => (TValue)(object)val.Int64,
			13 => HydrateObject<TValue>(source.ParentContext, val.Obj),
			7 => (TValue)(object)val.Str,
			3 => (TValue)(object)val.Uint32,
			5 => (TValue)(object)val.Uint64,
			_ => default(TValue),
		};
	}

	public static void CallStaticMethod(IMemorySource source, [CallerMemberName] string name = "", object[] arguments = null)
	{
		CallMethodCore(source.ParentContext, null, name, arguments);
	}

	private static TValue HydrateObject<TValue>(IMemorySource source, Il2CppObject obj)
	{
		Type type = LoadedTypes.GetType(Il2CppTypeName.GetTypeName(obj.Klass));
		if (type == null)
		{
			return default(TValue);
		}
		if (!Il2CppTypeCache.HasType(source.ParentContext, type))
		{
			ClassDefinition classDefinition = source.ParentContext.ReadValue<ClassDefinition>(obj.Address, 1);
			Il2CppTypeCache.GetTypeInfo(source.ParentContext, type, classDefinition.Address);
		}
		return (TValue)Activator.CreateInstance(type, source, obj.Address);
	}

	public static TValue GetValue<TValue>(IRuntimeObject obj, string name, byte indirection = 1)
	{
		Il2CppField val = ((IEnumerable<Il2CppField>)Il2CppTypeCache.GetTypeInfo(obj.Source.ParentContext, typeof(TClass), 0uL).Fields).First((Il2CppField fld) => fld.Name == name);
		Il2CppTypeCache.GetTypeInfo(obj.Source.ParentContext, typeof(TValue), val.KlassAddr);
		return obj.Source.ReadValue<TValue>(obj.Address + val.Offset, indirection);
	}

	public static TValue GetStaticValue<TValue>(Il2CsRuntimeContext context, string name, byte indirection = 1)
	{
		Il2CppTypeInfo typeInfo = Il2CppTypeCache.GetTypeInfo(context, typeof(TClass), 0uL);
		Il2CppField val = ((IEnumerable<Il2CppField>)typeInfo.Fields).First((Il2CppField fld) => fld.Name == name);
		Il2CppTypeCache.GetTypeInfo(context, typeof(TValue), val.KlassAddr);
		return context.ReadValue<TValue>(typeInfo.StaticFieldsAddress + val.Offset, indirection);
	}
}
