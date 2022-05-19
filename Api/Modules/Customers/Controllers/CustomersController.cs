using System.Collections.Generic;
using System.Security.Claims;
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
        /// <returns>A <see cref="CustomerModel"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerModel), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> Create(CustomerModel customer, [FromQuery]bool isWebShop = false, [FromQuery]bool isConfigurator = false)
        {
            return (await wiserCustomersService.CreateCustomerAsync(customer, isWebShop, isConfigurator)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a new environment for the authenticated customer.
        /// This will create a new database schema on the same server/cluster and then fill it with part of the data from the original database.
        /// </summary>
        /// <param name="name">The name of the environment</param>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerModel), StatusCodes.Status200OK)]
        [Authorize]
        [Route("create-new-environment/{name}")]
        public async Task<IActionResult> CreateNewEnvironmentAsync(string name)
        {
            return (await wiserCustomersService.CreateNewEnvironmentAsync((ClaimsIdentity)User.Identity, name)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the environments for the authenticated user.
        /// </summary>
        /// <returns>A list of <see cref="CustomerModel"/>.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<CustomerModel>), StatusCodes.Status200OK)]
        [Authorize]
        [Route("environments")]
        public async Task<IActionResult> GetEnvironmentsAsync()
        {
            return (await wiserCustomersService.GetEnvironmentsAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Synchronise all changes done to wiser items, from a specific environment, to the production environment.
        /// </summary>
        /// <param name="id">The ID of the environment to copy the changes from.</param>
        [HttpPost]
        [ProducesResponseType(typeof(SynchroniseChangesToProductionResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize]
        [Route("synchronise-changes/{id:int}")]
        public async Task<IActionResult> SynchroniseChangesToProductionAsync(int id)
        {
            return (await wiserCustomersService.SynchroniseChangesToProductionAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }
    }
}
