using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Globalization;
using System.Windows.Data;
using Autodesk.Revit.DB.Architecture;
using System.IO;
using Autodesk.Revit.Creation;
using System.Xml.Linq;
using System.Reflection;
using FirstPlugin;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    internal class Cleaner : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            string nameAppointmentRoom = "Назначение";
            string valueAppointmentRoomLiving = "Жилое помещение";
            string nameApartmentNumber = "Кврт.НомерКвартиры";
            string nameReducedArea = "Кврт.ПлощадьКвартиры";
            string nameLivingArea = "Кврт.ПлощадьКвартирыЖилая";
            string nameTotalArea = "Кврт.ПлощадьКвартирыОбщая";
            string nameNumberOfRooms = "Кврт.ЧислоКомнат";
            string nameType = "Кврт.ИндексПомещения";
            string nameAreaCoefficient = "Кврт.КоэффициентПлощади";
            string nameRoomType = "Кврт.ТипПомещения";
            string nameRoomStyle = "Стиль помещений";

            // Создание списка помещений            
            var rooms = new FilteredElementCollector(doc)
                .OfClass(typeof(SpatialElement)).Cast<SpatialElement>()
                .Where(it => it.SpatialElementType == SpatialElementType.Room)
                .Cast<Room>()
                .ToList();          

            // Список наименований параметров, которые будем чистить
            var parametersNameNecessary = new List<string>()
                {
                    nameApartmentNumber,
                    nameReducedArea,
                    nameLivingArea,
                    nameTotalArea,
                    nameNumberOfRooms,
                    nameType,
                    nameAreaCoefficient,
                    nameRoomType,
                    nameRoomStyle
                };

            // Дастаем спецификацию с ключем для помещений, а именно с ключом "Стиль помещений"
            var viewKeySchedule = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>()
                .Where(it => it.Name == "АР_Спецификация стилей помещений")
                .First();

            // Делаем список ID всех объектов, которые связаны со спецификацией
            var listElementIds = viewKeySchedule.GetDependentElements(null).ToList();

            // Фильтруем список и оставляем только ID всех Element-ключей
            var listKeyElementIds = listElementIds.Where(it => doc.GetElement(it).GetType() == typeof(Element)
                && doc.GetElement(it).Name != "АР_Спецификация стилей помещений"
                && doc.GetElement(it).Name != "")
                .ToList();

            var transaction = new Transaction(doc, "Cleaner");
            transaction.Start();
            
            foreach (Room room in rooms)
            {
                var parameterSet = room.Parameters;
                foreach (Parameter param in parameterSet)
                {
                    if (param.Definition.Name == nameAppointmentRoom && param.AsString() == valueAppointmentRoomLiving)
                    {
                        int counter = 0;
                        foreach (Parameter paramForClean in parameterSet)
                        {
                            foreach (string nameParam in parametersNameNecessary)
                            {
                                if (paramForClean.Definition.Name == nameParam)
                                {
                                    paramForClean.Set("");
                                    paramForClean.Set(0);
                                    paramForClean.SetValueString("");
                                    if (paramForClean.Definition.Name == nameRoomStyle)
                                    {
                                        paramForClean.Set(listKeyElementIds[1]);
                                    }
                                    counter++;
                                    break;
                                }
                            }
                            if (counter == parametersNameNecessary.Count()) { break; }
                        }
                        break;
                    }
                }
            }

            transaction.Commit();          
            return Result.Succeeded;
        }
    }
    

}