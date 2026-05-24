namespace Il2CppToolkit.Model;

public class Il2CppAssemblyNameDefinition
{
	public uint nameIndex;

	public uint cultureIndex;

	[Version(Max = 24.3)]
	public int hashValueIndex;

	public uint publicKeyIndex;

	public uint hash_alg;

	public int hash_len;

	public uint flags;

	public int major;

	public int minor;

	public int build;

	public int revision;

	[ArrayLength(Length = 8)]
	public byte[] public_key_token;
}
