using System;
using System.IO;

namespace Il2CppToolkit.Model;

public sealed class WebAssembly : BinaryStream
{
	private readonly DataSection[] dataSections;

	public WebAssembly(Stream stream)
		: base(stream)
	{
		Is32Bit = true;
		ReadUInt32();
		ReadInt32();
		while (base.Position < base.Length)
		{
			uint num = ReadULeb128();
			uint num2 = ReadULeb128();
			if (num == 11)
			{
				uint num3 = ReadULeb128();
				dataSections = new DataSection[num3];
				for (int i = 0; i < num3; i++)
				{
					DataSection dataSection = new DataSection();
					dataSections[i] = dataSection;
					dataSection.Index = ReadULeb128();
					if (ReadByte() != 65)
					{
						throw new InvalidOperationException();
					}
					dataSection.Offset = ReadULeb128();
					if (ReadByte() != 11)
					{
						throw new InvalidOperationException();
					}
					dataSection.Data = ReadBytes((int)ReadULeb128());
				}
				break;
			}
			base.Position += num2;
		}
	}

	public WebAssemblyMemory CreateMemory()
	{
		DataSection dataSection = dataSections[^1];
		uint bssStart = dataSection.Offset + (uint)dataSection.Data.Length;
		MemoryStream memoryStream = new MemoryStream(new byte[base.Length]);
		DataSection[] array = dataSections;
		foreach (DataSection dataSection2 in array)
		{
			memoryStream.Position = dataSection2.Offset;
			memoryStream.Write(dataSection2.Data, 0, dataSection2.Data.Length);
		}
		return new WebAssemblyMemory(memoryStream, bssStart);
	}
}
