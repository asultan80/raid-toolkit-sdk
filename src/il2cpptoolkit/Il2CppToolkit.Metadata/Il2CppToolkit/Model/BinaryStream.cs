using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppToolkit.Model;

public class BinaryStream : IDisposable
{
	public double Version;

	public bool Is32Bit;

	public ulong ImageBase;

	private readonly Stream stream;

	private readonly BinaryReader reader;

	private readonly BinaryWriter writer;

	private readonly MethodInfo readClass;

	private readonly MethodInfo readClassArray;

	private readonly Dictionary<Type, MethodInfo> genericMethodCache;

	private readonly Dictionary<FieldInfo, VersionAttribute[]> attributeCache;

	public ulong Position
	{
		get
		{
			return (ulong)stream.Position;
		}
		set
		{
			stream.Position = (long)value;
		}
	}

	public ulong Length => (ulong)stream.Length;

	public ulong PointerSize
	{
		get
		{
			if (!Is32Bit)
			{
				return 8uL;
			}
			return 4uL;
		}
	}

	public BinaryReader Reader => reader;

	public BinaryWriter Writer => writer;

	public BinaryStream(Stream input)
	{
		stream = input;
		reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
		writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
		readClass = GetType().GetMethod("ReadClass", Type.EmptyTypes);
		readClassArray = GetType().GetMethod("ReadClassArray", new Type[1] { typeof(long) });
		genericMethodCache = new Dictionary<Type, MethodInfo>();
		attributeCache = new Dictionary<FieldInfo, VersionAttribute[]>();
	}

	public bool ReadBoolean()
	{
		return reader.ReadBoolean();
	}

	public byte ReadByte()
	{
		return reader.ReadByte();
	}

	public byte[] ReadBytes(int count)
	{
		return reader.ReadBytes(count);
	}

	public sbyte ReadSByte()
	{
		return reader.ReadSByte();
	}

	public short ReadInt16()
	{
		return reader.ReadInt16();
	}

	public ushort ReadUInt16()
	{
		return reader.ReadUInt16();
	}

	public int ReadInt32()
	{
		return reader.ReadInt32();
	}

	public uint ReadUInt32()
	{
		return reader.ReadUInt32();
	}

	public long ReadInt64()
	{
		return reader.ReadInt64();
	}

	public ulong ReadUInt64()
	{
		return reader.ReadUInt64();
	}

	public float ReadSingle()
	{
		return reader.ReadSingle();
	}

	public double ReadDouble()
	{
		return reader.ReadDouble();
	}

	public string ReadString(int len)
	{
		return reader.ReadString(len);
	}

	public uint ReadCompressedUInt32()
	{
		return reader.ReadCompressedUInt32();
	}

	public int ReadCompressedInt32()
	{
		return reader.ReadCompressedInt32();
	}

	public uint ReadULeb128()
	{
		return reader.ReadULeb128();
	}

	public void Write(bool value)
	{
		writer.Write(value);
	}

	public void Write(byte value)
	{
		writer.Write(value);
	}

	public void Write(sbyte value)
	{
		writer.Write(value);
	}

	public void Write(short value)
	{
		writer.Write(value);
	}

	public void Write(ushort value)
	{
		writer.Write(value);
	}

	public void Write(int value)
	{
		writer.Write(value);
	}

	public void Write(uint value)
	{
		writer.Write(value);
	}

	public void Write(long value)
	{
		writer.Write(value);
	}

	public void Write(ulong value)
	{
		writer.Write(value);
	}

	public void Write(float value)
	{
		writer.Write(value);
	}

	public void Write(double value)
	{
		writer.Write(value);
	}

	private object ReadPrimitive(Type type)
	{
		return type.Name switch
		{
			"Int32" => ReadInt32(), 
			"UInt32" => ReadUInt32(), 
			"Int16" => ReadInt16(), 
			"UInt16" => ReadUInt16(), 
			"Byte" => ReadByte(), 
			"Int64" => ReadIntPtr(), 
			"UInt64" => ReadUIntPtr(), 
			_ => throw new NotSupportedException(), 
		};
	}

	public T ReadClass<T>(ulong addr) where T : new()
	{
		Position = addr;
		return ReadClass<T>();
	}

	public T ReadClass<T>() where T : new()
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle.IsPrimitive)
		{
			return (T)ReadPrimitive(typeFromHandle);
		}
		T val = new T();
		FieldInfo[] fields = val.GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (!attributeCache.TryGetValue(fieldInfo, out var value) && Attribute.IsDefined(fieldInfo, typeof(VersionAttribute)))
			{
				value = fieldInfo.GetCustomAttributes<VersionAttribute>().ToArray();
				attributeCache.Add(fieldInfo, value);
			}
			if (value != null && value.Length != 0)
			{
				bool flag = false;
				VersionAttribute[] array = value;
				foreach (VersionAttribute versionAttribute in array)
				{
					if (Version >= versionAttribute.Min && Version <= versionAttribute.Max)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
			}
			Type fieldType = fieldInfo.FieldType;
			if (fieldType.IsPrimitive)
			{
				fieldInfo.SetValue(val, ReadPrimitive(fieldType));
			}
			else if (fieldType.IsEnum)
			{
				Type fieldType2 = fieldType.GetField("value__").FieldType;
				fieldInfo.SetValue(val, ReadPrimitive(fieldType2));
			}
			else if (fieldType.IsArray)
			{
				ArrayLengthAttribute customAttribute = fieldInfo.GetCustomAttribute<ArrayLengthAttribute>();
				if (!genericMethodCache.TryGetValue(fieldType, out var value2))
				{
					value2 = readClassArray.MakeGenericMethod(fieldType.GetElementType());
					genericMethodCache.Add(fieldType, value2);
				}
				fieldInfo.SetValue(val, value2.Invoke(this, new object[1] { customAttribute.Length }));
			}
			else
			{
				if (!genericMethodCache.TryGetValue(fieldType, out var value3))
				{
					value3 = readClass.MakeGenericMethod(fieldType);
					genericMethodCache.Add(fieldType, value3);
				}
				fieldInfo.SetValue(val, value3.Invoke(this, null));
			}
		}
		return val;
	}

	public T[] ReadClassArray<T>(long count) where T : new()
	{
		T[] array = new T[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = ReadClass<T>();
		}
		return array;
	}

	public T[] ReadClassArray<T>(ulong addr, ulong count) where T : new()
	{
		return ReadClassArray<T>(addr, (long)count);
	}

	public T[] ReadClassArray<T>(ulong addr, long count) where T : new()
	{
		Position = addr;
		return ReadClassArray<T>(count);
	}

	public string ReadStringToNull(ulong addr)
	{
		Position = addr;
		List<byte> list = new List<byte>();
		byte item;
		while ((item = ReadByte()) != 0)
		{
			list.Add(item);
		}
		return Encoding.UTF8.GetString(list.ToArray());
	}

	public long ReadIntPtr()
	{
		if (!Is32Bit)
		{
			return ReadInt64();
		}
		return ReadInt32();
	}

	public virtual ulong ReadUIntPtr()
	{
		if (!Is32Bit)
		{
			return ReadUInt64();
		}
		return ReadUInt32();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			reader.Dispose();
			writer.Dispose();
			stream.Close();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
