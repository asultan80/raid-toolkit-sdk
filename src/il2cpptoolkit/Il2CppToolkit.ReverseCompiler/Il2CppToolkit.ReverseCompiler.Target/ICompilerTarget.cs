using System.Collections.Generic;

namespace Il2CppToolkit.ReverseCompiler.Target;

public interface ICompilerTarget
{
	string Name { get; }

	IEnumerable<CompilerTargetParameter> Parameters { get; }

	IEnumerable<CompilePhase> Phases { get; }
}
