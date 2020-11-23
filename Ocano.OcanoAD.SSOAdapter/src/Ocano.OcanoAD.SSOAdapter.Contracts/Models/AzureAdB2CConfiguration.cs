using System.Collections.Generic;

namespace Ocano.OcanoAD.SSOAdapter.Contracts.Models
{
    public class AzureAdB2CConfiguration
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string Domain { get; set; }
        public IEnumerable<string> AppScopes { get; set; }
        public string ExtensionsAppId { get; set; }
        public int TemporaryPasswordMinimumLength { get; set; }
    }
}
