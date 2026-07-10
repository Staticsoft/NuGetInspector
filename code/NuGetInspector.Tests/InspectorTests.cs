namespace Staticsoft.NuGetInspector.Tests;

public class InspectorTests
{
	static readonly string SolutionRoot = FindSolutionRoot();
	static readonly Inspector Inspector = new(SolutionRoot);

	static string FindSolutionRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null)
		{
			if (dir.GetFiles("*.sln*").Length > 0) return dir.FullName;
			dir = dir.Parent!;
		}
		throw new InvalidOperationException("Solution root not found");
	}

	[Fact]
	public async Task ListTypes_ReturnsKnownClass()
	{
		var output = await Inspector.ListTypesAsync("TestLibrary", "1.0.1");
		Assert.Contains("TestLibrary.SampleClass", output);
		Assert.Contains("CLASSES", output);
	}

	[Fact]
	public async Task ListTypes_ReturnsKnownInterface()
	{
		var output = await Inspector.ListTypesAsync("TestLibrary", "1.0.1");
		Assert.Contains("INTERFACES", output);
		Assert.Contains("TestLibrary.ISampleInterface", output);
	}

	[Fact]
	public async Task ListTypes_ReturnsKnownEnum()
	{
		var output = await Inspector.ListTypesAsync("TestLibrary", "1.0.1");
		Assert.Contains("ENUMS", output);
		Assert.Contains("TestLibrary.SampleEnum", output);
	}

	[Fact]
	public async Task ListTypes_ReturnsKnownStruct()
	{
		var output = await Inspector.ListTypesAsync("TestLibrary", "1.0.1");
		Assert.Contains("STRUCTS", output);
		Assert.Contains("TestLibrary.SampleStruct", output);
	}

	[Fact]
	public async Task ListTypes_OutputContainsPackageHeader()
	{
		var output = await Inspector.ListTypesAsync("TestLibrary", "1.0.1");
		Assert.Contains("Package: TestLibrary 1.0.1", output);
	}

	[Fact]
	public async Task DescribeType_ReturnsConstructors()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("CONSTRUCTORS", output);
		Assert.Contains("SampleClass(string name)", output);
	}

	[Fact]
	public async Task DescribeType_ReturnsStaticMethod()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("STATIC METHODS", output);
		Assert.Contains("string Create(string name)", output);
	}

	[Fact]
	public async Task DescribeType_ReturnsInstanceMethod()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("INSTANCE METHODS", output);
		Assert.Contains("string GetName()", output);
	}

	[Fact]
	public async Task DescribeType_ReturnsProperties()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("PROPERTIES", output);
		Assert.Contains("[static] string DefaultName", output);
		Assert.Contains("string Name", output);
	}

	[Fact]
	public async Task DescribeType_OutputContainsTypeKindAndAssembly()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("CLASS: TestLibrary.SampleClass", output);
		Assert.Contains("Assembly: TestLibrary.dll", output);
	}

	[Fact]
	public async Task DescribeType_ReturnsEnumValues()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleEnum", "TestLibrary", "1.0.1");
		Assert.Contains("VALUES:", output);
		Assert.Contains("Alpha", output);
		Assert.Contains("Beta", output);
		Assert.Contains("Gamma", output);
	}

	[Fact]
	public async Task DescribeType_ReturnsGenericInstanceMethodWithTypeParameters()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("T Parse<T>(string value)", output);
	}

	[Fact]
	public async Task DescribeType_ReturnsGenericStaticMethodWithAllTypeParameters()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("TResult Convert<TSource, TResult>(TSource source)", output);
	}

	[Fact]
	public async Task DescribeType_DoesNotAddTypeParametersToNonGenericMethod()
	{
		var output = await Inspector.DescribeTypeAsync("TestLibrary.SampleClass", "TestLibrary", "1.0.1");
		Assert.Contains("string GetName()", output);
		Assert.DoesNotContain("GetName<", output);
	}

	[Fact]
	public async Task DescribeType_ThrowsWithClearMessageForUnknownType()
	{
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => Inspector.DescribeTypeAsync("TestLibrary.NoSuchType", "TestLibrary", "1.0.1"));
		Assert.Equal("Type 'TestLibrary.NoSuchType' not found in package TestLibrary 1.0.1.", exception.Message);
	}
}
