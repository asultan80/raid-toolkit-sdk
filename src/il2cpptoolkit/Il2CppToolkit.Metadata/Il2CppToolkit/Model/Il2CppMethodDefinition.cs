namespace Il2CppToolkit.Model;

public class Il2CppMethodDefinition
{
	public uint nameIndex;

	public int declaringType;

	public int returnType;

	public int parameterStart;

	[Version(Max = 24.0)]
	public int customAttributeIndex;

	public int genericContainerIndex;

	[Version(Max = 24.1)]
	public int methodIndex;

	[Version(Max = 24.1)]
	public int invokerIndex;

	[Version(Max = 24.1)]
	public int delegateWrapperIndex;

	[Version(Max = 24.1)]
	public int rgctxStartIndex;

	[Version(Max = 24.1)]
	public int rgctxCount;

	[Version(Min = 31)]
	public uint returnParameterToken;

	public uint token;

	public ushort flags;

	public ushort iflags;

	public ushort slot;

	public ushort parameterCount;
}
