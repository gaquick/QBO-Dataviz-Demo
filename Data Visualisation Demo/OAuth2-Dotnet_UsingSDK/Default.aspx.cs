

/******************************************************
 * Intuit sample app for Oauth2 using Intuit .Net SDK
 * RFC docs- https://tools.ietf.org/html/rfc6749
 * ****************************************************/

//https://stackoverflow.com/questions/23562044/window-opener-is-undefined-on-internet-explorer/26359243#26359243
//IE issue- https://stackoverflow.com/questions/7648231/javascript-issue-in-ie-with-window-opener

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Web.UI;
using System.Configuration;
using System.Web;
using Intuit.Ipp.OAuth2PlatformClient;
using System.Threading.Tasks;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.LinqExtender;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Intuit.Ipp.Exception;
using System.Linq;
using Intuit.Ipp.ReportService;

namespace OAuth2_Dotnet_UsingSDK
{

    public partial class Default : System.Web.UI.Page
    {
        // OAuth2 client configuration
        static string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
        static string clientID = ConfigurationManager.AppSettings["clientID"];
        static string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
        static string logPath = ConfigurationManager.AppSettings["logPath"];
        static string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];
        static OAuth2Client oauthClient = new OAuth2Client(clientID, clientSecret, redirectURI, appEnvironment);
        static string authCode;
        static string idToken;
        public static IList<JsonWebKey> keys;
        public static Dictionary<string, string> dictionary = new Dictionary<string, string>();
        private IList<Payment> payments;


        protected async void Page_Load(object sender, EventArgs e)
        {
            AsyncMode = true;
            if (!dictionary.ContainsKey("accessToken"))
            {
                if (Request.QueryString.Count > 0)
                {
                    var response = new AuthorizeResponse(Request.QueryString.ToString());
                    if (response.State != null)
                    {
                        if (oauthClient.CSRFToken == response.State)
                        {
                            if (response.RealmId != null)
                            {
                                if (!dictionary.ContainsKey("realmId"))
                                {
                                    dictionary.Add("realmId", response.RealmId);
                                }
                            }

                            if (response.Code != null)
                            {
                                authCode = response.Code;
                                output("Authorization code obtained.");
                                PageAsyncTask t = new PageAsyncTask(performCodeExchange);
                                Page.RegisterAsyncTask(t);
                                Page.ExecuteRegisteredAsyncTasks();
                            }
                        }
                        else
                        {
                            output("Invalid State");
                            dictionary.Clear();
                        }
                    }
                }
            }
        }

        #region button click events



        protected void Authenticate()
        {
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.Accounting);
                    scopes.Add(OidcScopes.OpenId);
                    scopes.Add(OidcScopes.Phone);
                    scopes.Add(OidcScopes.Profile);
                    scopes.Add(OidcScopes.Address);
                    scopes.Add(OidcScopes.Email);

                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest);
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected void GetPayments()
        { 
            //Now assuming authentication successed, we can proceed with querying the API
            if (dictionary.ContainsKey("accessToken") && dictionary.ContainsKey("realmId"))
            {
                try
                {
                    OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(dictionary["accessToken"]);
                    ServiceContext serviceContext = new ServiceContext(dictionary["realmId"], IntuitServicesType.QBO, oauthValidator);
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";
                    serviceContext.IppConfiguration.MinorVersion.Qbo = "29";
                    
                    QueryService<Payment> pService = new QueryService<Payment>(serviceContext);
                
                    payments = pService.ExecuteIdsQuery("SELECT * from Payment");

                    JArray jPayments = new JArray();
                    for (int i=0; i<payments.Count; i++)
                    {
                        JObject jp = new JObject(new JProperty("key", payments[i].Id),
                                                 new JProperty("customer", payments[i].CustomerRef.name),
                                                 new JProperty("value", payments[i].TotalAmt),
                                                 new JProperty("TxnDate", payments[i].TxnDate));
                        jPayments.Add(jp);
                    }

                    File.WriteAllText(Server.MapPath("/Payments.json"), jPayments.ToString());

                    // write JSON directly to a file
                    using (StreamWriter file = File.CreateText(Server.MapPath("/Payments.json")))
                    using (JsonTextWriter writer = new JsonTextWriter(file))
                    {
                        jPayments.WriteTo(writer);
                    }

                }
                catch (IdsException ex)
                {
                    if (ex.Message == "Unauthorized-401")
                    {
                        output("Invalid/Expired Access Token.");
                    }
                    else
                    {
                        output(ex.Message);
                    }
                }
                catch (IOException ex)
                {
                    //just ignore for now - it will just use the last saved file instead
                }
            }
            else
            {
                output("Access token not found.");
            }
        }

        protected void GetVendorLocations()
        {
            //Now assuming authentication successed, we can proceed with querying the API
            if (dictionary.ContainsKey("accessToken") && dictionary.ContainsKey("realmId"))
            {
                try
                {
                    //output("Making QBO API Call.");
                    OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(dictionary["accessToken"]);
                    ServiceContext serviceContext = new ServiceContext(dictionary["realmId"], IntuitServicesType.QBO, oauthValidator);
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";
                    //serviceContext.IppConfiguration.BaseUrl.Qbo = "https://quickbooks.api.intuit.com/";//prod
                    serviceContext.IppConfiguration.MinorVersion.Qbo = "29";
                   

                    QueryService<Vendor> vService = new QueryService<Vendor>(serviceContext);
                    IList<Vendor> vendors = vService.ExecuteIdsQuery("SELECT * from Vendor");

                    JArray jVendors = new JArray();
                    for (int i = 0; i < vendors.Count; i++)
                    {
                        if (vendors[i].BillAddr != null)
                        {
                            JObject jp = new JObject(new JProperty("name", vendors[i].DisplayName),
                                                     new JProperty("street", vendors[i].BillAddr.Line1),
                                                     new JProperty("city", vendors[i].BillAddr.City),
                                                     new JProperty("state", vendors[i].BillAddr.CountrySubDivisionCode),
                                                     new JProperty("postcode", vendors[i].BillAddr.PostalCode),
                                                     new JProperty("balance", vendors[i].Balance),
                                                     new JProperty("lat", decimal.Parse(vendors[i].BillAddr.Lat)),
                                                     new JProperty("lng", decimal.Parse(vendors[i].BillAddr.Long)));
                            jVendors.Add(jp);
                        }
                    }

                    File.WriteAllText(Server.MapPath("/Vendors.json"), jVendors.ToString());

                    // write JSON directly to a file
                    using (StreamWriter file = File.CreateText(Server.MapPath("/Vendors.json")))
                    using (JsonTextWriter writer = new JsonTextWriter(file))
                    {
                        jVendors.WriteTo(writer);
                    }
                }
                catch (IdsException ex)
                {
                    if (ex.Message == "Unauthorized-401")
                    {
                        output("Invalid/Expired Access Token.");
                    }
                    else
                    {
                        output(ex.Message);
                    }
                }
                catch (IOException ex)
                {
                    //just ignore for now - it will just use the last saved file instead
                }
            }
            else
            {
                output("Access token not found.");
            }
        }

        protected async void btnUserInfo_Click(object sender, EventArgs e)
        {
            if (idToken != null)
            {
                var userInfoResp = await oauthClient.GetUserInfoAsync(dictionary["accessToken"]);
            }
            else
            {
                output("Go through OpenId flow first.");
            }
        }

        protected async void btnRefresh_Click(object sender, EventArgs e)
        {
            if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("refreshToken")))
            {
                output("Exchanging refresh token for access token.");
                var tokenResp = await oauthClient.RefreshTokenAsync(dictionary["refreshToken"]);
            }
        }

 
        #endregion

        /// <summary>
        /// Start code exchange to get the Access Token and Refresh Token
        /// </summary>
        public async System.Threading.Tasks.Task performCodeExchange()
        {
            output("Exchanging code for tokens.");
            try
            {
                var tokenResp = await oauthClient.GetBearerTokenAsync(authCode);
                if (!dictionary.ContainsKey("accessToken"))
                    dictionary.Add("accessToken", tokenResp.AccessToken);
                else
                    dictionary["accessToken"] = tokenResp.AccessToken;

                if (!dictionary.ContainsKey("refreshToken"))
                    dictionary.Add("refreshToken", tokenResp.RefreshToken);
                else
                    dictionary["refreshToken"] = tokenResp.RefreshToken;

                if (tokenResp.IdentityToken != null)
                    idToken = tokenResp.IdentityToken;
                if (Request.Url.Query == "")
                {
                    Response.Redirect(Request.RawUrl);
                }
                else
                {
                    Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
            }
            catch (Exception ex)
            {
                output("Problem while getting bearer tokens.");
            }
        }

   

        #region Helper methods for logging
        /// <summary>
        /// Gets log path
        /// </summary>
        public string GetLogPath()
        {
            try
            {
                if (logPath == "")
                {
                    logPath = Environment.GetEnvironmentVariable("TEMP");
                    if (!logPath.EndsWith("\\")) logPath += "\\";
                }
            }
            catch
            {
                output("Log error path not found.");
            }
            return logPath;
        }

        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="logMsg">string to be appended</param>
        public void output(string logMsg)
        {
            StreamWriter sw = File.AppendText(GetLogPath() + "OAuth2SampleAppLogs.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, logMsg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
        #endregion
    }

    /// <summary>
    /// Helper for calling self
    /// </summary>
    public static class ResponseHelper
    {
        public static void Redirect(this HttpResponse response, string url, string target, string windowFeatures)
        {
            if ((String.IsNullOrEmpty(target) || target.Equals("_self", StringComparison.OrdinalIgnoreCase)) && String.IsNullOrEmpty(windowFeatures))
            {
                response.Redirect(url);
            }
            else
            {
                Page page = (Page)HttpContext.Current.Handler;
                if (page == null)
                {
                    throw new InvalidOperationException("Cannot redirect to new window outside Page context.");
                }
                url = page.ResolveClientUrl(url);
                string script;
                if (!String.IsNullOrEmpty(windowFeatures))
                {
                    script = @"window.open(""{0}"", ""{1}"", ""{2}"");";
                }
                else
                {
                    script = @"window.open(""{0}"", ""{1}"");";
                }
                script = String.Format(script, url, target, windowFeatures);
                ScriptManager.RegisterStartupScript(page, typeof(Page), "Redirect", script, true);
            }
        }
    }
}
