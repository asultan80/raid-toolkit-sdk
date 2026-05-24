using System;
using System.Collections.Generic;

namespace Il2CppToolkit.Model;

internal static class BoyerMooreHorspool
{
	public static IEnumerable<int> Search(this byte[] source, byte[] pattern)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (pattern == null)
		{
			throw new ArgumentNullException("pattern");
		}
		int valueLength = source.Length;
		int patternLength = pattern.Length;
		if (valueLength == 0 || patternLength == 0 || patternLength > valueLength)
		{
			yield break;
		}
		int[] badCharacters = new int[256];
		for (int i = 0; i < 256; i++)
		{
			badCharacters[i] = patternLength;
		}
		int lastPatternByte = patternLength - 1;
		for (int j = 0; j < lastPatternByte; j++)
		{
			badCharacters[pattern[j]] = lastPatternByte - j;
		}
		for (int index = 0; index <= valueLength - patternLength; index += badCharacters[source[index + lastPatternByte]])
		{
			int num = lastPatternByte;
			while (source[index + num] == pattern[num])
			{
				if (num == 0)
				{
					yield return index;
					break;
				}
				num--;
			}
		}
	}

	public static IEnumerable<int> Search(this byte[] source, string stringPattern)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (stringPattern == null)
		{
			throw new ArgumentNullException("stringPattern");
		}
		string[] pattern = stringPattern.Split(' ');
		int valueLength = source.Length;
		int patternLength = pattern.Length;
		if (valueLength == 0 || patternLength == 0 || patternLength > valueLength)
		{
			yield break;
		}
		int[] badCharacters = new int[256];
		for (int i = 0; i < 256; i++)
		{
			badCharacters[i] = patternLength;
		}
		int lastPatternByte = patternLength - 1;
		for (int j = 0; j < lastPatternByte; j++)
		{
			if (pattern[j] != "?")
			{
				int num = Convert.ToInt32(pattern[j], 16);
				badCharacters[num] = lastPatternByte - j;
			}
		}
		for (int index = 0; index <= valueLength - patternLength; index += badCharacters[source[index + lastPatternByte]])
		{
			int num2 = lastPatternByte;
			while (CheckEqual(source, pattern, index, num2))
			{
				if (num2 == 0)
				{
					yield return index;
					break;
				}
				num2--;
			}
		}
	}

	private static bool CheckEqual(byte[] source, string[] pattern, int index, int i)
	{
		if (pattern[i] != "?")
		{
			int num = Convert.ToInt32(pattern[i], 16);
			return source[index + i] == num;
		}
		return true;
	}
}
