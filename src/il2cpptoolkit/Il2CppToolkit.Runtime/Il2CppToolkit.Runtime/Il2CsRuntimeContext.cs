using System;
using System.ComponentModel;
using System.Diagnostics;
using Il2CppToolkit.Injection.Client;
using Il2CppToolkit.Runtime.Types;
using ProcessMemoryUtilities.Managed;
using ProcessMemoryUtilities.Native;

namespace Il2CppToolkit.Runtime;

public class Il2CsRuntimeContext : IMemorySource, IDisposable
{
	public class ObjectEventArgs : EventArgs
	{
		public ulong Address { get; }

		public object Value { get; }

		public ObjectEventArgs(object obj, ulong address)
		{
			Value = obj;
			Address = address;
		}
	}

	private readonly IntPtr processHandle;

	internal Il2CppTypeCache TypeCache = new Il2CppTypeCache();

	public InjectionClient InjectionClient { get; private set; }

	public Process TargetProcess { get; }

	public ITypeInfoProvider FallbackTypeInfoProvider { get; set; }

	public IMemorySource Parent => null;

	public Il2CsRuntimeContext ParentContext => this;

	public Il2CsRuntimeContext(Process target)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		TargetProcess = target;
		InjectionClient = new InjectionClient(target);
		processHandle = NativeWrapper.OpenProcess((ProcessAccessFlags)16, true, TargetProcess.Id);
		if (processHandle == IntPtr.Zero)
		{
			int err = new Win32Exception().NativeErrorCode;
			throw new InvalidOperationException(
				$"[Il2CsRuntimeContext] OpenProcess(PROCESS_VM_READ) failed for pid={target.Id}: Win32Error={err} " +
				$"(5=ACCESS_DENIED means RAID runs elevated; run RTK as admin)");
		}
	}

	public void Dispose()
	{
		NativeWrapper.CloseHandle(processHandle);
		InjectionClient.Dispose();
	}

	public ReadOnlyMemory<byte> ReadMemory(ulong address, ulong size)
	{
		byte[] array = new byte[size];
		if (!NativeWrapper.ReadProcessMemoryArray<byte>(processHandle, (IntPtr)(long)address, array))
		{
			StructuredErrorExtensions.Raise<RuntimeError>(RuntimeError.ReadProcessMemoryReadFailed, $"Failed to read memory at 0x{address:X} (size={size}, handle=0x{processHandle.ToInt64():X}). GetLastError() = {NativeWrapper.LastError}");
		}
		return new ReadOnlyMemory<byte>(array);
	}

	public void WriteMemory(ulong address, ulong size, byte[] buffer)
	{
		if ((ulong)buffer.Length != size)
		{
			throw new ArgumentOutOfRangeException("Buffer length does not match size parameter;");
		}
		if (!NativeWrapper.WriteProcessMemoryArray<byte>(processHandle, (IntPtr)(long)address, buffer))
		{
			StructuredErrorExtensions.Raise<RuntimeError>(RuntimeError.WriteProcessMemoryWriteFailed, $"Failed to write memory location. GetLastError() = {NativeWrapper.LastError}");
		}
	}

	internal CachedMemoryBlock CacheMemory(ulong address, ulong size)
	{
		byte[] array = new byte[size];
		if (!NativeWrapper.ReadProcessMemoryArray<byte>(processHandle, (IntPtr)(long)address, array))
		{
			StructuredErrorExtensions.Raise<RuntimeError>(RuntimeError.ReadProcessMemoryReadFailed, $"Failed to read memory location. GetLastError() = {NativeWrapper.LastError}");
		}
		return new CachedMemoryBlock(this, address, array);
	}

	public static ulong GetTypeSize(Type type)
	{
		if (TypeSystem.TypeSizes.TryGetValue(type, out var value))
		{
			return (uint)value;
		}
		if (type.IsEnum)
		{
			return GetTypeSize(type.GetEnumUnderlyingType());
		}
		if (type.IsArray)
		{
			return 8uL;
		}
		if (!type.IsValueType)
		{
			return 8uL;
		}
		if (type.IsAssignableTo(typeof(RuntimeObject)))
		{
			return 8uL;
		}
		throw new NotSupportedException("Unexpected type === unknown size");
	}
}
