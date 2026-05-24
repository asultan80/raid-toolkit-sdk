using System;
using System.Threading.Tasks;

namespace Il2CppToolkit.ReverseCompiler;

public abstract class CompilePhase
{
	public BuildArtifactSpecification<object> PhaseSpec;

	private int Completed;

	private int Total;

	private int UpdateCounter;

	private string CurrentAction;

	public abstract string Name { get; }

	public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

	protected void OnProgressUpdated(int completed, int total, string? displayName = null)
	{
		this.ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs
		{
			Completed = completed,
			Total = total,
			DisplayName = displayName
		});
	}

	public abstract Task Initialize(ICompileContext context);

	public virtual Task Execute()
	{
		return Task.CompletedTask;
	}

	public virtual Task Finalize()
	{
		return Task.CompletedTask;
	}

	protected void AddWork(int count = 1)
	{
		Total += count;
		OnWorkUpdated();
	}

	protected void CompleteWork(int count = 1)
	{
		Completed += count;
		OnWorkUpdated();
	}

	protected void SetAction(string actionName)
	{
		if (!(CurrentAction == actionName))
		{
			CurrentAction = actionName;
			UpdateCounter = -1;
			OnWorkUpdated();
		}
	}

	protected void OnWorkUpdated()
	{
		if (++UpdateCounter % 50 == 0 && Total >= 10)
		{
			OnProgressUpdated(Completed, Math.Max(Total, 1), CurrentAction);
		}
	}

	protected CompilePhase()
	{
		PhaseSpec = new BuildArtifactSpecification<object>(Name);
	}
}
