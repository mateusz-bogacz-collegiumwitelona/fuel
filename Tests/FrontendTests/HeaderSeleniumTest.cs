using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Xunit;

namespace Tests.FrontendTests
{
    [Collection("Selenium")]
    public class HeaderSeleniumTest
    {
        private readonly SeleniumFixture _fixture;
        private readonly WebDriverWait _wait;

        public HeaderSeleniumTest(SeleniumFixture fixture)
        {
            _fixture = fixture;
            _wait = new WebDriverWait(_fixture.Driver, TimeSpan.FromSeconds(15));
        }

        [Fact]
        public void Header_Logout_RedirectsToLogin()
        {
            _fixture.EnsureLoggedIn();

            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/dashboard");

            IWebElement menuToggle = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("div.dropdown.dropdown-end label[aria-haspopup='true']")));
            menuToggle.Click();

            IWebElement logoutButton = _wait.Until(driver =>
            {
                try
                {
                    IWebElement button = driver.FindElements(By.CssSelector("div.dropdown.dropdown-end ul.menu button")).FirstOrDefault();
                    return button != null && button.Displayed && button.Enabled ? button : null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            });

            Assert.NotNull(logoutButton);
            logoutButton.Click();

            bool redirected = _wait.Until(driver => driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase));
            Assert.True(redirected, "User should be redirected to /login after logout");
        }

        [Fact]
        public void Header_MenuContainsExpectedLinks_ForLoggedUser()
        {
            _fixture.EnsureLoggedIn();

            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/dashboard");

            IWebElement menuToggle = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("div.dropdown.dropdown-end label[aria-haspopup='true']")));
            menuToggle.Click();

            _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div.dropdown.dropdown-end ul.menu")));

            int settingsCount = _fixture.Driver.FindElements(By.CssSelector("div.dropdown.dropdown-end a[href='/settings']")).Count;
            int dashboardCount = _fixture.Driver.FindElements(By.CssSelector("div.dropdown.dropdown-end a[href='/dashboard']")).Count;
            int mapCount = _fixture.Driver.FindElements(By.CssSelector("div.dropdown.dropdown-end a[href='/map']")).Count;
            int listCount = _fixture.Driver.FindElements(By.CssSelector("div.dropdown.dropdown-end a[href='/list']")).Count;

            Assert.True(settingsCount > 0, "Expected settings link in header menu.");
            Assert.True(dashboardCount > 0, "Expected dashboard link in header menu.");
            Assert.True(mapCount > 0, "Expected map link in header menu.");
            Assert.True(listCount > 0, "Expected list link in header menu.");

            int adminLinkCount = _fixture.Driver.FindElements(By.CssSelector("div.dropdown.dropdown-end a[href='/admin-dashboard']")).Count;
            Assert.True(adminLinkCount == 0, "Admin link should not be visible for non-admin user.");
        }

       

        [Fact]
        public void Header_DarkModeToggle_ChangesTheme()
        {
            _fixture.EnsureLoggedIn();

            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/dashboard");

            string initialTheme = (string)((IJavaScriptExecutor)_fixture.Driver).ExecuteScript("return localStorage.getItem('theme');");

            IWebElement themeToggle = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("input.toggle.theme-controller")));

            bool initialChecked = themeToggle.Selected;

            themeToggle.Click();

            System.Threading.Thread.Sleep(500);

            string newTheme = (string)((IJavaScriptExecutor)_fixture.Driver).ExecuteScript("return localStorage.getItem('theme');");
            string dataTheme = (string)((IJavaScriptExecutor)_fixture.Driver).ExecuteScript("return document.documentElement.getAttribute('data-theme');");

            Assert.NotEqual(initialTheme, newTheme);
            Assert.Equal(newTheme, dataTheme);

            bool newChecked = themeToggle.Selected;
            Assert.NotEqual(initialChecked, newChecked);

            themeToggle.Click();

            System.Threading.Thread.Sleep(500);

            string revertedTheme = (string)((IJavaScriptExecutor)_fixture.Driver).ExecuteScript("return localStorage.getItem('theme');");
            Assert.Equal(initialTheme, revertedTheme);
        }
    }
}