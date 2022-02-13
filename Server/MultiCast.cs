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

                    Socket socketEnviador = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Dgram,
                                         ProtocolType.Udp);

                    

                    for (int i = 0; i < 100; i++)
                    {
                        IPEndPoint endPoint = new IPEndPoint(multiCastIPAddress, multiCastPort+1000+i);

                        socketEnviador.SendTo(ASCIIEncoding.ASCII.GetBytes(Encoding.ASCII.GetString(bytes, 0, bytes.Length)), endPoint);                       
                    }
                    Console.WriteLine("Mensagem devolvida para todos no multicast.....");


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
    }
}