using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Tenants.Enums;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Tenants.Controllers
{
    /// <summary>
    /// A controller for getting data about users that can authenticate with Wiser.
    /// </summary>
    [Route("api/v3/wiser-tenants")]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly IWiserTenantsService wiserTenantsService;

        /// <summary>
        /// Creates a new instance of <see cref="TenantsController"/>.
        /// </summary>
        public TenantsController(IWiserTenantsService wiserTenantsService)
        {
            this.wiserTenantsService = wiserTenantsService;
        }

        /// <summary>
        /// Method for getting the title of a tenant, to show in the browser tab.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="WiserItemModel"/>, but only with names and IDs.</returns>
        [HttpGet]
        [Route("{subDomain}/title")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTitleAsync(string subDomain)
        {
            return (await wiserTenantsService.GetTitleAsync(subDomain)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Method for getting information about a single tenant.
        /// </summary>
        /// <param name="name">The name of a tenant.</param>
        /// <param name="subDomain">The sub domain for the tenant.</param>
        /// <returns>A <see cref="TenantExistsResults"/>.</returns>
        [HttpGet]
        [Route("{name}/exists")]
        [ProducesResponseType(typeof(TenantExistsResults), StatusCodes.Status200OK)]
        public async Task<IActionResult> Exists(string name, string subDomain)
        {
            return (await wiserTenantsService.TenantExistsAsync(name, subDomain)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Method for creating a new Wiser tenant.
        /// </summary>
        /// <param name="tenant">The tenant to create.</param>
        /// <param name="isWebShop">Is this tenant going to have a web shop?</param>
        /// <param name="isConfigurator">Is this tenant going to have a configurator?</param>
        /// <param name="isMultiLanguage">If the tenant's website going to support multiple languages?</param>
        /// <returns>A <see cref="TenantModel"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> Create(TenantModel tenant, [FromQuery]bool isWebShop = false, [FromQuery]bool isConfigurator = false, [FromQuery]bool isMultiLanguage = false)
        {
            return (await wiserTenantsService.CreateTenantAsync(tenant, isWebShop, isConfigurator, isMultiLanguage)).GetHttpResponseMessage();
        }
    }
}
