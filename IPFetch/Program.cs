using System.ServiceProcess;

namespace IPFetch
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new IPFetchService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
