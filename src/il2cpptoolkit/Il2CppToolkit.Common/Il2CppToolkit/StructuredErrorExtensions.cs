using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit;

public static class StructuredErrorExtensions
{
	private static class StructuredErrorData<TError> where TError : Enum
	{
		public static ErrorCategoryAttribute ErrorCategory;

		public static Dictionary<TError, ErrorSeverity> SeverityMap;

		static StructuredErrorData()
		{
			SeverityMap = new Dictionary<TError, ErrorSeverity>();
			Type typeFromHandle = typeof(TError);
			ErrorCategory = typeFromHandle.GetCustomAttribute<ErrorCategoryAttribute>() ?? throw new InvalidOperationException("Error enumeration '$" + typeFromHandle.FullName + "' does not have required 'ErrorCategoryAttribute'");
			FieldInfo[] fields = typeFromHandle.GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				ErrorSeverityAttribute customAttribute = fieldInfo.GetCustomAttribute<ErrorSeverityAttribute>();
				SeverityMap.Add((TError)fieldInfo.GetRawConstantValue(), customAttribute?.Severity ?? ErrorSeverity.Error);
			}
		}
	}

	public static ErrorSeverity GetSeverity<TError>(this TError errorCode) where TError : Enum
	{
		return StructuredErrorData<TError>.SeverityMap[errorCode];
	}

	public static ErrorCategoryAttribute GetCategory<TError>(this TError errorCode) where TError : Enum
	{
		return StructuredErrorData<TError>.ErrorCategory;
	}

	public static string GetName<TError>(this TError errorCode) where TError : Enum
	{
		return $"{StructuredErrorData<TError>.ErrorCategory.Abbreviation}{(int)(object)errorCode}";
	}

	public static void Raise<TError>(this TError errorCode, string message) where TError : Enum
	{
		errorCode.GetSeverity();
		ErrorHandler.HandleError(new StructuredException<TError>(errorCode, message));
	}
}
