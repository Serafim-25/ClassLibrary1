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

    internal class UploaderCSV : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            double factorConversionFootSqToMeterSq = 0.092903;
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
                .ToList();

            // Создание словаря Квартирографии: <int НомерКвартиры, список жилых помещений>
            var roomsApartments = new Dictionary<int, List<SpatialElement>>();

            foreach (SpatialElement room in rooms)
            {
                var parameterSet = room.Parameters;

                foreach (Parameter param in parameterSet)
                {
                    if (param.Definition.Name == nameAppointmentRoom && param.AsString() == valueAppointmentRoomLiving) 
                    {
                        foreach (Parameter paramApNum in parameterSet)
                        {
                            if (paramApNum.Definition.Name == nameApartmentNumber)
                            {
                                var valueApartmentNumber = int.Parse(paramApNum.AsValueString().Substring(3));
                                if (!roomsApartments.ContainsKey(valueApartmentNumber))
                                {
                                    roomsApartments.Add(valueApartmentNumber, new List<SpatialElement>());
                                }
                                roomsApartments[valueApartmentNumber].Add(room);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            // Создание нового словаря Квартирографии: <int НомерКвартиры, список жилых помещений>
            //var roomsApartments = new Dictionary<int, List<SpatialElement>>();
            //foreach (string numApart in roomsApartmentsTemp.Keys)
            //{
            //    roomsApartments.Add(int.Parse(numApart.Substring(3), roomsApartmentsTemp[numApart]);
            //}


            // Сортировка словаря Квартирографии
            roomsApartments = roomsApartments.OrderBy(it => it.Key).ToDictionary(it => it.Key, it => it.Value);

            // Создание базы данных для вывода CSV
            var apartments = new List<ApartmentCSV>();

            var roomsList = new List<string>()
            {
                "Kitchen",
                "Room1",
                "Room2",
                "Room3",
                "Room4",
                "Bathroom1",
                "CombinedBathroom1",
                "CombinedBathroom2",
                "Toilet1 ",
                "Toilet2 ",
                "Hall",
                "Hallway",
                "DressingRoom1",
                "DressingRoom2",
                "Storeroom1",
                "Storeroom2",
                "Loggia1K05",
                "Loggia2K05",
                "Loggia3K05",
                "Balcony1K03",
                "Balcony2K03",
                "Balcony3K03",
                "Loggia1K1",
                "Loggia2K1",
                "Loggia3K1",
                "Balcony1K1",
                "Balcony2K1",
                "Balcony3K1"
            };
            var roomsDictionary = new Dictionary<string, string>()
            {
                { "Кухня", "Kitchen"},
                { "Спальня1", "Room1"},
                { "Гостиная", "Room1"},
                { "Спальня2", "Room2"},
                { "Спальня3", "Room3"},
                { "Спальня4", "Room4"},
                { "Ванная", "Bathroom1"},
                { "Совмещенный санузел1", "CombinedBathroom1"},
                { "Совмещенный санузел2", "CombinedBathroom2"},
                { "Туалет1", "Toilet1 "},
                { "Туалет2", "Toilet2 "},
                { "Холл", "Hall"},
                { "Коридор", "Hallway"},
                { "Гардеробная1", "DressingRoom1"},
                { "Гардеробная2", "DressingRoom2"},
                { "Кладовая1", "Storeroom1"},
                { "Кладовая (гардероб)", "Storeroom1"},
                { "Кладовая2", "Storeroom2"},
                { "Лоджия1&K05", "Loggia1K05"},
                { "Лоджия2&K05", "Loggia2K05"},
                { "Лоджия3&K05", "Loggia3K05"},
                { "Балкон1&K03", "Balcony1K03"},
                { "Балкон2&K03", "Balcony2K03"},
                { "Балкон3&K03", "Balcony3K03"},
                { "Лоджия1&K1", "Loggia1K1"},
                { "Лоджия2&K1", "Loggia2K1"},
                { "Лоджия3&K1", "Loggia3K1"},
                { "Балкон1&K1", "Balcony1K1"},
                { "Балкон2&K1", "Balcony2K1"},
                { "Балкон3&K1", "Balcony3K1"}
            };

            var parametersNameNecessary = new List<string>()
                {
                    nameVerticalAxes,
                    nameHorizontalAxes,
                    nameBuilding,
                    nameSection,
                    nameEntranceNumber,
                    nameAppointmentRoom,
                    nameNumberOfRooms,
                    nameType,
                    nameLivingArea,
                    nameTotalArea,
                    nameReducedArea
                };
            foreach (List<SpatialElement> roomsApart in roomsApartments.Values)
            {
                var parameterSetFirstRoom = roomsApart[0].Parameters;               
                var parametersDicTemp = new Dictionary<string, string>();
                foreach (string paramName in parametersNameNecessary) 
                { 
                    foreach (Parameter param in parameterSetFirstRoom)
                    {
                        if (param.Definition.Name == paramName)
                        {
                            parametersDicTemp.Add(paramName, param.AsValueString());
                            break;
                        }
                    }
                }

                var roomsDicTemp = new Dictionary<string, string>();
                foreach (var room in roomsList)
                {
                    roomsDicTemp.Add(room, "0");
                }

                //var roomsDicTemp = new Dictionary<string, string>();
                foreach (SpatialElement room in roomsApart)
                {
                    var parameterSet = room.Parameters;
                    string roomStyle = null;
                    string roomArea = null;
                    string areaCoefficient = null;

                    foreach (Parameter param in parameterSet)
                    {
                        if (param.Definition.Name == nameRoomStyle) { roomStyle = param.AsValueString(); }
                        else if (param.Definition.Name == nameRoomArea) { roomArea = param.AsValueString(); }
                        else if (param.Definition.Name == nameAreaCoefficient) { areaCoefficient = param.AsValueString(); }
                    }

                    if (roomStyle.Contains("Лоджия") || roomStyle.Contains("Балкон"))
                    {
                        if (areaCoefficient == "0,5") { roomStyle += "&K05"; }
                        else if (areaCoefficient == "0,3") { roomStyle += "&K03"; }
                        else if (areaCoefficient == "1") { roomStyle += "&K1"; }
                    }
                    var a = parametersDicTemp[nameApartmentNumber];

                    roomsDicTemp[roomsDictionary[roomStyle]] = roomArea;
                    //roomsDicTemp.Add(roomsDictionary[roomStyle], roomArea);
                }

                apartments.Add(new ApartmentCSV
                {
                    ApartmentNumber = int.Parse(parametersDicTemp[nameApartmentNumber].Substring(3)),
                    VerticalAxes = parametersDicTemp[nameVerticalAxes],
                    HorizontalAxes = parametersDicTemp[nameHorizontalAxes],
                    Building = parametersDicTemp[nameBuilding],
                    Section = parametersDicTemp[nameSection],
                    EntranceNumber = parametersDicTemp[nameEntranceNumber],
                    Appointment = parametersDicTemp[nameAppointmentRoom],
                    NumberOfRooms = int.Parse(parametersDicTemp[nameNumberOfRooms]),
                    Type = parametersDicTemp[nameType],
                    LivingArea = Double.Parse(parametersDicTemp[nameLivingArea]),
                    TotalArea = Double.Parse(parametersDicTemp[nameTotalArea]),
                    ReducedArea = Double.Parse(parametersDicTemp[nameReducedArea]),
                    CheckingTotalArea = Double.Parse(parametersDicTemp[nameReducedArea]),
                    Kitchen = Math.Round(Double.Parse(roomsDicTemp["Kitchen"]), 2),
                    Room1 = Math.Round(Double.Parse(roomsDicTemp["Room1"]), 2),
                    Room2 = Math.Round(Double.Parse(roomsDicTemp["Room2"]), 2),
                    Room3 = Math.Round(Double.Parse(roomsDicTemp["Room3"]), 2),
                    Room4 = Math.Round(Double.Parse(roomsDicTemp["Room4"]) , 2),
                    Bathroom1 = Math.Round(Double.Parse(roomsDicTemp["Bathroom1"]), 2),
                    CombinedBathroom1 = Math.Round(Double.Parse(roomsDicTemp["CombinedBathroom1"]), 2),
                    CombinedBathroom2 = Math.Round(Double.Parse(roomsDicTemp["CombinedBathroom2"]), 2),
                    Hall = Math.Round(Double.Parse(roomsDicTemp["Hall"]), 2),
                    Hallway = Math.Round(Double.Parse(roomsDicTemp["Hallway"]), 2),
                    DressingRoom1 = Math.Round(Double.Parse(roomsDicTemp["DressingRoom1"]), 2),
                    DressingRoom2 = Math.Round(Double.Parse(roomsDicTemp["DressingRoom2"]), 2),
                    Storeroom1 = Math.Round(Double.Parse(roomsDicTemp["Storeroom1"]), 2),
                    Storeroom2 = Math.Round(Double.Parse(roomsDicTemp["Storeroom2"]), 2),
                    Loggia1K05 = Math.Round(Double.Parse(roomsDicTemp["Loggia1K05"]), 2),
                    Loggia2K05 = Math.Round(Double.Parse(roomsDicTemp["Loggia2K05"]), 2),
                    Loggia3K05 = Math.Round(Double.Parse(roomsDicTemp["Loggia3K05"]), 2),
                    NumberOfLoggias = (Convert.ToInt32(roomsDicTemp["Loggia1K05"] != "0")
                        + Convert.ToInt32(roomsDicTemp["Loggia2K05"] != "0")
                        + Convert.ToInt32(roomsDicTemp["Loggia3K05"] != "0")),
                    Balcony1K03 = Math.Round(Double.Parse(roomsDicTemp["Balcony1K03"]), 2),
                    Balcony2K03 = Math.Round(Double.Parse(roomsDicTemp["Balcony2K03"]), 2),
                    Balcony3K03 = Math.Round(Double.Parse(roomsDicTemp["Balcony3K03"]), 2),
                    NumberOfBalconies = (Convert.ToInt32(roomsDicTemp["Balcony1K03"] != "0")
                        + Convert.ToInt32(roomsDicTemp["Balcony2K03"] != "0")
                        + Convert.ToInt32(roomsDicTemp["Balcony3K03"] != "0")),
                    Loggia1K1 = Math.Round(Double.Parse(roomsDicTemp["Loggia1K1"]), 2),
                    Loggia2K1 = Math.Round(Double.Parse(roomsDicTemp["Loggia2K1"]), 2),
                    Loggia3K1 = Math.Round(Double.Parse(roomsDicTemp["Loggia3K1"]), 2),
                    Balcony1K1 = Math.Round(Double.Parse(roomsDicTemp["Balcony1K1"]), 2),
                    Balcony2K1 = Math.Round(Double.Parse(roomsDicTemp["Balcony2K1"]), 2),
                    Balcony3K1 = Math.Round(Double.Parse(roomsDicTemp["Balcony3K1"]), 2)
                });
            }

            var fullNamePathDoc = doc.PathName;
            var nameDirDoc = Path.GetDirectoryName(fullNamePathDoc);
            var nameDocRvt = fullNamePathDoc.Replace(nameDirDoc + "\\", "");
            var nameDoc = nameDocRvt.Replace(".rvt", "");
            var writer = new StreamWriter(nameDirDoc + "\\Квартирография_" + nameDoc + ".csv");
            var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteHeader<ApartmentCSV>();
            csv.NextRecord();
            foreach (var apartment in apartments)
            {
                csv.WriteRecord(apartment);
                csv.NextRecord();
            }

            return Result.Succeeded;
        }
    }
    public class ApartmentCSV
    {
        [Name("№ квартиры")]
        public int ApartmentNumber { get; set; }

        [Name("Оси верт.")]
        public string VerticalAxes { get; set; }

        [Name("Оси гор.")]
        public string HorizontalAxes { get; set; }

        [Name("Корпус")]
        public string Building { get; set; }

        [Name("Секция")]
        public string Section { get; set; }

        [Name("Номер подъезда")]
        public string EntranceNumber { get; set; }

        [Name("Назначение")]
        public string Appointment { get; set; }

        [Name("Число комнат")]
        public int NumberOfRooms { get; set; }

        [Name("Тип")]
        public string Type { get; set; }

        [Name("Площадь квартиры жилая")]
        public double LivingArea { get; set; }

        [Name("Общая площадь (без лоджий,балконов)")]
        public double TotalArea { get; set; }

        [Name("Приведенная площадь (с К*Sлоджии/балкона)")]
        public double ReducedArea { get; set; }

        [Name(" Кухня")]
        public double Kitchen { get; set; }

        [Name(" Комн.1")]
        public double Room1 { get; set; }

        [Name(" Комн.2")]
        public double Room2 { get; set; }

        [Name(" Комн.3")]
        public double Room3 { get; set; }

        [Name(" Комн.4")]
        public double Room4 { get; set; }

        [Name(" Ванная1")]
        public double Bathroom1 { get; set; }

        [Name(" Совмещенный санузел1")]
        public double CombinedBathroom1 { get; set; }

        [Name(" Совмещенный санузел2")]
        public double CombinedBathroom2 { get; set; }

        [Name(" Туалет1")]
        public double Toilet1 { get; set; }

        [Name(" Туалет2")]
        public double Toilet2 { get; set; }

        [Name(" Холл")]
        public double Hall { get; set; }

        [Name(" Коридор")]
        public double Hallway { get; set; }

        [Name(" Гардеробная1")]
        public double DressingRoom1 { get; set; }

        [Name(" Гардеробная2")]
        public double DressingRoom2 { get; set; }

        [Name(" Кладовая1")]
        public double Storeroom1 { get; set; }

        [Name(" Кладовая2")]
        public double Storeroom2 { get; set; }

        [Name(" Лоджия1(К=0,5)")]
        public double Loggia1K05 { get; set; }
        
        [Name(" Лоджия2(К=0,5)")]
        public double Loggia2K05 { get; set; }

        [Name(" Лоджия3(К=0,5)")]
        public double Loggia3K05 { get; set; }

        [Name(" Лоджии кол-во, шт")]
        public double NumberOfLoggias { get; set; }

        [Name(" Балкон1(К=0,3)")]
        public double Balcony1K03 { get; set; }

        [Name(" Балкон2(К=0,3)")]
        public double Balcony2K03 { get; set; }

        [Name(" Балкон3(К=0,3)")]
        public double Balcony3K03 { get; set; }

        [Name(" Балконы кол-во,шт.")]
        public double NumberOfBalconies { get; set; }

        [Name(" Лоджия1(К=1)")]
        public double Loggia1K1 { get; set; }

        [Name(" Лоджия2(К=1)")]
        public double Loggia2K1 { get; set; }

        [Name(" Лоджия3(К=1)")]
        public double Loggia3K1 { get; set; }

        [Name(" Балкон1(К=1)")]
        public double Balcony1K1 { get; set; }

        [Name(" Балкон2(К=1)")]
        public double Balcony2K1 { get; set; }

        [Name(" Балкон3(К=1)")]
        public double Balcony3K1 { get; set; }

        [Name(" Проверка Общая площадь  (без лоджий,балконов)")]
        public double CheckingTotalArea { get; set; }
    }

}