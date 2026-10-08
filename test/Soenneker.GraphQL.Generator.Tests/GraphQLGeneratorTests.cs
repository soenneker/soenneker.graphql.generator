using System.Linq;
using System.Threading.Tasks;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Dtos;
using Soenneker.Tests.HostedUnit;
using System.Threading;

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
    public async ValueTask Json_context_includes_operation_variables_and_transport_envelope(CancellationToken cancellationToken)
    {
        var generator = new GraphQlGenerator();
        GenerationResult result = generator.Generate("type Query { item(id: String!): String }", new GeneratorConfig
        {
            Namespace = "Generated",
            OutputDirectory = "generated"
        });
        GeneratedFile context = result.Files.Single(file => file.RelativePath.EndsWith("GraphQlJsonContext.cs"));
        await Assert.That(context.Content).Contains("typeof(GraphQlRequest)");
        await Assert.That(context.Content).Contains("typeof(GetItemVariables)");
        GeneratedFile http = result.Files.Single(file => file.RelativePath.EndsWith("GraphQlHttpClient.cs"));
        await Assert.That(http.Content).Contains("JsonTypeInfo<GraphQlRequest>");
        await Assert.That(http.Content).Contains("JsonTypeInfo<GraphQlResponse<T>>");
    }

    [Test]
    public async ValueTask Request_builder_with_list_result_should_include_generic_collections_using(CancellationToken cancellationToken)
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
        await Assert.That(requestBuilder.Content).Contains(".ConfigureAwait(false)");
        await Assert.That(requestBuilder.Content).DoesNotContain("Soenneker.Extensions");

        GeneratedFile httpClient = result.Files.Single(file => file.RelativePath.EndsWith("GraphQlHttpClient.cs"));
        await Assert.That(httpClient.Content).Contains(".ConfigureAwait(false)");
        await Assert.That(httpClient.Content).DoesNotContain("Soenneker.Extensions");
    }
}
