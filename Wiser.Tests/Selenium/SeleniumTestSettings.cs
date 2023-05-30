namespace Wiser.Tests.Selenium;

public class SeleniumTestSettings
{
    /// <summary>
    /// Gets or sets the milliseconds to attempt waiting for elements before a timeout.
    /// </summary>
    public int Timeout { get; set; } = 5000;
    
    /// <summary>
    /// Gets or sets the user name of a normal Wiser user.
    /// </summary>
    public string WiserAccountName { get; set; }

    /// <summary>
    /// Gets or sets the password of a normal Wiser user.
    /// </summary>
    public string WiserAccountPassword { get; set; }

    /// <summary>
    /// Gets or sets the user name of an admin Wiser user.
    /// </summary>
    public string WiserAdminAccountName { get; set; }

    /// <summary>
    /// Gets or sets the password of an admin Wiser user.
    /// </summary>
    public string WiserAdminAccountPassword { get; set; }
    
    /// <summary>
    /// Gets or sets the URLs to test. Must be the same customer but can be used to test multiple servers.
    /// </summary>
    public List<string> TestUrls { get; set; }
}