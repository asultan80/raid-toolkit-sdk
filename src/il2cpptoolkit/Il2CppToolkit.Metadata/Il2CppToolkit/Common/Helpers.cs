using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.Common;

internal static class Helpers
{
	public static readonly Dictionary<int, Type> TypeMap = new Dictionary<int, Type>
	{
		{
			1,
			typeof(void)
		},
		{
			2,
			typeof(bool)
		},
		{
			3,
			typeof(char)
		},
		{
			4,
			typeof(sbyte)
		},
		{
			5,
			typeof(byte)
		},
		{
			6,
			typeof(short)
		},
		{
			7,
			typeof(ushort)
		},
		{
			8,
			typeof(int)
		},
		{
			9,
			typeof(uint)
		},
		{
			10,
			typeof(long)
		},
		{
			11,
			typeof(ulong)
		},
		{
			12,
			typeof(float)
		},
		{
			13,
			typeof(double)
		},
		{
			14,
			typeof(string)
		},
		{
			22,
			typeof(IntPtr)
		},
		{
			24,
			typeof(IntPtr)
		},
		{
			25,
			typeof(UIntPtr)
		},
		{
			28,
			typeof(object)
		}
	};

	public static TypeAttributes GetTypeAttributes(Il2CppTypeDefinition typeDef)
	{
		TypeAttributes typeAttributes = TypeAttributes.NotPublic;
		switch (typeDef.flags & 7)
		{
		case 1u:
			typeAttributes |= TypeAttributes.Public;
			break;
		case 2u:
			typeAttributes |= TypeAttributes.NestedPublic;
			break;
		case 0u:
			typeAttributes |= TypeAttributes.Public;
			break;
		case 6u:
			typeAttributes |= TypeAttributes.NestedPublic;
			break;
		case 5u:
			typeAttributes |= TypeAttributes.NestedPublic;
			break;
		case 3u:
			typeAttributes |= TypeAttributes.NestedPublic;
			break;
		case 4u:
			typeAttributes |= TypeAttributes.NestedPublic;
			break;
		case 7u:
			typeAttributes |= TypeAttributes.NestedPublic;
			break;
		}
		if ((typeDef.flags & 0x80) == 0 || (typeDef.flags & 0x100) == 0)
		{
			if ((typeDef.flags & 0x20) == 0 && (typeDef.flags & 0x80u) != 0)
			{
				typeAttributes |= TypeAttributes.Abstract;
			}
			else if ((typeDef.flags & 0x100u) != 0)
			{
				typeAttributes |= TypeAttributes.Sealed;
			}
		}
		if ((typeDef.flags & 0x20u) != 0)
		{
			typeAttributes |= TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract;
		}
		if (((TypeAttributes)typeDef.flags).HasFlag(TypeAttributes.Serializable))
		{
			typeAttributes |= TypeAttributes.Serializable;
		}
		if (((TypeAttributes)typeDef.flags).HasFlag(TypeAttributes.NotPublic))
		{
			typeAttributes |= TypeAttributes.NotPublic;
		}
		return typeAttributes;
	}
}
