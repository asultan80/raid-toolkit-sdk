using System;

namespace Il2CppToolkit.Common.Errors;

public class StructuredException<TError> : StructuredExceptionBase where TError : Enum
{
	public StructuredException(TError errorCode, string message)
		: base((int)(object)errorCode, errorCode.GetSeverity(), errorCode.GetCategory(), message)
	{
	}
}
