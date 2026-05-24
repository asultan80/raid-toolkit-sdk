namespace Il2CppToolkit.Model;

public class Il2CppCodeRegistration
{
	[Version(Max = 24.1)]
	public ulong methodPointersCount;

	[Version(Max = 24.1)]
	public ulong methodPointers;

	[Version(Max = 21.0)]
	public ulong delegateWrappersFromNativeToManagedCount;

	[Version(Max = 21.0)]
	public ulong delegateWrappersFromNativeToManaged;

	[Version(Min = 22.0)]
	public ulong reversePInvokeWrapperCount;

	[Version(Min = 22.0)]
	public ulong reversePInvokeWrappers;

	[Version(Max = 22.0)]
	public ulong delegateWrappersFromManagedToNativeCount;

	[Version(Max = 22.0)]
	public ulong delegateWrappersFromManagedToNative;

	[Version(Max = 22.0)]
	public ulong marshalingFunctionsCount;

	[Version(Max = 22.0)]
	public ulong marshalingFunctions;

	[Version(Min = 21.0, Max = 22.0)]
	public ulong ccwMarshalingFunctionsCount;

	[Version(Min = 21.0, Max = 22.0)]
	public ulong ccwMarshalingFunctions;

	public ulong genericMethodPointersCount;

	public ulong genericMethodPointers;

	[Version(Min = 24.5, Max = 24.5)]
	[Version(Min = 27.1)]
	public ulong genericAdjustorThunks;

	public ulong invokerPointersCount;

	public ulong invokerPointers;

	[Version(Max = 24.5)]
	public ulong customAttributeCount;

	[Version(Max = 24.5)]
	public ulong customAttributeGenerators;

	[Version(Min = 21.0, Max = 22.0)]
	public ulong guidCount;

	[Version(Min = 21.0, Max = 22.0)]
	public ulong guids;

	[Version(Min = 22.0)]
	public ulong unresolvedVirtualCallCount;

	[Version(Min = 22.0)]
	public ulong unresolvedVirtualCallPointers;

	[Version(Min = 29.1)]
	public ulong unresolvedInstanceCallPointers;

	[Version(Min = 29.1)]
	public ulong unresolvedStaticCallPointers;

	[Version(Min = 23.0)]
	public ulong interopDataCount;

	[Version(Min = 23.0)]
	public ulong interopData;

	[Version(Min = 24.3)]
	public ulong windowsRuntimeFactoryCount;

	[Version(Min = 24.3)]
	public ulong windowsRuntimeFactoryTable;

	[Version(Min = 24.2)]
	public ulong codeGenModulesCount;

	[Version(Min = 24.2)]
	public ulong codeGenModules;
}
