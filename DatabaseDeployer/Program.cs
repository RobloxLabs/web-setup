using System;

namespace DatabaseDeployer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Deployer deployer = new Deployer();
                deployer.Deploy();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to deploy databases!");
                Console.WriteLine("Exception: " + ex.ToString());
            }

            //Console.ReadKey();
        }
    }
}
