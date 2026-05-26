using System;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime;

public interface ITypeInfoProvider
{
    bool TryGetTypeInfo(Il2CsRuntimeContext runtime, Type managedType, out Il2CppTypeInfo result);
}
