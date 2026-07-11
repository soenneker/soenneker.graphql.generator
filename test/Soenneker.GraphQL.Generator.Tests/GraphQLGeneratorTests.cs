using System.Linq;
using System.Threading.Tasks;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Dtos;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.GraphQL.Generator.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class GraphQlGeneratorTests : HostedUnitTest
{
    public GraphQlGeneratorTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {
    }

    [Test]
    public async Task Request_builder_with_list_result_should_include_generic_collections_using()
    {
        const string schema = "type Query { items: [String!]! }";
        var generator = new GraphQlGenerator();
        var config = new GeneratorConfig
        {
            Namespace = "Generated",
            OutputDirectory = "generated"
        };

        GenerationResult result = generator.Generate(schema, config);
        GeneratedFile requestBuilder = result.Files.Single(file => file.RelativePath.EndsWith("GetItemsRequestBuilder.cs"));

        await Assert.That(requestBuilder.Content).Contains("using System.Collections.Generic;");
        await Assert.That(requestBuilder.Content).Contains("ValueTask<List<string>?> GetValue");
    }
}
