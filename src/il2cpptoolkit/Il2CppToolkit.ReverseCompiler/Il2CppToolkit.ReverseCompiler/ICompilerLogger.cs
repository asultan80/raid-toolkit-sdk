namespace Il2CppToolkit.ReverseCompiler;

public interface ICompilerLogger
{
	void LogInfo(string message);

	void LogMessage(string message);

	void LogError(string message);

	void LogWarning(string message);
}
