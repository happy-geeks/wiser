using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using Api.Modules.VersionControl.Service;
using Api.Modules.VersionControl.Service.DataLayer;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace WiserTests
{
    [TestClass]
    public class VersionControlServiceTest
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserCustomersService wiserCustomersService;

        private readonly IVersionControlDataService dataService2;


        [TestMethod]
        public void TestMethod1()
        {
            var t = new VersionControlDataService(clientDatabaseConnection, wiserCustomersService);
            int commitId = 1;

            //List<TemplateCommitModel> templateList1 = template.Result.ToList();
            var test = dataService2.GetTemplatesFromCommitAsync(commitId);

            t.GetTemplatesFromCommitAsync(commitId);

            Assert.IsNotNull(test);
            
        }
    }
}