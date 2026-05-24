using System;
using System.Text;

namespace Il2CppToolkit.Runtime.Types.corelib;

public class Native__LPSTR : RuntimeObject
{
	private string m_value;

	public string Value
	{
		get
		{
			if (m_value == null)
			{
				base.Source.ReadPointer(base.Address);
				ReadOnlyMemory<byte> readOnlyMemory = base.Source.ReadMemory(base.Address, 512uL);
				m_value = Encoding.UTF8.GetString(readOnlyMemory.Span).Split(new char[1], 2)[0];
			}
			return m_value;
		}
	}

	public Native__LPSTR()
	{
	}

	public Native__LPSTR(IMemorySource source, ulong address)
		: base(source, address)
	{
	}
}
