// NBP: First implementation of a TCP server in C#
// This code cribbed from 
// https://www.codeproject.com/Articles/1415/Introduction-to-TCP-client-server-in-C
// and subsequently modified by me.
/*   Server Program    */

//TO-DO LIST NPB 4/6/2018:
//    * Add in some configured variables as in the client
//    * More try-catch to go back to listening if the connection is broken
//    * Idle timer. Use system time arithmetic
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.IO;
using System.Threading;
using NateBasicTCP.Utils;



namespace NateBasicTCP.TCPServer
{
    class Program
    {
        private static string hostName = ConfigurationManager.AppSettings["HostName"];
        private static int hostPort = Int32.Parse(ConfigurationManager.AppSettings["HostPort"]);
        private static int idleTimeout = Int32.Parse(ConfigurationManager.AppSettings["ConnectionIdleTimeoutSecs"]);
        private static int recvBufSize = Int32.Parse(ConfigurationManager.AppSettings["ReceiveBufferSize"]);

        public static void Main()
        {
            IPAddress myIP = TCPUtils.GetIPV4FromHostName(hostName);
            TcpListener myList;
            // In this outer block, if we have an error we are done.
            try
            {
                /* Initializes the Listener */
                myList = new TcpListener(myIP, hostPort);

                /* Start Listening at the specified port */
                myList.Start();

                Boolean connected = false;

                while (!connected)
                {
                    Console.WriteLine("TCP server local end point: " + myList.LocalEndpoint);
                    Console.WriteLine("Waiting for a connection.....");
                    Socket s = myList.AcceptSocket();
                    Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);
                    connected = true;
                    // DateTime connectionStartTime = DateTime.Now;
                    // while (TCPUtils.ElapsedSecondsSince(timer) < sockTimeout)

                        while (connected)
                    {
                        byte[] b = new byte[recvBufSize];
                        try
                        {
                            int k = s.Receive(b);
                            Console.WriteLine("\nReceived...");
                            for (int i = 0; i < k; i++)
                                Console.Write(Convert.ToChar(b[i]));

                            ASCIIEncoding asen = new ASCIIEncoding();
                            s.Send(asen.GetBytes("The string was received by the server."));
                            Console.WriteLine("\nSent Acknowledgement");
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine("Exception: " + e.ToString());
                            // Console.WriteLine("Error..... " + e.StackTrace);
                            s.Close();
                            connected = false;
                        }
                    }
                }
                    /* clean up */
                    myList.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
                // Console.WriteLine("Error..... " + e.StackTrace);
            }
        } // end Main
    } // end Class
}
