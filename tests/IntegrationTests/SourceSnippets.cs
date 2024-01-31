namespace IntegrationTests;

public static class SourceSnippets
{
	public const string CommonUsings = """
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using NodaTime;
""";

	public const string CommonDataStructures = """
	                                           public record TodoDto
	                                           {
	                                           		public string Title { get; init; } = null!;
	                                           		public string Description { get; init; } = null!;
	                                           		public Location Location { get; init; } = null!;
	                                           		public LocalDate? DueDate { get; init; }
	                                           		public Instant CreatedAt { get; init; }
	                                           }
	                                           
	                                           public static class TodoCommands
	                                           {
	                                           		public record CreateTodoCommand(
	                                           			[property: FromQuery(Name = "done")] bool Done,
	                                           			[property: FromBody] CommandBody Body);
	                                           
	                                           		public record CommandBody(string Title, string Description, Location Location);
	                                           
	                                           		public record ModifyTodoLocationCommand([property: FromRoute(Name = "todoId")] int Id, [property: FromBody] Location Location);
	                                           		public record GetTodoQuery([property: FromRoute(Name = "todoId")] int Id);
	                                           		public record DeleteTodoCommand([property: FromRoute(Name = "todoId")] int Id);
	                                           		public record DeleteTodoCommandResponse(bool Success);
	                                           		public record GetTodosQuery([property: FromQuery(Name = "page")] int Page, [property: FromQuery(Name = "pageSize")] int PageSize);
	                                           }
	                                           
	                                           public record Location(int Longitude, int Latitude, LocationType LocationType);
	                                           
	                                           public enum LocationType
	                                           {
	                                           		House,
	                                           		Business,
	                                           		Other,
	                                           }
	                                           
	                                           public class PaginatedResult<T>
	                                           {
	                                           		public int TotalCount { get; set; }
	                                           		public int Page { get; set; }
	                                           		public int PageSize { get; set; }
	                                           		public IReadOnlyList<T> Items { get; set; } = null!;
	                                           }
	                                           """;
}
