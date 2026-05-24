#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler;

public class CompileContext : ICompileContext
{
	private readonly HashSet<CompilePhase> m_phases = new HashSet<CompilePhase>();

	private readonly Dictionary<CompilePhase, int> m_phaseProgress = new Dictionary<CompilePhase, int>();

	private readonly ArtifactContainer m_artifacts = new ArtifactContainer();

	public ITypeModelMetadata Model { get; }

	public ArtifactContainer Artifacts => m_artifacts;

	public ICompilerLogger Logger { get; }

	public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

	public CompileContext(ITypeModelMetadata model, ICompilerLogger logger)
	{
		Model = model;
		Logger = logger;
	}

	public void AddPhase<T>(T compilePhase) where T : CompilePhase
	{
		m_phases.Add(compilePhase);
		m_phaseProgress.Add(compilePhase, 0);
		compilePhase.ProgressUpdated += delegate(object? _, ProgressUpdatedEventArgs e)
		{
			m_phaseProgress[compilePhase] = (int)((double)e.Completed / (double)e.Total * 100.0);
			int completed = m_phaseProgress.Values.Sum();
			int total = m_phaseProgress.Count * 100;
			this.ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs
			{
				Completed = completed,
				Total = total,
				DisplayName = e.DisplayName
			});
		};
	}

	public Task WaitForPhase<T>() where T : CompilePhase
	{
		return Artifacts.GetAsync(GetPhaseSpec());
		BuildArtifactSpecification<object> GetPhaseSpec()
		{
			return m_phases.Single((CompilePhase phase) => phase.GetType() == typeof(T)).PhaseSpec;
		}
	}

	public IEnumerable<CompilePhase> GetPhases()
	{
		foreach (CompilePhase phase in m_phases)
		{
			yield return phase;
		}
	}

	public async Task Execute()
	{
		_ = m_phases.Count;
		await Task.WhenAll(((IEnumerable<CompilePhase>)m_phases).Select((Func<CompilePhase, Task>)async delegate(CompilePhase phase)
		{
			Trace.WriteLine("[" + phase.Name + "]:Initialize");
			await phase.Initialize(this);
			Trace.WriteLine("[" + phase.Name + "]:Execute");
			await phase.Execute();
			Trace.WriteLine("[" + phase.Name + "]:Finalize");
			await phase.Finalize();
			Artifacts.Set(phase.PhaseSpec, new object());
			Trace.WriteLine("[" + phase.Name + "]:Completed");
		}).ToArray());
	}
}
