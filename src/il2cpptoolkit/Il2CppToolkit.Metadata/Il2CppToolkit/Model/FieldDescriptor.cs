using System.Reflection;
using System.Text.RegularExpressions;

namespace Il2CppToolkit.Model;

public class FieldDescriptor
{
	private static readonly Regex BackingFieldRegex = new Regex("<(.+)>k__BackingField", RegexOptions.Compiled);

	public readonly string OriginalName;

	public readonly string StorageName;

	public readonly string Name;

	public readonly ITypeReference Type;

	public FieldAttributes Attributes;

	public readonly ulong Offset;

	public object DefaultValue;

	public FieldDescriptor(string name, ITypeReference typeReference, FieldAttributes attrs, ulong offset)
	{
		OriginalName = name;
		Name = BackingFieldRegex.Replace(name, (Match match) => match.Groups[1].Value);
		StorageName = "<" + Name + ">k__BackingField";
		Type = typeReference;
		Attributes = attrs;
		Offset = offset;
	}
}
