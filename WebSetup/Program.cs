using System;
using DatabaseDeployer;
using DBWireup;

namespace WebSetup
{
    class Program
    {
        static void Main(string[] args)
        {
            Deployer.Run();
            Wirer.Run();
        }
    }
}
