namespace Il2CppToolkit.Model;

public class Il2CppAssemblyDefinition
{
	public int imageIndex;

	[Version(Min = 24.1)]
	public uint token;

	[Version(Max = 24.0)]
	public int customAttributeIndex;

	[Version(Min = 20.0)]
	public int referencedAssemblyStart;

	[Version(Min = 20.0)]
	public int referencedAssemblyCount;

	public Il2CppAssemblyNameDefinition aname;
}
