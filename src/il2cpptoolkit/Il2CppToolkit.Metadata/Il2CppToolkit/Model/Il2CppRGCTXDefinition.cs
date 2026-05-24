namespace Il2CppToolkit.Model;

public class Il2CppRGCTXDefinition
{
	[Version(Max = 27.1)]
	public int type_pre29;

	[Version(Min = 29.0)]
	public ulong type_post29;

	[Version(Max = 27.1)]
	public Il2CppRGCTXDefinitionData data;

	[Version(Min = 27.2)]
	public ulong _data;

	public Il2CppRGCTXDataType type
	{
		get
		{
			if (type_post29 != 0L)
			{
				return (Il2CppRGCTXDataType)type_post29;
			}
			return (Il2CppRGCTXDataType)type_pre29;
		}
	}
}
