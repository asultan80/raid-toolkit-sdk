using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

[TypeMapping(typeof(Stack<>))]
public class Native__Stack<T> : RuntimeObject, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, INullConstructable
{
	private readonly Stack<T> m_stack = new Stack<T>();

	private bool m_isLoaded;

	public Stack<T> UnderlyingStack
	{
		get
		{
			Load();
			return m_stack;
		}
	}

	public int Count => UnderlyingStack.Count;

	public Native__Stack(IMemorySource source, ulong address)
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
		uint num = base.Source.ReadValue<uint>(base.Address + 24, 1);
		foreach (T item in new Native__Array<T>(base.Source, base.Source.ReadPointer(base.Address + 16), num))
		{
			m_stack.Push(item);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return UnderlyingStack.GetEnumerator();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return UnderlyingStack.GetEnumerator();
	}
}
