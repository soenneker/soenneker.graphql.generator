using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.GraphQL.Generator.Tests;

[Collection("Collection")]
public sealed class GraphQLGeneratorTests : FixturedUnitTest
{
    public GraphQLGeneratorTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {
    }
}
