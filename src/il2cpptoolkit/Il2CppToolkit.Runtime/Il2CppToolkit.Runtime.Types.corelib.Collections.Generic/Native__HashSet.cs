using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

[TypeMapping(typeof(HashSet<>))]
public class Native__HashSet<T> : RuntimeObject, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, INullConstructable
{
	public struct Entry : IRuntimeObject
	{
		public IMemorySource Source { get; }

		public ulong Address { get; }

		public uint HashCode => Source.ReadValue<uint>(Address, 1);

		public uint Next => Source.ReadValue<uint>(Address + 4, 1);

		public T Value => Source.ReadValue<T>(Address + 8, 1);

		public Entry(IMemorySource source, ulong address)
		{
			Source = source;
			Address = address;
		}
	}

	private readonly HashSet<T> m_set = new HashSet<T>();

	private bool m_isLoaded;

	public HashSet<T> UnderlyingHashSet
	{
		get
		{
			Load();
			return m_set;
		}
	}

	public int Count => UnderlyingHashSet.Count;

	public Native__HashSet(IMemorySource source, ulong address)
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
		foreach (Entry item in new Native__Array<Entry>(base.Source, base.Source.ReadPointer(base.Address + 24), num))
		{
			m_set.Add(item.Value);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return UnderlyingHashSet.GetEnumerator();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return UnderlyingHashSet.GetEnumerator();
	}
}
