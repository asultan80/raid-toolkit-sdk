using System;
using System.Collections.Generic;

namespace Il2CppToolkit.Common;

public class UniqueName
{
	private readonly HashSet<string> uniqueNamesHash = new HashSet<string>(StringComparer.Ordinal);

	public string Get(string name)
	{
		string text = name;
		int num = 1;
		while (!uniqueNamesHash.Add(text))
		{
			text = $"{name}_{num++}";
		}
		return text;
	}
}
