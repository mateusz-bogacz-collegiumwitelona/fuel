using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace Tests.Selenium.Pages
{
    public class MapPage
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl;
        private readonly WebDriverWait _wait;

        public MapPage(IWebDriver driver, string baseUrl)
        {
            _driver = driver;
            _baseUrl = baseUrl;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        }

        public void GoTo()
        {
            _driver.Navigate().GoToUrl($"{_baseUrl}/map");
        }

        public bool IsMapContainerVisible()
        {
            try
            {
                IWebElement mapContainer = _wait.Until(ExpectedConditions.ElementExists(
                    By.CssSelector(".leaflet-container, [class*='map']")));
                return mapContainer != null;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public void EnterSearchText(string searchText)
        {
            IWebElement searchInput = _wait.Until(ExpectedConditions.ElementIsVisible(
                By.CssSelector("input[placeholder*='station' i], input[type='text']")));
            searchInput.Clear();
            searchInput.SendKeys(searchText);
        }

        public void ClickSearchButton()
        {
            IWebElement searchButton = _driver.FindElement(
                By.XPath("//button[contains(., 'Search') or contains(., 'Szukaj')]"));
            searchButton.Click();
        }

        public void ClickFirstMarker()
        {
            IWebElement marker = _wait.Until(ExpectedConditions.ElementExists(
                By.CssSelector(".leaflet-marker-icon:not(.leaflet-marker-shadow)")));
            
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].click();", marker);
        }

        public bool IsPopupVisible()
        {
            try
            {
                IWebElement popup = _wait.Until(ExpectedConditions.ElementIsVisible(
                    By.CssSelector(".leaflet-popup, [class*='popup'], [class*='details']")));
                return popup != null;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public bool HasMarkers()
        {
            try
            {
                return _driver.FindElements(By.CssSelector(".leaflet-marker-icon:not(.leaflet-marker-shadow)")).Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}