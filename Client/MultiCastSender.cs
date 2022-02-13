using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

// This sender example must be used in conjunction with the listener program.
// You must run this program as follows:
// Open a console window and run the listener from the command line.
// In another console window run the sender. In both cases you must specify
// the local IPAddress to use. To obtain this address,  run the ipconfig command
// from the command line.
//
namespace Client
{
    public class MultiCastSender
    {

        static IPAddress multiCastAddress;
        static int multiCastPort;
        static Socket multiCastSocket;

        static void JoinMulticastGroup()
        {
            try
            {
                // Create a multicast socket.
                multiCastSocket = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Dgram,
                                         ProtocolType.Udp);

                // Get the local IP address used by the listener and the sender to
                // exchange multicast messages.
                Console.Write("\nEnter local IPAddress for sending multicast packets: ");
                IPAddress localIPAddr = IPAddress.Parse(Console.ReadLine());

                // Create an IPEndPoint object.
                IPEndPoint IPlocal = new IPEndPoint(localIPAddr, 0);

                // Bind this endpoint to the multicast socket.
                multiCastSocket.Bind(IPlocal);

                // Define a MulticastOption object specifying the multicast group
                // address and the local IP address.
                // The multicast group address is the same as the address used by the listener.
                MulticastOption multiCastOption;
                multiCastOption = new MulticastOption(multiCastAddress, localIPAddr);

                multiCastSocket.SetSocketOption(SocketOptionLevel.IP,
                                            SocketOptionName.AddMembership,
                                            multiCastOption);

               
                    Thread ThreadStartListenMultiCast = new Thread(new ThreadStart(StartListenMultiCast));
                    ThreadStartListenMultiCast.Start();
                   
                

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.ToString());
            }
        }

        private static void StartListenMultiCast()
        {
            bool done = false;

            IPEndPoint groupEP = new IPEndPoint(multiCastAddress, multiCastPort);
            EndPoint remoteEP = (EndPoint)new IPEndPoint(IPAddress.Parse("10.0.0.103"), 55144);

            try
            {
                while (!done)
                {
                    byte[] bytes = new Byte[100];
                    Console.WriteLine("Waiting for multicast packets.......");

                    multiCastSocket.ReceiveFrom(bytes, ref remoteEP);

                    Console.WriteLine("Received broadcast from " + groupEP.ToString()
                        + " : " + Encoding.ASCII.GetString(bytes, 0, bytes.Length));

                }
                Console.WriteLine("close multiCastSocket.......");
                multiCastSocket.Close();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void BroadcastMessage(string message)
        {
            IPEndPoint endPoint;

            try
            {
                //Send multicast packets to the listener.
                endPoint = new IPEndPoint(multiCastAddress, multiCastPort);
                multiCastSocket.SendTo(ASCIIEncoding.ASCII.GetBytes(message), endPoint);
                Console.WriteLine("Multicast data sent.....");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.ToString());
            }

            //multiCastSocket.Close();
        }

        public static void Initialize(string MutiCastIPAddress, string MultiCastPort)
        {
            // Initialize the multicast address group and multicast port.
            // Both address and port are selected from the allowed sets as
            // defined in the related RFC documents. These are the same
            // as the values used by the sender.
            multiCastAddress = IPAddress.Parse(MutiCastIPAddress);
            multiCastPort = int.Parse(MultiCastPort);

            // Join the listener multicast group.
            JoinMulticastGroup();


            // Broadcast the message to the listener.
            //BroadcastMessage("Hello multicast listener.");
        }
    }
}