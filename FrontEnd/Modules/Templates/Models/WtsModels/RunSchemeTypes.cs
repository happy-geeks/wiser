namespace FrontEnd.Modules.Templates.Models.WtsModels
{
    /// <summary>
    /// All types a run scheme can be.
    /// </summary>
    public enum RunSchemeTypes
    {
        /// <summary>
        /// Have a delay between two runs.
        /// </summary>
        Continuous,
        /// <summary>
        /// Runs once a day.
        /// </summary>
        Daily,
        /// <summary>
        /// Runs once a week.
        /// </summary>
        Weekly,
        /// <summary>
        /// Runs once a month.
        /// </summary>
        Monthly
    }
}