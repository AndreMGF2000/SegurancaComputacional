using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace Client
{
    class ClientWindow
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int PORT = 100;
        private static string login = "";
        private static String response = new String("");
        private static string[] MultiCastIpPort;

        static void Main()
        {
            Console.Title = "Client";
            ConnectToServer();
            RequestResponseServer();
            //Exit();
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            MultiCastIpPort = response.Split("|");
            JoinMultiCast(MultiCastIpPort[0], MultiCastIpPort[1]);
            RequestActualBid();
            while (true)
            {
                RequestResponseMulticast();           
            }
        }



        private static void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                    ClientSocket.Connect(IPAddress.Loopback, PORT);
                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Connected");
        }

        private static void RequestResponseServer()
        {
            Console.WriteLine(@"<Type ""exit"" to properly disconnect client>");
            UserLogin();
            ReceiveResponse();
        }

        private static void RequestResponseMulticast()
        {
            RequestBidAndValidate();
        }

        private static void RequestActualBid()
        {
            string message = Convert.ToString(MultiCastSender.multiCastPort2);
            MultiCastSender.SendMessage("P"+message);
        }

        private static void RequestBidAndValidate()
        {
            string message = GetBid();
            if (IsPositiveNumber(message))
            {
                MultiCastSender.SendMessage(MultiCastSender.multiCastPort2+"|"+ message+"|"+login);
            }
            else
            {
                Console.WriteLine("Digite um Novo Lance:");
                RequestBidAndValidate();
            }
        }

        private static bool IsPositiveNumber(string message)
        {
            try
            {
                int intMessage = int.Parse(message);
                return intMessage > 0;
            }
            catch
            {
                Console.WriteLine("Digite apenas numeros positivos para o lance!");
                return false;
            }
        }

        private static string GetBid()
        {
            //Console.Write("Digite o valor do lance: ");
            string message = Console.ReadLine();
            return message;
        }

        private static void Exit()
        {
            SendString("exit"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            //Environment.Exit(0);
        }

        private static void UserLogin()
        {
            Console.Write("Type Your Username: ");
            login = Console.ReadLine();
            Console.Title = login;
            SendString(login);

            if (login.ToLower() == "exit")
            {
                Exit();
            }
        }

        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            Console.WriteLine(text);
            response = text;
        }

        private static void JoinMultiCast(string MultiCastIPAddress, string MultiCastPort)
        {
            try
            {
                MultiCastSender.Initialize(MultiCastIPAddress, MultiCastPort);
            }
            catch
            {
                Console.WriteLine("Falha ao acessar MultiCast");
            }
        }

    }
}