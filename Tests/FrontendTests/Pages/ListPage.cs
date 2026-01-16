using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace Tests.Selenium.Pages
{
    public class ListPage
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl;
        private readonly WebDriverWait _wait;

        public ListPage(IWebDriver driver, string baseUrl)
        {
            _driver = driver;
            _baseUrl = (baseUrl ?? "https://fuelly.com.pl").TrimEnd('/');
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        }

        public void GoTo()
            => _driver.Navigate().GoToUrl(_baseUrl + "/list");

        public void ToggleFilters()
        {
            WebDriverWait extendedWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

            IWebElement toggle = extendedWait.Until(driver =>
            {
                try
                {
                    IWebElement btn = driver.FindElement(By.XPath("//button[@aria-expanded and contains(@class, 'btn-outline')]"));
                    if (btn != null && btn.Displayed && btn.Enabled)
                    {
                        return btn;
                    }
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            });

            System.Threading.Thread.Sleep(300);
            toggle.Click();

            extendedWait.Until(driver =>
            {
                try
                {
                    string ariaExpanded = toggle.GetAttribute("aria-expanded");
                    return ariaExpanded == "true";
                }
                catch
                {
                    return false;
                }
            });

            System.Threading.Thread.Sleep(500);
        }

        public void ClickSearchInFilters()
        {
            WebDriverWait extendedWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

            IWebElement searchBtn = extendedWait.Until(driver =>
            {
                try
                {
                    IWebElement filterSection = driver.FindElement(By.XPath("//div[contains(@class, 'bg-base-300') and contains(@class, 'rounded-xl') and contains(@class, 'p-6')]"));
                    if (filterSection == null || !filterSection.Displayed)
                    {
                        return null;
                    }

                    IWebElement btn = filterSection.FindElement(By.XPath(".//button[contains(@class, 'btn-primary') and contains(@class, 'w-full')]"));
                    if (btn != null && btn.Displayed && btn.Enabled)
                    {
                        return btn;
                    }
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            });

            System.Threading.Thread.Sleep(300);
            searchBtn.Click();
            System.Threading.Thread.Sleep(1000);
        }

        public void EnterInvalidSearchCriteria()
        {
            try
            {
                IWebElement cityInput = _wait.Until(driver =>
                {
                    try
                    {
                        IWebElement input = driver.FindElement(By.CssSelector("input[placeholder*='miasto' i], input[placeholder*='city' i], input[name='city']"));
                        return input != null && input.Displayed && input.Enabled ? input : null;
                    }
                    catch
                    {
                        return null;
                    }
                });

                if (cityInput != null)
                {
                    cityInput.Clear();
                    cityInput.SendKeys("NonExistentCity123456789XYZ");
                }
            }
            catch (WebDriverTimeoutException)
            {
                
                try
                {
                    IWebElement input = _driver.FindElement(By.CssSelector("input[type='text']"));
                    if (input != null && input.Displayed && input.Enabled)
                    {
                        input.Clear();
                        input.SendKeys("NonExistentValue123456789XYZ");
                    }
                }
                catch
                {
                   
                }
            }
        }

        public bool IsTableVisible()
        {
            try
            {
                WebDriverWait shortWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                IWebElement table = shortWait.Until(driver =>
                {
                    try
                    {
                        IWebElement tbl = driver.FindElement(By.CssSelector("table.table-zebra, table.table"));
                        return tbl != null && tbl.Displayed ? tbl : null;
                    }
                    catch
                    {
                        return null;
                    }
                });
                return table != null;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public bool IsNoStationsMessageVisible()
        {
            try
            {
                WebDriverWait shortWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                IWebElement el = shortWait.Until(driver =>
                {
                    try
                    {
                        IWebElement msg = driver.FindElement(By.XPath("//div[contains(@class, 'text-center') and contains(@class, 'py-8') and contains(@class, 'text-gray-400')]"));
                        return msg != null && msg.Displayed && !string.IsNullOrWhiteSpace(msg.Text) ? msg : null;
                    }
                    catch
                    {
                        return null;
                    }
                });
                return el != null;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
    }
}