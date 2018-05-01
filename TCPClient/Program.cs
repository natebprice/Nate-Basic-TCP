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
using System.Threading;
using System.Timers;
using NateBasicTCP.Utils;

namespace NateBasicTCP.TCPClient
{
    class Program
    {
        private static string hostName = ConfigurationManager.AppSettings["HostName"];
        private static int sockTimeout = Int32.Parse(ConfigurationManager.AppSettings["ConnectTimeoutSecs"]);
        private static int hostPort = Int32.Parse(ConfigurationManager.AppSettings["HostPort"]);
        // private static System.Timers.Timer aTimer;


        // private static void SetTimer()
        // {
        //   // Create a timer with a two second interval.
        //    aTimer = new System.Timers.Timer(10000);
        //    // Hook up the Elapsed event for the timer. 
        //    aTimer.Elapsed += OnTimedEvent;
        //    aTimer.AutoReset = true;
        //    aTimer.Enabled = true;
        // }

        // private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        // {
        //     Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
        //                      e.SignalTime);
        //}

        public static void Main()
        {
            IPAddress myIP = TCPUtils.GetIPV4FromHostName(hostName);

            // // Start our timer which will interrupt us every ten seconds.
            // SetTimer();


            try
            {
                // Outer loop: we enter this loop without an instantiated TCPClient object. If we get back to the
                // top of the loop again it's because the client has been closed and we need to start it up again
                while (true)
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
                            Console.WriteLine("Inner SocketException 1: " + e.ToString());
                            // Console.WriteLine("Exception: " + e.GetType().ToString());
                            // Console.WriteLine("Error..... " + e.StackTrace);
                        }
                        if (!tcpclnt.Connected)
                        {
                            // Wait "socktimeout" seconds and then we'll try again
                            DateTime timer = DateTime.Now;
                            
                            while (TCPUtils.ElapsedSecondsSince(timer) < sockTimeout)
                            {
                                // elapsed = TCPUtils.ElapsedSecondsSince(timer);
                                // Console.WriteLine("Connect retry loop: elapsed seconds: " + elapsed.ToString());
                                Thread.Sleep(1000);
                            }
                        }
                        Console.WriteLine("Connected");
                    } // while (!tcpclnt.connected)

                    // Loop until a null string is entered
                    while (true)
                    {
                        Console.Write("Enter the string to be transmitted : ");

                        String str = Console.ReadLine();
                        if (str.Length == 0)
                        {
                            break;
                        }
                        // add back the terminating newline that we'll use as end-of-record separator
                        str = str + "\n";

                        // Try clause for writing to and reading from the socket.
                        // Catch exceptions like other end closed
                        try
                        {
                            Stream stm = tcpclnt.GetStream();

                            ASCIIEncoding asen = new ASCIIEncoding();

                            // HACK: Add a prefix with no trailing record separator
                            string prefix = ":prefix:";
                            byte[] ba = asen.GetBytes(prefix);
                            Console.WriteLine("Transmitting.....");

                            stm.Write(ba, 0, ba.Length);

                            // Now send the terminated string
                            //byte[] ba = asen.GetBytes(str);
                            ba = asen.GetBytes(str);
                            Console.WriteLine("Transmitting.....");

                            stm.Write(ba, 0, ba.Length);

                            // byte[] bb = new byte[100];
                            byte[] bb = new byte[100];
                            int k = stm.Read(bb, 0, 100);

                            for (int i = 0; i < k; i++)
                                Console.Write(Convert.ToChar(bb[i]));
                            Console.WriteLine("");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Inner Exception 2: " + e.ToString());
                            Console.WriteLine("Exception Type: " + e.GetType().ToString());
                            tcpclnt.Close();
                            break;
                        }
                    }
                    Console.WriteLine("Broke out of loop. Closing TCP client.");
                    tcpclnt.Close();
                } // while (true()
            }

            // We can have multiple catch clauses tailored to specific error conditions. These are all
            // redundant because they don't do anything specific to the type of exception being caught.
            catch (SocketException e)
            {
                Console.WriteLine("Outer Socket Exception: " + e.ToString());
                Console.WriteLine("Exception: " + e.GetType().ToString());
            }
            catch (IOException e)
            {
                Console.WriteLine("Outer IO Exception: " + e.ToString());
                Console.WriteLine("Exception: " + e.GetType().ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Outer General Exception: " + e.ToString());
                Console.WriteLine("Exception: " + e.GetType().ToString());
            }

            // // Clean up our timer
            // aTimer.Stop();
            // aTimer.Dispose();
        } // end public static void Main()
    } //class Program
} //namespace TCPClient
