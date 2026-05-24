using System.Collections.Generic;
using System.Linq;

namespace Il2CppToolkit.Model;

internal static class IEnumerableExtensions
{
	public static IEnumerable<T> Range<T>(this IEnumerable<T> target, int start, int count)
	{
		return target.Skip(start).Take(count);
	}

	public static IEnumerable<(int index, T value)> RangeWithIndexes<T>(this IEnumerable<T> target, int start, int count)
	{
		return target.Range(start, count).Select((T value, int idx) => (start + idx, value: value));
	}

	public static IEnumerable<(int index, T value)> WithIndexes<T>(this IEnumerable<T> target)
	{
		int index = 0;
		foreach (T item in target)
		{
			yield return (index: index++, value: item);
		}
	}
}
