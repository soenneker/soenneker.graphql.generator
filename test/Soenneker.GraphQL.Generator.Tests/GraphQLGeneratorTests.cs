using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.GraphQL.Generator.Tests;

[Collection("Collection")]
public sealed class GraphQlGeneratorTests : FixturedUnitTest
{
    public GraphQlGeneratorTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {
    }
}
