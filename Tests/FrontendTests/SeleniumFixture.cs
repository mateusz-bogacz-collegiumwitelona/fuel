using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Tests.Selenium.Pages;

namespace Tests.Selenium
{
    public class SeleniumFixture : IDisposable
    {
        public ChromeDriver Driver { get; }

        public SeleniumFixture()
        {
            ChromeOptions options = new ChromeOptions();

            string headless = Environment.GetEnvironmentVariable("SELENIUM_HEADLESS");
            if (headless == "1")
            {
                options.AddArgument("--headless=new");
            }

            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            Driver = new ChromeDriver(service, options);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        public void Dispose()
        {
            try
            {
                Driver.Quit();
            }
            catch
            {
           
            }

            Driver.Dispose();
        }

     
        public void EnsureLoggedIn(string? email = null, string? password = null)
        {
            string baseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "https://fuelly.com.pl";

            
            string defaultEmail = "szymon.mikolajek@studenci.collegiumwitelona.pl";
            string defaultPassword = "1Qweasdzxc@";

            string user = email ?? defaultEmail;
            string pass = password ?? defaultPassword;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
               
                return;
            }

            try
            {
                var js = (IJavaScriptExecutor)Driver;
               
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
            catch
            {
                
            }
        }
    }
}