using System.Linq;

namespace Il2CppToolkit.Model;

public class GenericTypeReference : ITypeReference
{
	public string Name { get; }

	public ITypeReference GenericType { get; }

	public ITypeReference[] TypeArguments { get; }

	public GenericTypeReference(ITypeReference genericType, params ITypeReference[] typeArguments)
	{
		Name = genericType.Name + "[" + string.Join(", ", typeArguments.Select((ITypeReference arg) => arg.Name)) + "]";
		GenericType = genericType;
		TypeArguments = typeArguments;
	}
}
