namespace Il2CppToolkit.Runtime.Types;

public interface ITypeFactory
{
	object ReadValue(IMemorySource source, ulong address);

	void WriteValue(IMemorySource source, ulong address, object value);
}
