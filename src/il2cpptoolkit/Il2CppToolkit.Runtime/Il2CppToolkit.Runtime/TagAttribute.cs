using System;

namespace Il2CppToolkit.Runtime;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class TagAttribute : Attribute
{
	public string Tag { get; }

	public TagAttribute(string token)
	{
		Tag = token;
	}
}
