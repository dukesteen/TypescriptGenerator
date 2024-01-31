using FluentAssertions;

using TypescriptGenerator.Console.TsGen;

namespace IntegrationTests.Tests;

public partial class DiscoverApiEndpoints
{
	[Fact]
	public async Task DiscoverApiEndpoints_MultipleVersionAttributesOnController_ShouldThrow()
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
			.Throw<InvalidOperationException>("multiple API version attributes on controllers are not supported")
			.WithMessage("Multiple ApiVersion attributes found on controller");
	}

	[Fact]
	public async Task DiscoverApiEndpoints_MultipleVersionAttributesOnAction_ShouldThrow()
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
				[HttpGet]
				[ApiVersion("2.0")]
				[ApiVersion("2.0")]
			    public async Task<PaginatedResult<TodoDto>> GetTodosAsync([FromQuery] TodoCommands.GetTodosQuery query)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
			}
			""");

		// Act / Assert
		_ = FluentActions.Invoking(() => generator.DiscoverApiEndpoints(compilation)).Should()
			.Throw<InvalidOperationException>("multiple API version attributes are not supported on actions")
			.WithMessage("Multiple ApiVersion attributes found on action");
	}
}
