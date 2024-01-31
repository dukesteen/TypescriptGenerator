using FluentAssertions;

using TypescriptGenerator.Console.TsGen;

namespace IntegrationTests.Tests;

public partial class DiscoverApiEndpoints
{
	[Fact]
	public async Task DiscoverApiEndpoints_ApiVersionAttributeWithDouble_ShouldNotThrow()
	{
		// Arrange
		GetService(out Generator generator);
		var compilation = await CSharpCompilationHelpers.CreateCompilationAsync($$"""
			{{SourceSnippets.CommonUsings}}
			
			namespace Timespace.Api.Application.Features.Modules.Employees;
			
			{{SourceSnippets.CommonDataStructures}}
			
			[ApiController]
			[Route("v{version:apiVersion}/todos")]
			[ApiVersion(1)]
			public class TodoController : ControllerBase
			{
				[HttpGet]
			    public async Task<PaginatedResult<TodoDto>> GetTodosAsync([FromQuery] TodoCommands.GetTodosQuery query)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
			}
			""");

		// Act
		var endpoints = generator.DiscoverApiEndpoints(compilation);

		// Assert
		_ = endpoints.Should().HaveCount(1);
		_ = endpoints.Should().ContainSingle(e => e.RelativePath == "v1/todos");
	}
}
