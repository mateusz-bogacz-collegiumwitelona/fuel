using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Tests.Selenium.Pages;

namespace Tests.FrontendTests
{
    public class SeleniumFixture : IDisposable
    {
        public IWebDriver Driver { get; }
        public string BaseUrl { get; }

        public SeleniumFixture()
        {

            BaseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "https://localhost";

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--allow-insecure-localhost");

            string gridUrl = Environment.GetEnvironmentVariable("SELENIUM_GRID_URL");

            if (!string.IsNullOrEmpty(gridUrl))
            {
                Driver = new RemoteWebDriver(new Uri(gridUrl), options);
            }
            else
            {
                string headless = Environment.GetEnvironmentVariable("SELENIUM_HEADLESS");
                if (headless == "1")
                {
                    options.AddArgument("--headless=new");
                }

                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.SuppressInitialDiagnosticInformation = true;
                service.HideCommandPromptWindow = true;

                Driver = new ChromeDriver(service, options);
            }

            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            if (string.IsNullOrEmpty(gridUrl) && Environment.GetEnvironmentVariable("SELENIUM_HEADLESS") != "1")
            {
                Driver.Manage().Window.Maximize();
            }
        }

        public void Dispose()
        {
            try
            {
                Driver.Quit();
            }
            catch
            {
                //
            }
            Driver.Dispose();
        }

        public void EnsureLoggedIn(string? email = null, string? password = null)
        {
            string baseUrl = BaseUrl;

            string user = email ?? SeleniumConst.DEFAULT_EMAIL;
            string pass = password ?? SeleniumConst.DEFAULT_PASSWORD;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass)) return;

            try
            {
                var js = (IJavaScriptExecutor)Driver;

                if (Driver.Url.StartsWith("data:") || Driver.Url == "about:blank")Driver.Navigate().GoToUrl(baseUrl);

                var script = @"
                    var callback = arguments[arguments.length - 1];
                    fetch('" + baseUrl + @"/api/me', { credentials: 'include' })
                        .then(function(r) { callback(r.ok); })
                        .catch(function() { callback(false); });
                ";
                var isAuthObj = js.ExecuteAsyncScript(script);
                bool isAuth = isAuthObj is bool b && b;

                if (isAuth) return;


                var loginPage = new LoginPage(Driver, baseUrl);
                loginPage.GoTo();
                loginPage.Login(user, pass);


                if (!loginPage.WaitForUrlContains("/dashboard", TimeSpan.FromSeconds(10)))
                {
                    loginPage.WaitForUrlContains("/", TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"B³¹d w EnsureLoggedIn: {ex.Message}");
            }
        }
    }
}