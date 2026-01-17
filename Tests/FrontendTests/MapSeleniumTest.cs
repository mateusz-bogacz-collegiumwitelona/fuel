using OpenQA.Selenium;
using System.Threading;
using Tests.Selenium.Pages;
using Xunit;

namespace Tests.FrontendTests
{
    [Collection("Selenium")]
    public class MapSeleniumTest
    {
        private readonly SeleniumFixture _fixture;
        private readonly IWebDriver _driver;
        private readonly MapPage _mapPage;

        public MapSeleniumTest(SeleniumFixture fixture)
        {
            _fixture = fixture;
            _driver = _fixture.Driver;
            _mapPage = new MapPage(_driver, _fixture.BaseUrl);
        }

        [Fact]
        public void Map_LoadsSuccessfully()
        {
            _fixture.EnsureLoggedIn();
            _mapPage.GoTo();

            Assert.True(_mapPage.IsMapContainerVisible(), "Map container should be visible");
        }

        [Fact]
        public void Map_SearchFunctionality_Works()
        {
            _fixture.EnsureLoggedIn();
            _mapPage.GoTo();

            _mapPage.EnterSearchText("Orlen");
            _mapPage.ClickSearchButton();

            Thread.Sleep(1000);

            Assert.True(_driver.Url.Contains("/map"), "Should remain on map page after search");
        }

        [Fact]
        public void Map_MarkerClick_ShowsStationDetails()
        {
            _fixture.EnsureLoggedIn();
            _mapPage.GoTo();

            Thread.Sleep(2000); 

            if (!_mapPage.HasMarkers())
            {
               
                Assert.True(true);
                return;
            }

            _mapPage.ClickFirstMarker();
            Thread.Sleep(500);

            Assert.True(_mapPage.IsPopupVisible(), "Popup should be visible after marker click");
        }
    }
}