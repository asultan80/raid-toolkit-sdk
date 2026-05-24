using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler.Target;

namespace Il2CppToolkit.ReverseCompiler;

public class Compiler
{
	private readonly CompileContext m_context;

	private readonly List<ICompilerTarget> m_targets = new List<ICompilerTarget>();

	public ArtifactContainer Artifacts => m_context.Artifacts;

	public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

	public Compiler(TypeModel model, ICompilerLogger logger = null)
	{
		m_context = new CompileContext((ITypeModelMetadata)(object)model, logger);
		m_context.ProgressUpdated += delegate(object? _, ProgressUpdatedEventArgs e)
		{
			this.ProgressUpdated?.Invoke(this, e);
		};
	}

	public void AddTarget(ICompilerTarget target)
	{
		m_targets.Add(target);
	}

	public void AddConfiguration(params IStateSpecificationValue[] configuration)
	{
		foreach (IStateSpecificationValue stateSpecificationValue in configuration)
		{
			m_context.Artifacts.Set(stateSpecificationValue.Specification, stateSpecificationValue.Value);
		}
	}

	public async Task Compile()
	{
		foreach (ICompilerTarget target in m_targets)
		{
			foreach (CompilerTargetParameter parameter in target.Parameters)
			{
				if (parameter.Required && !m_context.Artifacts.Has(parameter.Specification))
				{
					StructuredErrorExtensions.Raise<CompilerError>(CompilerError.MissingParameter, "Compiler target " + target.Name + " is missing required parameter " + parameter.Specification.Name);
				}
			}
			foreach (CompilePhase phase in target.Phases)
			{
				m_context.AddPhase(phase);
			}
		}
		await m_context.Execute();
	}
}
