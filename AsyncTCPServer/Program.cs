// NBP: this is the fully async version of TCPServer.
// Most of this code is cribbed from 
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example
// then modified by me to include configuration variables and an idle timeout on connected sockets.
// NBP 5/1/18:
// The idle timeout was implemented using the System.Timers.Timer class and related methods
// When idle time exceeds configured limit, socket is closed and exception is thrown.
// This is done with a bit of a hack, making the connected socket a class-static variable that can be seen by all threads
// There should be some sort of cleaner signaling mechanism that I haven't worked out, but this method
// does work.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using NateBasicTCP.Utils;
using System.Configuration;

namespace NateBasicTCP.AsyncTCPServer
{

    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public static int RecvBufSize = Int32.Parse(ConfigurationManager.AppSettings["ReceiveBufferSize"]);
        // public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[RecvBufSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousSocketListener
    {
        private static string hostName = ConfigurationManager.AppSettings["HostName"];
        private static int hostPort = Int32.Parse(ConfigurationManager.AppSettings["HostPort"]);
        private static int idleTimeout = Int32.Parse(ConfigurationManager.AppSettings["ConnectionIdleTimeoutSecs"]);

        // This variable will be initalized when a connection is made
        private static DateTime ConnectionIdleTimer;

        private static System.Timers.Timer aTimer;

        private static Socket connectedSocket;

        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        private static void SetTimer()
        {

            Console.WriteLine("SetTimer()");

            // Initialize our connection idle timer
            ConnectionIdleTimer = DateTime.Now;

            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private static void StopTimer()
        {
            // Clean up our timer
            aTimer.AutoReset = false;
            aTimer.Enabled = false;
            aTimer.Stop();
            aTimer.Dispose();
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            long idleTime = TCPUtils.ElapsedSecondsSince(ConnectionIdleTimer);

            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                              e.SignalTime);
            Console.WriteLine("The connection has been idle {0} seconds", idleTime);
            try
            {
                if (idleTime > idleTimeout)
                {
                    Console.WriteLine("The connection has been idle for longer than the maximum {0} seconds. Closing connection", idleTimeout);
                    StopTimer();
                    connectedSocket.Shutdown(SocketShutdown.Both);
                    connectedSocket.Close();
                    throw new System.Net.Sockets.SocketException(10060);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        public AsynchronousSocketListener()
        {
        }

        public static void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.  
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            //IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
            //Console.WriteLine("TCP server local end point: " + localEndPoint);

            IPAddress ipAddress = TCPUtils.GetIPV4FromHostName(hostName);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, hostPort);
            Console.WriteLine("TCP server local end point: " + localEndPoint);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                
                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                    // Console.WriteLine("allDone.WaitOne() complete");

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nStartListening done. Press ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            connectedSocket = listener.EndAccept(ar); // this is the class-global static variable

            // Now we have a connected socket -- tell us about it.
            Console.WriteLine("ACB: Connection accepted from " + connectedSocket.RemoteEndPoint);

            // Initialize our timers
            SetTimer();

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = connectedSocket;
            Console.WriteLine("AcceptCallBack: About to call BeginReceive");

            connectedSocket.BeginReceive(state.buffer, 0, StateObject.RecvBufSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            try
            {
                // Read data from the client socket.   
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {

                    // Reset our connection idle timer
                    ConnectionIdleTimer = DateTime.Now;

                    Console.WriteLine("ReadCallBack: bytes received: " + bytesRead.ToString());

                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    // Look for end-of-record; if that's the case, echo string back to client,
                    // zero out the receive buffer before resuming listen
                    content = state.sb.ToString();
                    if (content.IndexOf("\n") != -1) // This is a cheat; should look only at end of buffer
                    {
                        // All data has been read. Display the data
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                                content.Length, content);
                        // Echo the data back to the client.  
                        Send(handler, content);

                        // Initialize the string buffer for a new read
                        state.sb = new StringBuilder();
                    }
                    // Start to listen again
                    handler.BeginReceive(state.buffer, 0, StateObject.RecvBufSize, 0,
                       new AsyncCallback(ReadCallback), state);
                }
                else
                {
                    // This section gets activated only if bytesRead <= 0.
                    // This condition occurs if the other end has been closed.

                    // No more data to read; socket closed at other end
                    content = state.sb.ToString();
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}\nAssuming other end closed; throwing exception",
                            content.Length, content);
                    throw new System.Net.Sockets.SocketException(10054);
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("ReadCallback loop: " + e.ToString());
                StopTimer();
            }

        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.      
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                // .Shutdown(SocketShutdown.Both);
                // handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static int Main(String[] args)
        {
            StartListening();
            Console.WriteLine("Moving on...");
            return 0;
        }
    }
}