using System;
using System.Reflection;
using Autodesk.Revit.UI;

namespace G3Plugins.Application
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            const string tabName = "G3 plugins";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // A aba já existe
            }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Cotas");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData buttonData = new PushButtonData(
                "DimensionWall",
                "Cotar\nParede",
                assemblyPath,
                "G3Plugins.Commands.DimensionWallCommand");

            panel.AddItem(buttonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}