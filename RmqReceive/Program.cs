// Simple RabbitMQ consumer client. To the simple model we add:
//  * Persistent retry on startup if initial connection attempt fails
//  * Enable built-in connection recovery; detect model/channel shutdown
//  * Make queue persistent; so messages published while no consumer
//      is running will stay in the queue even if the broker restarts.


using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using NateBasicTCP.Utils;


namespace RmqReceive
{
    class Receive
    {
        private const int RMQConnectTimeout = 40;

        private static ConnectionFactory factory;
        private static IConnection connection;

        public static void Main()
        {

            // Set up the new connection factory and enable connection recovery with an interval of 20 seconds  
            factory = new ConnectionFactory() { HostName = "helixlin1t.ynhh.org", UserName = "datasci", Password = "datascipw1!", RequestedHeartbeat = 30 };
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(20);

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
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(   queue: "hello", 
                                            durable: true, 
                                            exclusive: false, 
                                            autoDelete: false, 
                                            arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
        
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                     
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine(" [x] Received {0}", message);
                    };
                    consumer.Shutdown += (model, ea) =>
                    {
                        Console.WriteLine(" [xx] Queue consumer shutdown detected");
                    };
                    channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
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
