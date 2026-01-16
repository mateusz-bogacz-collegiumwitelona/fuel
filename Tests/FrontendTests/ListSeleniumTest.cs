using OpenQA.Selenium;
using System;
using System.Threading;
using Tests.Selenium.Attributes;
using Tests.Selenium.Pages;
using Xunit;

namespace Tests.FrontendTests
{
    [Collection("Selenium")]
    public class ListPageSeleniumTests 
    {
        private readonly SeleniumFixture _fixture;
        private readonly IWebDriver _driver;
        private readonly string _baseUrl;
        private readonly ListPage _listPage;

        public ListPageSeleniumTests(SeleniumFixture fixture)
        {
            _fixture = fixture;
            _driver = _fixture.Driver;
            _baseUrl = _fixture.BaseUrl;
            _listPage = new ListPage(_driver, _baseUrl);
        }


        [Fact]
        public void ListPage_Redirects_WhenAuthNotReady()
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            try
            {
                _driver.Navigate().GoToUrl(_baseUrl);
                js.ExecuteScript("try { localStorage.clear(); sessionStorage.clear(); } catch(e) {}");
                _driver.Manage().Cookies.DeleteAllCookies();
            }
            catch { 
              
            }

            _listPage.GoTo();

            Thread.Sleep(2000);

            var url = _driver.Url;

            Assert.True(
                url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                $"Expected redirect to /login for unathenticated user, but got: {url}"
            );
        }

        [RequireEnvironmentVariablesFact("SELENIUM_GRID_URL", "FRONTEND_URL")]
        public void List_OpenFilters_And_SearchButton_IsClickable()
        {
            _fixture.EnsureLoggedIn();

            _listPage.GoTo();
            _listPage.ToggleFilters();
            _listPage.ClickSearchInFilters();

            Thread.Sleep(1000);

            bool tableVisible = _listPage.IsTableVisible();
            bool noStations = _listPage.IsNoStationsMessageVisible();
            Assert.True(tableVisible || noStations, "Expected either table with results or a 'no stations' message after search.");
        }

      
    }
}