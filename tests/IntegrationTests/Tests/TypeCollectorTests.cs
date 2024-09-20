using TypescriptGenerator.Console.ImmediateApisTsGen.Types;
using TypescriptGenerator.Console.ImmediateApisTsGen.Visitors;

namespace IntegrationTests.Tests;

public class TypeCollectorTests(SharedFixture sharedFixture) : IntegrationTest(sharedFixture)
{
	[Fact]
	public async Task CollectsTypes()
	{
		var compilation = await CSharpCompilationHelpers.CreateCompilationAsync("""
			using System;
			using System.Collections.Generic;
			
			public record Score(int Value);
			
			public record Address
			{
			    public required string Street { get; init; }
			    public required string City { get; init; }
			    public string? ZipCode { get; init; } = string.Empty;
			}
			
			public record Person
			{
			    public string? FirstName { get; init; } = string.Empty;
			    public string? LastName { get; init; } = string.Empty;
			    public int Age { get; init; }
			    public Address? Address { get; init; }
			}
			
			public class PaginatedResult<T>
			{
				public int TotalCount { get; set; }
				public int Page { get; set; }
				public int PageSize { get; set; }
				public IReadOnlyList<T> Items { get; set; } = null!;
			}
			
			public class OtherGeneric<T>
			{
				public T Value { get; set; } = default!;
			}
			
			public record Organization
			{
			    public List<string> Departments { get; set; } = [];
			    public List<Person> Employees { get; set; } = [];
			    public Dictionary<string, Address> OfficeLocations { get; set; } = new Dictionary<string, Address>();
			    public Dictionary<string, List<Score>> DepartmentScores { get; set; } = new Dictionary<string, List<Score>>();
			    public Dictionary<int, Person> EmployeeRecords { get; set; } = new Dictionary<int, Person>();
			    public PaginatedResult<OtherGeneric<Person>> EmployeeList { get; set; } = new PaginatedResult<OtherGeneric<Person>>();
			}
			""");

		var topLevelType = compilation.GetTypeByMetadataName("Organization");
		var typeCollector = new GeneratableTypeCollector([], TypeUsage.Request);
		typeCollector.CollectFrom(topLevelType!);

		Assert.Equal(6, typeCollector.GeneratableTypes.DistinctBy(x => x.FullyQualifiedName).Count());
	}
}
