namespace Il2CppToolkit.Model;

public class Il2CppGenericClass
{
	[Version(Max = 24.5)]
	public long typeDefinitionIndex;

	[Version(Min = 27.0)]
	public ulong type;

	public Il2CppGenericContext context;

	public ulong cached_class;
}
