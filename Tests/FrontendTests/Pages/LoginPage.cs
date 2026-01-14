using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Tests.Selenium.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        public LoginPage(IWebDriver driver, string baseUrl)
        {
            _driver = driver;
            _baseUrl = (baseUrl ?? "https://fuelly.com.pl").TrimEnd('/');
        }

        public void GoTo()
        {
            _driver.Navigate().GoToUrl(_baseUrl + "/login");
        }

        public void EnterEmail(string email)
        {
            WebDriverWait wait = new WebDriverWait(_driver, _timeout);
            wait.Until(driver =>
            {
                try
                {
                    IWebElement el = driver.FindElement(By.CssSelector("input[type='email']"));
                    if (el.Displayed)
                    {
                        el.Clear();
                        el.SendKeys(email);
                        return true;
                    }
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
                catch
                {
                    return false;
                }

                return false;
            });
        }

        public void EnterPassword(string password)
        {
            WebDriverWait wait = new WebDriverWait(_driver, _timeout);
            wait.Until(driver =>
            {
                try
                {
                    IWebElement el = driver.FindElement(By.CssSelector("input[type='password']"));
                    if (el.Displayed)
                    {
                        el.Clear();
                        el.SendKeys(password);
                        return true;
                    }
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
                catch
                {
                    return false;
                }

                return false;
            });
        }

        public void Submit()
        {
            WebDriverWait wait = new WebDriverWait(_driver, _timeout);
            wait.Until(driver =>
            {
                try
                {
                    IWebElement button = driver.FindElement(By.CssSelector("button[type='submit']"));
                    if (button.Displayed && button.Enabled)
                    {
                        button.Click();
                        return true;
                    }
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
                catch
                {
                    return false;
                }

                return false;
            });
        }


        public void Login(string email, string password)
        {
            EnterEmail(email);
            EnterPassword(password);
            Submit();
        }

        public string? GetMessageText()
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(_driver, _timeout);
                return wait.Until(driver =>
                {
                    try
                    {
                        IWebElement message = driver.FindElement(By.CssSelector("form p, .text-center.text-sm, .alert, .text-sm.text-gray-400"));
                        return message.Displayed ? message.Text : null;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return null;
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
            catch
            {
                return null;
            }
        }

        public bool WaitForUrlContains(string fragment, TimeSpan? timeout = null)
        {
            var waitTime = timeout ?? _timeout;
            try
            {
                var wait = new WebDriverWait(_driver, waitTime);
                return wait.Until(d => d.Url != null && d.Url.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }


        public void Logout()
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(_driver, _timeout);


                bool opened = wait.Until(driver =>
                {
                    try
                    {
                        IWebElement toggle = driver.FindElement(By.CssSelector("label[aria-haspopup='true'], .btn.btn-ghost.btn-square, button[aria-haspopup='true']"));
                        if (toggle.Displayed && toggle.Enabled)
                        {
                            toggle.Click();
                            return true;
                        }
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                    catch
                    {
                        return false;
                    }

                    return false;
                });

                if (opened)
                {

                    bool clicked = wait.Until(driver =>
                    {
                        try
                        {
                            IWebElement logoutButton = driver.FindElement(By.XPath("//button[normalize-space(.)='Wyloguj' or normalize-space(.)='Logout']"));
                            if (logoutButton.Displayed && logoutButton.Enabled)
                            {
                                logoutButton.Click();
                                return true;
                            }
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                        catch
                        {
                            return false;
                        }

                        return false;
                    });

                    if (clicked)
                    {

                        WaitForUrlContains("/login", TimeSpan.FromSeconds(5));
                        return;
                    }
                }
            }
            catch
            {

            }


            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                const string script = @"
                    try {
                        fetch('/api/logout', { method: 'POST', credentials: 'include' })
                            .catch(function(){});
                    } catch(e) {}
                    try { localStorage.removeItem('token'); } catch(e) {}
                    try { localStorage.removeItem('token_expiration'); } catch(e) {}
                    try { localStorage.removeItem('role'); } catch(e) {}
                    try { localStorage.removeItem('email'); } catch(e) {}
                ";
                js.ExecuteScript(script);
                WaitForUrlContains("/login", TimeSpan.FromSeconds(5));
            }
            catch
            {

            }
        }
    }
}