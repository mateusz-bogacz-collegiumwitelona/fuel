using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Xunit;

namespace Tests.FrontendTests
{
    [Collection("Selenium")]
    public class AdminPanelSeleniumTest
    {
        private readonly SeleniumFixture _fixture;
        private readonly WebDriverWait _wait;

        public AdminPanelSeleniumTest(SeleniumFixture fixture)
        {
            _fixture = fixture;
            _wait = new WebDriverWait(_fixture.Driver, TimeSpan.FromSeconds(15));
        }

        private void EnsureLoggedInAsAdmin()
        {
           
            if (_fixture.Driver.Url.StartsWith("data:") || _fixture.Driver.Url == "about:blank")
            {
                _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl);
                System.Threading.Thread.Sleep(500); 
            }

            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/login");
            
          
            _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
            
            _fixture.Driver.Manage().Cookies.DeleteAllCookies();
            
        
            _fixture.Driver.Navigate().Refresh();

            try
            {
                IWebElement emailInput = _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("input[type='email']")));
                emailInput.Clear();
                emailInput.SendKeys(SeleniumConst.DEFAULT_ADMINEMAIL);

                IWebElement passwordInput = _fixture.Driver.FindElement(By.CssSelector("input[type='password']"));
                passwordInput.Clear();
                passwordInput.SendKeys(SeleniumConst.DEFAULT_ADMINPASSWORD);

                IWebElement submitButton = _fixture.Driver.FindElement(By.CssSelector("button[type='submit']"));
                submitButton.Click();

              
                bool redirected = _wait.Until(driver => 
                {
                    string currentUrl = driver.Url.ToLower();
                    return currentUrl.Contains("/admin") || 
                           currentUrl.Contains("/dashboard") || 
                           !currentUrl.Contains("/login");
                });

               
                _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
            }
            catch (WebDriverTimeoutException ex)
            {
         
                string currentUrl = _fixture.Driver.Url;
                string pageSource = _fixture.Driver.PageSource;
                throw new Exception($"Nie uda³o siê zalogowaæ jako admin. Obecny URL: {currentUrl}. Pierwsze 500 znaków strony: {pageSource.Substring(0, Math.Min(500, pageSource.Length))}", ex);
            }
        }

        private T RetryOnStaleElement<T>(Func<T> action, int maxAttempts = 3)
        {
            int attempts = 0;
            while (attempts < maxAttempts)
            {
                try
                {
                    return action();
                }
                catch (StaleElementReferenceException)
                {
                    attempts++;
                    if (attempts >= maxAttempts)
                    {
                        throw;
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
            throw new Exception("Max retry attempts reached");
        }

        [Fact]
        public void AdminLogin_ValidCredentials_RedirectsToAdminDashboard()
        {
          
            if (_fixture.Driver.Url.StartsWith("data:") || _fixture.Driver.Url == "about:blank")
            {
                _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl);
                System.Threading.Thread.Sleep(500);
            }

            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/login");
            _fixture.Driver.Manage().Cookies.DeleteAllCookies();
            _fixture.Driver.Navigate().Refresh();

            _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("input[type='email']")));

            RetryOnStaleElement(() =>
            {
                IWebElement emailInput = _fixture.Driver.FindElement(By.CssSelector("input[type='email']"));
                emailInput.Clear();
                emailInput.SendKeys(SeleniumConst.DEFAULT_ADMINEMAIL);
                return true;
            });

            RetryOnStaleElement(() =>
            {
                IWebElement passwordInput = _fixture.Driver.FindElement(By.CssSelector("input[type='password']"));
                passwordInput.Clear();
                passwordInput.SendKeys(SeleniumConst.DEFAULT_ADMINPASSWORD);
                return true;
            });

            RetryOnStaleElement(() =>
            {
                IWebElement submitButton = _fixture.Driver.FindElement(By.CssSelector("button[type='submit']"));
                submitButton.Click();
                return true;
            });

           
            _wait.Until(driver => 
            {
                string currentUrl = driver.Url.ToLower();
                return currentUrl.Contains("/admin") || 
                       currentUrl.Contains("/dashboard") || 
                       !currentUrl.Contains("/login");
            });

            bool isAdminPage = _fixture.Driver.Url.ToLower().Contains("/admin");
            Assert.True(isAdminPage, $"Admin should be redirected to /admin dashboard, but URL is: {_fixture.Driver.Url}");
        }

        [Fact]
        public void AdminDashboard_DisplaysAllPanelButtons()
        {
            EnsureLoggedInAsAdmin();
            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/admin");

            _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
            _wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//h1[contains(text(), 'Admin') or contains(text(), 'Panel')]")));

            IWebElement brandPanelButton = _wait.Until(ExpectedConditions.ElementExists(By.CssSelector("a[href='/admin/brands']")));
            IWebElement userPanelButton = _fixture.Driver.FindElement(By.CssSelector("a[href='/admin/users']"));
            IWebElement stationPanelButton = _fixture.Driver.FindElement(By.CssSelector("a[href='/admin/stations']"));
            IWebElement proposalPanelButton = _fixture.Driver.FindElement(By.CssSelector("a[href='/admin/proposals']"));

            Assert.NotNull(brandPanelButton);
            Assert.NotNull(userPanelButton);
            Assert.NotNull(stationPanelButton);
            Assert.NotNull(proposalPanelButton);
            Assert.True(brandPanelButton.Displayed);
            Assert.True(userPanelButton.Displayed);
            Assert.True(stationPanelButton.Displayed);
            Assert.True(proposalPanelButton.Displayed);
        }

        [Fact]
        public void AdminDashboard_BrandPanelButton_NavigatesToBrandPanel()
        {
            EnsureLoggedInAsAdmin();
            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/admin");

            IWebElement brandPanelButton = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("a[href='/admin/brands']")));
            brandPanelButton.Click();

            _wait.Until(driver => driver.Url.Contains("/admin/brands"));
            Assert.Contains("/admin/brands", _fixture.Driver.Url);
        }

        [Fact]
        public void AdminDashboard_UserPanelButton_NavigatesToUserPanel()
        {
            EnsureLoggedInAsAdmin();
            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/admin");

            IWebElement userPanelButton = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("a[href='/admin/users']")));
            userPanelButton.Click();

            _wait.Until(driver => driver.Url.Contains("/admin/users"));
            Assert.Contains("/admin/users", _fixture.Driver.Url);
        }

        [Fact]
        public void AdminDashboard_StationPanelButton_NavigatesToStationPanel()
        {
            EnsureLoggedInAsAdmin();
            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/admin");

            IWebElement stationPanelButton = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("a[href='/admin/stations']")));
            stationPanelButton.Click();

            _wait.Until(driver => driver.Url.Contains("/admin/stations"));
            Assert.Contains("/admin/stations", _fixture.Driver.Url);
        }

        [Fact]
        public void AdminDashboard_ProposalPanelButton_NavigatesToProposalPanel()
        {
            EnsureLoggedInAsAdmin();
            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/admin");

            IWebElement proposalPanelButton = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("a[href='/admin/proposals']")));
            proposalPanelButton.Click();

            _wait.Until(driver => driver.Url.Contains("/admin/proposals"));
            Assert.Contains("/admin/proposals", _fixture.Driver.Url);
        }

        [Fact]
        public void AdminDashboard_DisplaysLoggedInEmail()
        {
            EnsureLoggedInAsAdmin();
            _fixture.Driver.Navigate().GoToUrl(_fixture.BaseUrl + "/admin");

            IWebElement emailElement = _wait.Until(driver =>
            {
                try
                {
                    IWebElement element = driver.FindElement(By.XPath($"//p[contains(text(), '{SeleniumConst.DEFAULT_ADMINEMAIL}')]"));
                    return element != null && element.Displayed ? element : null;
                }
                catch
                {
                    return null;
                }
            });

            Assert.NotNull(emailElement);
            Assert.Contains(SeleniumConst.DEFAULT_ADMINEMAIL, emailElement.Text);
        }
    }
}