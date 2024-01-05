using FrontEnd.Core.Models;

namespace FrontEnd.Core.Interfaces
{
    public interface IBaseService
    {
        /// <summary>
        /// Gets the current sub domain. Every tenant has their own sub domain for Wiser.
        /// </summary>
        /// <returns>The name of the sub domain.</returns>
        string GetSubDomain();

        /// <summary>
        /// Gets the base URL for Wiser 1.0/2.0, for opening old Wiser modules.
        /// </summary>
        /// <returns>The base URL for old Wiser.</returns>
        string GetWiser1Url();

        /// <summary>
        /// Creates a <see cref="BaseViewModel"/> with default settings.
        /// </summary>
        T CreateBaseViewModel<T>() where T : BaseViewModel;

        /// <summary>
        /// Creates a <see cref="BaseViewModel"/> with default settings.
        /// </summary>
        BaseViewModel CreateBaseViewModel();
    }
}
