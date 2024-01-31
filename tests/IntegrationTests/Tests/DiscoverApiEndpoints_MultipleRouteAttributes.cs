using FluentAssertions;

using TypescriptGenerator.Console.TsGen;

namespace IntegrationTests.Tests;

public partial class DiscoverApiEndpoints
{
	[Fact]
	public async Task DiscoverApiEndpoints_MultipleRouteAttributes_ShouldThrow()
	{
		// Arrange
		GetService(out Generator generator);
		var compilation = await CSharpCompilationHelpers.CreateCompilationAsync($$"""
			{{SourceSnippets.CommonUsings}}
			
			namespace Timespace.Api.Application.Features.Modules.Employees;
			
			{{SourceSnippets.CommonDataStructures}}
			
			[ApiController]
			[Route("v{version:apiVersion}/todos")]
			[Route("v{version:apiVersion}/todos")]
			[ApiVersion("1.0")]
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

		// Act / Assert
		_ = FluentActions.Invoking(() => generator.DiscoverApiEndpoints(compilation)).Should()
			.Throw<InvalidOperationException>("Multiple route attributes are not supported")
			.WithMessage("Multiple Route attributes found on controller");
	}
}
