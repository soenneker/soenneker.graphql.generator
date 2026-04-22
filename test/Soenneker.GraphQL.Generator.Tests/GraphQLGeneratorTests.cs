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
}
