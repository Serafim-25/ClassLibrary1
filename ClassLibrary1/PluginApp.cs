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
            

            application.CreateRibbonTab(tabName);
            var ribbonPanel = application.CreateRibbonPanel(tabName, ribbonPanelName);

            var roomNameName = "Распознование помещений";
            var roomName = ribbonPanel.AddItem(new PushButtonData(roomNameName, roomNameName, assemblyName, "ClassLibrary1.RoomName"));

            var apartamentMakerName = "ApartamentMaker";
            var apartamentMaker = ribbonPanel.AddItem(new PushButtonData(apartamentMakerName, apartamentMakerName, assemblyName, "ClassLibrary1.ApartamentMaker"));

            // !!! Задать правильное наименование класса !!!
            var calculatedParametersName = "ApartamentMaker";
            var calculatedParameters = ribbonPanel.AddItem(new PushButtonData(calculatedParametersName, calculatedParametersName, assemblyName, "ClassLibrary1.Test"));

            var uploaderCSVCommandName = "Выгрузка CSV";
            var uploaderCSVCommand = ribbonPanel.AddItem(new PushButtonData(uploaderCSVCommandName, uploaderCSVCommandName, assemblyName, "ClassLibrary1.UploaderCSV"));

            var cleanerName = "Очистка параметров квартир";
            var cleaner = ribbonPanel.AddItem(new PushButtonData(cleanerName, cleanerName, assemblyName, "ClassLibrary1.Cleaner"));

            return Result.Succeeded;
        }
    }
}
