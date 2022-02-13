using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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

        private static RSAParameters rsaKeyInfo;

        static void Main()
        {
            Console.Title = "Client";
            CreateAssimetricKey();
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

        private static void CreateAssimetricKey()
        {
            //Generate a public/private key pair.  
            RSA rsa = RSA.Create();
            //Save the public key information to an RSAParameters structure.  
            rsaKeyInfo = rsa.ExportParameters(false);
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
            string message = Convert.ToString(MultiCastSender.multiCastPortListener);
            MultiCastSender.SendMessage("P"+message);
        }

        private static void RequestBidAndValidate()
        {
            string message = GetBid();
            if (IsPositiveNumber(message))
            {
                MultiCastSender.SendMessage(MultiCastSender.multiCastPortListener+"|"+ message+"|"+login);
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
            SendParametersPublicKey(rsaKeyInfo);
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
        private static void SendParametersPublicKey(RSAParameters rsaKeyInfo)
        {            
            byte[] bufferExponent = (rsaKeyInfo.Exponent);
            ClientSocket.Send(bufferExponent, 0, bufferExponent.Length, SocketFlags.None);
            byte[] bufferModulus = (rsaKeyInfo.Modulus);
            ClientSocket.Send(bufferModulus, 0, bufferModulus.Length, SocketFlags.None);          
        }

        private static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private static Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
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