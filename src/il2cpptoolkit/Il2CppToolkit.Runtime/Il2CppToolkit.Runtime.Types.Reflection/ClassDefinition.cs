using System.Diagnostics;
using Il2CppToolkit.Runtime.Types.corelib;

namespace Il2CppToolkit.Runtime.Types.Reflection;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ClassDefinition : RuntimeObject
{
	private string m_name;

	private string m_namespace;

	private ClassDefinition m_parent;

	private ClassDefinition m_base;

	private string DebuggerDisplay
	{
		get
		{
			if (string.IsNullOrEmpty(Namespace) && string.IsNullOrEmpty(Name))
			{
				return "(none)";
			}
			return FullName;
		}
	}

	public string FullName
	{
		get
		{
			if (Parent == null)
			{
				return GetLocalTypeName(Namespace, Name);
			}
			return Parent.FullName + "+" + Name;
		}
	}

	public string Name
	{
		get
		{
			if (m_name == null)
			{
				m_name = base.Source.ReadValue<Native__LPSTR>(base.Address + 16, 1).Value;
			}
			return m_name;
		}
	}

	public string Namespace
	{
		get
		{
			if (m_namespace == null)
			{
				m_namespace = base.Source.ReadValue<Native__LPSTR>(base.Address + 24, 1).Value;
			}
			return m_namespace;
		}
	}

	public ClassDefinition Parent
	{
		get
		{
			if (m_parent == null)
			{
				m_parent = base.Source.ReadValue<ClassDefinition>(base.Address + 80, 1);
			}
			return m_parent;
		}
	}

	public ClassDefinition Base
	{
		get
		{
			if (m_base == null)
			{
				m_base = base.Source.ReadValue<ClassDefinition>(base.Address + 88, 1);
			}
			return m_base;
		}
	}

	public ClassDefinition(IMemorySource source, ulong address)
		: base(source, address)
	{
	}

	private static string GetLocalTypeName(string ns, string name)
	{
		if (string.IsNullOrEmpty(ns))
		{
			return name;
		}
		return ns + "." + name;
	}
}
