using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

// This is the listener example that shows how to use the MulticastOption class.
// In particular, it shows how to use the MulticastOption(IPAddress, IPAddress)
// constructor, which you need to use if you have a host with more than one
// network card.
// The first parameter specifies the multicast group address, and the second
// specifies the local address of the network card you want to use for the data
// exchange.
// You must run this program in conjunction with the sender program as
// follows:
// Open a console window and run the listener from the command line.
// In another console window run the sender. In both cases you must specify
// the local IPAddress to use. To obtain this address run the ipconfig command
// from the command line.
//
namespace Server
{

    public class MultiCast
    {

        private static IPAddress multiCastIPAddress;
        private static int multiCastPort;
        private static Socket multiCastSocket;
        private static MulticastOption multiCastOption;
        private static Socket socketEnviador = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Dgram,
                                         ProtocolType.Udp);
        private static string auctionItem = "";
        private static int actualAuctionBid = 0;

        private static int actualClientPort = 0;
        private static string actualClientName = "";

        private static byte[] IV = ServerWindow.aes.IV;
        private static byte[] Key = ServerWindow.aes.Key;

        private static void MulticastOptionProperties()
        {
            Console.WriteLine("Current multicast group is: " + multiCastOption.Group);
            Console.WriteLine("Current multicast local address is: " + multiCastOption.LocalAddress);
        }

        private static void StartMulticast()
        {

            try
            {
                multiCastSocket = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Dgram,
                                         ProtocolType.Udp);

                Console.WriteLine("instacia multicastsocker ");
                //Console.Write("Enter the local IP address: ");

                IPAddress localIPAddr = IPAddress.Parse("192.168.1.106");

                //IPAddress localIP = IPAddress.Any;
                EndPoint localEP = (EndPoint)new IPEndPoint(localIPAddr, multiCastPort);

                multiCastSocket.Bind(localEP);


                // Define a MulticastOption object specifying the multicast group
                // address and the local IPAddress.
                // The multicast group address is the same as the address used by the server.
                multiCastOption = new MulticastOption(multiCastIPAddress, localIPAddr);

                multiCastSocket.SetSocketOption(SocketOptionLevel.IP,
                                            SocketOptionName.AddMembership,
                                            multiCastOption);
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveMulticastMessages()
        {
            bool done = false;
            
            IPEndPoint groupEP = new IPEndPoint(multiCastIPAddress, multiCastPort);
            EndPoint remoteEP = (EndPoint)new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!done)
                {
                    byte[] byteSujo = new Byte[100];
                    Console.WriteLine("Waiting for multicast packets.......");

                    multiCastSocket.ReceiveFrom(byteSujo, ref remoteEP);

                    byte[] byteLimpo = CleanByteArray(byteSujo);

                    string messageReceivedDecrypeted = SimetricDecrypt(byteLimpo, Key, IV);


                    Console.WriteLine("Received message from " + groupEP.ToString()
                        + " : " + messageReceivedDecrypeted);


                    if (messageReceivedDecrypeted.Contains("P"))
                    {
                        int clientPort = int.Parse(messageReceivedDecrypeted.Trim('P'));

                        IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, clientPort);
                        SendEncrypetedMessage("Bem vindo ao Leilao!", endPoint);
                        SendEncrypetedMessage("Item atual eh " + auctionItem, endPoint);
                        SendEncrypetedMessage("Lance atual eh " + actualAuctionBid, endPoint);
                        SendEncrypetedMessage("Digite um Novo Lance:", endPoint);
                    }
                    else
                    {
                        string[] messageReceivedList = new string[2];
                        messageReceivedList = messageReceivedDecrypeted.Split("|");
                        string clientPort = messageReceivedList[0];
                        string clientBid = messageReceivedList[1];
                        string clientName = messageReceivedList[2];

                        if (actualAuctionBid >= int.Parse(clientBid))
                        {
                            IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, int.Parse(clientPort));
                            SendEncrypetedMessage("Seu lance foi abaixo ou igual ao lance atual", endPoint);
                            SendEncrypetedMessage("Digite um Novo Lance:", endPoint);
                        }
                        else
                        {
                            actualAuctionBid = int.Parse(clientBid);
                            actualClientPort = int.Parse(clientPort);
                            actualClientName = clientName;

                            for (int i = 0; i < 100; i++)
                            {
                                IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, multiCastPort + 1000 + i);

                                SendEncrypetedMessage("Item atual eh " + auctionItem, endPoint);
                                SendEncrypetedMessage("Lance atual eh " + actualAuctionBid, endPoint);
                                SendEncrypetedMessage("Digite um Novo Lance:", endPoint);
                            }
                            Console.WriteLine("Mensagem devolvida para todos no multicast.....");
                        }
                    }
                }
                Console.WriteLine("close multiCastSocket.......");
                multiCastSocket.Close();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static byte[] CleanByteArray(byte[] byteSujo)
        {
            // populate foo
            int byteCounter = byteSujo.Length - 1;
            while (byteSujo[byteCounter] == 0)
                --byteCounter;
            // now foo[i] is the last non-zero byte
            byte[] byteLimpo = new byte[byteCounter + 1];
            Array.Copy(byteSujo, byteLimpo, byteCounter + 1);
            return byteLimpo;
        }

        private static void SendEncrypetedMessage(string message, IPEndPoint endPoint)
        {          
            byte[] messageEncrypeted = SimetricEncrypt(message, Key, IV);
            socketEnviador.SendTo(messageEncrypeted, endPoint);
        }

        public static void Initialize()
        {
            // Initialize the multicast address group and multicast port.
            // Both address and port are selected from the allowed sets as
            // defined in the related RFC documents. These are the same
            // as the values used by the sender.
            multiCastIPAddress = IPAddress.Parse("224.168.100.2");
            multiCastPort = 11000;

            // Start a multicast group.
            StartMulticast();

            // Display MulticastOption properties.
            MulticastOptionProperties();

            // Receive broadcast messages.
            ReceiveMulticastMessages();
        }

        static byte[] SimetricEncrypt(string plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted;
            // Create a new AesManaged.    
            using (AesManaged aes = new AesManaged())
            {
                // Create encryptor    
                ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
                // Create MemoryStream    
                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt    
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream    
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(plainText);
                        encrypted = ms.ToArray();
                    }
                }
            }
            // Return encrypted data    
            return encrypted;
        }
        static string SimetricDecrypt(byte[] cipherText, byte[] Key, byte[] IV)
        {
            try
            {
                string plaintext = null;
                // Create AesManaged    
                using (AesManaged aes = new AesManaged())
                {
                    // Create a decryptor    
                    ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);
                    // Create the streams used for decryption.    
                    using (MemoryStream ms = new MemoryStream(cipherText))
                    {
                        // Create crypto stream    
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            // Read crypto stream    
                            using (StreamReader reader = new StreamReader(cs))
                                plaintext = reader.ReadToEnd();
                        }
                    }
                }
                return plaintext;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            
        }

        public static void FirstAuction()
        {

            Console.WriteLine("Digite: 'name'|'value' para iniciar o primeiro leilao");
            string auction = Console.ReadLine();

            if (auction.Contains("|"))
            {
                string[] auctionDivided = auction.Split("|");
                string name = auctionDivided[0];
                string value = auctionDivided[1];
                if (IsPositiveNumber(value))
                {
                    auctionItem = name;
                    actualAuctionBid = Convert.ToInt32(value);

                    Console.WriteLine("Primeiro Leilao criado com sucesso");
                }
                else
                {
                    Console.WriteLine("Value invalido, digite um numero positivo");
                }
            }
            else
            {
                Console.WriteLine("Comando invalido, tente novamente");
            }

        }

        public static void ManageAuction()
        {

            Console.WriteLine("Digite: 'name'|'value' para encerrar o leilão atual e iniciar um novo");
            string auction = Console.ReadLine();

            if (auction.Contains("|"))
            {
                string[] auctionDivided = auction.Split("|");
                string name = auctionDivided[0];
                string value = auctionDivided[1];
                if (IsPositiveNumber(value))
                {
                    //Envia mensagem vencedor
                    for (int i = 0; i < 100; i++)
                    {
                        IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, multiCastPort + 1000 + i);

                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Leilao encerrado!!!"), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Vencedor foi:"), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes(actualClientName), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Item era " + auctionItem), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Lance final foi " + actualAuctionBid), endPoint);
                    }

                    IPEndPoint endPointVencedor = new IPEndPoint(multiCastIPAddress, actualClientPort);
                    socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Parabens, voce venceu o leilao!!!"), endPointVencedor);
                    socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Entre em contato com o telefone 999111888"), endPointVencedor);

                    auctionItem = name;
                    actualAuctionBid = Convert.ToInt32(value);

                    //Envia mensagem novo leilão
                    for (int i = 0; i < 100; i++)
                    {
                        IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, multiCastPort + 1000 + i);

                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Novo leilao iniciado!!!"), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Item atual eh " + auctionItem), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Lance atual eh " + actualAuctionBid), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Digite um Novo Lance:"), endPoint);
                    }
                    Console.WriteLine("Mensagem devolvida para todos no multicast.....");
                }
                else
                {
                    Console.WriteLine("Value inválido, digite um número positivo");
                }
            }
            else
            {
                Console.WriteLine("Comando inválido, tente novamente");            
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
    }
}