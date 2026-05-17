using System;
using System.ServiceModel;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(DroneService)))
            {
                host.Open();
                Console.WriteLine("===========================================");
                Console.WriteLine("  DRONE WCF Server");
                Console.WriteLine("  Adress: net.tcp://localhost:4000/Drone");
                Console.WriteLine("  Server is open. Press ENTER to close it.");
                Console.WriteLine("===========================================");
                Console.ReadLine();
                host.Close();
            }
            Console.WriteLine("Server is closed.");
            Console.ReadLine();
        }
    }
}
