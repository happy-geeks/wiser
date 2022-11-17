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
        driver.Manage().Window.Maximize();
    }

    [TearDown]
    public void TearDown()
    {
        driver.Quit();
    }

    private void WaitTillElementIsFound(By by, IWebElement? element = null)
    {
        var timeoutCount = 0;
        
        while (timeoutCount < testSettings.Timeout)
        {
            try
            {
                if (element == null)
                {
                    driver.FindElement(by);
                }
                else
                {
                    element.FindElement(by);
                }

                return;
            }
            catch { }
            
            Thread.Sleep(100);
            timeoutCount += 100;
        }
    }
    
    private void WaitTillElementIsDisplayed(By by, IWebElement? element = null)
    {
        var timeoutCount = 0;
        
        while (timeoutCount < testSettings.Timeout)
        {
            try
            {
                if (element == null)
                {
                    if (driver.FindElement(by).Displayed) return;
                }
                else
                {
                    if (element.FindElement(by).Displayed) return;
                }
            }
            catch { }
            
            Thread.Sleep(100);
            timeoutCount += 100;
        }
    }

    private void LoginWiserUser()
    {
        WaitTillElementIsFound(By.CssSelector("#loginForm .btn"));
        driver.FindElement(By.Id("username")).SendKeys(testSettings.WiserAccountName);
        driver.FindElement(By.Id("password")).SendKeys(testSettings.WiserAccountPassword);
        driver.FindElement(By.CssSelector("#loginForm .btn")).Click();
        WaitTillElementIsFound(By.ClassName("sub-title"));
    }

    private void Logout()
    {
        var actions = new Actions(driver);
        actions.MoveToElement(driver.FindElement(By.CssSelector(".sub-title"))).Perform();
        WaitTillElementIsDisplayed(By.CssSelector(".sub-menu > li:nth-child(7) span"));
        driver.FindElement(By.CssSelector(".sub-menu > li:nth-child(7) span")).Click();
    }
    
    [Test]
    public void LoginPortal()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            
            LoginWiserUser();
            Assert.That(driver.FindElement(By.ClassName("sub-title")).Text, Is.EqualTo("TestMark"));
            
            Logout();
            Assert.That(driver.FindElements(By.ClassName("sub-title")).Count, Is.EqualTo(0));
            
            // Check if flow can be repeated after logging out.
            LoginWiserUser();
            Assert.That(driver.FindElement(By.ClassName("sub-title")).Text, Is.EqualTo("TestMark"));
            
            Logout();
            Assert.That(driver.FindElements(By.ClassName("sub-title")).Count, Is.EqualTo(0));
        }
    }

    [Test]
    public void WiserItem()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            LoginWiserUser();

            // Open Stamgegevens module.
            WaitTillElementIsFound(By.CssSelector("a[title='Stamgegevens']"));
            driver.FindElement(By.CssSelector("a[title='Stamgegevens']")).Click();
            driver.SwitchTo().Frame(driver.FindElement(By.Id("700_1")));
            
            // Select the correct folder.
            WaitTillElementIsFound(By.ClassName("k-treeview-leaf"));
            foreach (var element in driver.FindElements(By.ClassName("k-treeview-leaf")))
            {
                if (element.FindElement(By.CssSelector(".k-treeview-leaf-text")).Text != "Selenium test folder") continue;
                element.Click();
                break;
            }
            
            // Add a new item.
            WaitTillElementIsFound(By.Id("addButton"));
            driver.FindElement(By.Id("addButton")).Click();
            foreach (var element in driver.FindElements(By.CssSelector("#newItemEntityTypeField_listbox li")))
            {
                // Because the dropdown is reloaded the element will not be in the DOM. Retry in that case.
                try
                {
                    if (element.FindElement(By.ClassName("k-list-item-text")).Text != "SeleniumTestItem") continue;
                    element.Click();
                    break;
                }
                catch
                {
                    foreach (var element2 in driver.FindElements(By.CssSelector("#newItemEntityTypeField_listbox li")))
                    {
                        if (element2.FindElement(By.ClassName("k-list-item-text")).Text != "SeleniumTestItem") continue;
                        element2.Click();
                        break;
                    }

                    break;
                }
            }
            driver.FindElement(By.Id("newItemNameField")).SendKeys("Selenium test item");
            driver.FindElement(By.CssSelector(".k-button-solid-primary")).Click();
            
            // Modify item.
            WaitTillElementIsFound(By.Id("field_2394"));
            driver.FindElement(By.Id("field_2394")).SendKeys("Test value");
            driver.FindElement(By.Id("saveButton")).Click();
            
            // Reopen item.
            foreach (var element in driver.FindElements(By.ClassName("k-treeview-leaf")))
            {
                if (element.FindElement(By.ClassName("k-treeview-leaf-text")).Text != "Selenium test folder") continue;
                element.Click();
                break;
            }
            foreach (var element in driver.FindElements(By.ClassName("k-treeview-leaf")))
            {
                if (element.FindElement(By.CssSelector(".k-treeview-leaf-text")).Text != "Selenium test item") continue;
                element.Click();
                WaitTillElementIsFound(By.Id("field_2394"));
                var a = driver.FindElement(By.Id("field_2394")).GetDomAttribute("value");
                Assert.That(driver.FindElement(By.Id("field_2394")).GetDomAttribute("value"), Is.EqualTo("Test value"));
                
                // Open delete window.
                var actions = new Actions(driver);
                actions.ContextClick(element).Perform();
                break;
            }
            
            // Delete item.
            WaitTillElementIsDisplayed(By.CssSelector(".k-item[action='REMOVE_ITEM'] .k-menu-link-text"));
            driver.FindElement(By.CssSelector(".k-item[action='REMOVE_ITEM'] .k-menu-link-text")).Click();
            WaitTillElementIsDisplayed(By.CssSelector(".delete-button"));
            driver.FindElement(By.CssSelector(".delete-button")).Click();
            Thread.Sleep(1000);
            
            // Close module.
            driver.SwitchTo().DefaultContent();
            driver.FindElement(By.CssSelector(".close-module")).Click();

            Logout();
        }
    }
}