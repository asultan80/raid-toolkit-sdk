using System;

namespace Il2CppToolkit.Model;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
internal class VersionAttribute : Attribute
{
	public double Min { get; set; }

	public double Max { get; set; } = 99.0;

}
