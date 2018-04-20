// NBP: First implementation of a TCP client in C#
// Much of this code cribbed from 
// https://www.codeproject.com/Articles/1415/Introduction-to-TCP-client-server-in-C
// but then further modified by me.


/*       Client Program      */


using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace TCPClient
{
    class Program
    {
        private static string hostName = ConfigurationManager.AppSettings["HostName"];
        private static int sockTimeout = Int32.Parse(ConfigurationManager.AppSettings["ConnectTimeoutSecs"]) * 1000;
        private static int hostPort = Int32.Parse(ConfigurationManager.AppSettings["HostPort"]);

        public static void Main()
        {
            try
            {

                // go through a bunch of rigamarole to use the "localhost" DNS name. It would be easier to 
                // use a quoted literal address in dotted-quad notation, but this is more interesting.

                // Also this doesn't work because the first item in the address list may not be the right
                // address family (ipv4), but we'll use this to initialize a value
                //IPAddress myIP = Dns.GetHostEntry(hostName).AddressList[0];
                IPAddress myIP = IPAddress.Parse("0.0.0.0");

                // Get localhost-related information. An IPHostEntry is an array of IPaddress
                IPHostEntry myself = Dns.GetHostEntry(hostName);
                foreach (IPAddress curAdd in myself.AddressList)
                {
                    // Display the type of address family supported by the server. If the
                    // server is IPv6-enabled this value is: InternNetworkV6. If the server
                    // is also IPv4-enabled there will be an additional value of InterNetwork.
                    Console.WriteLine("AddressFamily: " + curAdd.AddressFamily.ToString());

                    // Display the ScopeId property in case of IPV6 addresses.
                    if (curAdd.AddressFamily.ToString() == ProtocolFamily.InterNetworkV6.ToString())
                        Console.WriteLine("Scope Id: " + curAdd.ScopeId.ToString());

                    // Display the server IP address in the standard format. In 
                    // IPv4 the format will be dotted-quad notation, in IPv6 it will be
                    // in in colon-hexadecimal notation.
                    Console.WriteLine("Address: " + curAdd.ToString());

                    // Display the server IP address in byte format.
                    Console.Write("AddressBytes: ");
                    Byte[] bytes = curAdd.GetAddressBytes();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        Console.Write(bytes[i]);
                    }

                    Console.WriteLine("\r\n");

                    // If this is IPv4 then this is the address we want, and we can break out of the loop
                    if (curAdd.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString())
                    {
                        myIP = curAdd;
                        break;
                    }

                }

                Console.WriteLine("AddressFamily: " + myIP.AddressFamily.ToString());
                Console.WriteLine("Ip address for localhost: " + myIP.ToString());

                TcpClient tcpclnt = new TcpClient();
                // Inner try clause. We want to intercept socket connection failures, try again after 
                // socktimeout seconds
                while (!tcpclnt.Connected)
                {
                    try
                    {
                        Console.WriteLine("Connecting to {0:S} on port {1:D}...", hostName, hostPort);
                        //Console.WriteLine("Connecting...");
                        tcpclnt.Connect(myIP, hostPort);
                        if (!tcpclnt.Connected)
                        {
                            //Thread.Sleep(sockTimeout);
                            Thread.Sleep(sockTimeout);
                        }
                    }
                    // Inner catch clause. We want to intercept socket connection failures here.
                    // But we are only catching socket exceptions; anything else will blow up the program
                    catch (SocketException e)
                    {
                        Console.WriteLine("Inner SocketException: " + e.ToString());
                        // Console.WriteLine("Exception: " + e.GetType().ToString());
                        // Console.WriteLine("Error..... " + e.StackTrace);
                    }

                }

                Console.WriteLine("Connected");
                // Loop until a null string is entered
                while (true)
                {
                    Console.Write("Enter the string to be transmitted : ");

                    String str = Console.ReadLine();
                    if (str.Length == 0)
                    {
                        break;
                    }

                    Stream stm = tcpclnt.GetStream();

                    ASCIIEncoding asen = new ASCIIEncoding();
                    byte[] ba = asen.GetBytes(str);
                    Console.WriteLine("Transmitting.....");

                    stm.Write(ba, 0, ba.Length);

                    byte[] bb = new byte[100];
                    int k = stm.Read(bb, 0, 100);

                    for (int i = 0; i < k; i++)
                        Console.Write(Convert.ToChar(bb[i]));
                    Console.WriteLine("");
                }
                tcpclnt.Close();
            }

            catch (SocketException e)
            {
                Console.WriteLine("Outer Socket Exception: " + e.ToString());
                Console.WriteLine("Exception: " + e.GetType().ToString());
                Console.WriteLine("Error..... " + e.StackTrace);
            }
            catch (IOException e)
            {
                Console.WriteLine("Outer IO Exception: " + e.ToString());
                Console.WriteLine("Exception: " + e.GetType().ToString());
                Console.WriteLine("Error..... " + e.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine("Outer General Exception: " + e.ToString());
                Console.WriteLine("Exception: " + e.GetType().ToString());
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        } // end public static void Main()
    } //class Program
} //namespace TCPClient
