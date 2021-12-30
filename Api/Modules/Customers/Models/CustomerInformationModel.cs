namespace Api.Modules.Customers.Models
{
    public class CustomerInformationModel
    {
        public string encryptedCustomerId { get; set; }

        public string encryptedUserId { get; set; }

        public string username { get; set; }

        public string userEmailAddress { get; set; }

        public string userType { get; set; }

        public bool isTest { get; set; }

        public string subDomain { get; set; }

        public bool isForExport { get; set; }
    }
}