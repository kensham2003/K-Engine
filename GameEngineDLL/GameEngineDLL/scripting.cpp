#include <iostream>
#include <sstream>
#include <fstream>
#include <memory>
#include "scripting.h"

static MonoDomain* s_CSharpDomain;
static MonoDomain* s_RootDomain;

char* ReadBytes(const std::string& filePath, uint32_t* outSize);

//Loads C# assembly
MonoAssembly* Scripting::LoadCSharpAssembly(const std::string& assemblyPath)
{
    mono_set_assemblies_path("mono/lib");

    MonoDomain* rootDomain = mono_jit_init("MyScriptRuntime");
    if (rootDomain == nullptr)
    {
        // Maybe log some error here
        return nullptr;
    }

    // Store the root domain pointer
    s_RootDomain = rootDomain;

    // Create an App Domain
    s_CSharpDomain = mono_domain_create_appdomain(_strdup("MyAppDomain"), nullptr);
    mono_domain_set(s_CSharpDomain, true);

    uint32_t fileSize = 0;
    char* fileData = ReadBytes(assemblyPath, &fileSize);

    // NOTE: We can't use this image for anything other than loading the assembly because this image doesn't have a reference to the assembly
    MonoImageOpenStatus status;
    MonoImage* image = mono_image_open_from_data_full(fileData, fileSize, 1, &status, 0);

    if (status != MONO_IMAGE_OK)
    {
        const char* errorMessage = mono_image_strerror(status);
        // Log some error message using the errorMessage data
        return nullptr;
    }

    MonoAssembly* assembly = mono_assembly_load_from_full(image, assemblyPath.c_str(), &status, 0);
    mono_image_close(image);

    // Don't forget to free the file data
    delete[] fileData;

    return assembly;
}

//Print the assembly out
void Scripting::PrintAssemblyTypes(MonoAssembly* assembly)
{
    MonoImage* image = mono_assembly_get_image(assembly);
    const MonoTableInfo* typeDefinitionsTable = mono_image_get_table_info(image, MONO_TABLE_TYPEDEF);
    int32_t numTypes = mono_table_info_get_rows(typeDefinitionsTable);

    for (int32_t i = 0; i < numTypes; i++)
    {
        uint32_t cols[MONO_TYPEDEF_SIZE];
        mono_metadata_decode_row(typeDefinitionsTable, i, cols, MONO_TYPEDEF_SIZE);

        const char* nameSpace = mono_metadata_string_heap(image, cols[MONO_TYPEDEF_NAMESPACE]);
        const char* name = mono_metadata_string_heap(image, cols[MONO_TYPEDEF_NAME]);

        printf("%s.%s\n", nameSpace, name);
    }
}

MonoClass* Scripting::GetClassInAssembly(MonoAssembly* assembly, const char* namespaceName, const char* className)
{
    MonoImage* image = mono_assembly_get_image(assembly);
    MonoClass* klass = mono_class_from_name(image, namespaceName, className);

    if (klass == nullptr)
    {
        // Log error here
        return nullptr;
    }

    return klass;
}

MonoObject* Scripting::CreateInstance(MonoClass* c)
{
    if (!c) return nullptr;

    MonoObject* classInstance = mono_object_new(s_CSharpDomain, c);

    // Call the parameterless (default) constructor
    mono_runtime_object_init(classInstance);

    return classInstance;
}

void Scripting::CallMethod(MonoClass* monoClass, MonoObject* instance, const char* methodName)
{
    // Get a reference to the method in the class
    MonoMethod* method = mono_class_get_method_from_name(monoClass, methodName, 0);

    if (!method)
        return;

    // Call the C# method on the instance, and get any potential exceptions
    MonoObject* exception = nullptr;

    // static method->instance = nullptr
    mono_runtime_invoke(method, instance, nullptr, &exception);
}



char* ReadBytes(const std::string& filePath, uint32_t* outSize) {
    std::ifstream stream(filePath, std::ios::binary | std::ios::ate);

    if (!stream)
    {
        // Failed to open the file
        return nullptr;
    }

    std::streampos end = stream.tellg();
    stream.seekg(0, std::ios::beg);
    uint32_t size = end - stream.tellg();

    if (size == 0)
    {
        // File is empty
        return nullptr;
    }

    char* buffer = new char[size];
    stream.read((char*)buffer, size);
    stream.close();

    *outSize = size;
    return buffer;
}