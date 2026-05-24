namespace Il2CppToolkit.Runtime;

public class ObjectPointer : RuntimeObject
{
	public ObjectPointer()
	{
	}

	public ObjectPointer(IMemorySource source, ulong address)
		: base(source, address)
	{
	}
}
