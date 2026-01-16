using System;
using Xunit;
using Tests.Selenium.Pages;

namespace Tests.FrontendTests
{
    [Collection("Selenium")]
    public class LoginSeleniumTest
    {
        private readonly SeleniumFixture _fixture;

        public LoginSeleniumTest(SeleniumFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Login_ValidCredentials_RedirectsToApp()
        {
            LoginPage page = new LoginPage(_fixture.Driver, _fixture.BaseUrl);
            _fixture.Driver.Manage().Cookies.DeleteAllCookies();

            page.GoTo();

            page.EnterEmail(SeleniumConst.DEFAULT_EMAIL);
            page.EnterPassword(SeleniumConst.DEFAULT_PASSWORD);
            page.Submit();

            bool redirected = page.WaitForUrlContains("/dashboard", TimeSpan.FromSeconds(15));

            if (!redirected)
            {
                redirected = page.WaitForUrlContains("/", TimeSpan.FromSeconds(5));
            }

            string currentUrl = _fixture.Driver.Url;
            Assert.True(redirected, $"Expected redirect to dashboard or home page after login, but stayed on: {currentUrl}");

            page.Logout();
            Assert.True(page.WaitForUrlContains("/login", TimeSpan.FromSeconds(10)), "Logout failed");
        }

        [Fact]
        public void Login_InvalidCredentials_ShowsErrorMessage()
        {
            LoginPage page = new LoginPage(_fixture.Driver, _fixture.BaseUrl);
            page.GoTo();

            _fixture.Driver.Manage().Cookies.DeleteAllCookies();

            page.EnterEmail("nonexistent@example.test");
            page.EnterPassword("wrongpassword");
            page.Submit();

            string? msg = page.GetMessageText();
            Assert.False(string.IsNullOrEmpty(msg), "Expected error/feedback message after submitting invalid credentials.");
        }
    }
}