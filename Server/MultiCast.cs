using System;
using System.Net;
using System.Net.Sockets;
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

                IPAddress localIPAddr = IPAddress.Parse("192.168.7.104");

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

        private static void ReceiveBroadcastMessages()
        {
            bool done = false;
            
            IPEndPoint groupEP = new IPEndPoint(multiCastIPAddress, multiCastPort);
            EndPoint remoteEP = (EndPoint)new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!done)
                {
                    byte[] bytes = new Byte[100];
                    Console.WriteLine("Waiting for multicast packets.......");

                    multiCastSocket.ReceiveFrom(bytes, ref remoteEP);

                    Console.WriteLine("Received broadcast from " + groupEP.ToString() 
                        + " : " + Encoding.ASCII.GetString(bytes, 0, bytes.Length));


                    

                    string messageReceived = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
              
                    if (messageReceived.Contains("P"))
                    {
                        int clientPort = int.Parse(messageReceived.Trim('P'));
                        
                        IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, clientPort);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Bem vindo ao Leilao!"), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Item atual eh " + auctionItem), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Lance atual eh " + actualAuctionBid), endPoint);
                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Digite um Novo Lance:"), endPoint);
                    }
                    else
                    {
                        string[] messageReceivedList = new string[2];
                        messageReceivedList = messageReceived.Split("|");
                        string clientPort = messageReceivedList[0];
                        string clientBid = messageReceivedList[1];
                        string clientName = messageReceivedList[2];

                        if (actualAuctionBid >= int.Parse(clientBid))
                        {
                            IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, int.Parse(clientPort));
                            socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Seu lance foi abaixo ou igual ao lance atual"), endPoint);
                            socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Digite um Novo Lance:"), endPoint);
                        }
                        else
                        {
                            actualAuctionBid = int.Parse(clientBid);
                            actualClientPort = int.Parse(clientPort);
                            actualClientName = clientName;

                            for (int i = 0; i < 100; i++)
                            {
                                IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, multiCastPort + 1000 + i);

                                socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Item atual eh " + auctionItem), endPoint);
                                socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Lance atual eh " + actualAuctionBid), endPoint);
                                socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes("Digite um Novo Lance:"), endPoint);
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
            ReceiveBroadcastMessages();
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