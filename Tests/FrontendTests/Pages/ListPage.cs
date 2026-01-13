using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Tests.Selenium.Pages
{
    public class ListPage
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        public ListPage(IWebDriver driver, string baseUrl)
        {
            _driver = driver;
            _baseUrl = (baseUrl ?? "https://fuelly.com.pl").TrimEnd('/');
        }

        public void GoTo()
        {
            _driver.Navigate().GoToUrl(_baseUrl + "/list");
        }

        public void ToggleFilters()
        {
            IWebElement toggle = WaitUntilClickable(By.CssSelector("button[aria-expanded], button.btn.btn-outline"));
            toggle.Click();
        }

        public void ClickSearchInFilters()
        {
            IWebElement searchBtn = WaitUntilClickable(By.CssSelector("button.btn.btn-primary.w-full, button.btn.btn-primary"));
            searchBtn.Click();
        }

        public bool IsTableVisible()
        {
            try
            {
                IWebElement table = WaitUntilVisible(By.CssSelector("table.table-zebra, table"));
                return table != null;
            }
            catch
            {
                return false;
            }
        }

        public bool IsNoStationsMessageVisible()
        {
            try
            {
                IWebElement el = WaitUntilVisible(By.CssSelector("div.text-center.py-8, .text-center.py-8, .text-gray-400, .text-center"));
                return el != null;
            }
            catch
            {
                return false;
            }
        }

        private IWebElement WaitUntilVisible(By by)
        {
            WebDriverWait wait = new WebDriverWait(_driver, _timeout);
            return wait.Until(driver =>
            {
                try
                {
                    IWebElement el = driver.FindElement(by);
                    return el.Displayed ? el : null;
                }
                catch
                {
                    return null;
                }
            });
        }

        private IWebElement WaitUntilClickable(By by)
        {
            WebDriverWait wait = new WebDriverWait(_driver, _timeout);
            return wait.Until(driver =>
            {
                try
                {
                    IWebElement el = driver.FindElement(by);
                    return (el.Displayed && el.Enabled) ? el : null;
                }
                catch
                {
                    return null;
                }
            });
        }
    }
}