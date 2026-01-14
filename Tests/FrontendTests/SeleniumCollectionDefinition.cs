using Xunit;

namespace Tests.FrontendTests
{
    [CollectionDefinition("Selenium", DisableParallelization = true)]
    public class SeleniumCollectionDefinition : ICollectionFixture<SeleniumFixture>
    {
    }
}