namespace Il2CppToolkit.Model;

public class Il2CppEventDefinition
{
	public uint nameIndex;

	public int typeIndex;

	public int add;

	public int remove;

	public int raise;

	[Version(Max = 24.0)]
	public int customAttributeIndex;

	[Version(Min = 19.0)]
	public uint token;
}
