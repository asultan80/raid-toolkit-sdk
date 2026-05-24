using System;
using System.Collections.Generic;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler;

public static class ArtifactSpecs
{
	[Flags]
	public enum TypeSelectorResult
	{
		None = 0,
		Include = 1,
		Nominal = 2,
		Exclude = 4
	}

	public static SynchronousVariableSpecification<IReadOnlyList<Func<TypeDescriptor, TypeSelectorResult>>> TypeSelectors = new SynchronousVariableSpecification<IReadOnlyList<Func<TypeDescriptor, TypeSelectorResult>>>("TypeSelectors");

	public static SynchronousVariableSpecification<string> AssemblyName = new SynchronousVariableSpecification<string>("AssemblyName");

	public static SynchronousVariableSpecification<Version> AssemblyVersion = new SynchronousVariableSpecification<Version>("AssemblyVersion");

	public static SynchronousVariableSpecification<string> OutputPath = new SynchronousVariableSpecification<string>("OutputPath");

	public static SynchronousVariableSpecification<bool> IncludeCompilerGeneratedTypes = new SynchronousVariableSpecification<bool>("IncludeCompilerGeneratedTypes", false);
}
