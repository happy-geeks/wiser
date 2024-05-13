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

    /// <summary>
    /// Wait till an element can be found or the timeout has been surpassed.
    /// If an element can't be found an exception will be given.
    /// This method ignores it to wait until the element can be found.
    /// </summary>
    /// <param name="by">The <see cref="By"/> selector to use to find the element.</param>
    /// <param name="element">Optional: An <see cref="IWebElement"/> to perform the selector on instead of the <see cref="driver"/>.</param>
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
    
    /// <summary>
    /// Wait till an element is displayed or the timeout has been surpassed.
    /// If an element can't be found an exception will be given and if it is not displayed no action can be performed on it.
    /// This method ignores it to wait until the element can be found and is displayed.
    /// </summary>
    /// <param name="by">The <see cref="By"/> selector to use to find the element.</param>
    /// <param name="element">Optional: An <see cref="IWebElement"/> to perform the selector on instead of the <see cref="driver"/>.</param>
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

    /// <summary>
    /// Login a normal user. Used in all tests.
    /// </summary>
    private void LoginWiserUser()
    {
        WaitTillElementIsFound(By.CssSelector("#loginForm .btn"));
        driver.FindElement(By.Id("username")).SendKeys(testSettings.WiserAccountName);
        driver.FindElement(By.Id("password")).SendKeys(testSettings.WiserAccountPassword);
        driver.FindElement(By.CssSelector("#loginForm .btn")).Click();
        WaitTillElementIsFound(By.ClassName("sub-title"));
    }

    /// <summary>
    /// Logout a user. Used in all tests.
    /// </summary>
    private void Logout()
    {
        var actions = new Actions(driver);
        actions.MoveToElement(driver.FindElement(By.CssSelector(".sub-title"))).Perform();
        WaitTillElementIsDisplayed(By.CssSelector(".sub-menu > li:last-child span"));
        driver.FindElement(By.CssSelector(".sub-menu > li:last-child span")).Click();
        WaitTillElementIsDisplayed(By.Id("loginForm"));
    }
    
    [Test]
    // 1. Login normal user;
    // 2. Logout normal user;
    // 3. Login normal user after logout;
    // 4. Login as Wiser admin user;
    // 5. Select user to login as;
    // 6. Logout.
    public void LoginPortal()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            
            LoginWiserUser();
            Assert.That(driver.FindElement(By.ClassName("sub-title")).Text, Is.EqualTo(testSettings.WiserAccountName));
            
            Logout();
            Assert.That(driver.FindElements(By.ClassName("sub-title")).Count, Is.EqualTo(0));
            
            // Check if flow can be repeated after logging out.
            LoginWiserUser();
            Assert.That(driver.FindElement(By.ClassName("sub-title")).Text, Is.EqualTo(testSettings.WiserAccountName));
            
            Logout();
            Assert.That(driver.FindElements(By.ClassName("sub-title")).Count, Is.EqualTo(0));
            
            // Login as Admin user.
            driver.FindElement(By.Id("username")).SendKeys(testSettings.WiserAdminAccountName);
            driver.FindElement(By.Id("password")).SendKeys(testSettings.WiserAdminAccountPassword);
            driver.FindElement(By.CssSelector("#loginForm .btn-primary")).Click();
            Thread.Sleep(1000); // Wait for a moment because the DOM is being modified and the selectors would get the previous values.
            
            // Select the user to login as.
            driver.FindElement(By.CssSelector("#loginForm input")).Clear();
            driver.FindElement(By.CssSelector("#loginForm input")).SendKeys(testSettings.WiserAccountName);
            driver.FindElement(By.CssSelector("#loginForm input")).SendKeys(Keys.Enter);
            driver.FindElement(By.CssSelector("#loginForm .btn-primary")).Click();
            
            // Validate the admin is logged in as the selected user.
            WaitTillElementIsFound(By.ClassName("sub-title"));
            Assert.That(driver.FindElement(By.ClassName("sub-title")).Text, Is.EqualTo(testSettings.WiserAccountName));
            
            Logout();
            Assert.That(driver.FindElements(By.ClassName("sub-title")).Count, Is.EqualTo(0));
        }
    }

    [Test]
    // 1. Open tree view module;
    // 2. Select folder;
    // 3. Add item;
    // 4. Modify item + save;
    // 5. Reopen item;
    // 6. Delete item;
    // 7. Close tree view module.
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
    
    [Test]
    // 1. Open grid module;
    // 2. Filter grid;
    // 3. Open item;
    // 4. Close item;
    // 5. Reset filter;
    // 6. Close grid module.
    public void WiserGrid()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            LoginWiserUser();

            // Open Wiser user module.
            WaitTillElementIsFound(By.CssSelector("a[title='Gebruikers - Wiser']"));
            driver.FindElement(By.CssSelector("a[title='Gebruikers - Wiser']")).Click();
            driver.SwitchTo().Frame(driver.FindElement(By.Id("806_1")));
            
            // Filter items.
            WaitTillElementIsFound(By.CssSelector("th[data-field=title_wiseruser] .k-icon"));
            driver.FindElement(By.CssSelector("th[data-field=title_wiseruser] .k-icon")).Click();
            WaitTillElementIsDisplayed(By.CssSelector(".k-animation-container input"));
            driver.FindElement(By.CssSelector(".k-animation-container input")).SendKeys("Admin");
            driver.FindElement(By.CssSelector(".k-animation-container button[type=submit]")).Click();

            Thread.Sleep(1000); // Grid is refreshed, when searching directly the found element will be removed and causing an error.
            Assert.That(driver.FindElements(By.CssSelector(".k-grid-content tr")).Count, Is.EqualTo(1));
            
            // Open item.
            WaitTillElementIsDisplayed(By.CssSelector(".k-grid-openDetails"));
            driver.FindElement(By.CssSelector(".k-grid-openDetails")).Click();
            
            // Close item.
            Thread.Sleep(1000); // Window is being opened, when searching element will be found and clicked before the window fully opened causing an error because the click would trigger another item.
            Assert.That(driver.FindElement(By.CssSelector("input[name=username_")).GetDomAttribute("value"), Is.EqualTo("Admin"));
            driver.FindElement(By.CssSelector(".cancelItemPopup.k-button")).Click();
            
            // Reset filter.
            driver.FindElement(By.CssSelector("th[data-field=title_wiseruser] .k-icon")).Click();
            Thread.Sleep(1000); // Popup animation is being played, when searching element will be found and clicked before the popup fully opened causing an error because the click would trigger another item.
            driver.FindElement(By.CssSelector(".k-animation-container button[type=reset]")).Click();
            Thread.Sleep(1000); // Wait a moment for the filter to be saved. If module is closed to quickly the filter will stay active for the user causing a problem during test.
            Assert.That(driver.FindElements(By.CssSelector(".k-grid-content tr")).Count, Is.GreaterThan(1));
            
            // Close module.
            driver.SwitchTo().DefaultContent();
            driver.FindElement(By.CssSelector(".close-module")).Click();

            Logout();
        }
    }

    [Test]
    // 1. Open search module;
    // 2. Search for item;
    // 3. Close search module.
    public void Search()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            LoginWiserUser();

            // Open search module.
            WaitTillElementIsDisplayed(By.CssSelector(".icon-line-search"));
            driver.FindElement(By.CssSelector(".icon-line-search")).Click();
            driver.SwitchTo().Frame(driver.FindElement(By.Id("709_1")));
            
            // Search for item.
            WaitTillElementIsFound(By.CssSelector(".search-container .k-input-inner"));
            driver.FindElement(By.CssSelector(".search-container .k-input-inner")).SendKeys("Wiser gebruiker");
            driver.FindElement(By.Id("search-field")).SendKeys("Admin");
            driver.FindElement(By.Id("search-field")).SendKeys(Keys.Enter);
            
            Thread.Sleep(1000); // Grid is refreshed, when searching directly the results are not yet loaded.
            Assert.That(driver.FindElements(By.CssSelector("#search-grid tr.k-master-row")).Count, Is.EqualTo(1));
            
            // Close search module.
            driver.SwitchTo().DefaultContent();
            driver.FindElement(By.CssSelector(".close-module")).Click();
            
            Logout();
        }
    }

    [Test]
    // 1. Open the Wiser configuration module;
    // 2. Open popup to open item by ID;
    // 3. Search for item;
    // 4. Close item window.
    public void OpenWiserItemById()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            LoginWiserUser();

            // Open popup to open an item by ID.
            var actions = new Actions(driver);
            actions.MoveToElement(driver.FindElement(By.CssSelector(".sub-title"))).Perform();
            WaitTillElementIsDisplayed(By.CssSelector(".sub-menu > li:nth-child(2)"));
            driver.FindElement(By.CssSelector(".sub-menu > li:nth-child(2)")).Click();
            driver.SwitchTo().Frame(driver.FindElement(By.Id("0_1")));
            driver.FindElement(By.CssSelector("a.group-item[data-action='OpenWiserIdPrompt']")).Click();
            
            // Open the item with the ID of the admin user.
            driver.SwitchTo().DefaultContent();
            driver.FindElement(By.Id("wiserId")).SendKeys("51111");
            driver.FindElement(By.CssSelector(".btn-primary")).Click();
            WaitTillElementIsFound(By.Id("wiserItem_51111_wiseruser_1"));
            driver.SwitchTo().Frame(driver.FindElement(By.Id("wiserItem_51111_wiseruser_1")));
            WaitTillElementIsDisplayed(By.Id("field_734"));
            Assert.That(driver.FindElement(By.Id("field_734")).GetDomAttribute("value"), Is.EqualTo("Admin"));
            
            // Close the opened item.
            driver.SwitchTo().DefaultContent();
            driver.FindElement(By.CssSelector(".close-module")).Click();
            
            Logout();
        }
    }

    [Test]
    // 1. Open data selector module;
    // 2. Load data selector;
    // 3. Get results of data selector;
    // 4. Close data selector module.
    public void DataSelector()
    {
        foreach (var url in testSettings.TestUrls)
        {
            driver.Navigate().GoToUrl(url);
            LoginWiserUser();

            // Open data selector module.
            WaitTillElementIsFound(By.CssSelector("a[title='Dataselector']"));
            driver.FindElement(By.CssSelector("a[title='Dataselector']")).Click();
            driver.SwitchTo().Frame(driver.FindElement(By.Id("706_1")));
            
            // Load data selector.
            driver.FindElement(By.Id("loadButton")).Click();
            WaitTillElementIsDisplayed(By.CssSelector(".k-dialog .k-input-value-text"));
            Thread.Sleep(1000); // Options are shown by animation, when clicking to soon focus will be lost and no value can be entered causing the test to fail.
            driver.FindElement(By.CssSelector(".k-dialog .k-input-value-text")).Click();
            WaitTillElementIsDisplayed(By.CssSelector(".k-animation-container .k-input-inner"));
            driver.FindElement(By.CssSelector(".k-animation-container .k-input-inner")).SendKeys("Alle users in Wiser");
            Thread.Sleep(1000); // Wait a moment for the filter to give the results.
            WaitTillElementIsDisplayed(By.CssSelector("#dataSelectorItems-list li"));
            driver.FindElement(By.CssSelector("#dataSelectorItems-list li")).Click();
            driver.FindElement(By.CssSelector(".k-actions button")).Click();
            
            // Open results.
            Thread.Sleep(1000); // Data selector is reloaded, wait a moment to prevent the previous data selector to be used.
            WaitTillElementIsDisplayed(By.Id("viewJsonResultButton"));
            driver.FindElement(By.Id("viewJsonResultButton")).Click();
            WaitTillElementIsFound(By.CssSelector("#viewResult .CodeMirror-code .CodeMirror-line"));
            Assert.That(driver.FindElements(By.CssSelector("#viewResult .CodeMirror-code .CodeMirror-line")).Count, Is.GreaterThan(1));
            
            // Close data selector.
            driver.SwitchTo().DefaultContent();
            driver.FindElement(By.CssSelector(".close-module")).Click();
            
            Logout();
        }
    }
}