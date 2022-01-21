using System;

namespace DBWireup
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Wirer wirer = new Wirer();
                wirer.Wireup();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to wireup databases!");
                Console.WriteLine("Exception: " + ex.ToString());
            }

            Console.ReadKey();
        }
    }
}
