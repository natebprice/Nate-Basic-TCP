using System;
using System.Net;
using System.Net.Sockets;

// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

namespace NateBasicTCP.Utils
{
    public class TCPUtils
    {
        public static long ElapsedSecondsSince(DateTime startTime)
        {
            DateTime now = DateTime.Now;
            long elapsedTicks = now.Ticks - startTime.Ticks;
            long elapsedSec = elapsedTicks / 10000000;
            return (elapsedSec);
        }

        public static IPAddress GetIPV4FromHostName(string hostname)
        {
            // go through a bunch of rigamarole to look up an IP V4 IP address from a host name.
            // Seems like it should not be so complicated, but this works for now.

            // Also this doesn't work because the first item in the address list may not be the right
            // address family (ipv4), but we'll use this to initialize a value
            //IPAddress myIP = Dns.GetHostEntry(hostName).AddressList[0];
            IPAddress myIP = IPAddress.Parse("0.0.0.0");

            // Get host-related information. An IPHostEntry is an array of IPaddress
            IPHostEntry myself = Dns.GetHostEntry(hostname);
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
            return (myIP);
        }
    }


}
