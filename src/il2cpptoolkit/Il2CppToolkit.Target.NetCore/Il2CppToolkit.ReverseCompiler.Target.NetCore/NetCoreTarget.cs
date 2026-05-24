using System.Collections.Generic;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore;

public class NetCoreTarget : ICompilerTarget
{
	public string Name => "NetCore";

	public IEnumerable<CompilerTargetParameter> Parameters { get; } = new List<CompilerTargetParameter>
	{
		new CompilerTargetParameter
		{
			Specification = (ISynchronousState)(object)ArtifactSpecs.AssemblyName,
			Required = true
		},
		new CompilerTargetParameter
		{
			Specification = (ISynchronousState)(object)ArtifactSpecs.OutputPath,
			Required = true
		},
		new CompilerTargetParameter
		{
			Specification = (ISynchronousState)(object)ArtifactSpecs.IncludeCompilerGeneratedTypes,
			Required = false
		},
		new CompilerTargetParameter
		{
			Specification = (ISynchronousState)(object)ArtifactSpecs.TypeSelectors,
			Required = true
		}
	};


	public IEnumerable<CompilePhase> Phases { get; } = new List<CompilePhase> { (CompilePhase)(object)new BuildModulePhase() };

}
