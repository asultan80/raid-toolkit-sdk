namespace Il2CppToolkit.Runtime;

public class UnknownObject : RuntimeObject
{
	public UnknownObject()
	{
	}

	public UnknownObject(IMemorySource source, ulong address)
		: base(source, address)
	{
	}
}
