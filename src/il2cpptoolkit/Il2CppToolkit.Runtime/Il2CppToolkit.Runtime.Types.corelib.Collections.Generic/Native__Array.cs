using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

public class Native__Array<T> : RuntimeObject, IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, INullConstructable
{
	private bool m_isLoaded;

	private CachedMemoryBlock m_cache;

	private readonly ulong? m_specifiedLength;

	private readonly ulong? m_specifiedElementSize;

	private readonly List<T> m_items = new List<T>();

	private List<T> Items
	{
		get
		{
			Load();
			return m_items;
		}
	}

	public T[] Array
	{
		get
		{
			Load();
			return m_items.ToArray();
		}
	}

	public int Count => Items.Count;

	public T this[int index] => Items[index];

	public Native__Array(IMemorySource source, ulong address)
		: base(source, address)
	{
		m_specifiedLength = null;
	}

	public Native__Array(IMemorySource source, ulong address, ulong length)
		: base(source, address)
	{
		m_specifiedLength = length;
	}

	public Native__Array(IMemorySource source, ulong address, ulong length, ulong elementSize)
		: base(source, address)
	{
		m_specifiedLength = length;
		m_specifiedElementSize = elementSize;
	}

	private void Load()
	{
		if (base.Address == 0L || m_isLoaded)
		{
			return;
		}
		m_isLoaded = true;
		ulong num = m_specifiedLength ?? base.Source.ReadValue<ulong>(base.Address + 24, 1);
		if (num != 0L)
		{
			ulong num2 = m_specifiedElementSize ?? Il2CsRuntimeContext.GetTypeSize(typeof(T));
			m_cache = base.Source.ParentContext.CacheMemory(base.Address + 32, num2 * num);
			for (ulong num3 = 0uL; num3 < num; num3++)
			{
				T item = m_cache.ReadValue<T>(base.Address + 32 + num3 * num2, 1);
				m_items.Add(item);
			}
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		return Items.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Items.GetEnumerator();
	}
}
