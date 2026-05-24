namespace Il2CppToolkit.Model;

public class Il2CppType
{
	public class Union
	{
		public ulong dummy;

		public long klassIndex => (long)dummy;

		public ulong typeHandle => dummy;

		public ulong type => dummy;

		public ulong array => dummy;

		public long genericParameterIndex => (long)dummy;

		public ulong genericParameterHandle => dummy;

		public ulong generic_class => dummy;
	}

	public ulong datapoint;

	public uint bits;

	public Union data { get; set; }

	public uint attrs { get; set; }

	public Il2CppTypeEnum type { get; set; }

	public uint num_mods { get; set; }

	public uint byref { get; set; }

	public uint pinned { get; set; }

	public uint valuetype { get; set; }

	public void Init(double version)
	{
		attrs = bits & 0xFFFFu;
		type = (Il2CppTypeEnum)((int)(bits >> 16) & 0xFF);
		if (version >= 27.2)
		{
			num_mods = (bits >> 24) & 0x1Fu;
			byref = (bits >> 29) & 1u;
			pinned = (bits >> 30) & 1u;
			valuetype = bits >> 31;
		}
		else
		{
			num_mods = (bits >> 24) & 0x3Fu;
			byref = (bits >> 30) & 1u;
			pinned = bits >> 31;
		}
		data = new Union
		{
			dummy = datapoint
		};
	}
}
