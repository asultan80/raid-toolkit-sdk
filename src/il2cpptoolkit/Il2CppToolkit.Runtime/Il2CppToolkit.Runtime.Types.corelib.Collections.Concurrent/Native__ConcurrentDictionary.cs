using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Concurrent;

[TypeMapping(typeof(ConcurrentDictionary<, >))]
public class Native__ConcurrentDictionary<TKey, TValue> : RuntimeObject, IReadOnlyDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, INullConstructable
{
	public class Node : RuntimeObject
	{
		public TKey Key => base.Source.ReadValue<TKey>(base.Address + 16, 1);

		public TValue Value => base.Source.ReadValue<TValue>(base.Address + 24, 1);

		public Node Next => base.Source.ReadValue<Node>(base.Address + 32, 1);

		public Node(IMemorySource source, ulong address)
			: base(source, address)
		{
		}
	}

	public class Table : RuntimeObject
	{
		public Native__Array<Node> Buckets => base.Source.ReadValue<Native__Array<Node>>(base.Address + 16, 1);

		public Table(IMemorySource source, ulong address)
			: base(source, address)
		{
		}
	}

	[Ignore]
	private readonly Dictionary<TKey, TValue> m_dict = new Dictionary<TKey, TValue>();

	private bool m_isLoaded;

	private Table m_table => base.Source.ReadValue<Table>(base.Address + 16, 1);

	private Dictionary<TKey, TValue> Dict
	{
		get
		{
			Load();
			return m_dict;
		}
	}

	public int Count => Dict.Count;

	public TValue this[TKey key] => Dict[key];

	public IEnumerable<TKey> Keys => Dict.Keys;

	public IEnumerable<TValue> Values => Dict.Values;

	public Native__ConcurrentDictionary(IMemorySource source, ulong address)
		: base(source, address)
	{
	}

	private void Load()
	{
		if (base.Address == 0L || m_isLoaded)
		{
			return;
		}
		m_isLoaded = true;
		foreach (Node bucket in m_table.Buckets)
		{
			for (Node node = bucket; node != null; node = node.Next)
			{
				m_dict.Add(node.Key, node.Value);
			}
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return Dict.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Dict.GetEnumerator();
	}

	public bool ContainsKey(TKey key)
	{
		return Dict.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		return Dict.TryGetValue(key, out value);
	}
}
