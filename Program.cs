using System;
using System.Diagnostics;
using System.Threading;
using temperature_logger.Modules;
using static temperature_logger.Modules.Display;

namespace temperature_logger
{
    public class Program
    {
        public static void Main()
        {
            Display.DisplayOled();
            Lysdioder.Setup(0.5);
            UpdateSystem();



            Debug.WriteLine("Hello from nanoFramework!");

            Thread.Sleep(Timeout.Infinite);


            // Browse our samples repository: https://github.com/nanoframework/samples
            // Check our documentation online: https://docs.nanoframework.net/
            // Join our lively Discord community: https://discord.gg/gCyBu8T
        }

        private static void UpdateSystem()
        {
            // Opdater displayet
            Display.ShowTemperatureDisplay();

            // Opdater lysdioder ud fra temperaturforskellen
            Lysdioder.UpdateStatus(Display.CurrentTemperature, Display.DesiredTemperature);
        }

    }
}
