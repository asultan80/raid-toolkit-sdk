using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

[TypeMapping(typeof(List<>))]
public class Native__List<T> : RuntimeObject, IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, INullConstructable
{
	private IReadOnlyList<T> m_list;

	private bool m_isLoaded;

	public int Count
	{
		get
		{
			Load();
			return m_list.Count;
		}
	}

	public T this[int index]
	{
		get
		{
			Load();
			return m_list[index];
		}
	}

	public Native__List(IMemorySource source, ulong address)
		: base(source, address)
	{
	}

	private void Load()
	{
		if (base.Address == 0L)
		{
			m_list = new List<T>();
		}
		else if (!m_isLoaded)
		{
			m_isLoaded = true;
			uint num = (uint)base.Source.ReadValue<int>(base.Address + 24, 1);
			Native__Array<T> list = new Native__Array<T>(base.Source, base.Source.ReadPointer(base.Address + 16), num);
			m_list = list;
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		Load();
		return m_list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		Load();
		return ((IEnumerable)m_list).GetEnumerator();
	}
}
