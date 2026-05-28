using System;

namespace Il2CppToolkit.Common.Errors;

public class StructuredExceptionBase : Exception
{
	public ErrorCategoryAttribute Category { get; }

	public ErrorSeverity Severity { get; }

	public int ErrorCode { get; }

	public StructuredExceptionBase(int errorCode, ErrorSeverity severity, ErrorCategoryAttribute category, string message)
		: base(message)
	{
		ErrorCode = errorCode;
		Severity = severity;
		Category = category;
	}

	public override string ToString()
	{
		string header = $"{Severity.ToString().ToLowerInvariant()} {Category.Abbreviation}{ErrorCode}: {Message}";
		string stack = StackTrace;
		return string.IsNullOrEmpty(stack) ? header : $"{header}\n{stack}";
	}
}
