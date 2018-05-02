using System;
using RabbitMQ.Client;
using System.Text;
using System.Threading;
using NateBasicTCP.Utils;

namespace RmqSend
{
    class Send
    {
        private const int RMQConnectTimeout = 30;

        private static ConnectionFactory factory;
        private static IConnection connection;

        public static void Main()
        {

            factory = new ConnectionFactory() { HostName = "helixlin1t.ynhh.org" };

            // Try to connect to our RabbitMQ server, wait and retry if not successful
            Boolean rmqConnected = false;
            while (!rmqConnected)
            {
                try
                {
                    // factory = new ConnectionFactory() { HostName = "helixlin1t.ynhh.org" };
                    Console.WriteLine("\nTrying RabbitMQ Connection...");
                    connection = factory.CreateConnection();
                    rmqConnected = true;
                }
                catch (Exception e)
                {
                    // Console.WriteLine("RMQ Connection attempt: Caught exception: " + e.ToString());
                    Console.WriteLine("RMQ Connection attempt: Caught Exception type: " + e.GetType().ToString());
                }
                if (!rmqConnected)
                {
                    // Wait "socktimeout" seconds and then we'll try again
                    DateTime timer = DateTime.Now;

                    while (TCPUtils.ElapsedSecondsSince(timer) < RMQConnectTimeout)
                    {
                        // elapsed = TCPUtils.ElapsedSecondsSince(timer);
                        // Console.WriteLine("Connect retry loop: elapsed seconds: " + elapsed.ToString());
                        Thread.Sleep(1000);
                        Console.Write(".");
                    }
                }
                else
                {
                    Console.WriteLine("Connected");
                }
            }

            // Now we have a connection to the ConnectionFactory. Set up the queue
            try
            {
                // using (connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "hello",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    while (true)
                    {
                        Console.Write("Enter the string to be queued (<enter> to exit): ");

                        String message = Console.ReadLine();
                        if (message.Length == 0)
                        {
                            break;
                        }
                        // string message = "Hello World!";
                        var body = Encoding.UTF8.GetBytes(message);

                        channel.BasicPublish(exchange: "",
                                             routingKey: "hello",
                                             basicProperties: null,
                                             body: body);
                        Console.WriteLine(" [x] Sent {0}", message);
                    }
                }               
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.ToString());
                Console.WriteLine("Exception type: " + e.GetType().ToString());
            }


            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }

}