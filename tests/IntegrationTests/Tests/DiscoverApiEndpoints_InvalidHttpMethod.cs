using FluentAssertions;

using TypescriptGenerator.Console.TsGen;

namespace IntegrationTests.Tests;

public partial class DiscoverApiEndpoints
{
	[Fact]
	public async Task DiscoverApiEndpoints_HttpOptionsMethod_ShouldBeEmpty()
	{
		// Arrange
		GetService(out Generator generator);
		var compilation = await CSharpCompilationHelpers.CreateCompilationAsync($$"""
			{{SourceSnippets.CommonUsings}}
			
			namespace Timespace.Api.Application.Features.Modules.Employees;
			
			{{SourceSnippets.CommonDataStructures}}
			
			[ApiController]
			[Route("v{version:apiVersion}/todos")]
			[ApiVersion("1.0")]
			public class TodoController : ControllerBase
			{
				[HttpOptions]
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
		_ = endpoints.Should().BeEmpty("HTTP OPTIONS is not supported");
	}
}
