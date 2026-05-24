#define TRACE
using System;
using System.Diagnostics;

namespace Il2CppToolkit.Common.Errors;

public static class ErrorHandler
{
	public class ErrorEventArgs : EventArgs
	{
		public StructuredExceptionBase Exception;

		public ErrorEventArgs(StructuredExceptionBase ex)
		{
			Exception = ex;
		}
	}

	public static ErrorSeverity ErrorThreshhold { get; set; } = ErrorSeverity.Error;


	public static event EventHandler<ErrorEventArgs> OnError;

	public static void HandleError<TError>(StructuredException<TError> ex) where TError : Enum
	{
		if (ErrorHandler.OnError != null)
		{
			ErrorHandler.OnError(null, new ErrorEventArgs(ex));
		}
		if (ex.Severity >= ErrorThreshhold)
		{
			throw ex;
		}
	}

	public static void VerifyElseThrow<TError>(bool condition, TError errorCode, string message) where TError : Enum
	{
		if (!condition)
		{
			Trace.WriteLine("Fatal error: " + message);
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			errorCode.Raise(message);
		}
	}

	public static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			Trace.TraceWarning("Assertion failed: " + message);
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
		}
	}
}
