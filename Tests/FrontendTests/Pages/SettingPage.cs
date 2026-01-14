
using OpenQA.Selenium;

namespace Tests.FrontendTests.Pages
{
    public class SettingsPage
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl;

        public SettingsPage(IWebDriver driver, string baseUrl)
        {
            _driver = driver;
            _baseUrl = baseUrl;
        }

        public void GoTo()
        {
            _driver.Navigate().GoToUrl($"{_baseUrl}/settings");
        }

        public void SetUsername(string username)
        {
            var usernameInput = _driver.FindElement(By.Id("username"));
            usernameInput.Clear();
            usernameInput.SendKeys(username);
        }

        public void SaveChanges()
        {
            var saveButton = _driver.FindElement(By.Id("save-button"));
            saveButton.Click();
        }

        public bool IsSuccessMessageVisible()
        {
            try
            {
                var successMessage = _driver.FindElement(By.Id("success-message"));
                return successMessage.Displayed;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public string GetDisplayedUsername()
        {
            var usernameElement = _driver.FindElement(By.Id("username"));
            var value = usernameElement.GetAttribute("value");
            return string.IsNullOrEmpty(value) ? usernameElement.Text : value;
        }
    }
}