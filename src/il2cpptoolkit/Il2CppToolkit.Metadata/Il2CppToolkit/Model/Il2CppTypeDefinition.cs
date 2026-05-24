namespace Il2CppToolkit.Model;

public class Il2CppTypeDefinition
{
	public uint nameIndex;

	public uint namespaceIndex;

	[Version(Max = 24.0)]
	public int customAttributeIndex;

	public int byvalTypeIndex;

	[Version(Max = 24.5)]
	public int byrefTypeIndex;

	public int declaringTypeIndex;

	public int parentIndex;

	public int elementTypeIndex;

	[Version(Max = 24.1)]
	public int rgctxStartIndex;

	[Version(Max = 24.1)]
	public int rgctxCount;

	public int genericContainerIndex;

	[Version(Max = 22.0)]
	public int delegateWrapperFromManagedToNativeIndex;

	[Version(Max = 22.0)]
	public int marshalingFunctionsIndex;

	[Version(Min = 21.0, Max = 22.0)]
	public int ccwFunctionIndex;

	[Version(Min = 21.0, Max = 22.0)]
	public int guidIndex;

	public uint flags;

	public int fieldStart;

	public int methodStart;

	public int eventStart;

	public int propertyStart;

	public int nestedTypesStart;

	public int interfacesStart;

	public int vtableStart;

	public int interfaceOffsetsStart;

	public ushort method_count;

	public ushort property_count;

	public ushort field_count;

	public ushort event_count;

	public ushort nested_type_count;

	public ushort vtable_count;

	public ushort interfaces_count;

	public ushort interface_offsets_count;

	public uint bitfield;

	[Version(Min = 19.0)]
	public uint token;

	public bool IsValueType => (bitfield & 1) == 1;

	public bool IsEnum => ((bitfield >> 1) & 1) == 1;
}
