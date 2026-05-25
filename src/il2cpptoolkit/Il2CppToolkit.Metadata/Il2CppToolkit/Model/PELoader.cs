using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Il2CppToolkit.Model;

public class PELoader
{
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr LoadLibraryEx(string path, IntPtr hFile, uint dwFlags);

	private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

	public static PE Load(string fileName)
	{
		using BinaryStream binaryStream = new BinaryStream(new MemoryStream(File.ReadAllBytes(fileName)));
		DosHeader dosHeader = binaryStream.ReadClass<DosHeader>();
		if (dosHeader.Magic != 23117)
		{
			throw new InvalidDataException("ERROR: Invalid PE file");
		}
		binaryStream.Position = dosHeader.Lfanew;
		if (binaryStream.ReadUInt32() != 17744)
		{
			throw new InvalidDataException("ERROR: Invalid PE file");
		}
		FileHeader fileHeader = binaryStream.ReadClass<FileHeader>();
		if (fileHeader.Machine == 332 && Environment.Is64BitProcess)
		{
			throw new InvalidOperationException("The file is a 32-bit file, please try to load it with Il2CppDumper-x86.exe");
		}
		if (fileHeader.Machine == 34404 && !Environment.Is64BitProcess)
		{
			throw new InvalidOperationException("The file is a 64-bit file, please try to load it with Il2CppDumper.exe");
		}
		ulong position = binaryStream.Position;
		binaryStream.Position = position + fileHeader.SizeOfOptionalHeader;
		SectionHeader[] array = binaryStream.ReadClassArray<SectionHeader>(fileHeader.NumberOfSections);
		SectionHeader sectionHeader = array[^1];
		byte[] array2 = new byte[sectionHeader.VirtualAddress + sectionHeader.VirtualSize];
		IntPtr intPtr = LoadLibraryEx(fileName, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
		if (intPtr == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		SectionHeader[] array3 = array;
		foreach (SectionHeader sectionHeader2 in array3)
		{
			uint characteristics = sectionHeader2.Characteristics;
			if (characteristics == 1073741888 || characteristics == 1610612768 || characteristics == 3221225536u)
			{
				Marshal.Copy(new IntPtr(intPtr.ToInt64() + sectionHeader2.VirtualAddress), array2, (int)sectionHeader2.VirtualAddress, (int)sectionHeader2.VirtualSize);
			}
		}
		MemoryStream memoryStream = new MemoryStream(array2);
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
		ulong position2 = binaryStream.Position;
		binaryStream.Position = 0uL;
		byte[] buffer = binaryStream.ReadBytes((int)position2);
		binaryWriter.Write(buffer);
		binaryWriter.Flush();
		binaryWriter.Close();
		memoryStream.Position = 0L;
		PE pE = new PE(memoryStream);
		pE.LoadFromMemory((ulong)intPtr.ToInt64());
		return pE;
	}
}
