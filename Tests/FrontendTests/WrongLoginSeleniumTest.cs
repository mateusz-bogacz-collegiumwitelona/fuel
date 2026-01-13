using System;
using Xunit;
using Tests.Selenium.Pages;

namespace Tests.Selenium
{
    [Collection("Selenium")]
    public class WrongLoginSeleniumTest
    {
        private readonly SeleniumFixture _fixture;
        private readonly string _baseUrl;

        public WrongLoginSeleniumTest(SeleniumFixture fixture)
        {
            _fixture = fixture;
            string? env = Environment.GetEnvironmentVariable("FRONTEND_URL");
            _baseUrl = !string.IsNullOrWhiteSpace(env) ? env : "https://fuelly.com.pl";
        }

        [Fact]
        public void Login_InvalidCredentials_ShowsErrorMessage()
        {
            LoginPage page = new LoginPage(_fixture.Driver, _baseUrl);
            page.GoTo();
            page.EnterEmail("nonexistent@example.test");
            page.EnterPassword("wrongpassword");
            page.Submit();

            string? msg = page.GetMessageText();
            Assert.False(string.IsNullOrEmpty(msg), "Expected error/feedback message after submitting invalid credentials.");
        }
    }
}
