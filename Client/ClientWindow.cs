﻿using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Client
{
    class ClientWindow
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int PORT = 100;

        private static String response = new String("");
        private static string[] MultiCastIpPort;
        static void Main()
        {
            Console.Title = "Client";
            ConnectToServer();
            RequestResponse();
            //Exit();
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            MultiCastIpPort = response.Split("|");
            JoinMultiCast(MultiCastIpPort[0], MultiCastIpPort[1]);
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

        private static void RequestResponse()
        {
            Console.WriteLine(@"<Type ""exit"" to properly disconnect client>");

            SendRequest();
            ReceiveResponse();
            
        }

        /// <summary>
        /// Close socket and exit program.
        /// </summary>
        private static void Exit()
        {
            SendString("exit"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            //Environment.Exit(0);
        }

        private static void SendRequest()
        {
            Console.Write("Type Your Username: ");
            string request = Console.ReadLine();
            Console.Title = request;
            SendString(request);

            if (request.ToLower() == "exit")
            {
                Exit();
            }
        }

        /// <summary>
        /// Sends a string to the server with ASCII encoding.
        /// </summary>
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

        private static void JoinMultiCast(string MutiCastIPAddress, string MultiCastPort)
        {
            MultiCastSender.Initialize(MutiCastIPAddress,  MultiCastPort);
        }

    }
}