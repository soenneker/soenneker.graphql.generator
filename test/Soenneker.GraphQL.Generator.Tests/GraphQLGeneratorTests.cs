using System.Net.Http;
//using ConsoleApp1.Generated;
//using ConsoleApp1.Generated;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.GraphQL.Generator.Tests;

[Collection("Collection")]
public sealed class GraphQLGeneratorTests : FixturedUnitTest
{
    private readonly IGraphQLGenerator _util;

    public GraphQLGeneratorTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IGraphQLGenerator>(true);
    }

    [Fact]
    public void Default()
    {
      //  var client = new GraphQlClient(new GraphQlHttpClient(new HttpClient()));

    }
}
