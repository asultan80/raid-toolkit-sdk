using System.Collections.Generic;

namespace Il2CppToolkit.Model;

public interface ITypeModel
{
	Il2Cpp Il2Cpp { get; }

	Metadata Metadata { get; }

	string ModuleName { get; }

	IReadOnlyList<TypeDescriptor> TypeDescriptors { get; }
}
