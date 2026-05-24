namespace Il2CppToolkit.Model;

public class Il2CppImageDefinition
{
	public uint nameIndex;

	public int assemblyIndex;

	public int typeStart;

	public uint typeCount;

	[Version(Min = 24.0)]
	public int exportedTypeStart;

	[Version(Min = 24.0)]
	public uint exportedTypeCount;

	public int entryPointIndex;

	[Version(Min = 19.0)]
	public uint token;

	[Version(Min = 24.1)]
	public int customAttributeStart;

	[Version(Min = 24.1)]
	public uint customAttributeCount;
}
