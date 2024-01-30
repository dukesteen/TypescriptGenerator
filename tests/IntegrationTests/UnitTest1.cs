using TypescriptGenerator.Console.TsGen;

namespace IntegrationTests;

public class UnitTest1(SharedFixture sharedFixture) : IntegrationTest(sharedFixture)
{
	[Fact]
	public void Test1()
	{
		GetService<Generator>(out var _);
	}
}
