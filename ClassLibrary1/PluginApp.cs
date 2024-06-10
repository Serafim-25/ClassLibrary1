using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ClassLibrary1
{
    internal class PluginApp : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            var assemblyName = Assembly.GetExecutingAssembly().Location;

            var tabName = "ApartBuild";
            var ribbonPanelName = "Квартирография";
            var commandName1 = "Распознование помещений";

            application.CreateRibbonTab(tabName);
            var ribbonPanel = application.CreateRibbonPanel(tabName, ribbonPanelName);

            //var firstCommandName = "Определение жилых помещений";
            //ribbonPanel.AddItem(new PushButtonData(firstCommandName, firstCommandName, assemblyName, "FirstPlugin.FirstCommand"));

            //var firstCommandName = "Сбор квартирографии";
            //ribbonPanel.AddItem(new PushButtonData(firstCommandName, firstCommandName, assemblyName, "FirstPlugin.FirstCommand"));

            //var firstCommandName = "Заполнение параметров квартир";
            //ribbonPanel.AddItem(new PushButtonData(firstCommandName, firstCommandName, assemblyName, "FirstPlugin.FirstCommand"));

            var UploaderCSVCommandName = "Выгрузка CSV";
            ribbonPanel.AddItem(new PushButtonData(UploaderCSVCommandName, UploaderCSVCommandName, assemblyName, "ClassLibrary1.UploaderCSV"));

            var Test= "Test";
            ribbonPanel.AddItem(new PushButtonData(Test, Test, assemblyName, "ClassLibrary1.Test"));

            var apartamentMaker = "ApartamentMaker";
            ribbonPanel.AddItem(new PushButtonData(apartamentMaker, apartamentMaker, assemblyName, "ClassLibrary1.ApartamentMaker"));

            ribbonPanel.AddItem(new PushButtonData(commandName1, commandName1, assemblyName, "ClassLibrary1.RoomName"));

            var cleaner = "Очистка параметров квартир";
            ribbonPanel.AddItem(new PushButtonData(cleaner, cleaner, assemblyName, "ClassLibrary1.Cleaner"));

            return Result.Succeeded;
        }
    }
}
