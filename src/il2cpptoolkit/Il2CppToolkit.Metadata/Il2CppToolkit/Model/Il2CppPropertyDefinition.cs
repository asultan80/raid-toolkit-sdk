namespace Il2CppToolkit.Model;

public class Il2CppPropertyDefinition
{
	public uint nameIndex;

	public int get;

	public int set;

	public uint attrs;

	[Version(Max = 24.0)]
	public int customAttributeIndex;

	[Version(Min = 19.0)]
	public uint token;
}
