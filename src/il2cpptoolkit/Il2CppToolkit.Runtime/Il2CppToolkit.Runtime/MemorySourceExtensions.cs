using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppToolkit.Runtime.Types;
using Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

namespace Il2CppToolkit.Runtime;

public static class MemorySourceExtensions
{
	private class ConvertPrimitive
	{
		public Func<IMemorySource, ulong, object> ReadFn;

		public Action<IMemorySource, ulong, object> WriteFn;
	}

	private static Assembly ThisAsm;

	private static readonly Dictionary<Type, ConvertPrimitive> s_implMap;

	public static event EventHandler<MemoryAccessEventArgs> ObjectReadFromMemory;

	public static event EventHandler<MemoryAccessErrorEventArgs> ObjectReadError;

	static MemorySourceExtensions()
	{
		s_implMap = new Dictionary<Type, ConvertPrimitive>();
		ThisAsm = typeof(MemorySourceExtensions).Assembly;
		s_implMap.Add(typeof(char), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToChar(context.ReadMemory(address, 2uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 2uL, BitConverter.GetBytes((char)value));
			}
		});
		s_implMap.Add(typeof(bool), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToBoolean(context.ReadMemory(address, 1uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 1uL, BitConverter.GetBytes((bool)value));
			}
		});
		s_implMap.Add(typeof(double), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToDouble(context.ReadMemory(address, 8uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 8uL, BitConverter.GetBytes((double)value));
			}
		});
		s_implMap.Add(typeof(float), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToSingle(context.ReadMemory(address, 4uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 4uL, BitConverter.GetBytes((float)value));
			}
		});
		s_implMap.Add(typeof(short), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToInt16(context.ReadMemory(address, 2uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 2uL, BitConverter.GetBytes((short)value));
			}
		});
		s_implMap.Add(typeof(int), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToInt32(context.ReadMemory(address, 4uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 4uL, BitConverter.GetBytes((int)value));
			}
		});
		s_implMap.Add(typeof(long), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToInt64(context.ReadMemory(address, 8uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 8uL, BitConverter.GetBytes((long)value));
			}
		});
		s_implMap.Add(typeof(ushort), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToUInt16(context.ReadMemory(address, 2uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 2uL, BitConverter.GetBytes((ushort)value));
			}
		});
		s_implMap.Add(typeof(uint), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToUInt32(context.ReadMemory(address, 4uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 4uL, BitConverter.GetBytes((uint)value));
			}
		});
		s_implMap.Add(typeof(ulong), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToUInt64(context.ReadMemory(address, 8uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 8uL, BitConverter.GetBytes((ulong)value));
			}
		});
		s_implMap.Add(typeof(IntPtr), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToIntPtr(context.ReadMemory(address, 8uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 8uL, BitConverter.GetBytes((long)value));
			}
		});
		s_implMap.Add(typeof(UIntPtr), new ConvertPrimitive
		{
			ReadFn = (IMemorySource context, ulong address) => ReadOnlyMemoryExtensions.ToUIntPtr(context.ReadMemory(address, 8uL)),
			WriteFn = delegate(IMemorySource context, ulong address, object value)
			{
				context.ParentContext.WriteMemory(address, 8uL, BitConverter.GetBytes((ulong)value));
			}
		});
	}

	public static T ReadValue<T>(this IMemorySource source, ulong address, byte indirection = 1)
	{
		return (T)source.ReadValue(typeof(T), address, indirection);
	}

	public static object ReadValue(this IMemorySource source, Type type, ulong address, byte indirection = 1)
	{
		try
		{
			MemorySourceExtensions.ObjectReadFromMemory?.Invoke(source, new MemoryAccessEventArgs(type, address));
			if (!type.IsValueType)
			{
				indirection++;
			}
			while (indirection > 1)
			{
				address = source.ReadPointer(address);
				if (address == 0L)
				{
					break;
				}
				indirection--;
			}
			if (address == 0L && !type.IsAssignableTo(typeof(INullConstructable)))
			{
				return null;
			}
			if (TypeSystem.TryGetTypeFactory(type, out var typeFactory))
			{
				return typeFactory.ReadValue(source, address);
			}
			if (type.IsEnum)
			{
				return source.ReadPrimitive(type.GetEnumUnderlyingType(), address);
			}
			if (type.IsPrimitive)
			{
				return source.ReadPrimitive(type, address);
			}
			return source.ReadStruct(type, address);
		}
		catch (Exception ex)
		{
			MemorySourceExtensions.ObjectReadError?.Invoke(source, new MemoryAccessErrorEventArgs(type, address, ex));
			throw;
		}
	}

	public static ulong ReadPointer(this IMemorySource source, ulong address)
	{
		return source.ReadPrimitive<ulong>(address);
	}

	private static object ReadStruct(this IMemorySource source, Type type, ulong address)
	{
		if (address == 0L && !type.IsAssignableTo(typeof(INullConstructable)))
		{
			return null;
		}
		if (type.IsArray)
		{
			dynamic val = Activator.CreateInstance(typeof(Native__Array<>).MakeGenericType(type.GetElementType()), source, address);
			return val.Array;
		}
		if (type.Assembly != ThisAsm && !type.IsValueType)
		{
			Type type2 = type;
			UnknownClass unknownClass = (UnknownClass)source.ReadStruct(typeof(UnknownClass), address);
			if (unknownClass?.ClassDefinition == null)
			{
				if (!(type2 == typeof(object)))
				{
					return null;
				}
				return unknownClass;
			}
			type = LoadedTypes.GetType(unknownClass.ClassDefinition);
			if (type == null)
			{
				if (!(type2 == typeof(object)))
				{
					return null;
				}
				return unknownClass;
			}
			if (!Il2CppTypeCache.HasType(source.ParentContext, type))
			{
				Il2CppTypeCache.GetTypeInfo(source.ParentContext, type, unknownClass.ClassDefinition.Address);
			}
			if (type.IsGenericType && type.ContainsGenericParameters)
			{
				if (type2.IsGenericType && type2.ContainsGenericParameters)
				{
					if (!(type2 == typeof(object)))
					{
						return null;
					}
					return unknownClass;
				}
				type = type2;
			}
		}
		if (type.IsAssignableTo(typeof(IRuntimeObject)))
		{
			return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[2] { source, address }, null);
		}
		if (type.GetConstructors().Length != 0)
		{
			_ = type.GetConstructor(Array.Empty<Type>()) == null;
			return null;
		}
		return null;
	}

	private static object ReadPrimitive(this IMemorySource context, Type type, ulong address)
	{
		if (s_implMap.TryGetValue(type, out var value))
		{
			return value.ReadFn(context, address);
		}
		throw new ArgumentException("Type '" + type.FullName + "' is not a valid primitive type");
	}

	private static T ReadPrimitive<T>(this IMemorySource context, ulong address)
	{
		if (s_implMap.TryGetValue(typeof(T), out var value))
		{
			return (T)value.ReadFn(context, address);
		}
		throw new ArgumentException("Type '" + typeof(T).FullName + "' is not a valid primitive type");
	}
}
