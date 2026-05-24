namespace Il2CppToolkit.Model;

public class Il2CppGenericMethodIndices
{
	public int methodIndex;

	public int invokerIndex;

	[Version(Min = 24.5, Max = 24.5)]
	[Version(Min = 27.1)]
	public int adjustorThunk;
}
