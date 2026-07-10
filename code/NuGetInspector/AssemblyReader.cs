using System.Reflection;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;

namespace Staticsoft.NuGetInspector;

public record TypeParameterInfo(string TypeName, string Name);
public record TypeMethodInfo(string Name, string ReturnTypeName, IReadOnlyList<TypeParameterInfo> Parameters, bool IsStatic, bool IsConstructor, IReadOnlyList<string> GenericParameters);
public record TypePropertyInfo(string Name, string TypeName, bool CanRead, bool CanWrite, bool IsStatic);
public record TypeInfo(string FullName, string Name, string Kind, IReadOnlyList<TypeMethodInfo> Methods, IReadOnlyList<TypePropertyInfo> Properties, IReadOnlyList<string> EnumValues);
public record PackageTypeInfo(string SelectedFramework, IReadOnlyList<TypeInfo> AllTypes);

public static class AssemblyReader
{
    static readonly HashSet<string> ExcludedMethods = new(StringComparer.Ordinal)
    {
        "ToString", "GetHashCode", "Equals", "GetType"
    };

    public static PackageTypeInfo ReadTypes(string dllPath, IEnumerable<string>? dependencyDirs = null)
    {
        var tfm = FrameworkSelector.GetSelectedFramework(dllPath);
        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var siblingDir = Path.GetDirectoryName(dllPath)!;

        IEnumerable<string> dlls = Directory.GetFiles(runtimeDir, "*.dll")
            .Concat(Directory.GetFiles(siblingDir, "*.dll"));

        if (dependencyDirs != null)
            dlls = dlls.Concat(dependencyDirs
                .Select(d => FrameworkSelector.TrySelectDll(d, tfm))
                .OfType<string>());

        var resolver = new PathAssemblyResolver(dlls.Distinct());

        using var mlc = new MetadataLoadContext(resolver);
        var assembly = mlc.LoadFromAssemblyPath(dllPath);

        IEnumerable<Type> types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null)!;
        }

        var result = types.Select(ReadType).ToList();
        return new PackageTypeInfo(tfm, result);
    }

    static TypeInfo ReadType(Type type)
    {
        var kind = ClassifyType(type);

        var methods = new List<TypeMethodInfo>();
        var properties = new List<TypePropertyInfo>();

        var enumValues = new List<string>();
        if (kind == "ENUM")
        {
            enumValues.AddRange(
                type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(f => f.IsLiteral)
                    .Select(f => f.Name));
        }

        if (kind != "ENUM")
        {
            var propertyAccessors = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (prop.GetMethod != null) propertyAccessors.Add(prop.GetMethod.Name);
                if (prop.SetMethod != null) propertyAccessors.Add(prop.SetMethod.Name);

                var isStatic = prop.GetMethod?.IsStatic ?? prop.SetMethod?.IsStatic ?? false;
                properties.Add(new TypePropertyInfo(
                    prop.Name,
                    FormatTypeName(prop.PropertyType),
                    prop.CanRead,
                    prop.CanWrite,
                    isStatic));
            }

            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                methods.Add(new TypeMethodInfo(
                    type.Name.Split('`')[0],
                    "void",
                    ctor.GetParameters().Select(p => new TypeParameterInfo(FormatTypeName(p.ParameterType), p.Name ?? "")).ToList(),
                    false,
                    true,
                    []));
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName) continue;
                if (propertyAccessors.Contains(method.Name)) continue;
                if (ExcludedMethods.Contains(method.Name)) continue;

                methods.Add(new TypeMethodInfo(
                    method.Name,
                    FormatTypeName(method.ReturnType),
                    method.GetParameters().Select(p => new TypeParameterInfo(FormatTypeName(p.ParameterType), p.Name ?? "")).ToList(),
                    method.IsStatic,
                    false,
                    method.IsGenericMethodDefinition
                        ? method.GetGenericArguments().Select(a => a.Name).ToList()
                        : []));
            }
        }

        return new TypeInfo(
            FormatFullTypeName(type),
            type.Name.Split('`')[0],
            kind,
            methods,
            properties,
            enumValues);
    }

    static string ClassifyType(Type type)
    {
        if (type.IsInterface) return "INTERFACE";
        if (type.IsEnum) return "ENUM";
        if (type.IsValueType) return "STRUCT";
        return "CLASS";
    }

    static string FormatFullTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var baseName = (type.FullName ?? type.Name).Split('`')[0];
        var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
        return $"{baseName}<{args}>";
    }

    static string FormatTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var baseName = type.Name.Split('`')[0];
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            return $"{baseName}<{args}>";
        }

        return type.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Boolean" => "bool",
            "Double" => "double",
            "Single" => "float",
            "Decimal" => "decimal",
            "Byte" => "byte",
            "Char" => "char",
            "Object" => "object",
            "Void" => "void",
            _ => type.Name
        };
    }
}
