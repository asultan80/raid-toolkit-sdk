using System;

namespace Il2CppToolkit.Model;

[AttributeUsage(AttributeTargets.Field)]
internal class ArrayLengthAttribute : Attribute
{
	public int Length { get; set; }
}
