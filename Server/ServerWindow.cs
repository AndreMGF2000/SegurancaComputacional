using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Server
{
    class ServerWindow
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 100;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        private static RSAParameters rsaKeyInfo;
        private static int contadorChavePublica = 0;
        private static RSA rsa;
        public static Aes aes;
        static void Main()
        {
            //CloseAllSockets();
            Console.Title = "Server";
            CreateAssimetricKey();
            CreateSimetricKey();
            MultiCast.FirstAuction();
            SetupServer();
            while (true)
            {
                MultiCast.ManageAuction();
            }
            
        }
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);

            Thread Multicast = new Thread(new ThreadStart(MultiCast.Initialize));
            Multicast.Start();

            //MultiCast.Initialize();
            Console.WriteLine("Server setup complete");
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            
        }


        private static void CreateAssimetricKey()
        {
            //Generate a public/private key pair.  
            rsa = RSA.Create();
            //Save the public key information to an RSAParameters structure.  
            rsaKeyInfo = rsa.ExportParameters(false);
        }

        private static void CreateSimetricKey()
        {
            aes = Aes.Create();
            aes.GenerateIV();
            aes.GenerateKey();
        }

        

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = "";
            if(contadorChavePublica == 0)
            {
                rsaKeyInfo.Exponent = recBuf;
                contadorChavePublica++;
                Console.WriteLine("Recebendo rsaKey.Exponent");
                
            }
            else if (contadorChavePublica == 1)
            {
                Console.WriteLine("Recebendo rsaKey.Modulus");
                rsaKeyInfo.Modulus = recBuf;
                contadorChavePublica++;
            }
            else if (contadorChavePublica == 2)
            {
                text = Encoding.ASCII.GetString(recBuf);
                Console.WriteLine("Cliente |" + text + "| Conectou-se");
                contadorChavePublica++;
            }

            //if (text.ToLower() == "exit") // Client wants to exit gracefully
            //{
            //    // Always Shutdown before closing
            //    current.Shutdown(SocketShutdown.Both);
            //    current.Close();
            //    clientSockets.Remove(current);
            //    Console.WriteLine("Client disconnected");
            //    return;
            //}

            if (contadorChavePublica == 3)
            {
                contadorChavePublica = 0;

                

                

                //tem que responder o cliente direito aqui
                byte[] data = Encoding.ASCII.GetBytes("224.168.100.2|11000");
                rsa.ImportParameters(rsaKeyInfo);
                byte[] dataEncrypeted = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
                byte[] IVEncrypeted = rsa.Encrypt(aes.IV, RSAEncryptionPadding.Pkcs1);
                byte[] KeyEncrypeted = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);

                current.Send(dataEncrypeted);
                Thread.Sleep(500);
                current.Send(IVEncrypeted);
                Thread.Sleep(500);
                current.Send(KeyEncrypeted);
                Thread.Sleep(500);

                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine("Client connected, waiting for request...");
            }
            serverSocket.BeginAccept(AcceptCallback, null);
            if(contadorChavePublica != 0)
            {
                current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
            }
 
        }

        
    }
}