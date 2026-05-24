namespace Il2CppToolkit.Model;

public class Il2CppCodeGenModule
{
	public ulong moduleName;

	public long methodPointerCount;

	public ulong methodPointers;

	[Version(Min = 24.5, Max = 24.5)]
	[Version(Min = 27.1)]
	public long adjustorThunkCount;

	[Version(Min = 24.5, Max = 24.5)]
	[Version(Min = 27.1)]
	public ulong adjustorThunks;

	public ulong invokerIndices;

	public ulong reversePInvokeWrapperCount;

	public ulong reversePInvokeWrapperIndices;

	public long rgctxRangesCount;

	public ulong rgctxRanges;

	public long rgctxsCount;

	public ulong rgctxs;

	public ulong debuggerMetadata;

	[Version(Min = 27.0, Max = 27.2)]
	public ulong customAttributeCacheGenerator;

	[Version(Min = 27.0)]
	public ulong moduleInitializer;

	[Version(Min = 27.0)]
	public ulong staticConstructorTypeIndices;

	[Version(Min = 27.0)]
	public ulong metadataRegistration;

	[Version(Min = 27.0)]
	public ulong codeRegistaration;
}
