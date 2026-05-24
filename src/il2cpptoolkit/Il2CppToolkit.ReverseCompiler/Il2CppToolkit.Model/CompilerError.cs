using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Model;

[ErrorCategory("Compiler Error", "CE")]
public enum CompilerError
{
	[ErrorSeverity(ErrorSeverity.Error)]
	InternalError = 1,
	[ErrorSeverity(ErrorSeverity.Error)]
	ILGenerationError = 2,
	[ErrorSeverity(ErrorSeverity.Error)]
	MissingParameter = 3,
	[ErrorSeverity(ErrorSeverity.Error)]
	UnknownTypeReference = 500,
	[ErrorSeverity(ErrorSeverity.Error)]
	IncompleteGenericType = 501,
	[ErrorSeverity(ErrorSeverity.Error)]
	InterfaceNotSupportedOrEmitted = 502,
	[ErrorSeverity(ErrorSeverity.Error)]
	UnknownType = 503,
	[ErrorSeverity(ErrorSeverity.Error)]
	LoadTypeError = 504,
	[ErrorSeverity(ErrorSeverity.Error)]
	ResolveTypeError = 505
}
