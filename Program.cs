namespace StockMarketNSC
{
    internal static class Program
    {
        static void Main()
        {
#if DEBUG
            Service1 MyService = new Service1();
            MyService.OnDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
                                   ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
