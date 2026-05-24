using System;

namespace Il2CppToolkit.Runtime;

[AttributeUsage(AttributeTargets.Field)]
public class DynamicOffsetAttribute : Attribute
{
	public string FieldName { get; }

	public DynamicOffsetAttribute(string fieldName)
	{
		FieldName = fieldName;
	}
}
