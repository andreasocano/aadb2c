using Microsoft.Graph;
using Microsoft.Identity.Client;
using Ocano.OcanoAD.SSOAdapter.Contracts.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocano.OcanoAD.SSOAdapter.Core.Providers
{
    public class ClientCredentialsProvider : IAuthenticationProvider
    {
        private readonly AzureAdB2CConfiguration _configuration;
        private readonly IConfidentialClientApplication _confidentialClientApplication;

        public ClientCredentialsProvider(IConfidentialClientApplication confidentialClientApplication, AzureAdB2CConfiguration configuration)
        {
            _confidentialClientApplication = confidentialClientApplication;
            _configuration = configuration;
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            try
            {
                var result = await _confidentialClientApplication.AcquireTokenForClient(_configuration.AppScopes).ExecuteAsync();

                var authorizationHeader = result.CreateAuthorizationHeader();
                request.Headers.Add(Constants.HeaderName.Authorization, authorizationHeader);
            }
            catch (Exception ex)
            {
                throw ex; //Throw an exception as a token is paramount to our adapter communicating confidentially with AADB2C.
            }
        }
    }
}
