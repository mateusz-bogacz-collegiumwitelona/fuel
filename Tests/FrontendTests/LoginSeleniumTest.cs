using System;
using Xunit;
using Tests.Selenium.Pages;

namespace Tests.FrontendTests
{
    [Collection("Selenium")]
    public class LoginSeleniumTest
    {
        private readonly SeleniumFixture _fixture;
        private readonly string _baseUrl;

        public LoginSeleniumTest(SeleniumFixture fixture)
        {
            _fixture = fixture;
            string? env = Environment.GetEnvironmentVariable("FRONTEND_URL");
            _baseUrl = !string.IsNullOrWhiteSpace(env) ? env : "https://fuelly.com.pl";
        }

        [Fact]
        public void Login_ValidCredentials_RedirectsToApp()
        {
            LoginPage page = new LoginPage(_fixture.Driver, _baseUrl);
            page.GoTo();
            page.EnterEmail("szymon.mikolajek@studenci.collegiumwitelona.pl");
            page.EnterPassword("1Qweasdzxc@");
            page.Submit();

            bool redirectedToList = page.WaitForUrlContains("/dashboard");
            bool redirectedToHome = page.WaitForUrlContains("/", TimeSpan.FromSeconds(5));

            Assert.True(redirectedToList || redirectedToHome, "Expected redirect to application page after successful login.");

           
            page.Logout();
            bool redirectedToLogin = page.WaitForUrlContains("/login", TimeSpan.FromSeconds(5));
            Assert.True(redirectedToLogin, "Expected redirect to /login after logout.");
        }
    }
}