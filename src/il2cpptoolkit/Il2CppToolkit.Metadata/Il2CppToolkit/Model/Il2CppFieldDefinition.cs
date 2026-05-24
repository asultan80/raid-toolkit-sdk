namespace Il2CppToolkit.Model;

public class Il2CppFieldDefinition
{
	public uint nameIndex;

	public int typeIndex;

	[Version(Max = 24.0)]
	public int customAttributeIndex;

	[Version(Min = 19.0)]
	public uint token;
}
