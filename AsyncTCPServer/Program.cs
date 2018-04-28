// NBP: this is the fully async version of TCPServer.
// Most of this code is cribbed from 
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example
// then modified by me to include configuration variables and an idle timeout on connected sockets.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

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
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            Console.WriteLine("AcceptCallBack: About to call BeginReceive");

            handler.BeginReceive(state.buffer, 0, StateObject.RecvBufSize, 0,
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

                //                // Data was read from the client socket.  
                //                if (read > 0)
                //                {
                //                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
                //                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                //                        new AsyncCallback(readCallback), state);
                //               }
                //                else
                //                {
                //                    if (state.sb.Length > 1)
                //                    {
                //                        // All the data has been read from the client;  
                //                        // display it on the console.  
                //                        string content = state.sb.ToString();
                //                       Console.WriteLine("Read {0} bytes from socket.\n Data : {1}",
                //                           content.Length, content);
                //                    }
                //                    handler.Close();
                //                }

                if (bytesRead > 0)
                {
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
                    // This section gets activated only if bytesRead = 0 or -1.
                    // I don't think this code will ever be reached.
                    // ReadCallBack only gets invoked if there is data to be read

                    // All data has been read. Display the data
                    content = state.sb.ToString();
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                            content.Length, content);
                    // Echo the data back to the client.  
                    Send(handler, content);

                    // Initialize the string buffer for a new read
                    state.sb = new StringBuilder();

                    // Start to listen again
                    handler.BeginReceive(state.buffer, 0, StateObject.RecvBufSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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