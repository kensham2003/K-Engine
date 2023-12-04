#pragma once
#include <string>
#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>

class Scripting {
public:
	MonoAssembly* LoadCSharpAssembly(const std::string& assemblyPath);
	void PrintAssemblyTypes(MonoAssembly* assembly);
	MonoClass* GetClassInAssembly(MonoAssembly* assembly, const char* namespaceName, const char* className);
	MonoObject* CreateInstance(MonoClass* c);
	void CallMethod(MonoClass* monoClass, MonoObject* instance, const char* methodName);
};