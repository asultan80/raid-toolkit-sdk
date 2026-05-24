using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler;
using static Il2CppToolkit.ReverseCompiler.ArtifactSpecs;
using Mono.Cecil;
using Vestris.ResourceLib;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore;

public class BuildModulePhase : CompilePhase
{
	private IReadOnlyList<Func<TypeDescriptor, TypeSelectorResult>> m_typeSelectors;

	private string m_asmName;

	private Version m_asmVersion;

	private ICompileContext m_context;

	private string m_outputPath;

	private bool m_includeCompilerGeneratedTypes;

	public override string Name => "Build Module";

	public override Task Initialize(ICompileContext context)
	{
		m_context = context;
		m_outputPath = m_context.Artifacts.Get<string>((ITypedSynchronousState<string>)(object)ArtifactSpecs.OutputPath);
		m_typeSelectors = m_context.Artifacts.Get<IReadOnlyList<Func<TypeDescriptor, TypeSelectorResult>>>((ITypedSynchronousState<IReadOnlyList<Func<TypeDescriptor, TypeSelectorResult>>>)(object)ArtifactSpecs.TypeSelectors);
		m_asmName = context.Artifacts.Get<string>((ITypedSynchronousState<string>)(object)ArtifactSpecs.AssemblyName);
		m_asmVersion = context.Artifacts.Get<Version>((ITypedSynchronousState<Version>)(object)ArtifactSpecs.AssemblyVersion);
		m_includeCompilerGeneratedTypes = context.Artifacts.Get<bool>((ITypedSynchronousState<bool>)(object)ArtifactSpecs.IncludeCompilerGeneratedTypes);
		return Task.CompletedTask;
	}

	public override Task Execute()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_004d: Expected O, but got Unknown
		OnProgressUpdated(0, 100, "Initializing");
		IReadOnlyDictionary<Il2CppTypeDefinition, TypeSelectorResult> includedDescriptors = FilterTypes(m_typeSelectors);
		AssemblyDefinition val = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(m_asmName, m_asmVersion), ((ITypeModel)m_context.Model).ModuleName, new ModuleParameters
		{
			Kind = (ModuleKind)0
		});
		try
		{
			ModuleBuilder moduleBuilder = new ModuleBuilder(m_context, val, includedDescriptors, m_includeCompilerGeneratedTypes);
			moduleBuilder.ProcessDescriptors();
			moduleBuilder.ProgressUpdated += OnBuilderProgressUpdated;
			moduleBuilder.Build();
			Write(val);
			OnProgressUpdated(100, 100, "");
			return Task.CompletedTask;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void OnBuilderProgressUpdated(object sender, ProgressUpdatedEventArgs e)
	{
		OnProgressUpdated((int)((double)e.Completed / (double)e.Total * 100.0), 100, e.DisplayName);
	}

	private void Write(AssemblyDefinition assemblyDefinition)
	{
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		string text = m_outputPath;
		if (Path.IsPathRooted(text) && !Directory.Exists(Path.GetDirectoryName(text)))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(text));
		}
		if (Path.GetExtension(text) != ".dll")
		{
			text = Path.Combine(m_outputPath, ((ITypeModel)m_context.Model).ModuleName + ".dll");
		}
		AssemblyNameReference[] array = ((IEnumerable<AssemblyNameReference>)assemblyDefinition.MainModule.AssemblyReferences).Where((AssemblyNameReference asmRef) => asmRef.Name == "mscorlib" || asmRef.Name == "System.Private.CoreLib").ToArray();
		foreach (AssemblyNameReference val in array)
		{
			assemblyDefinition.MainModule.AssemblyReferences.Remove(val);
		}
		assemblyDefinition.Write(text, new WriterParameters());
		try
		{
			new VersionResource
			{
				FileVersion = m_asmVersion.ToString()
			}.SaveTo(text);
		}
		catch (Exception)
		{
			OnProgressUpdated(0, 99, "Failed to set file version, retrying in 1s...");
			Thread.Sleep(1000);
			try
			{
				new VersionResource
				{
					FileVersion = m_asmVersion.ToString()
				}.SaveTo(text);
			}
			catch
			{
			}
		}
	}

	private IReadOnlyDictionary<Il2CppTypeDefinition, TypeSelectorResult> FilterTypes(IReadOnlyList<Func<TypeDescriptor, TypeSelectorResult>> typeSelectors)
	{
		return (from descriptor in ((ITypeModel)m_context.Model).TypeDescriptors
			group typeSelectors.Select((Func<TypeDescriptor, TypeSelectorResult> selector) => selector(descriptor)).Aggregate((TypeSelectorResult a, TypeSelectorResult b) => (TypeSelectorResult)(a | b)) by descriptor.TypeDef).ToDictionary((IGrouping<Il2CppTypeDefinition, TypeSelectorResult> group) => group.Key, (IGrouping<Il2CppTypeDefinition, TypeSelectorResult> group) => group.Aggregate((TypeSelectorResult a, TypeSelectorResult b) => (TypeSelectorResult)(a | b)));
	}
}
