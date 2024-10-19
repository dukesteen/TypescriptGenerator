using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

[Collection("SharedFixture")]
public abstract class IntegrationTest : IDisposable, IAsyncLifetime
{
	private readonly IServiceScope _serviceScope;

	protected IntegrationTest(SharedFixture sharedFixture)
	{
		SharedFixture = sharedFixture;
		_serviceScope = SharedFixture.Services.CreateScope();
	}

	private SharedFixture SharedFixture { get; }

	public Task InitializeAsync()
	{
		return Task.CompletedTask;
	}

	public Task DisposeAsync()
	{
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing) _serviceScope.Dispose();
	}

	protected void GetService<T>(out T service)
		where T : notnull
	{
		service = _serviceScope.ServiceProvider.GetRequiredService<T>();
	}
}
