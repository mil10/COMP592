using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IoTSimulator
{
    class Program
    {
        static int MIDDLEWARE_PORT = 8001;
        static int CLIENT_PORT = 8526;
        static void Main(string[] args)
        {
            RegisterForFileWatch();

            // Wait for the user to quit the program.
            Console.Write("Press \'q\' to quit the sample.\n");
            while (Console.Read() != 'q') ;
        }    
        
        static void RegisterForFileWatch()
        {
            string filename = "sensorvalue.txt";
            string fullpath = Path.GetTempPath() + filename;
            if (!File.Exists(fullpath))
                File.Create(fullpath);
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Path.GetTempPath();
            watcher.Filter = filename;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += SensorValueUpdated;
            watcher.EnableRaisingEvents = true;
            Console.Write("Waiting for sensorvalue update at " + fullpath + "\n");
           // watcher.WaitForChanged(WatcherChangeTypes.Changed);
        }

        private static void SensorValueUpdated(object sender, FileSystemEventArgs e)
        {
            Console.Write("Sensor value updated\n");
            string clientIP = PollWebApi();// ListenToWebApi();

            ProgressConnectionWithClientIP(clientIP);
        }

        static string PollWebApi()
        {
            string webApiIP = "192.168.1.2";
            Console.Write("Waiting for a connection with server.....\n");

            TcpClient tcpclnt = new TcpClient();
            while (true)
            {
                try
                {
                    tcpclnt.Connect(webApiIP, MIDDLEWARE_PORT);
                    break;
                }
                catch(SocketException )
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }

            Stream stm = tcpclnt.GetStream();
            ASCIIEncoding asen = new ASCIIEncoding();
            string message = "Do you have any client?";
            byte[] ba = asen.GetBytes(message);
            stm.Write(ba, 0, ba.Length);

            byte[] response = new byte[100];
            int k = stm.Read(response, 0, 100);
            var clientIP = System.Text.Encoding.Default.GetString(response);

            message = CLIENT_PORT.ToString();
            ba = asen.GetBytes(message);
            stm.Write(ba, 0, ba.Length);

            //byte[] resp = new byte[1];
            //resp[0] = 1; // OK
            Console.WriteLine("Recieved client IP from connection: " + clientIP + "\n");
            //for (int i = 0; i < k; i++)
            //Console.Write(clientIP);


            return clientIP;
        }

        static void ProgressConnectionWithClientIP(string clientIp)
        {
            Console.WriteLine("Waiting for a connection with client.....\n");

            TcpClient tcpclnt = new TcpClient();
            while (true)
            {
                try
                {
                    tcpclnt.Connect(clientIp, CLIENT_PORT);
                    break;
                }
                catch (SocketException ex)
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }

            Stream stm = tcpclnt.GetStream();
            ASCIIEncoding asen = new ASCIIEncoding();
            string message = "Hi from IOT device";
            byte[] messageBytes = asen.GetBytes(message);
            stm.Write(messageBytes, 0, messageBytes.Length);

            byte[] responseBytes = new byte[100];
            int k = stm.Read(responseBytes, 0, 100);
            var response = System.Text.Encoding.Default.GetString(responseBytes);

            Console.WriteLine("Message received  from client: " + response + "\n");
            Console.Read();
        }
    }
}
