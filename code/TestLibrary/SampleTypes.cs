namespace TestLibrary;

public class SampleClass
{
    public static string DefaultName { get; } = "Default";
    public string Name { get; set; } = "";
    public SampleClass() {}
    public SampleClass(string name) { Name = name; }
    public string GetName() => Name;
    public static string Create(string name) => new SampleClass(name).GetName();
    public T Parse<T>(string value) => default!;
    public static TResult Convert<TSource, TResult>(TSource source) => default!;
}

public interface ISampleInterface
{
    string Name { get; }
    void Process(int value);
}

public enum SampleEnum { Alpha, Beta, Gamma }

public struct SampleStruct { public int Value { get; set; } }
