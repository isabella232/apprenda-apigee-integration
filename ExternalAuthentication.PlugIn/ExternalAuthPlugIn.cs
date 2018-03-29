using Apprenda.SaaSGrid.Users;
using Apprenda.Services.Logging;
using RestSharp;
using System.Collections.Generic;
using System.Linq;

namespace ExternalAuthenticationTesting.PlugIn
{
    public class ExternalAuthPlugIn : AuthenticationProviderBase
    {
        private static readonly ILogger _logger = LogManager.Instance().GetLogger(typeof(ExternalAuthPlugIn));
        private static readonly Settings _settings = new Settings();

        public override bool Test()
        {
            return true;
        }
       
        public override string GetUserIdentifier(IReadOnlyDictionary<string, string> headers)
        {
            //get list of whitelisted URLs from registry
            var RegistryController = new Apprenda.Platform.API.Registry.PlatformRegistryController();
            var whitelist_str = RegistryController.GetSetting("Authentication.ExternalServicesWhitelist", "", false);
            List<string> url_whitelist = whitelist_str.Split(';').Select(p => p.Trim()).ToList();

            //check for required headers and access their data
            var url = "";
            var access_token = "";
            foreach (var key in headers.Keys)
            {
                if (key.ToLower() == "apigeehost")
                {
                    url = string.Format("https://{0}", headers[key]);
                    if (!url_whitelist.Contains(url))
                    {
                        //url is not in whitelist
                        _logger.ErrorFormat("Request denied -- URL provided by header ({0}) is not in External Services Whitelist.", url);
                        return null;
                    }
                }
                if (key.ToLower() == "authorization")
                {
                    access_token = headers[key];
                }
            }
            //either ApigeeHost or Authorization header is not present
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(access_token))
            {
                _logger.Error("Request denied -- missing expected headers.");
                return null;
            }

            //Call back to Apigee proxy to verify email address connected to access token is valid
            var restClient = new RestClient(url);
            var request = new RestRequest("verify", Method.GET);
            request.AddHeader("Authorization", access_token);
            var restResult = restClient.Execute(request);
            try
            {
                string userId = restResult.Headers.ToList()
                .Find(x => x.Name == "Identifier")
                .Value.ToString();
                _logger.DebugFormat("Found User ID belonging to provided token: {0}", userId);
                //return user ID associated with provided token
                return userId;
            }
            catch
            {
                _logger.Error("Can't find header: 'Identifier' in response from verification call.");
            }
            return null;
        }
    }
}
