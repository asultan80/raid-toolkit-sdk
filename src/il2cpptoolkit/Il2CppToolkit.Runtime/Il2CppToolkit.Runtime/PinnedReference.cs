using System;
using System.Threading;
using Grpc.Core;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime;

public class PinnedReference<T> : IDisposable where T : class, IRuntimeObject
{
	private uint? Handle;

	private bool IsDisposed;

	private T Reference;

	public T Value
	{
		get
		{
			if (!Handle.HasValue)
			{
				throw new ObjectDisposedException(typeof(PinnedReference<T>).FullName);
			}
			return Reference;
		}
	}

	public PinnedReference(T reference)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_006a: Expected O, but got Unknown
		Reference = reference;
		PinObjectMessage val = Reference.Source.ParentContext.InjectionClient.Il2Cpp.PinObject(new PinObjectMessage
		{
			Obj = new Il2CppObject
			{
				Address = Reference.Address
			}
		}, (Metadata)null, (DateTime?)null, default(CancellationToken));
		Handle = val.Obj.Handle;
	}

	public PinnedReference(T reference, uint handle)
	{
		Reference = reference;
		Handle = handle;
	}

	protected virtual void Dispose(bool disposing)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		if (!IsDisposed)
		{
			if (Reference != null && Reference is IDisposable disposable)
			{
				ErrorHandler.Assert(disposing, $"Object {typeof(T)} is being finalized without calling Dispose!");
				disposable.Dispose();
			}
			if (Handle.HasValue)
			{
				Reference.Source.ParentContext.InjectionClient.Il2Cpp.FreeObject(new FreeObjectRequest
				{
					Handle = Handle.Value
				}, (Metadata)null, (DateTime?)null, default(CancellationToken));
			}
			Reference = null;
			Handle = null;
			IsDisposed = true;
		}
	}

	~PinnedReference()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
