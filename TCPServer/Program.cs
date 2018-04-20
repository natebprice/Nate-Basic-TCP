// NBP: First implementation of a TCP server in C#
// This code cribbed from 
// https://www.codeproject.com/Articles/1415/Introduction-to-TCP-client-server-in-C
// and subsequently modified by me.
/*   Server Program    */

TO-DO LIST NPB 4/6/2018:
    * Add in some configured variables as in the client
    * More try-catch to go back to listening if the connection is broken
    * Idle timer. Use system time arithmetic
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.IO;
using System.Threading;



namespace TCPServer
{
    class Program
    {
        public static void Main()
        {
            try
            {
                // Use the "localhost" address
                IPAddress ipAd = IPAddress.Parse("127.0.0.1");
                // use local m/c IP address, and 
                // use the same in the client

                /* Initializes the Listener */
                TcpListener myList = new TcpListener(ipAd, 8001);

                /* Start Listening at the specified port */
                myList.Start();

                Console.WriteLine("The server is running at port 8001...");
                Console.WriteLine("The local End point is  :" +
                                  myList.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                Socket s = myList.AcceptSocket();
                Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);

                while (true)
                {
                    byte[] b = new byte[100];
                    int k = s.Receive(b);
                    Console.WriteLine("\nReceived...");
                    for (int i = 0; i < k; i++)
                        Console.Write(Convert.ToChar(b[i]));

                    ASCIIEncoding asen = new ASCIIEncoding();
                    s.Send(asen.GetBytes("The string was received by the server."));
                    Console.WriteLine("\nSent Acknowledgement");
                }
                /* clean up */
                s.Close();
                myList.Stop();

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        } // end Main
    } // end Class
}
