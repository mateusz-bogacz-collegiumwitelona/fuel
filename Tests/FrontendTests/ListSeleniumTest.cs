using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Tests.Selenium.Pages;
using Xunit;

namespace Tests.FrontendTests
{
    public class ListPageSeleniumTests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl = "https://fuelly.com.pl/";
        private readonly ListPage _listPage;

        public ListPageSeleniumTests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            _driver = new ChromeDriver(options);
            _listPage = new ListPage(_driver, _baseUrl);
        }

        public void Dispose()
        {
            _driver.Quit();
            _driver.Dispose();
        }

        [Fact]
        public void ListPage_Redirects_WhenAuthNotReady()
        {
            LoginPage page2 = new LoginPage(_driver, _baseUrl);
            page2.GoTo();
            page2.EnterEmail("szymon.mikolajek@studenci.collegiumwitelona.pl");
            page2.EnterPassword("1Qweasdzxc@");
            page2.Submit();

            _listPage.GoTo();

            Thread.Sleep(2000);

            var url = _driver.Url;

            Assert.True(
                url.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase),
                $"Unexpected URL: {url}"
            );
        }

        [Fact]
        public void List_OpenFilters_And_SearchButton_IsClickable()
        {
            ListPage page = new ListPage(_driver, _baseUrl);
            LoginPage page2 = new LoginPage(_driver, _baseUrl);
            page2.GoTo();
            page2.EnterEmail("szymon.mikolajek@studenci.collegiumwitelona.pl");
            page2.EnterPassword("1Qweasdzxc@");
            page2.Submit();

            page.GoTo();

            page.ToggleFilters();

            page.ClickSearchInFilters();

            bool tableVisible = page.IsTableVisible();
            bool noStations = page.IsNoStationsMessageVisible();
            Assert.True(tableVisible || noStations, "Expected either table with results or a 'no stations' message after search.");
        }

        [Fact]
        public void List_SearchWithNoResults_ShowsNoStationsMessage()
        {
            LoginPage loginPage = new LoginPage(_driver, _baseUrl);
            loginPage.GoTo();
            loginPage.EnterEmail("szymon.mikolajek@studenci.collegiumwitelona.pl");
            loginPage.EnterPassword("1Qweasdzxc@");
            loginPage.Submit();

            _listPage.GoTo();

            _listPage.ToggleFilters();

            

            _listPage.ClickSearchInFilters();

            Assert.True(_listPage.IsNoStationsMessageVisible());
        }
    }
}
