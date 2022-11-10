using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;

namespace Wiser.Tests.Selenium;

public class Tests
{
    private IWebDriver driver;
    private SeleniumTestSettings testSettings;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // First set the base settings for the application.
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);

        // We need to build here already, so that we can read the base directory for secrets.
        var configuration = builder.Build();

        // Get the base directory for secrets and then load the secrets file from that directory.
        var secretsBasePath = configuration.GetSection("GCL").GetValue<string>("SecretsBaseDirectory");
        builder.AddJsonFile($"{secretsBasePath}appsettings-tests-secrets.json", true, false);

        // Build the final configuration with all combined settings.
        configuration = builder.Build();

        // Get the test settings for the Selenium tests from the app settings.
        testSettings = new SeleniumTestSettings();
        configuration.GetSection("SeleniumTestSettings").Bind(testSettings);
    }
    
    [SetUp]
    public void SetUp()
    {
        driver = new EdgeDriver();
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(5000);
        driver.Manage().Window.Maximize();
    }

    [TearDown]
    public void TearDown()
    {
        driver.Quit();
    }

    private void WaitTillElementFound(By by)
    {
        var timeoutCount = 0;
        
        while (timeoutCount < testSettings.Timeout)
        {
            try
            {
                driver.FindElement(by);
                return;
            }
            catch { }
            
            Thread.Sleep(100);
            timeoutCount += 100;
        }
    }
    
    private void WaitTillElementDisplayed(By by)
    {
        var timeoutCount = 0;
        
        while (timeoutCount < testSettings.Timeout)
        {
            try
            {
                if(driver.FindElement(by).Displayed) return;
            }
            catch { }
            
            Thread.Sleep(100);
            timeoutCount += 100;
        }
    }

    private void LoginWiserUser()
    {
        driver.FindElement(By.Id("username")).SendKeys(testSettings.WiserAccountName);
        driver.FindElement(By.Id("password")).SendKeys(testSettings.WiserAccountPassword);
        WaitTillElementFound(By.CssSelector("#loginForm .btn"));
        driver.FindElement(By.CssSelector("#loginForm .btn")).Click();
    }

    private void Logout()
    {
        var actions = new Actions(driver);
        actions.MoveToElement(driver.FindElement(By.CssSelector(".sub-title"))).Perform();
        WaitTillElementDisplayed(By.CssSelector(".sub-menu > li:nth-child(7) span"));
        driver.FindElement(By.CssSelector(".sub-menu > li:nth-child(7) span")).Click();
    }
    
    [Test]
    public void LoginPortal()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            
            LoginWiserUser();
            Logout();
        }
    }
}