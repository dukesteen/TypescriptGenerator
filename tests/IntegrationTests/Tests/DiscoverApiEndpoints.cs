using FluentAssertions;

using TypescriptGenerator.Console.TsGen;

namespace IntegrationTests.Tests;

public class DiscoverApiEndpoints(SharedFixture sharedFixture) : IntegrationTest(sharedFixture)
{
	[Fact]
	public async Task DiscoversApiEndpoints()
	{
		// Arrange
		GetService(out Generator _);
		var compilation = await CSharpCompilationHelpers.CreateCompilationAsync($$"""
			{{SourceSnippets.CommonUsings}}
			
			namespace Timespace.Api.Application.Features.Modules.Employees;
			
			{{SourceSnippets.CommonDataStructures}}
			
			[ApiController]
			[Route("v{version:apiVersion}/todos")]
			[ApiVersion("1.0")]
			public class TodoController : ControllerBase
			{
			    [HttpGet("{todoId:int}")]
			    public async Task<PaginatedResult<TodoDto>> GetTodoAsync([FromQuery] TodoCommands.GetTodoQuery query)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
			    
			    [HttpPost("complex/{taskGroup:string}")]
			    public async Task<TodoDto> CreateTodoAsync([FromQuery] TodoCommands.CreateTodoCommand command)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
			    
			    [HttpPost("simple")]
				public async Task<TodoDto> CreateSimpleTodoAsync([FromBody] TodoCommands.CommandBody command)
				{
					await Task.Run(() => { });
				    throw new NotImplementedException();
				}
			    
			    [HttpPut("{todoId:int}")]
			    public async Task<TodoDto> ModifyTodoLocationAsync([FromQuery] TodoCommands.ModifyTodoLocationCommand command)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
			}
			""");

		// Act
		var endpoints = Generator.DiscoverApiEndpoints(compilation);

		// Assert
		_ = endpoints.Should().HaveCount(4, "because there are 4 endpoints");
	}
}
