using System;
using DatabaseDeployer;
using DBWireup;

namespace WebSetup
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Deployer deployer = new Deployer();
                deployer.Deploy();
                Wirer wirer = new Wirer();
                wirer.Wireup();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to setup and configure website!");
                Console.WriteLine("Error: " + ex.Message); Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
            }
            Console.ReadKey();
        }
    }
}
