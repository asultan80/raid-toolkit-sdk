using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler;
using static Il2CppToolkit.ReverseCompiler.ArtifactSpecs;
using Il2CppToolkit.Runtime;
using Il2CppToolkit.Runtime.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using GenericParameterAttributes = Mono.Cecil.GenericParameterAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodSemanticsAttributes = Mono.Cecil.MethodSemanticsAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using TypeSystem = Il2CppToolkit.Runtime.Types.TypeSystem;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore;

public class ModuleBuilder
{
	private sealed class TypeInfoBuilder : IDisposable
	{
		private const MethodAttributes kGetterAttrs = (MethodAttributes)2182;

		private static readonly Type StaticFieldMemberType = typeof(StaticFieldMember<, >);

		private static readonly ConstructorInfo StaticFieldMemberTypeCtor = typeof(StaticFieldMember<, >).GetConstructors()[0];

		private readonly TypeDefinition ForType;

		private readonly TypeReference ForTypeRef;

		private readonly ModuleDefinition ModuleDefinition;

		private readonly ModuleBuilder ModuleBuilder;

		private ILProcessor CctorIL;

		public TypeInfoBuilder(TypeDefinition forType, ModuleDefinition moduleDefinition, ModuleBuilder moduleBuilder)
		{
			ForType = forType;
			ForTypeRef = ((TypeReference)(object)ForType).AsGenericThis();
			ModuleDefinition = moduleDefinition;
			ModuleBuilder = moduleBuilder;
		}

		public ILProcessor GetCCtor()
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			if (CctorIL != null)
			{
				return CctorIL;
			}
			MethodDefinition val = new MethodDefinition(".cctor", (MethodAttributes)6289, ModuleDefinition.TypeSystem.Void);
			CctorIL = val.Body.GetILProcessor();
			ForType.Methods.Add(val);
			return CctorIL;
		}

		public void DefineMethod(string name, Il2CppType cppReturnType, Il2CppType[] parameters, MethodAttributes methodAttributes)
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Expected O, but got Unknown
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			if (((Enum)methodAttributes).HasFlag((Enum)(object)(MethodAttributes)2048))
			{
				return;
			}
			MethodDefinition methodDef = new MethodDefinition(name, methodAttributes, ModuleDefinition.TypeSystem.Void);
			((MethodReference)methodDef).ReturnType = ModuleBuilder.UseTypeReference((MemberReference)(object)methodDef, cppReturnType);
			if (((MethodReference)methodDef).ReturnType == null)
			{
				return;
			}
			TypeReference[] array = parameters.Select((Il2CppType cppParamType) => ModuleBuilder.UseTypeReference((MemberReference)(object)methodDef, cppParamType)).ToArray();
			if (!array.Contains(null))
			{
				TypeReference[] array2 = array;
				foreach (TypeReference val in array2)
				{
					((MethodReference)methodDef).Parameters.Add(new ParameterDefinition(val));
				}
				if (!((Enum)methodAttributes).HasFlag((Enum)(object)(MethodAttributes)1024))
				{
					ILProcessor iLProcessor = methodDef.Body.GetILProcessor();
					iLProcessor.Emit(OpCodes.Newobj, ModuleBuilder.ImportReference(typeof(NotImplementedException)).GetConstructor(ModuleDefinition));
					iLProcessor.Emit(OpCodes.Throw);
				}
				ForType.Methods.Add(methodDef);
			}
		}

		public void DefineField(string name, string storageName, TypeReference fieldType, byte indirection)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Expected O, but got Unknown
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Expected O, but got Unknown
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Expected O, but got Unknown
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Expected O, but got Unknown
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Expected O, but got Unknown
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0128: Expected O, but got Unknown
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_0163: Unknown result type (might be due to invalid IL or missing references)
			//IL_0177: Unknown result type (might be due to invalid IL or missing references)
			//IL_0184: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b0: Expected O, but got Unknown
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			GenericInstanceType val = ModuleDefinition.ImportReference(typeof(Il2CppTypeInfoLookup<>)).MakeGenericType(ForTypeRef);
			MethodDefinition val2 = new MethodDefinition("get_" + name, (MethodAttributes)2182, fieldType)
			{
				HasThis = true,
				SemanticsAttributes = (MethodSemanticsAttributes)2
			};
			ILProcessor iLProcessor = val2.Body.GetILProcessor();
			MethodReference val3 = new MethodReference("GetValue", fieldType, (TypeReference)(object)val)
			{
				HasThis = false
			};
			val3.GenericParameters.Add(new GenericParameter("TValue", (IGenericParameterProvider)(object)val3));
			GenericInstanceMethod val4 = val3.MakeGeneric(fieldType);
			((MethodReference)val4).ReturnType = (TypeReference)(object)val3.GenericParameters[0];
			((MethodReference)val4).Parameters.Add(new ParameterDefinition("instance", (ParameterAttributes)0, ModuleDefinition.ImportReference(typeof(IRuntimeObject))));
			((MethodReference)val4).Parameters.Add(new ParameterDefinition("name", (ParameterAttributes)0, ModuleDefinition.TypeSystem.String));
			((MethodReference)val4).Parameters.Add(new ParameterDefinition("indirection", (ParameterAttributes)4096, ModuleDefinition.TypeSystem.Byte));
			iLProcessor.Emit(OpCodes.Ldarg_0);
			if (((TypeReference)ForType).IsValueType)
			{
				iLProcessor.Emit(OpCodes.Ldobj, ForTypeRef);
				iLProcessor.Emit(OpCodes.Box, ForTypeRef);
			}
			iLProcessor.Emit(OpCodes.Ldstr, storageName);
			iLProcessor.EmitByte(indirection);
			iLProcessor.Emit(OpCodes.Call, (MethodReference)(object)val4);
			iLProcessor.Emit(OpCodes.Ret);
			ForType.Methods.Add(val2);
			PropertyDefinition val5 = new PropertyDefinition(name, (PropertyAttributes)0, fieldType)
			{
				GetMethod = val2
			};
			ForType.Properties.Add(val5);
		}

		public FieldDefinition DefineStaticField(string name, string storageName, TypeReference fieldType, byte indirection)
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Expected O, but got Unknown
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Expected O, but got Unknown
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Expected O, but got Unknown
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			TypeReference val = (TypeReference)(object)ModuleDefinition.ImportReference(StaticFieldMemberType).MakeGenericType(ForTypeRef, fieldType);
			MethodReference constructor = val.GetConstructor(ModuleDefinition);
			ParameterInfo[] parameters = StaticFieldMemberTypeCtor.GetParameters();
			foreach (ParameterInfo parameterInfo in parameters)
			{
				constructor.Parameters.Add(new ParameterDefinition(parameterInfo.Name, (ParameterAttributes)(ushort)parameterInfo.Attributes, ModuleDefinition.ImportReference(parameterInfo.ParameterType)));
			}
			FieldDefinition val2 = new FieldDefinition(name, (FieldAttributes)54, val);
			ForType.Fields.Add(val2);
			FieldReference val3 = new FieldReference(name, val, ForTypeRef);
			ILProcessor cCtor = GetCCtor();
			cCtor.Emit(OpCodes.Ldstr, storageName);
			cCtor.EmitByte(indirection);
			cCtor.Emit(OpCodes.Newobj, constructor);
			cCtor.Emit(OpCodes.Stsfld, val3);
			return val2;
		}

		public void Dispose()
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			ILProcessor cctorIL = CctorIL;
			if (cctorIL != null)
			{
				cctorIL.Emit(OpCodes.Ret);
			}
		}
	}

	private readonly HashSet<Il2CppTypeDefinition> EnqueuedTypes = new HashSet<Il2CppTypeDefinition>();

	private Queue<Il2CppTypeDefinition> TypeDefinitionQueue = new Queue<Il2CppTypeDefinition>();

	private int Completed;

	private int Total;

	private int UpdateCounter;

	private string CurrentAction;

	private readonly ICompileContext Context;

	private readonly AssemblyDefinition AssemblyDefinition;

	private readonly Dictionary<int, MethodDefinition> MethodDefs = new Dictionary<int, MethodDefinition>();

	private readonly TypeReference RuntimeObjectTypeRef;

	private readonly TypeReference IRuntimeObjectTypeRef;

	private readonly TypeReference IMemorySourceTypeRef;

	private readonly MethodReference ObjectCtorMethodRef;

	private readonly AssemblyNameReference SystemRuntimeRef;

	private readonly bool IncludeCompilerGeneratedTypes;

	private static readonly Regex BackingFieldRegex = new Regex("<(.+)>k__BackingField", RegexOptions.Compiled);

	private static readonly Regex splitExplicitImpl = new Regex("^(.*)\\.(.+)$", RegexOptions.Compiled);

	private static readonly Regex templateBrackets = new Regex("<[^<>]*?>", RegexOptions.Compiled);

	private const MethodAttributes kRTObjGetterAttrs = (MethodAttributes)2534;

	private const MethodAttributes kCtorAttrs = (MethodAttributes)6278;

	private readonly Dictionary<Type, TypeReference> ImportedTypes = new Dictionary<Type, TypeReference>();

	private readonly Dictionary<Il2CppTypeEnum, TypeReference> BuiltInTypes = new Dictionary<Il2CppTypeEnum, TypeReference>();

	private readonly Dictionary<Il2CppGenericParameter, GenericParameter> GenericParameters = new Dictionary<Il2CppGenericParameter, GenericParameter>();

	private readonly Dictionary<Il2CppTypeDefinition, TypeDefinition> TypeDefinitions = new Dictionary<Il2CppTypeDefinition, TypeDefinition>();

	private readonly IReadOnlyDictionary<Il2CppTypeDefinition, TypeSelectorResult> IncludedDescriptors;

	private Il2Cpp Il2Cpp => ((ITypeModel)Context.Model).Il2Cpp;

	private Metadata Metadata => ((ITypeModel)Context.Model).Metadata;

	private ModuleDefinition Module => AssemblyDefinition.MainModule;

	public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

	private void AddWork(int count = 1)
	{
		Total++;
		OnWorkUpdated();
	}

	private void CompleteWork(int count = 1)
	{
		Completed++;
		OnWorkUpdated();
	}

	private void SetAction(string actionName)
	{
		if (!(CurrentAction == actionName))
		{
			CurrentAction = actionName;
			UpdateCounter = -1;
			OnWorkUpdated();
		}
	}

	private void OnWorkUpdated()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		if (++UpdateCounter % 50 == 0 && Total >= 10)
		{
			this.ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs
			{
				Total = Math.Max(Total, 1),
				Completed = Completed,
				DisplayName = CurrentAction
			});
		}
	}

	public void Build()
	{
		BuildDefinitionQueue();
	}

	private void BuildDefinitionQueue()
	{
		Queue<Il2CppTypeDefinition> queue = new Queue<Il2CppTypeDefinition>();
		do
		{
			SetAction("Processing");
			do
			{
				Queue<Il2CppTypeDefinition> typeDefinitionQueue = TypeDefinitionQueue;
				TypeDefinitionQueue = new Queue<Il2CppTypeDefinition>();
				Il2CppTypeDefinition result;
				while (typeDefinitionQueue.TryDequeue(out result))
				{
					CompleteWork();
					TypeReference val = UseTypeDefinition(result);
					if (val == null)
					{
						continue;
					}
					ICompilerLogger logger = Context.Logger;
					if (logger != null)
					{
						logger.LogInfo("[" + ((MemberReference)val).FullName + "] Dequeued");
					}
					TypeDefinition val2 = (TypeDefinition)(object)((val is TypeDefinition) ? val : null);
					if (val2 == null)
					{
						continue;
					}
					if (!result.IsEnum)
					{
						ICompilerLogger logger2 = Context.Logger;
						if (logger2 != null)
						{
							logger2.LogInfo("[" + ((MemberReference)val).FullName + "] Init Type");
						}
						InitializeTypeDefinition(result, val2);
						DefineConstructors(val2);
					}
					else
					{
						val2.BaseType = ImportReference(typeof(Enum));
					}
					ICompilerLogger logger3 = Context.Logger;
					if (logger3 != null)
					{
						logger3.LogInfo("[" + ((MemberReference)val).FullName + "] Marked->Build");
					}
					queue.Enqueue(result);
					AddWork();
				}
			}
			while (TypeDefinitionQueue.Count > 0);
			ICompilerLogger logger4 = Context.Logger;
			if (logger4 != null)
			{
				logger4.LogInfo("Building marked types");
			}
			SetAction("Compiling");
			Il2CppTypeDefinition result2;
			while (queue.TryDequeue(out result2))
			{
				CompleteWork();
				TypeReference val3 = UseTypeDefinition(result2);
				if (val3 == null)
				{
					continue;
				}
				ICompilerLogger logger5 = Context.Logger;
				if (logger5 != null)
				{
					logger5.LogInfo("[" + ((MemberReference)val3).FullName + "] Building");
				}
				TypeDefinition val4 = (TypeDefinition)(object)((val3 is TypeDefinition) ? val3 : null);
				if (val4 == null)
				{
					continue;
				}
				if (result2.declaringTypeIndex >= 0)
				{
					Il2CppTypeDefinition typeDefinitionFromIl2CppType = Context.Model.GetTypeDefinitionFromIl2CppType(Il2Cpp.Types[result2.declaringTypeIndex], true);
					if (typeDefinitionFromIl2CppType != null)
					{
						UseTypeDefinition(typeDefinitionFromIl2CppType);
					}
				}
				ICompilerLogger logger6 = Context.Logger;
				if (logger6 != null)
				{
					logger6.LogInfo("[" + ((MemberReference)val3).FullName + "] Fields");
				}
				DefineFields(result2, val4);
				ICompilerLogger logger7 = Context.Logger;
				if (logger7 != null)
				{
					logger7.LogInfo("[" + ((MemberReference)val3).FullName + "] Methods");
				}
				DefineMethods(result2, val4);
				ICompilerLogger logger8 = Context.Logger;
				if (logger8 != null)
				{
					logger8.LogInfo("[" + ((MemberReference)val3).FullName + "] Properties");
				}
				DefineProperties(result2, val4);
			}
		}
		while (TypeDefinitionQueue.Count > 0 || queue.Count > 0);
	}

	public ModuleBuilder(ICompileContext context, AssemblyDefinition assemblyDefinition, IReadOnlyDictionary<Il2CppTypeDefinition, TypeSelectorResult> includedDescriptors, bool includeCompilerGeneratedTypes)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected O, but got Unknown
		Context = context;
		AssemblyDefinition = assemblyDefinition;
		IncludedDescriptors = includedDescriptors;
		Module.AssemblyReferences.Add(new AssemblyNameReference("Il2CppToolkit.Runtime", new Version(2, 0, 0, 0)));
		AssemblyNameReference val = new AssemblyNameReference("System.Runtime", new Version(5, 0, 0, 0));
		val.PublicKeyToken = new byte[8] { 176, 63, 95, 127, 17, 213, 10, 58 };
		SystemRuntimeRef = val;
		Module.AssemblyReferences.Add(SystemRuntimeRef);
		assemblyDefinition.CustomAttributes.Add(new CustomAttribute(ImportReference(typeof(GeneratedAttribute)).GetConstructor(Module)));
		AddBuiltInTypes(Module);
		RuntimeObjectTypeRef = ImportReference(typeof(RuntimeObject));
		IRuntimeObjectTypeRef = ImportReference(typeof(IRuntimeObject));
		IMemorySourceTypeRef = ImportReference(typeof(IMemorySource));
		ObjectCtorMethodRef = ImportReference(typeof(object)).GetConstructor(Module);
		IncludeCompilerGeneratedTypes = includeCompilerGeneratedTypes;
	}

	private void DefineFields(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		using TypeInfoBuilder typeInfoBuilder = new TypeInfoBuilder(typeDef, Module, this);
		int num = cppTypeDef.fieldStart + cppTypeDef.field_count;
		Il2CppFieldDefaultValue val6 = default(Il2CppFieldDefaultValue);
		object constant = default(object);
		for (int i = cppTypeDef.fieldStart; i < num; i++)
		{
			Il2CppFieldDefinition val = Metadata.fieldDefs[i];
			Il2CppType val2 = Il2Cpp.Types[val.typeIndex];
			string stringFromIndex = Metadata.GetStringFromIndex(val.nameIndex);
			string text = BackingFieldRegex.Replace(stringFromIndex, (Match match) => match.Groups[1].Value);
			TypeReference val3 = UseTypeReference((MemberReference)(object)typeDef, val2);
			if (val3 == null)
			{
				continue;
			}
			FieldAttributes val4 = (FieldAttributes)(ushort)val2.attrs;
			bool flag = ((Enum)val4).HasFlag((Enum)(object)(FieldAttributes)16);
			if (((Enum)val4).HasFlag((Enum)(object)(FieldAttributes)64) || ((Enum)val4).HasFlag((Enum)(object)(FieldAttributes)32768) || cppTypeDef.IsEnum)
			{
				FieldDefinition val5 = new FieldDefinition(text, val4, val3)
				{
					DeclaringType = typeDef
				};
				typeDef.Fields.Add(val5);
				if ((((Enum)val4).HasFlag((Enum)(object)(FieldAttributes)64) || ((Enum)val4).HasFlag((Enum)(object)(FieldAttributes)32768)) && Metadata.GetFieldDefaultValueFromIndex(i, out val6) && val6.dataIndex != -1 && Context.Model.TryGetDefaultValue(val6, out constant))
				{
					val5.Constant = constant;
				}
			}
			else if (flag)
			{
				typeInfoBuilder.DefineStaticField(text, stringFromIndex, val3, 1);
			}
			else
			{
				typeInfoBuilder.DefineField(text, stringFromIndex, val3, 1);
			}
		}
	}

	private void DefineMethods(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
	{
		int num = cppTypeDef.methodStart + cppTypeDef.method_count;
		for (int i = cppTypeDef.methodStart; i < num; i++)
		{
			Il2CppMethodDefinition cppMethodDef = Metadata.methodDefs[i];
			MethodDefinition val = DefineMethod(typeDef, cppMethodDef);
			if (val != null)
			{
				MethodDefs.Add(i, val);
			}
		}
	}

	public MethodDefinition DefineMethod(TypeDefinition typeDef, Il2CppMethodDefinition cppMethodDef)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Expected O, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Expected O, but got Unknown
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Expected O, but got Unknown
		//IL_0615: Unknown result type (might be due to invalid IL or missing references)
		//IL_061f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0628: Unknown result type (might be due to invalid IL or missing references)
		//IL_062e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_071a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0825: Unknown result type (might be due to invalid IL or missing references)
		//IL_0833: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
		//IL_057b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Expected O, but got Unknown
		//IL_0590: Expected O, but got Unknown
		//IL_0805: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0560: Unknown result type (might be due to invalid IL or missing references)
		//IL_0562: Invalid comparison between Unknown and I4
		//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05db: Unknown result type (might be due to invalid IL or missing references)
		MethodAttributes val = (MethodAttributes)cppMethodDef.flags;
		string name = Metadata.GetStringFromIndex(cppMethodDef.nameIndex);
		if (name == ".ctor" || name == ".cctor")
		{
			return null;
		}
		bool flag = name.StartsWith("get_") || name.StartsWith("set_");
		MethodDefinition val2 = ((IEnumerable<MethodDefinition>)typeDef.Methods).FirstOrDefault((Func<MethodDefinition, bool>)((MethodDefinition method) => ((MemberReference)method).Name == name));
		if (((Enum)val).HasFlag((Enum)(object)(MethodAttributes)2048) && val2 != null)
		{
			ICompilerLogger logger = Context.Logger;
			if (logger != null)
			{
				logger.LogInfo($"Skipping existing method: {((MemberReference)typeDef).FullName}.{name} ");
			}
			if (((Enum)val).HasFlag((Enum)(object)(MethodAttributes)2048) && flag)
			{
				val2.Attributes = (MethodAttributes)((int)val2.Attributes | ((int)val & 0xFFF8));
			}
			return null;
		}
		if (name.StartsWith("op_") && ((Enum)val).HasFlag((Enum)(object)(MethodAttributes)2048))
		{
			ICompilerLogger logger2 = Context.Logger;
			if (logger2 != null)
			{
				logger2.LogInfo($"Skipping operator (unsupported): {((MemberReference)typeDef).FullName}.{name} ");
			}
			return null;
		}
		bool flag2 = ((Enum)val).HasFlag((Enum)(object)(MethodAttributes)16);
		if (flag2 && flag)
		{
			ICompilerLogger logger3 = Context.Logger;
			if (logger3 != null)
			{
				logger3.LogInfo($"Skipping static property accessor (unsupported): {((MemberReference)typeDef).FullName}.{name} ");
			}
			return null;
		}
		Il2CppType val3 = Il2Cpp.Types[cppMethodDef.returnType];
		MethodDefinition val4 = new MethodDefinition(name, val, ImportReference(typeof(void)))
		{
			DeclaringType = typeDef
		};
		typeDef.Methods.Add(val4);
		if (cppMethodDef.genericContainerIndex >= 0)
		{
			Il2CppGenericContainer val5 = Metadata.genericContainers[cppMethodDef.genericContainerIndex];
			for (int i = 0; i < val5.type_argc; i++)
			{
				int num = val5.genericParameterStart + i;
				Il2CppGenericParameter param = Metadata.genericParameters[num];
				GenericParameter val6 = CreateGenericParameter(param, (IGenericParameterProvider)(object)val4);
				((MethodReference)val4).GenericParameters.Add(val6);
			}
		}
		if (flag2)
		{
			ParameterDefinition val7 = new ParameterDefinition("source", (ParameterAttributes)0, IMemorySourceTypeRef);
			((MethodReference)val4).Parameters.Add(val7);
		}
		((MethodReference)val4).ReturnType = UseTypeReference((MemberReference)(object)val4, val3);
		if (((MethodReference)val4).ReturnType == null)
		{
			ICompilerLogger logger4 = Context.Logger;
			if (logger4 != null)
			{
				logger4.LogWarning(((MemberReference)typeDef).FullName + "." + name + "(...) Unsupported return type");
			}
			typeDef.Methods.Remove(val4);
			return null;
		}
		int num2 = cppMethodDef.parameterStart + cppMethodDef.parameterCount;
		for (int j = cppMethodDef.parameterStart; j < num2; j++)
		{
			Il2CppParameterDefinition val8 = Metadata.parameterDefs[j];
			Il2CppType cppType = Il2Cpp.Types[val8.typeIndex];
			string stringFromIndex = Metadata.GetStringFromIndex(val8.nameIndex);
			TypeReference val9 = UseTypeReference((MemberReference)(object)val4, cppType);
			if (val9 == null)
			{
				ICompilerLogger logger5 = Context.Logger;
				if (logger5 != null)
				{
					logger5.LogWarning(((MemberReference)typeDef).FullName + "." + name + "(...) Unsupported parameter type");
				}
				typeDef.Methods.Remove(val4);
				return null;
			}
			((MethodReference)val4).Parameters.Add(new ParameterDefinition(stringFromIndex, (ParameterAttributes)0, val9));
		}
		int num3 = name.LastIndexOf('.');
		if (val4.IsPrivate && val4.IsFinal && val4.IsVirtual && num3 >= 0)
		{
			ErrorHandler.Assert(num3 != -1, "explicit impl without interface in name");
			Match match2 = splitExplicitImpl.Match(name);
			string value = match2.Groups[1].Captures[0].Value;
			string value2 = match2.Groups[2].Captures[0].Value;
			value = templateBrackets.Replace(value, (Match match) => $"`{match.Value.Split(',').Count()}{match.Value}");
			Queue<TypeReference> queue = new Queue<TypeReference>();
			queue.Enqueue((TypeReference)(object)typeDef);
			TypeReference result;
			while (queue.TryDequeue(out result))
			{
				if (((MemberReference)result).FullName == value || (value.EndsWith("." + ((MemberReference)result).Name) && ((int)result.Resolve().Attributes & 7) > 1))
				{
					Collection<MethodReference> overrides = val4.Overrides;
					MethodReference val10 = new MethodReference(value2, ((MethodReference)val4).ReturnType, result)
					{
						HasThis = ((MethodReference)val4).HasThis
					};
					MethodReference val11 = val10;
					overrides.Add(val10);
					val11.Parameters.AddRange((IEnumerable<ParameterDefinition>)((MethodReference)val4).Parameters);
					break;
				}
				TypeDefinition val12 = (TypeDefinition)(object)((result is TypeDefinition) ? result : null);
				if (val12 == null)
				{
					continue;
				}
				if (val12.BaseType != null)
				{
					queue.Enqueue(val12.BaseType);
				}
				var enumerator = val12.Interfaces.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						InterfaceImplementation current = enumerator.Current;
						queue.Enqueue(current.InterfaceType);
					}
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
			}
		}
		else
		{
			val4.Attributes = (MethodAttributes)((int)val4.Attributes & 0xFFF8);
			val4.Attributes = (MethodAttributes)((int)val4.Attributes | 6);
		}
		TypeReference val13 = ((TypeReference)(object)typeDef).AsGenericThis();
		int num4 = (flag2 ? 1 : 0);
		GenericInstanceType typeLookupInst = Module.ImportReference(typeof(Il2CppTypeInfoLookup<>)).MakeGenericType(val13);
		MethodReference val14 = PrepareMethodRefAndGetImplementationToCall(val4, val3, typeLookupInst);
		if (!((Enum)val).HasFlag((Enum)(object)(MethodAttributes)1024))
		{
			ILProcessor iLProcessor = val4.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			if (!flag2 && val13.IsValueType)
			{
				iLProcessor.Emit(OpCodes.Ldobj, val13);
				iLProcessor.Emit(OpCodes.Box, val13);
			}
			iLProcessor.Emit(OpCodes.Ldstr, name);
			iLProcessor.EmitI4(cppMethodDef.parameterCount);
			iLProcessor.Emit(OpCodes.Newarr, ImportReference(typeof(object)));
			for (byte b = 0; b < cppMethodDef.parameterCount; iLProcessor.Emit(OpCodes.Stelem_Ref), b++)
			{
				iLProcessor.Emit(OpCodes.Dup);
				iLProcessor.EmitI4(b);
				iLProcessor.EmitArg(b + 1);
				TypeReference parameterType = ((ParameterReference)((MethodReference)val4).Parameters[b + num4]).ParameterType;
				if (parameterType.IsGenericInstance)
				{
					GenericInstanceType val15 = (GenericInstanceType)(object)((parameterType is GenericInstanceType) ? parameterType : null);
					if (val15 != null && ((MemberReference)val15).Name == "Nullable`1")
					{
						GenericInstanceType val16 = ImportReference(typeof(Nullable<>)).MakeGenericType((IEnumerable<TypeReference>)((TypeSpecification)val15).ElementType.GenericParameters);
						MethodReference constructor = ((TypeReference)(object)ImportReference(typeof(NullableArg<>)).MakeGenericType((IEnumerable<TypeReference>)val15.GenericArguments)).GetConstructor(Module, (TypeReference)val16);
						iLProcessor.Emit(OpCodes.Newobj, constructor);
						continue;
					}
				}
				if (parameterType.IsValueType || parameterType.IsGenericParameter)
				{
					iLProcessor.Emit(OpCodes.Box, parameterType);
				}
			}
			iLProcessor.Emit(OpCodes.Call, val14);
			iLProcessor.Emit(OpCodes.Ret);
		}
		return val4;
	}

	private MethodReference PrepareMethodRefAndGetImplementationToCall(MethodDefinition methodDef, Il2CppType returnType, GenericInstanceType typeLookupInst)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		TypeReference returnType2 = ((MethodReference)methodDef).ReturnType;
		bool isStatic = methodDef.IsStatic;
		MethodReference val = new MethodReference(isStatic ? "CallStaticMethod" : "CallMethod", ((MethodReference)methodDef).ReturnType, (TypeReference)(object)typeLookupInst)
		{
			HasThis = false
		};
		if ((int)returnType.type != 1)
		{
			val.GenericParameters.Add(new GenericParameter("TValue", (IGenericParameterProvider)(object)val));
			val.ReturnType = (TypeReference)(object)val.GenericParameters[0];
		}
		if (isStatic)
		{
			val.Parameters.Add(new ParameterDefinition(IMemorySourceTypeRef));
		}
		else
		{
			val.Parameters.Add(new ParameterDefinition(IRuntimeObjectTypeRef));
		}
		val.Parameters.Add(new ParameterDefinition(ImportReference(typeof(string))));
		val.Parameters.Add(new ParameterDefinition((TypeReference)new ArrayType(ImportReference(typeof(object)))));
		if ((int)returnType.type != 1)
		{
			return (MethodReference)(object)val.MakeGeneric(returnType2);
		}
		return val;
	}

	private void InitializeTypeDefinition(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		for (int i = 0; i < cppTypeDef.nested_type_count; i++)
		{
			int num = Metadata.nestedTypeIndices[cppTypeDef.nestedTypesStart + i];
			Il2CppTypeDefinition cppTypeDef2 = Metadata.typeDefs[num];
			TypeReference obj = UseTypeDefinition(cppTypeDef2);
			TypeDefinition val = (TypeDefinition)(object)((obj is TypeDefinition) ? obj : null);
			if (val != null)
			{
				typeDef.NestedTypes.Add(val);
			}
		}
		int j = cppTypeDef.interfacesStart;
		for (int num2 = cppTypeDef.interfacesStart + cppTypeDef.interfaces_count; j < num2; j++)
		{
			Il2CppType cppType = Il2Cpp.Types[Metadata.interfaceIndices[j]];
			TypeReference val2 = UseTypeReference((MemberReference)(object)typeDef, cppType);
			if (val2 != null)
			{
				typeDef.Interfaces.Add(new InterfaceImplementation(val2));
			}
		}
		if (cppTypeDef.genericContainerIndex >= 0)
		{
			Il2CppGenericContainer val3 = Metadata.genericContainers[cppTypeDef.genericContainerIndex];
			for (int k = 0; k < val3.type_argc; k++)
			{
				int num3 = val3.genericParameterStart + k;
				Il2CppGenericParameter param = Metadata.genericParameters[num3];
				GenericParameter val4 = CreateGenericParameter(param, (IGenericParameterProvider)(object)typeDef);
				((TypeReference)typeDef).GenericParameters.Add(val4);
			}
		}
		if (cppTypeDef.parentIndex >= 0)
		{
			Il2CppType cppType2 = Il2Cpp.Types[cppTypeDef.parentIndex];
			TypeReference baseType = UseTypeReference((MemberReference)(object)typeDef, cppType2);
			typeDef.BaseType = baseType;
		}
	}

	private MethodDefinition FindMethod(TypeDefinition typeDef, int methodIndex)
	{
		if (!MethodDefs.TryGetValue(methodIndex, out var value))
		{
			string methodName = Metadata.GetStringFromIndex(Metadata.methodDefs[methodIndex].nameIndex);
			return ((IEnumerable<MethodDefinition>)typeDef.Methods).FirstOrDefault((Func<MethodDefinition, bool>)((MethodDefinition method) => ((MemberReference)method).Name == methodName));
		}
		return value;
	}

	private PropertyDefinition EnsurePropertyDefinition(TypeDefinition typeDef, string propertyName, TypeReference typeRef)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		PropertyDefinition val = ((IEnumerable<PropertyDefinition>)typeDef.Properties).FirstOrDefault((Func<PropertyDefinition, bool>)((PropertyDefinition method) => ((MemberReference)method).Name == propertyName));
		if (val != null)
		{
			return val;
		}
		val = new PropertyDefinition(propertyName, (PropertyAttributes)0, typeRef);
		typeDef.Properties.Add(val);
		return val;
	}

	private void DefineProperties(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
	{
		int num = cppTypeDef.propertyStart + cppTypeDef.property_count;
		for (int i = cppTypeDef.propertyStart; i < num; i++)
		{
			Il2CppPropertyDefinition val = Metadata.propertyDefs[i];
			string stringFromIndex = Metadata.GetStringFromIndex(val.nameIndex);
			MethodDefinition val2 = ((val.get >= 0) ? FindMethod(typeDef, cppTypeDef.methodStart + val.get) : null);
			MethodDefinition val3 = ((val.set >= 0) ? FindMethod(typeDef, cppTypeDef.methodStart + val.set) : null);
			if (val2 != null || val3 != null)
			{
				PropertyDefinition val4;
				PropertyDefinition obj = (val4 = EnsurePropertyDefinition(typeDef, stringFromIndex, ((val2 != null) ? ((MethodReference)val2).ReturnType : null) ?? ((ParameterReference)((MethodReference)val3).Parameters[0]).ParameterType));
				if (val4.GetMethod == null)
				{
					MethodDefinition val6 = (val4.GetMethod = val2);
				}
				val4 = obj;
				if (val4.SetMethod == null)
				{
					MethodDefinition val6 = (val4.SetMethod = val3);
				}
			}
		}
	}

	private void CreateDefaultConstructor(TypeDefinition typeDef)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (!((TypeReference)typeDef).IsValueType)
		{
			MethodDefinition val = new MethodDefinition(".ctor", (MethodAttributes)6278, ImportReference(typeof(void)));
			ILProcessor iLProcessor = val.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			if (typeDef.BaseType != null)
			{
				MethodReference constructor = typeDef.BaseType.GetConstructor(Module);
				iLProcessor.Emit(OpCodes.Call, constructor);
			}
			else if (!((TypeReference)typeDef).IsValueType)
			{
				iLProcessor.Emit(OpCodes.Call, ObjectCtorMethodRef);
			}
			iLProcessor.Emit(OpCodes.Ret);
			typeDef.Methods.Add(val);
		}
	}

	private void DefineConstructors(TypeDefinition typeDef)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		if (typeDef.IsInterface)
		{
			return;
		}
		if (typeDef.BaseType == null || typeDef.BaseType == ImportReference(typeof(object)))
		{
			typeDef.BaseType = RuntimeObjectTypeRef;
		}
		CreateDefaultConstructor(typeDef);
		if (((TypeReference)typeDef).IsValueType)
		{
			ImplementIRuntimeObject(typeDef);
			return;
		}
		MethodDefinition val = new MethodDefinition(".ctor", (MethodAttributes)6278, ImportReference(typeof(void)));
		((MethodReference)val).Parameters.Add(new ParameterDefinition(IMemorySourceTypeRef));
		((MethodReference)val).Parameters.Add(new ParameterDefinition(ImportReference(typeof(ulong))));
		MethodReference constructor = typeDef.BaseType.GetConstructor(Module);
		var enumerator = ((MethodReference)val).Parameters.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				ParameterDefinition current = enumerator.Current;
				constructor.Parameters.Add(current);
			}
		}
		finally
		{
			((IDisposable)enumerator).Dispose();
		}
		ILProcessor iLProcessor = val.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Ldarg_2);
		iLProcessor.Emit(OpCodes.Call, constructor);
		iLProcessor.Emit(OpCodes.Ret);
		typeDef.Methods.Add(val);
	}

	private void ImplementIRuntimeObject(TypeDefinition typeDef)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Expected O, but got Unknown
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Expected O, but got Unknown
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Expected O, but got Unknown
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		TypeReference thisTypeRef = ((TypeReference)(object)typeDef).AsGenericThis();
		typeDef.Interfaces.Add(new InterfaceImplementation(IRuntimeObjectTypeRef));
		FieldReference val = AddIRuntimeObjectProperty("Source", IMemorySourceTypeRef);
		FieldReference val2 = AddIRuntimeObjectProperty("Address", ImportReference(typeof(ulong)));
		MethodDefinition val3 = new MethodDefinition(".ctor", (MethodAttributes)6278, ImportReference(typeof(void)));
		val3.Body.GetILProcessor().Emit(OpCodes.Ldarg_0);
		val3.Body.GetILProcessor().Emit(OpCodes.Ldc_I4_0);
		val3.Body.GetILProcessor().Emit(OpCodes.Conv_I8);
		val3.Body.GetILProcessor().Emit(OpCodes.Stfld, val2);
		val3.Body.GetILProcessor().Emit(OpCodes.Ldarg_0);
		val3.Body.GetILProcessor().Emit(OpCodes.Ldnull);
		val3.Body.GetILProcessor().Emit(OpCodes.Stfld, val);
		val3.Body.GetILProcessor().Emit(OpCodes.Ret);
		typeDef.Methods.Add(val3);
		MethodDefinition val4 = new MethodDefinition(".ctor", (MethodAttributes)6278, ImportReference(typeof(void)));
		((MethodReference)val4).Parameters.Add(new ParameterDefinition(IMemorySourceTypeRef));
		((MethodReference)val4).Parameters.Add(new ParameterDefinition(ImportReference(typeof(ulong))));
		ILProcessor iLProcessor = val4.Body.GetILProcessor();
		if (!((TypeReference)typeDef).IsValueType)
		{
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Call, ObjectCtorMethodRef);
		}
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Stfld, val);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_2);
		iLProcessor.Emit(OpCodes.Stfld, val2);
		iLProcessor.Emit(OpCodes.Ret);
		typeDef.Methods.Add(val4);
		FieldReference AddIRuntimeObjectProperty(string name, TypeReference typeReference)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Expected O, but got Unknown
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Expected O, but got Unknown
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Expected O, but got Unknown
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Expected O, but got Unknown
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Expected O, but got Unknown
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Expected O, but got Unknown
			FieldDefinition val5 = new FieldDefinition("<" + name + ">k__BackingField", (FieldAttributes)33, typeReference);
			val5.CustomAttributes.Add(new CustomAttribute(ImportReference(typeof(CompilerGeneratedAttribute)).GetConstructor(Module)));
			typeDef.Fields.Add(val5);
			FieldReference val6 = new FieldReference(((MemberReference)val5).Name, ((FieldReference)val5).FieldType, thisTypeRef);
			MethodDefinition val7 = new MethodDefinition("get_" + name, (MethodAttributes)2534, typeReference)
			{
				SemanticsAttributes = (MethodSemanticsAttributes)2
			};
			val7.CustomAttributes.Add(new CustomAttribute(ImportReference(typeof(CompilerGeneratedAttribute)).GetConstructor(Module)));
			typeDef.Methods.Add(val7);
			ILProcessor iLProcessor2 = val7.Body.GetILProcessor();
			iLProcessor2.Emit(OpCodes.Ldarg_0);
			iLProcessor2.Emit(OpCodes.Ldfld, val6);
			iLProcessor2.Emit(OpCodes.Ret);
			PropertyDefinition val8 = new PropertyDefinition(name ?? "", (PropertyAttributes)0, typeReference)
			{
				GetMethod = val7
			};
			typeDef.Properties.Add(val8);
			return val6;
		}
	}

	internal TypeReference ImportReference(Type type)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		if (type == null)
		{
			return null;
		}
		if (ImportedTypes.TryGetValue(type, out var typeRef))
		{
			return typeRef;
		}
		if (SystemRuntimeRef != null && type.Assembly == typeof(string).Assembly)
		{
			typeRef = Module.ImportReference(new TypeReference(type.Namespace, type.Name, (ModuleDefinition)null, (IMetadataScope)(object)SystemRuntimeRef, type.IsValueType));
			if (type.ContainsGenericParameters)
			{
				typeRef.GenericParameters.AddRange(((IEnumerable<Type>)type.GetGenericArguments()).Select((Func<Type, GenericParameter>)((Type _) => new GenericParameter((IGenericParameterProvider)(object)typeRef))));
			}
		}
		else
		{
			typeRef = Module.ImportReference(type);
		}
		if (typeRef == null)
		{
			return null;
		}
		ImportedTypes.Add(type, typeRef);
		return typeRef;
	}

	internal void ProcessDescriptors()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<Il2CppTypeDefinition, TypeSelectorResult> includedDescriptor in IncludedDescriptors)
		{
			if (((Enum)includedDescriptor.Value).HasFlag((Enum)(object)(TypeSelectorResult)1))
			{
				IncludeTypeDefinition(includedDescriptor.Key);
			}
		}
	}

	public void IncludeTypeDefinition(Il2CppTypeDefinition cppTypeDef)
	{
		UseTypeDefinition(cppTypeDef);
	}

	private TypeReference UseTypeDefinition(Il2CppTypeDefinition cppTypeDef)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (!IncludedDescriptors.TryGetValue(cppTypeDef, out var value) || ((Enum)value).HasFlag((Enum)(object)(TypeSelectorResult)4))
		{
			string stringFromIndex = Metadata.GetStringFromIndex(cppTypeDef.nameIndex);
			ICompilerLogger logger = Context.Logger;
			if (logger != null)
			{
				logger.LogInfo("Excluding '" + stringFromIndex + "' based on exclusion rule");
			}
			return null;
		}
		TypeReference orCreateTypeDefinition = GetOrCreateTypeDefinition(cppTypeDef);
		if (orCreateTypeDefinition == null)
		{
			return null;
		}
		if (EnqueuedTypes.Contains(cppTypeDef))
		{
			return orCreateTypeDefinition;
		}
		TypeDefinition val = (TypeDefinition)(object)((orCreateTypeDefinition is TypeDefinition) ? orCreateTypeDefinition : null);
		if (val != null && cppTypeDef.declaringTypeIndex == -1)
		{
			Module.Types.Add(val);
		}
		EnqueuedTypes.Add(cppTypeDef);
		TypeDefinitionQueue.Enqueue(cppTypeDef);
		AddWork();
		return orCreateTypeDefinition;
	}

	private TypeReference GetOrCreateTypeDefinition(Il2CppTypeDefinition cppTypeDef)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Expected O, but got Unknown
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Expected O, but got Unknown
		if (TypeDefinitions.TryGetValue(cppTypeDef, out var value))
		{
			return (TypeReference)(object)value;
		}
		string stringFromIndex = Metadata.GetStringFromIndex(cppTypeDef.nameIndex);
		string text = string.Empty;
		if (stringFromIndex.Contains("<") && !IncludeCompilerGeneratedTypes)
		{
			return null;
		}
		if (cppTypeDef.parentIndex != -1)
		{
			Il2CppType val = Il2Cpp.Types[cppTypeDef.parentIndex];
			if ((int)val.type == 18)
			{
				Il2CppTypeDefinition typeDefinitionFromIl2CppType = Context.Model.GetTypeDefinitionFromIl2CppType(val, true);
				string stringFromIndex2 = Metadata.GetStringFromIndex(typeDefinitionFromIl2CppType.nameIndex);
				if (stringFromIndex2 == "Delegate" || stringFromIndex2 == "MulticastDelegate")
				{
					ICompilerLogger logger = Context.Logger;
					if (logger != null)
					{
						logger.LogWarning("Excluding delegate type " + stringFromIndex);
					}
					return null;
				}
			}
		}
		if (stringFromIndex.Contains("<") && !IncludeCompilerGeneratedTypes)
		{
			return null;
		}
		if (cppTypeDef.declaringTypeIndex != -1)
		{
			Il2CppTypeDefinition typeDefinitionFromIl2CppType2 = Context.Model.GetTypeDefinitionFromIl2CppType(Il2Cpp.Types[cppTypeDef.declaringTypeIndex], true);
			if (Metadata.GetStringFromIndex(typeDefinitionFromIl2CppType2.namespaceIndex).StartsWith("System"))
			{
				return null;
			}
			TypeReference orCreateTypeDefinition = GetOrCreateTypeDefinition(typeDefinitionFromIl2CppType2);
			if (orCreateTypeDefinition == null || orCreateTypeDefinition.Namespace.StartsWith("System"))
			{
				return null;
			}
			text = ((MemberReference)orCreateTypeDefinition).FullName + "\\";
		}
		string stringFromIndex3 = Metadata.GetStringFromIndex(cppTypeDef.namespaceIndex);
		if (!string.IsNullOrEmpty(stringFromIndex3))
		{
			text = text + stringFromIndex3 + ".";
		}
		text += stringFromIndex;
		Type type = default(Type);
		if (TypeSystem.TryGetSubstituteType(text, out type, (Type)null))
		{
			return ImportReference(type);
		}
		TypeAttributes val2 = (TypeAttributes)((int)cppTypeDef.flags & -32);
		val2 = (TypeAttributes)((cppTypeDef.declaringTypeIndex == -1) ? ((int)val2 | 1) : ((int)val2 | 2));
		value = new TypeDefinition(stringFromIndex3, stringFromIndex, val2);
		value.CustomAttributes.Add(new CustomAttribute(ImportReference(typeof(GeneratedAttribute)).GetConstructor(Module)));
		TypeDefinitions.Add(cppTypeDef, value);
		return (TypeReference)(object)value;
	}

	internal TypeReference UseTypeReference(MemberReference memberReference, Il2CppType cppType)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected I4, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Invalid comparison between Unknown and I4
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Expected O, but got Unknown
		if (BuiltInTypes.TryGetValue(cppType.type, out var value))
		{
			return value;
		}
		Il2CppTypeEnum type = cppType.type;
		switch ((int)type - 15)
		{
		default:
		{
			if ((int)type != 29)
			{
				if ((int)type != 30)
				{
					break;
				}
				MethodDefinition val = (MethodDefinition)(object)((memberReference is MethodDefinition) ? memberReference : null);
				if (val == null)
				{
					throw new NotSupportedException();
				}
				return (TypeReference)(object)CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), (IGenericParameterProvider)(object)val);
			}
			Il2CppType il2CppType2 = ((ITypeModel)Context.Model).Il2Cpp.GetIl2CppType(cppType.data.type);
			value = UseTypeReference(memberReference, il2CppType2);
			if (value == null)
			{
				return null;
			}
			return (TypeReference)new ArrayType(value);
		}
		case 2:
		case 3:
		{
			Il2CppTypeDefinition typeDefinitionFromIl2CppType = Context.Model.GetTypeDefinitionFromIl2CppType(cppType, true);
			return UseTypeDefinition(typeDefinitionFromIl2CppType);
		}
		case 5:
		{
			Il2CppArrayType val8 = ((ITypeModel)Context.Model).Il2Cpp.MapVATR<Il2CppArrayType>(cppType.data.array);
			Il2CppType il2CppType4 = ((ITypeModel)Context.Model).Il2Cpp.GetIl2CppType(val8.etype);
			value = UseTypeReference(memberReference, il2CppType4);
			if (value == null)
			{
				return null;
			}
			return (TypeReference)new ArrayType(value, (int)val8.rank);
		}
		case 6:
		{
			Il2CppGenericClass val4 = ((ITypeModel)Context.Model).Il2Cpp.MapVATR<Il2CppGenericClass>(cppType.data.generic_class);
			Il2CppTypeDefinition genericClassTypeDefinition = Context.Model.GetGenericClassTypeDefinition(val4);
			TypeReference val5 = UseTypeDefinition(genericClassTypeDefinition);
			if (val5 == null)
			{
				return null;
			}
			GenericInstanceType val6 = new GenericInstanceType(val5);
			Il2CppGenericInst val7 = ((ITypeModel)Context.Model).Il2Cpp.MapVATR<Il2CppGenericInst>(val4.context.class_inst);
			ulong[] array = ((ITypeModel)Context.Model).Il2Cpp.MapVATR<ulong>(val7.type_argv, val7.type_argc);
			foreach (ulong num in array)
			{
				Il2CppType il2CppType3 = ((ITypeModel)Context.Model).Il2Cpp.GetIl2CppType(num);
				value = UseTypeReference(memberReference, il2CppType3);
				if (value == null)
				{
					return null;
				}
				val6.GenericArguments.Add(value);
			}
			return (TypeReference)(object)val6;
		}
		case 4:
		{
			MethodDefinition val2 = (MethodDefinition)(object)((memberReference is MethodDefinition) ? memberReference : null);
			if (val2 == null)
			{
				TypeDefinition val3 = (TypeDefinition)(object)((memberReference is TypeDefinition) ? memberReference : null);
				if (val3 != null)
				{
					return (TypeReference)(object)CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), (IGenericParameterProvider)(object)val3);
				}
				throw new NotSupportedException();
			}
			return (TypeReference)(object)CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), (IGenericParameterProvider)(object)val2.DeclaringType);
		}
		case 0:
		{
			Il2CppType il2CppType = ((ITypeModel)Context.Model).Il2Cpp.GetIl2CppType(cppType.data.type);
			value = UseTypeReference(memberReference, il2CppType);
			if (value == null)
			{
				return null;
			}
			return (TypeReference)new PointerType(value);
		}
		case 1:
			break;
		}
		throw new ArgumentOutOfRangeException();
	}

	private GenericParameter CreateGenericParameter(Il2CppGenericParameter param, IGenericParameterProvider iGenericParameterProvider)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		if (!GenericParameters.TryGetValue(param, out var value))
		{
			value = new GenericParameter(((ITypeModel)Context.Model).Metadata.GetStringFromIndex(param.nameIndex), iGenericParameterProvider)
			{
				Attributes = (GenericParameterAttributes)param.flags
			};
			GenericParameters.Add(param, value);
			for (int i = 0; i < param.constraintsCount; i++)
			{
				Il2CppType cppType = ((ITypeModel)Context.Model).Il2Cpp.Types[((ITypeModel)Context.Model).Metadata.constraintIndices[param.constraintsStart + i]];
				TypeReference val = UseTypeReference((MemberReference)iGenericParameterProvider, cppType);
				TypeDefinition val2 = (TypeDefinition)(object)((val is TypeDefinition) ? val : null);
				if (val2 != null)
				{
					val = new TypeReference(((TypeReference)val2).Namespace, ((MemberReference)val2).Name, ((MemberReference)val2).Module, ((TypeReference)val2).Scope);
				}
				if (val == null)
				{
					ICompilerLogger logger = Context.Logger;
					if (logger != null)
					{
						logger.LogWarning("Unsupported constraint");
					}
				}
				else
				{
					GenericParameterConstraint val3 = new GenericParameterConstraint(val);
					value.Constraints.Add(val3);
				}
			}
		}
		return value;
	}

	private void AddBuiltInTypes(ModuleDefinition moduleDef)
	{
		BuiltInTypes.Add((Il2CppTypeEnum)28, ImportReference(typeof(object)));
		BuiltInTypes.Add((Il2CppTypeEnum)1, ImportReference(typeof(void)));
		BuiltInTypes.Add((Il2CppTypeEnum)2, ImportReference(typeof(bool)));
		BuiltInTypes.Add((Il2CppTypeEnum)3, ImportReference(typeof(char)));
		BuiltInTypes.Add((Il2CppTypeEnum)4, ImportReference(typeof(sbyte)));
		BuiltInTypes.Add((Il2CppTypeEnum)5, ImportReference(typeof(byte)));
		BuiltInTypes.Add((Il2CppTypeEnum)6, ImportReference(typeof(short)));
		BuiltInTypes.Add((Il2CppTypeEnum)7, ImportReference(typeof(ushort)));
		BuiltInTypes.Add((Il2CppTypeEnum)8, ImportReference(typeof(int)));
		BuiltInTypes.Add((Il2CppTypeEnum)9, ImportReference(typeof(uint)));
		BuiltInTypes.Add((Il2CppTypeEnum)24, ImportReference(typeof(IntPtr)));
		BuiltInTypes.Add((Il2CppTypeEnum)25, ImportReference(typeof(UIntPtr)));
		BuiltInTypes.Add((Il2CppTypeEnum)10, ImportReference(typeof(long)));
		BuiltInTypes.Add((Il2CppTypeEnum)11, ImportReference(typeof(ulong)));
		BuiltInTypes.Add((Il2CppTypeEnum)12, ImportReference(typeof(float)));
		BuiltInTypes.Add((Il2CppTypeEnum)13, ImportReference(typeof(double)));
		BuiltInTypes.Add((Il2CppTypeEnum)14, ImportReference(typeof(string)));
		BuiltInTypes.Add((Il2CppTypeEnum)22, ImportReference(typeof(TypedReference)));
	}
}
