using System.Buffers.Binary;
using System.IO;

namespace Il2CppToolkit.Model;

public sealed class MachoFat : BinaryStream
{
	public Fat[] fats;

	public MachoFat(Stream stream)
		: base(stream)
	{
		base.Position += 4uL;
		int num = BinaryPrimitives.ReadInt32BigEndian(ReadBytes(4));
		fats = new Fat[num];
		for (int i = 0; i < num; i++)
		{
			base.Position += 8uL;
			fats[i] = new Fat
			{
				offset = BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4)),
				size = BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4))
			};
			base.Position += 4uL;
		}
		for (int j = 0; j < num; j++)
		{
			base.Position = fats[j].offset;
			fats[j].magic = ReadUInt32();
		}
	}

	public byte[] GetMacho(int index)
	{
		base.Position = fats[index].offset;
		return ReadBytes((int)fats[index].size);
	}
}
