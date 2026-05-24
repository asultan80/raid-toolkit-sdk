using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Il2CppToolkit.ReverseCompiler;

public class ArtifactContainer
{
	private class ArtifactState
	{
		internal static object EmptyValue = new object();

		public TaskCompletionSource<object> CompletionSource = new TaskCompletionSource<object>();

		public object Value = EmptyValue;

		public ArtifactState()
		{
		}

		public ArtifactState(object value)
		{
			Value = value;
			CompletionSource.TrySetResult(value);
		}
	}

	private readonly Dictionary<IStateSpecification, ArtifactState> m_artifacts = new Dictionary<IStateSpecification, ArtifactState>();

	public void Set<T>(ITypedSpecification<T> spec, T value)
	{
		if (m_artifacts.TryGetValue(spec, out var value2))
		{
			if (value2.Value != ArtifactState.EmptyValue)
			{
				throw new InvalidOperationException("Value already set");
			}
			value2.Value = value;
			value2.CompletionSource.TrySetResult(value);
		}
		else
		{
			m_artifacts.Add(spec, new ArtifactState(value));
		}
	}

	public void Set(IStateSpecification spec, object value)
	{
		if (m_artifacts.TryGetValue(spec, out var value2))
		{
			if (value2.Value != ArtifactState.EmptyValue)
			{
				throw new InvalidOperationException("Value already set");
			}
			value2.Value = value;
			value2.CompletionSource.TrySetResult(value);
		}
		else
		{
			m_artifacts.Add(spec, new ArtifactState(value));
		}
	}

	public async Task<T> GetAsync<T>(ITypedSpecification<T> spec)
	{
		if (!m_artifacts.TryGetValue(spec, out var value))
		{
			value = new ArtifactState();
			m_artifacts.Add(spec, value);
		}
		return (T)(await value.CompletionSource.Task);
	}

	public T Get<T>(ITypedSynchronousState<T> spec)
	{
		if (!m_artifacts.TryGetValue(spec, out var value))
		{
			value = new ArtifactState();
			m_artifacts.Add(spec, value);
		}
		if (value.Value != ArtifactState.EmptyValue)
		{
			return (T)value.Value;
		}
		return (T)spec.DefaultValue;
	}

	public bool Has(IStateSpecification spec)
	{
		if (m_artifacts.TryGetValue(spec, out var value))
		{
			return value.Value != ArtifactState.EmptyValue;
		}
		return false;
	}
}
