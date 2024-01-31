using FluentAssertions;

using TypescriptGenerator.Console.TsGen;
using TypescriptGenerator.Console.TsGen.Types;

namespace IntegrationTests.Tests;

public partial class DiscoverApiEndpoints
{
	[Fact]
	public async Task DiscoverApiEndpoints_HappyPath()
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
			    public async Task<PaginatedResult<TodoDto>> GetTodosAsync([FromQuery] TodoCommands.GetTodosQuery query)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
				
			    [HttpGet("{todoId:int}")]
			    public async Task<TodoDto> GetTodoAsync([FromQuery] TodoCommands.GetTodoQuery query)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
			    
			    [HttpPost]
			    public async Task<TodoDto> CreateTodoAsync([FromQuery] TodoCommands.CreateTodoCommand command)
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
				
				[HttpPatch("{todoId:int}")]
			    public async Task<TodoDto> PatchTodoLocationAsync([FromQuery] TodoCommands.ModifyTodoLocationCommand command)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
				
				[HttpDelete("{todoId:int}")]
			    public async Task<TodoCommands.DeleteTodoCommandResponse> DeleteTodoAsync([FromQuery] TodoCommands.DeleteTodoCommand command)
			    {
					await Task.Run(() => { });
			        throw new NotImplementedException();
			    }
			}
			""");

		// Act
		var endpoints = generator.DiscoverApiEndpoints(compilation);

		// Assert
		_ = endpoints.Should().HaveCount(6, "because there are 6 endpoints");
		// Assert endpoint HTTP methods
		_ = endpoints[0].EndpointHttpMethod.Should().Be(EndpointHttpMethod.Get);
		_ = endpoints[1].EndpointHttpMethod.Should().Be(EndpointHttpMethod.Get);
		_ = endpoints[2].EndpointHttpMethod.Should().Be(EndpointHttpMethod.Post);
		_ = endpoints[3].EndpointHttpMethod.Should().Be(EndpointHttpMethod.Put);
		_ = endpoints[4].EndpointHttpMethod.Should().Be(EndpointHttpMethod.Patch);
		_ = endpoints[5].EndpointHttpMethod.Should().Be(EndpointHttpMethod.Delete);

		// Assert endpoint relative paths
		_ = endpoints[0].RelativePath.Should().Be("v1/todos");
		_ = endpoints[1].RelativePath.Should().Be("v1/todos/{todoId}");
		_ = endpoints[2].RelativePath.Should().Be("v1/todos");
		_ = endpoints[3].RelativePath.Should().Be("v1/todos/{todoId}");
		_ = endpoints[4].RelativePath.Should().Be("v1/todos/{todoId}");
		_ = endpoints[5].RelativePath.Should().Be("v1/todos/{todoId}");
	}
}
