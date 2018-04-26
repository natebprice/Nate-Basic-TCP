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
using System.Net.Sockets;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using NateBasicTCP.Utils;

namespace NateBasicTCP.TCPClient
{
    class Program
    {
        private static string hostName = ConfigurationManager.AppSettings["HostName"];
        private static int sockTimeout = Int32.Parse(ConfigurationManager.AppSettings["ConnectTimeoutSecs"]);
        private static int hostPort = Int32.Parse(ConfigurationManager.AppSettings["HostPort"]);

        public static void Main()
        {
            IPAddress myIP = TCPUtils.GetIPV4FromHostName(hostName);
            try
            {

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
                    }
                    // Inner catch clause. We want to intercept socket connection failures here.
                    // But we are only catching socket exceptions; anything else will blow up the program
                    catch (SocketException e)
                    {
                        Console.WriteLine("Inner SocketException: " + e.ToString());
                        // Console.WriteLine("Exception: " + e.GetType().ToString());
                        // Console.WriteLine("Error..... " + e.StackTrace);
                    }
                    if (!tcpclnt.Connected)
                    {
                        // Wait "socktimeout" seconds and then we'll try again
                        DateTime timer = DateTime.Now;
                        long elapsed;
                        while (TCPUtils.ElapsedSecondsSince(timer) < sockTimeout)
                        {
                            // elapsed = TCPUtils.ElapsedSecondsSince(timer);
                            // Console.WriteLine("Connect retry loop: elapsed seconds: " + elapsed.ToString());
                            Thread.Sleep(1000);
                        }
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
