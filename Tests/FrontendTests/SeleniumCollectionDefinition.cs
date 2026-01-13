using Xunit;

namespace Tests.Selenium
{
    [CollectionDefinition("Selenium", DisableParallelization = true)]
    public class SeleniumCollectionDefinition : ICollectionFixture<SeleniumFixture>
    {
    }
}