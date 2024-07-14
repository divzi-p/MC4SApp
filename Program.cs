using MC4SApp;
using Microsoft.Identity.Client;  // Microsoft Authentication Library (MSAL)
using System.Net.Http.Headers;
using System.Text.Json;

namespace MC4SApp
{
class Program
{
static async Task Main()
{
// TODO Specify the Dataverse environment name to connect with.
string resource = "https://<<>>.api.crm.dynamics.com";
// TODO Specify the Microsoft Entra ID app registration id.
var clientId = "";
var tenantId="";
var clientSecret = "";
var redirectUri = "http://localhost"; // Loopback for the interactive login.

#region Authentication

var authBuilder = ConfidentialClientApplicationBuilder.Create(clientId)
                            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                            .WithRedirectUri(redirectUri)
                            .WithClientSecret(clientSecret)
                            .Build();
            var scope = resource + "/.default";
            string[] scopes = { scope };

            AuthenticationResult token =
                authBuilder.AcquireTokenForClient(scopes).ExecuteAsync().Result;
            #endregion Authentication

            #region Client configuration

            var client = new HttpClient
            {
                // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#web-api-url-and-versions
                BaseAddress = new Uri(resource + "/api/data/v9.2/"),
                Timeout = new TimeSpan(0, 2, 0)    // Standard two minute timeout on web service calls.
            };

            // Default headers for each Web API call.
            // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#http-headers
            HttpRequestHeaders headers = client.DefaultRequestHeaders;
            headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            headers.Add("OData-MaxVersion", "4.0");
            headers.Add("OData-Version", "4.0");
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            #endregion Client configuration

            #region Web API call

            var response = await client.GetAsync("msdyn_emissions?$top=10");

            if (response.IsSuccessStatusCode)
            {
                using (var stream = response.Content.ReadAsStreamAsync())
                {
                    var result = await JsonSerializer.DeserializeAsync<DataverseQueryResult<Emission>>(await stream)!;
                    await foreach (var emission in result!.value!)
                        Console.WriteLine($"{emission.msdyn_activityname} activity on {emission.msdyn_transactiondate} emitted {emission.msdyn_co2e} CO2 Equivalent");
                }
            }
            else
                Console.WriteLine($"Web API call failed with reason {response.ReasonPhrase}, {response.ToString()}");
            #endregion Web API call

            Console.ReadKey();
        }
    }
    public class DataverseQueryResult<T>
    {
        public IAsyncEnumerable<T> value { get; set; }
    }
    public class Emission
    {
        public string? msdyn_activityname { get; set; }
        public DateTime msdyn_transactiondate { get; set; }
        public decimal msdyn_co2e { get; set; }
    }
}