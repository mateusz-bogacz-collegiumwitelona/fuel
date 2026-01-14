using Tests.FrontendTests.Pages;
using Tests.Selenium.Pages;
using Xunit;

namespace Tests.FrontendTests
{
    [Collection("Selenium collection")]
    public class SettingsSeleniumTest
    {
        private readonly SeleniumFixture _fixture;
        private readonly SettingsPage _settingsPage;
        private readonly string _baseUrl;

        public SettingsSeleniumTest(SeleniumFixture fixture)
        {
            _fixture = fixture;
            _settingsPage = new SettingsPage(_fixture.Driver, _baseUrl);
        }

        [Fact]
        public void Settings_CanChangeUsername()
        {
            var loginPage = new LoginPage(_fixture.Driver, _baseUrl);
            loginPage.GoTo();
            loginPage.EnterEmail("szymon.mikolajek@studenci.collegiumwitelona.pl");
            loginPage.EnterPassword("1Qweasdzxc@");
            loginPage.Submit();

            _settingsPage.GoTo();

            const string newUsername = "NowaNazwaUzytkownika";

            _settingsPage.SetUsername(newUsername);
            _settingsPage.SaveChanges();

            Assert.True(_settingsPage.IsSuccessMessageVisible());
            Assert.Equal(newUsername, _settingsPage.GetDisplayedUsername());
        }
    }
}