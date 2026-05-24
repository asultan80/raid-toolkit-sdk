using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppToolkit.Common;

public class MutableTypeListIterator<T> : IEnumerable<T>, IEnumerable
{
	private IList<T> Owner;

	private Queue<T> OpenList;

	public MutableTypeListIterator(IList<T> owner)
	{
		Owner = owner;
		OpenList = new Queue<T>(owner);
	}

	private IEnumerable<T> Iterate()
	{
		T result;
		while (OpenList.TryDequeue(out result))
		{
			yield return result;
		}
	}

	public void Add(T item)
	{
		Owner.Add(item);
		OpenList.Append(item);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return Iterate().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Iterate().GetEnumerator();
	}
}
