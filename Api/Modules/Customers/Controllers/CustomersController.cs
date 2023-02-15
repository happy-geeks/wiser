using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Customers.Enums;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Customers.Controllers
{
    /// <summary>
    /// A controller for getting data about users that can authenticate with Wiser.
    /// </summary>
    [Route("api/v3/wiser-customers")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly IWiserCustomersService wiserCustomersService;

        /// <summary>
        /// Creates a new instance of <see cref="CustomersController"/>.
        /// </summary>
        public CustomersController(IWiserCustomersService wiserCustomersService)
        {
            this.wiserCustomersService = wiserCustomersService;
        }

        /// <summary>
        /// Method for getting the title of a customer, to show in the browser tab.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="WiserItemModel"/>, but only with names and IDs.</returns>
        [HttpGet]
        [Route("{subDomain}/title")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTitleAsync(string subDomain)
        {
            return (await wiserCustomersService.GetTitleAsync(subDomain)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Method for getting information about a single customer.
        /// </summary>
        /// <param name="name">The name of a customer.</param>
        /// <param name="subDomain">The sub domain for the customer.</param>
        /// <returns>A <see cref="CustomerExistsResults"/>.</returns>
        [HttpGet]
        [Route("{name}/exists")]
        [ProducesResponseType(typeof(CustomerExistsResults), StatusCodes.Status200OK)]
        public async Task<IActionResult> Exists(string name, string subDomain)
        {
            return (await wiserCustomersService.CustomerExistsAsync(name, subDomain)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Method for creating a new Wiser customer.
        /// </summary>
        /// <param name="customer">The customer to create.</param>
        /// <param name="isWebShop">Is this customer going to have a web shop?</param>
        /// <param name="isConfigurator">Is this customer going to have a configurator?</param>
        /// <param name="isMultiLanguage">If the customer's website going to support multiple languages?</param>
        /// <returns>A <see cref="CustomerModel"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerModel), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> Create(CustomerModel customer, [FromQuery]bool isWebShop = false, [FromQuery]bool isConfigurator = false, [FromQuery]bool isMultiLanguage = false)
        {
            return (await wiserCustomersService.CreateCustomerAsync(customer, isWebShop, isConfigurator, isMultiLanguage)).GetHttpResponseMessage();
        }
    }
}
