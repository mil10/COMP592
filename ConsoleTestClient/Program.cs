using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //authorization server parameters owned from the client
                //this values are issued from the authorization server to the client through a separate process (registration, etc...)
                Uri authorizationServerTokenIssuerUri = new Uri("http://192.168.1.2:54482/connect/token");
                string clientId = "ClientIdThatCanOnlyRead";
                string clientSecret = "secret1";
                string scope = "scope.readaccess";

                //access token request
                string rawJwtToken = RequestTokenToAuthorizationServer(
                     authorizationServerTokenIssuerUri,
                     clientId,
                     scope,
                     clientSecret)
                    .GetAwaiter()
                    .GetResult();

                AuthorizationServerAnswer authorizationServerToken;
                authorizationServerToken = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthorizationServerAnswer>(rawJwtToken);

                Console.WriteLine("Token acquired from Authorization Server:");
                Console.WriteLine(authorizationServerToken.access_token);

                //secured web api request
                string response = RequestValuesToSecuredWebApi(authorizationServerToken)
                    .GetAwaiter()
                    .GetResult();
                var port = JArray.Parse(response)[0];
                //response = response.Replace("\"", "");

                //Console.WriteLine("IOT IP address received from WebAPI: ");
                //Console.WriteLine(ipAddr);
                Console.WriteLine("Port received from WebAPI: ");
                Console.WriteLine(port);

                Console.WriteLine("Communicating directly with IOT device:");
                SendRequestToIoTDevice(port.ToString())
                    .GetAwaiter()
                    .GetResult();

                Console.ReadKey();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Message:\n" + ex.Message);
                Console.WriteLine("InnerException:\n" + ex.InnerException);
                Console.WriteLine("StackTrace:\n" + ex.StackTrace);
                Console.WriteLine("Source:\n" + ex.Source);
                Console.WriteLine("HResult:\n" + ex.HResult);
            }
        }

        private static async Task<bool> SendRequestToIoTDevice(string port)
        {
            Console.WriteLine("Listening to the port received from Web api " + port);
            TcpListener tcpLis = new TcpListener(IPAddress.Any, Convert.ToInt32(port));
            /* Start Listeneting at the specified port */
            tcpLis.Start();
            Socket s = await tcpLis.AcceptSocketAsync();
            byte[] messageBytes = new byte[100];
            int k = s.Receive(messageBytes);
            ASCIIEncoding asen = new ASCIIEncoding();
            var message = asen.GetString(messageBytes, 0, k);

            Console.WriteLine("Message received from IOT: " + message);
            string response = "Hi from Client App";
            byte[] responseBytes = asen.GetBytes(response);
            s.Send(responseBytes);
            Console.ReadLine();

            return true;
        }

        private static async Task<string> RequestTokenToAuthorizationServer(Uri uriAuthorizationServer, string clientId, string scope, string clientSecret)
        {
            HttpResponseMessage responseMessage;
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, uriAuthorizationServer);
                HttpContent httpContent = new FormUrlEncodedContent(
                    new[]
                    {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("scope", scope),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                    });
                tokenRequest.Content = httpContent;
                responseMessage = await client.SendAsync(tokenRequest);
            }
            return await responseMessage.Content.ReadAsStringAsync();
        }

        private static async Task<string> RequestValuesToSecuredWebApi(AuthorizationServerAnswer authorizationServerToken)
        {
            HttpResponseMessage responseMessage;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationServerToken.access_token);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://192.168.1.2:56086/api/values");
                responseMessage = await httpClient.SendAsync(request);
            }

            return await responseMessage.Content.ReadAsStringAsync();
        }

        private class AuthorizationServerAnswer
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
            public string token_type { get; set; }

        }
    }
}