using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

public class Native__RawArray<T> : RuntimeObject, IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, INullConstructable
{
	private readonly ulong m_length;

	private readonly ulong m_elementSize;

	private bool m_isLoaded;

	private CachedMemoryBlock m_cache;

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

	public Native__RawArray(IMemorySource source, ulong address, ulong length)
		: base(source, address)
	{
		m_length = length;
		m_elementSize = Il2CsRuntimeContext.GetTypeSize(typeof(T));
	}

	protected internal void Load()
	{
		if (base.Address != 0L && !m_isLoaded)
		{
			m_isLoaded = true;
			m_cache = base.Source.ParentContext.CacheMemory(base.Address, m_elementSize * m_length);
			for (ulong num = 0uL; num < m_length; num++)
			{
				T item = m_cache.ReadValue<T>(base.Address + num * m_elementSize, 1);
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
