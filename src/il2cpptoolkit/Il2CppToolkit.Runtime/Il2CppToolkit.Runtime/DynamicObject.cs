using System;
using System.Linq;
using System.Threading;
using Grpc.Core;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime;

public class DynamicObject<T> : IDisposable
{
	private readonly Il2CsRuntimeContext Runtime;

	private PinnedReference<IRuntimeObject>? PinnedRef;

	private bool IsDisposed;

	public IRuntimeObject? Pointer { get; private set; }

	public DynamicObject(Il2CsRuntimeContext runtime)
	{
		Runtime = runtime;
	}

	public DynamicObject(T value)
	{
		if (!(value is IRuntimeObject runtimeObject))
		{
			throw new InvalidCastException("Cannot create a DynamicObject for a non-IRuntimeObject type");
		}
		Runtime = runtimeObject.Source.ParentContext;
		Pointer = runtimeObject;
		PinnedRef = new PinnedReference<IRuntimeObject>(Pointer);
	}

	public void Create(params object[] arguments)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		CreateObjectRequest val = new CreateObjectRequest
		{
			Klass = Il2CppTypeName<T>.klass
		};
		val.Arguments.AddRange(arguments.Select(Il2CppTypeInfoLookup<T>.ValueFrom));
		CreateObjectResponse val2 = Runtime.InjectionClient.Il2Cpp.CreateObject(val, (Metadata)null, (DateTime?)null, default(CancellationToken));
		if ((int)val2.ReturnValue.ValueCase != 14)
		{
			throw new InvalidCastException("Cannot create a DynamicObject for a non-object type");
		}
		Pointer = new ObjectPointer(Runtime, val2.ReturnValue.Obj.Address);
		PinnedRef = new PinnedReference<IRuntimeObject>(Pointer, val2.ReturnValue.Obj.Handle);
	}

	public T Hydrate()
	{
		return (T)Activator.CreateInstance(typeof(T), Runtime, Pointer.Address);
	}

	public U Call<U>(string name, params object[] arguments)
	{
		return Il2CppTypeInfoLookup<T>.CallMethod<U>(Pointer, name, arguments);
	}

	public void Call(string name, params object[] arguments)
	{
		Il2CppTypeInfoLookup<T>.CallMethod(Pointer, name, arguments);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				PinnedRef?.Dispose();
				PinnedRef = null;
				Pointer = null;
			}
			IsDisposed = true;
		}
	}

	~DynamicObject()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
