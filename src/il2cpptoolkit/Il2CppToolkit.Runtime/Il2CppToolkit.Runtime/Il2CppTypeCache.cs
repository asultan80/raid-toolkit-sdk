using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Grpc.Core;
using Il2CppToolkit.Injection.Client;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime;

public class Il2CppTypeCache
{
	private readonly ConcurrentDictionary<ulong, Il2CppTypeInfo> TypeInfoByAddr = new ConcurrentDictionary<ulong, Il2CppTypeInfo>();

	private readonly ConcurrentDictionary<Type, Il2CppTypeInfo> TypeInfoByType = new ConcurrentDictionary<Type, Il2CppTypeInfo>();

	public static bool HasType(Il2CsRuntimeContext runtime, Type managedType)
	{
		Il2CppTypeInfo value;
		return runtime.TypeCache.TypeInfoByType.TryGetValue(managedType, out value);
	}

	internal static readonly TimeSpan kRpcDeadline = TimeSpan.FromSeconds(10);

	public static bool TryGetOrLoadTypeInfoCore(Il2CsRuntimeContext runtime, Type managedType, ulong classAddr, out Il2CppTypeInfo result)
	{
		Il2CppTypeCache typeCache = runtime.TypeCache;
		if (managedType == null)
		{
			throw new ArgumentNullException("managedType");
		}
		result = typeCache.TypeInfoByType.GetOrAdd(managedType, (Type mt) =>
		{
			try
			{
				return (classAddr == 0)
					? runtime.InjectionClient.Il2Cpp.GetTypeInfo(new GetTypeInfoRequest { Klass = Il2CppTypeName.GetKlass(mt) }, (Metadata)null, DateTime.UtcNow.Add(kRpcDeadline), default(CancellationToken)).TypeInfo
					: runtime.InjectionClient.Il2Cpp.GetTypeInfo(new GetTypeInfoRequest { Address = classAddr }, (Metadata)null, DateTime.UtcNow.Add(kRpcDeadline), default(CancellationToken)).TypeInfo;
			}
			catch (RpcException)
			{
				if (runtime.FallbackTypeInfoProvider?.TryGetTypeInfo(runtime, mt, out Il2CppTypeInfo fallback) == true)
					return fallback;
				throw;
			}
		});
		if (result == null)
		{
			return false;
		}
		return typeCache.TypeInfoByAddr.TryAdd(result.KlassId.Address, result);
	}

	public static Il2CppTypeInfo GetTypeInfo(Il2CsRuntimeContext runtime, Type managedType, ulong classAddr = 0uL)
	{
		if (managedType == null)
		{
			throw new ArgumentNullException("managedType");
		}
		if (!TryGetOrLoadTypeInfoCore(runtime, managedType, classAddr, out var result) || result == null)
		{
			return result;
		}
		if (managedType.IsValueType)
		{
			return result;
		}
		Type type = managedType;
		ClassDefinition classDefinition = null;
		if (classAddr != 0)
		{
			classDefinition = new ClassDefinition(runtime, classAddr);
		}
		while ((type = type.BaseType) != null && type.GetCustomAttribute<GeneratedAttribute>() != null)
		{
			classDefinition = classDefinition?.Base;
			if (!TryGetOrLoadTypeInfoCore(runtime, type, classDefinition?.Address ?? 0, out var _))
			{
				break;
			}
		}
		return result;
	}
}
