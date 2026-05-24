using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime;

public class UnknownClass : RuntimeObject
{
	private ClassDefinition m_classDef;

	public virtual ClassDefinition ClassDefinition
	{
		get
		{
			if (m_classDef == null)
			{
				m_classDef = base.Source.ReadValue<ClassDefinition>(base.Address, 1);
			}
			return m_classDef;
		}
	}

	public UnknownClass(IMemorySource source, ulong address)
		: base(source, address)
	{
	}
}
