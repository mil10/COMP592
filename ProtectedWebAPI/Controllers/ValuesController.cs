using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Net;

namespace ProtectedWebAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ValuesController : Controller
    {
        //string IOT_IP_STR = "192.162.56.103"; //TODO: Change this to may be a vm?

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            //this is a basic code snippet to validate the scope inside the API
            bool userHasRightScope = User.HasClaim("scope", "scope.readaccess");
            
            if (userHasRightScope == false)
            {
                throw new Exception("Invalid scope");
            }

            string[] response = ForwardRequestToIoTServer()
                .GetAwaiter()
                .GetResult();

            return response;

            //string response = isReady ? IOT_IP_STR : "";
            //return new string[] { response };
        }

        private async Task<string[]> ForwardRequestToIoTServer()
        {
            /* Initializes the Listener */
            TcpListener tcpLis = new TcpListener(IPAddress.Any, 8001);
            /* Start Listeneting at the specified port */
            tcpLis.Start();
            Socket s = await tcpLis.AcceptSocketAsync();
            byte[] message = new byte[100];
            int k = s.Receive(message);

            ASCIIEncoding asen = new ASCIIEncoding();
            var str = asen.GetString(message, 0, k);

            string clientIP = "192.168.1.14"; //TODO
            byte[] response = asen.GetBytes(clientIP);
            s.Send(response);

            k = s.Receive(message);
            var port = asen.GetString(message, 0, k);

            /* clean up */
            s.Shutdown(SocketShutdown.Both);
            tcpLis.Stop();

            return new string[] { port };          
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
