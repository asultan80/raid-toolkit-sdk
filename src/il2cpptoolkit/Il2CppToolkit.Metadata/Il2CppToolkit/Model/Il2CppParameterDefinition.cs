namespace Il2CppToolkit.Model;

public class Il2CppParameterDefinition
{
	public uint nameIndex;

	public uint token;

	[Version(Max = 24.0)]
	public int customAttributeIndex;

	public int typeIndex;
}
