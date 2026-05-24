using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

[TypeMapping(typeof(Dictionary<, >))]
public class Native__Dictionary<TKey, TValue> : RuntimeObject, IReadOnlyDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, INullConstructable
{
	public struct Entry : IRuntimeObject
	{
		private static readonly bool IsNarrow = Il2CsRuntimeContext.GetTypeSize(typeof(TValue)) <= 4 && Il2CsRuntimeContext.GetTypeSize(typeof(TKey)) <= 4;

		public static ulong ElementSize = (ulong)(IsNarrow ? 16 : 24);

		public IMemorySource Source { get; }

		public ulong Address { get; }

		public int HashCode => Source.ReadValue<int>(Address, 1);

		public int Next => Source.ReadValue<int>(Address + 4, 1);

		public TKey Key => Source.ReadValue<TKey>(Address + 8, 1);

		public TValue Value => Source.ReadValue<TValue>(Address + (ulong)(IsNarrow ? 12 : 16), 1);

		public Entry(IMemorySource source, ulong address)
		{
			Source = source;
			Address = address;
		}
	}

	private readonly Dictionary<TKey, TValue> m_dict = new Dictionary<TKey, TValue>();

	private bool m_isLoaded;

	public Dictionary<TKey, TValue> UnderlyingDictionary
	{
		get
		{
			Load();
			return m_dict;
		}
	}

	public int Count => UnderlyingDictionary.Count;

	public TValue this[TKey key] => UnderlyingDictionary[key];

	public IEnumerable<TKey> Keys => UnderlyingDictionary.Keys;

	public IEnumerable<TValue> Values => UnderlyingDictionary.Values;

	public Native__Dictionary(IMemorySource source, ulong address)
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
		uint num = base.Source.ReadValue<uint>(base.Address + 32, 1);
		foreach (Entry item in new Native__Array<Entry>(base.Source, base.Source.ReadPointer(base.Address + 24), num, Entry.ElementSize))
		{
			if (item.HashCode != -1)
			{
				m_dict.Add(item.Key, item.Value);
			}
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return UnderlyingDictionary.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return UnderlyingDictionary.GetEnumerator();
	}

	public bool ContainsKey(TKey key)
	{
		return UnderlyingDictionary.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		return UnderlyingDictionary.TryGetValue(key, out value);
	}
}
