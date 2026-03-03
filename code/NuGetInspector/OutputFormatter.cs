namespace Staticsoft.NuGetInspector;

public static class OutputFormatter
{
    public static string FormatTypeList(string packageId, string version, string selectedFramework, IReadOnlyList<TypeInfo> types)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Package: {packageId} {version}");
        sb.AppendLine($"Target: {selectedFramework}");

        AppendGroup(sb, "CLASSES", types.Where(t => t.Kind == "CLASS").OrderBy(t => t.FullName).Select(t => t.FullName));
        AppendGroup(sb, "INTERFACES", types.Where(t => t.Kind == "INTERFACE").OrderBy(t => t.FullName).Select(t => t.FullName));
        AppendGroup(sb, "ENUMS", types.Where(t => t.Kind == "ENUM").OrderBy(t => t.FullName).Select(t => t.FullName));
        AppendGroup(sb, "STRUCTS", types.Where(t => t.Kind == "STRUCT").OrderBy(t => t.FullName).Select(t => t.FullName));

        return sb.ToString().TrimEnd();
    }

    public static string FormatTypeDescription(TypeInfo type, string assemblyName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{type.Kind}: {type.FullName}");
        sb.AppendLine($"Assembly: {assemblyName}");

        var constructors = type.Methods.Where(m => m.IsConstructor).ToList();
        var staticMethods = type.Methods.Where(m => m.IsStatic && !m.IsConstructor).ToList();
        var instanceMethods = type.Methods.Where(m => !m.IsStatic && !m.IsConstructor).ToList();
        var staticProps = type.Properties.Where(p => p.IsStatic).ToList();
        var instanceProps = type.Properties.Where(p => !p.IsStatic).ToList();

        if (constructors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("CONSTRUCTORS:");
            foreach (var m in constructors)
                sb.AppendLine($"  {FormatMethod(m)}");
        }

        if (staticMethods.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("STATIC METHODS:");
            foreach (var m in staticMethods)
                sb.AppendLine($"  {FormatMethod(m)}");
        }

        if (instanceMethods.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("INSTANCE METHODS:");
            foreach (var m in instanceMethods)
                sb.AppendLine($"  {FormatMethod(m)}");
        }

        if (type.EnumValues.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("VALUES:");
            foreach (var v in type.EnumValues)
                sb.AppendLine($"  {v}");
        }

        var allProps = staticProps.Concat(instanceProps).ToList();
        if (allProps.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("PROPERTIES:");
            foreach (var p in staticProps)
                sb.AppendLine($"  {FormatProperty(p)}");
            foreach (var p in instanceProps)
                sb.AppendLine($"  {FormatProperty(p)}");
        }

        return sb.ToString().TrimEnd();
    }

    static void AppendGroup(System.Text.StringBuilder sb, string header, IEnumerable<string> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine($"{header} ({list.Count}):");
        foreach (var item in list)
            sb.AppendLine($"  {item}");
    }

    static string FormatMethod(TypeMethodInfo m)
    {
        var parameters = string.Join(", ", m.Parameters.Select(p => $"{p.TypeName} {p.Name}"));
        if (m.IsConstructor)
            return $"{m.Name}({parameters})";
        return $"{m.ReturnTypeName} {m.Name}({parameters})";
    }

    static string FormatProperty(TypePropertyInfo p)
    {
        var prefix = p.IsStatic ? "[static] " : "";
        var accessors = (p.CanRead, p.CanWrite) switch
        {
            (true, true) => "{ get; set; }",
            (true, false) => "{ get; }",
            (false, true) => "{ set; }",
            _ => ""
        };
        return $"{prefix}{p.TypeName} {p.Name} {accessors}".TrimEnd();
    }
}
