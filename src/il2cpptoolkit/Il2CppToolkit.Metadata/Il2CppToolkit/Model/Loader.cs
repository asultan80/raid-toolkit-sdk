#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Il2CppToolkit.Model;

public class Loader
{
	public LoaderOptions Options { get; set; } = new LoaderOptions();


	public Metadata Metadata { get; private set; }

	public Il2Cpp Il2Cpp { get; private set; }

	public string ModuleName { get; private set; }

	public Loader()
	{
	}

	public Loader(LoaderOptions options)
	{
		Options = options;
	}

	public void Init(string il2cppPath, string metadataPath)
	{
		ModuleName = Path.GetFileName(il2cppPath);
		Trace.WriteLine("Initializing Metadata...");
		byte[] buffer = File.ReadAllBytes(metadataPath);
		Metadata = new Metadata(new MemoryStream(buffer));
		Trace.WriteLine($"Metadata Version: {Metadata.Version}");
		Trace.WriteLine("Initializing il2cpp file...");
		byte[] array = File.ReadAllBytes(il2cppPath);
		uint num = BitConverter.ToUInt32(array, 0);
		MemoryStream stream = new MemoryStream(array);
		if (num <= 1836278016)
		{
			if (num <= 810505038)
			{
				if (num != 9460301)
				{
					if (num != 810505038)
					{
						goto IL_00f2;
					}
					NSO nSO = new NSO(stream);
					Il2Cpp = nSO.UnCompress();
				}
				else
				{
					Il2Cpp = new PE(stream);
				}
			}
			else if (num != 1179403647)
			{
				if (num != 1836278016)
				{
					goto IL_00f2;
				}
				WebAssembly webAssembly = new WebAssembly(stream);
				Il2Cpp = webAssembly.CreateMemory();
			}
			else if (array[4] == 2)
			{
				Il2Cpp = new Elf64(stream);
			}
			else
			{
				Il2Cpp = new Elf(stream);
			}
		}
		else
		{
			if (num <= 3405691582u)
			{
				if (num != 3199925962u && num != 3405691582u)
				{
					goto IL_00f2;
				}
				MachoFat machoFat = new MachoFat(new MemoryStream(array));
				LoaderOptions.ResolveFatPlatformEventArgs resolveFatPlatformEventArgs = new LoaderOptions.ResolveFatPlatformEventArgs(machoFat.fats);
				Options.FireResolveFatPlatform(this, resolveFatPlatformEventArgs);
				if (resolveFatPlatformEventArgs.ResolveToIndex == -1)
				{
					StructuredErrorExtensions.Raise<MetadataError>(MetadataError.ConfigurationError, "ResolveFatPlatform was unhandled");
				}
				int resolveToIndex = resolveFatPlatformEventArgs.ResolveToIndex;
				uint magic = machoFat.fats[resolveToIndex % 2].magic;
				array = machoFat.GetMacho(resolveToIndex % 2);
				stream = new MemoryStream(array);
				if (magic == 4277009103u)
				{
					goto IL_01da;
				}
			}
			else if (num != 4277009102u)
			{
				if (num != 4277009103u)
				{
					goto IL_00f2;
				}
				goto IL_01da;
			}
			Il2Cpp = new Macho(stream);
		}
		goto IL_01f4;
		IL_01da:
		Il2Cpp = new Macho64(stream);
		goto IL_01f4;
		IL_01f4:
		double version = Options.ForceVersion ?? Metadata.Version;
		Il2Cpp.SetProperties(version, Metadata.metadataUsagesCount);
		Trace.WriteLine($"Il2Cpp Version: {Il2Cpp.Version}");
		if (Il2Cpp.Version >= 27.0)
		{
			Il2Cpp il2Cpp = Il2Cpp;
			if (il2Cpp is ElfBase && il2Cpp.IsDumped)
			{
				if (!Options.GlobalMetadataDumpAddress.HasValue)
				{
					StructuredErrorExtensions.Raise<MetadataError>(MetadataError.ConfigurationError, "global-Metadata.data dump address must be provided");
				}
				Metadata.ImageBase = Options.GlobalMetadataDumpAddress.Value;
			}
		}
		Trace.WriteLine("Searching...");
		bool flag = Il2Cpp.PlusSearch(Metadata.methodDefs.Count((Il2CppMethodDefinition x) => x.methodIndex >= 0), Metadata.typeDefs.Length, Metadata.imageDefs.Length);
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !flag && Il2Cpp is PE)
		{
			Trace.WriteLine("Use custom PE loader");
			Il2Cpp = PELoader.Load(il2cppPath);
			Il2Cpp.SetProperties(version, Metadata.metadataUsagesCount);
			flag = Il2Cpp.PlusSearch(Metadata.methodDefs.Count((Il2CppMethodDefinition x) => x.methodIndex >= 0), Metadata.typeDefs.Length, Metadata.imageDefs.Length);
		}
		if (!flag)
		{
			flag = Il2Cpp.Search();
		}
		if (!flag)
		{
			flag = Il2Cpp.SymbolSearch();
		}
		if (!flag)
		{
			StructuredErrorExtensions.Raise<MetadataError>(MetadataError.UnknownFormat, "Can't use auto mode to process file, try manual mode.");
		}
		return;
		IL_00f2:
		throw new NotSupportedException("ERROR: il2cpp file not supported.");
	}
}
