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

            // !!! Подумать про них !!!
            string nameVerticalAxes = "Кврт.СтроительныеОсиВерт";
            string nameHorizontalAxes = "Кврт.СтроительныеОсиГор";
            string nameBuilding = "Кврт.Корпус";
            string nameSection = "Кврт.НомерСекции";
            string nameEntranceNumber = "Кврт.НомерПодъезда";
            string nameRoomArea = "Площадь";

            // Создание списка помещений            
            var rooms = new FilteredElementCollector(doc)
                .OfClass(typeof(SpatialElement)).Cast<SpatialElement>()
                .Where(it => it.SpatialElementType == SpatialElementType.Room)
                .Cast<Room>()
                .ToList();          

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


            var transaction = new Transaction(doc, "Floor creation");
            transaction.Start();
            foreach (Room room in rooms)
            {
                var parameterSet = room.Parameters;

                
                foreach (Parameter param in parameterSet)
                {
                    room.LookupParameter("Стиль помещений").Set("Тарная");
                    if (param.Definition.Name == nameAppointmentRoom && param.AsString() == valueAppointmentRoomLiving)
                    {
                        foreach (Parameter paramForClean in parameterSet)
                        {
                            foreach (string nameParam in parametersNameNecessary)
                            {
                                if (paramForClean.Definition.Name == nameParam) 
                                { 
                                    paramForClean.Set(" ");
                                    paramForClean.Set(0);
                                    paramForClean.SetValueString(" ");
                                }
                            }
                        }
                        
                    }
                }
            }
            transaction.Commit();

            
            return Result.Succeeded;
        }
    }
    

}