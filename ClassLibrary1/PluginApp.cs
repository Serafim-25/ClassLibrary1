using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace ClassLibrary1
{
    internal class PluginApp : IExternalApplication
    {
        private string apartamentMakerName;

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

            var roomNameName = "NameRoom";
            var roomName = (RibbonButton)ribbonPanel.AddItem(new PushButtonData(roomNameName, roomNameName, assemblyName, "ClassLibrary1.RoomName"));
            roomName.ToolTip = "Задать наименование помещениям";
            roomName.LongDescription = "Заполниться параметр \"Стиль помещений\" для всех жилых помещений";
            roomName.LargeImage = ImageSourceFromIcon(Properties.Resources._1);

            var apartamentMakerName = "ApartMaker";
            var apartamentMaker = (RibbonButton)ribbonPanel.AddItem(new PushButtonData(apartamentMakerName, apartamentMakerName, assemblyName, "ClassLibrary1.ApartamentMaker"));
            apartamentMaker.ToolTip = "Собрать квартиры";
            apartamentMaker.LongDescription = "Собрать помещения в квартиры и заполнить параметр \"Кврт.НомерКвартиры\"";
            apartamentMaker.LargeImage = ImageSourceFromIcon(Properties.Resources._2);

            var calculatedParametersName = "CalcParam";
            var calculatedParameters = (RibbonButton)ribbonPanel.AddItem(new PushButtonData(calculatedParametersName, calculatedParametersName, assemblyName, "ClassLibrary1.ApartmentCalculator"));
            calculatedParameters.ToolTip = "Расчитать параметры квартирографии";
            calculatedParameters.LongDescription = "Рассчитать и заполнить параметры" +
                "\"Кврт.ПлощадьКвартиры\", \"Кврт.ПлощадьКвартирыЖилая\", \"Кврт.ПлощадьКвартирыОбщая\", " +
                "\"Кврт.ЧислоКомнат\", \"Кврт.ИндексПомещения\"";
            calculatedParameters.LargeImage = ImageSourceFromIcon(Properties.Resources._3);

            var uploaderCSVCommandName = "UploadCSV";
            var uploaderCSVCommand = (RibbonButton)ribbonPanel.AddItem(new PushButtonData(uploaderCSVCommandName, uploaderCSVCommandName, assemblyName, "ClassLibrary1.UploaderCSV"));
            uploaderCSVCommand.ToolTip = "Выгрузить квартирографию в формате CSV";
            uploaderCSVCommand.LongDescription = "Выгрузить квартирографию в формате CSV. Файл сохранится рядом с файлом-RVT";
            uploaderCSVCommand.LargeImage = ImageSourceFromIcon(Properties.Resources._4);

            var cleanerName = "CleanParam";
            var cleaner = (RibbonButton)ribbonPanel.AddItem(new PushButtonData(cleanerName, cleanerName, assemblyName, "ClassLibrary1.Cleaner"));
            cleaner.ToolTip = "Очистить параметры квартирографии";
            cleaner.LargeImage = ImageSourceFromIcon(Properties.Resources._5);

            return Result.Succeeded;
        }

        public static ImageSource ImageSourceFromIcon (Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();
            return Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
            
    }
}
