using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ClassLibrary1
{
    using FlatRoomsMap = Dictionary<string, List<Room>>;

    [Transaction(TransactionMode.Manual)]
    public class ApartmentCalculator : IExternalCommand
    {
        private const string c_flatNumberParameterName = "Кврт.НомерКвартиры";
        private const string c_roomsCountParameterName = "Кврт.ЧислоКомнат";
        private const string c_roomsTypeParameterName = "Кврт.ТипПомещения";
        private const string c_flatAreaParameterName = "Кврт.ПлощадьКвартиры";
        private const string c_flatLivingAreaParameterName = "Кврт.ПлощадьКвартирыЖилая";
        private const string c_flatTotalAreaParameterName = "Кврт.ПлощадьКвартирыОбщая";
        private const string c_RoomNameParameterName = "Имя";
        private const string c_RoomStyleParameterName = "Стиль помещений";
        private const string c_RoomIndexParameterName = "Кврт.ИндексПомещения";


        private FlatRoomsMap CalcFlatRoomsMap(List<Room> allRooms)
        {
            FlatRoomsMap result = new FlatRoomsMap();

            // Перебираем вообще все помещения
            foreach (Room room in allRooms)
            {

                // Поулчаем номер квартиры для этого помещения
                string flatNumber = room.LookupParameter(c_flatNumberParameterName).AsValueString();
                if (flatNumber == null || flatNumber == "")
                {
                    continue; // Иногда номера нет, просто пропускаем это помещение
                }

                if (result.ContainsKey(flatNumber))
                {
                    // Если уже встречали эту квартиру, то добавляем помещение в список
                    result[flatNumber].Add(room);
                }
                else
                {
                    // Если этого номера квартиры еще не было, создаем новый список помещений для этой квартиры
                    result.Add(flatNumber, new List<Room> { room });
                }
            }
            return result;
        }

        /*
        Кврт.ТипПомещения:
          1 - Жилая комната, спальня
          2 - Нежилые комнаты, кухня, ...
          3 - Лоджия, балкон
        */

        // Три предиката (функция, которая возвращает bool), нужны для SumArea:
        // Кварт.ПлощадьКвартиры - Это сумма всех помещений в рамках квартиры, Но без лоджии и балконов.
        private bool FlatAreaPredicate(Room room)
        {
            // Возвращаем true для всех помещений, кроме лоджий и балконов
            int roomType = room.LookupParameter(c_roomsTypeParameterName).AsInteger();
            return roomType != 3;
        }
        // Кварт.ПлощадьКвартирыЖилая – Это только сумма площадей спален.
        private bool FlatLivingAreaPredicate(Room room)
        {
            int roomType = room.LookupParameter(c_roomsTypeParameterName).AsInteger();
            return roomType == 1;
        }
        // Кварт.ПлощадьКвартирыОбщая – Это сумма всех помещений в рамках квартиры, включая лоджии и балконы
        private bool FlatTotalAreaPredicate(Room room)
        {
            return true;
        }

        // Суммируем площадь помещений, которые подошли под условие roomPredicate
        private double SumArea(List<Room> rooms, Predicate<Room> roomPredicate)
        {
            double sumArea = 0.0;
            foreach (Room room in rooms)
            {
                if (roomPredicate(room))
                {
                    int roomType = room.LookupParameter(c_roomsTypeParameterName).AsInteger();
                    if (roomType == 3)
                        sumArea += room.Area * 0.5;
                    else
                        sumArea += room.Area;


                }
            }
            return sumArea;
        }

        // Рассчитать и записать параметры в модель
        private void CalcAndAddParameters(Document doc, FlatRoomsMap flatRoomsMap, List<Room> allRooms)
        {
            foreach (var item in flatRoomsMap)
            {

                // Все помещения из текущей квартиры
                List<Room> rooms = item.Value;
                // item.Key - номер этой квартиры

                // Рассчитываем параметры по списку всех помещений в квартире
                int roomsCount = rooms.Count(FlatLivingAreaPredicate);
                double area = SumArea(rooms, FlatAreaPredicate);
                double livingArea = SumArea(rooms, FlatLivingAreaPredicate);
                double totalArea = SumArea(rooms, FlatTotalAreaPredicate);

                // Транзакция для того, чтобы поменять параметры
                using (Transaction t = new Transaction(doc, "Add parameters"))
                {
                    t.Start();

                    foreach (Room room in item.Value)
                    {
                        // Ищем нужный паарметр и меняем его
                        {
                            Parameter parameter = room.LookupParameter(c_roomsCountParameterName);
                            parameter.Set(roomsCount);
                        }
                        {
                            Parameter parameter = room.LookupParameter(c_flatAreaParameterName);
                            parameter.Set(area);
                        }
                        {
                            Parameter parameter = room.LookupParameter(c_flatLivingAreaParameterName);
                            parameter.Set(livingArea);
                        }
                        {
                            Parameter parameter = room.LookupParameter(c_flatTotalAreaParameterName);
                            parameter.Set(totalArea);
                        }
                    }

                    // Подтверждаем транзакцию
                    t.Commit();
                }

            }
        }

        private void SetRoomsStyle(Document doc, FlatRoomsMap flatRoomsMap)
        {
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

            Dictionary<string, ElementId> map = new Dictionary<string, ElementId>();
            foreach (var id in listKeyElementIds)
            {
                Element e = doc.GetElement(id);
                map.Add(e.Name, id);
            }


            foreach (var flat in flatRoomsMap)
            {
                Dictionary<string, List<Room>> nameMap = new Dictionary<string, List<Room>>();

                bool haveLivingRoom = false; // Есть ли гостинная

                // Составляем словарь. Ключ - например "Спальня". Значение - список комнат с таким именем ("Спальня")
                foreach (Room room in flat.Value)
                {
                    string name = room.LookupParameter(c_RoomStyleParameterName).AsValueString();
                    if (name == null)
                    {
                        continue; // Иногда номера нет, просто пропускаем это помещение
                    }
                    name = Regex.Replace(name, @"[\d-]", string.Empty);
                    if (nameMap.ContainsKey(name))
                    {
                        nameMap[name].Add(room);
                    }
                    else
                    {
                        nameMap.Add(name, new List<Room> { room });
                    }
                    if (name == "Гостиная")
                    {
                        haveLivingRoom = true;
                    }
                }

                // Переименовываем список комнат (если число комнат с одинаковым именем больше 1)
                foreach (var pair in nameMap)
                {
                    if ((pair.Value.Count > 1) || (haveLivingRoom && pair.Key == "Спальня"))
                    {
                        int index = 1;
                        if (haveLivingRoom && pair.Key == "Спальня")
                        {
                            index = 2;
                        }
                        foreach (Room room in pair.Value)
                        {
                            using (Transaction t = new Transaction(doc, "Rename rooms"))
                            {
                                t.Start();

                                Parameter parameter = room.LookupParameter("Стиль помещений");
                                string str = pair.Key + index;
                                if (!map.ContainsKey(str))
                                {
                                    continue;

                                }
                                var id = map[str];

                                parameter.Set(id);
                                index++;

                                t.Commit();
                            }

                        }
                    }
                }
            }
        }

        private void SetRoomsIndex(Document doc, FlatRoomsMap flatRoomsMap)
        {
            foreach (var flat in flatRoomsMap)
            {
                var rooms = flat.Value;



                // Нашли кухню
                Room kitchen = rooms.Find(
                    (Room room) => {
                        string name = room.LookupParameter(c_RoomNameParameterName).AsValueString();
                        return name == "Кухня";
                    });

                bool isStudio = kitchen.Area * 0.092903 < 8; ; // Это студия?
                bool isEuro = kitchen.Area * 0.092903 > 14; // Это евро?

                // Присваиваем индекс в зависимости от двух флагов (isStudio, isEuro)
                foreach (Room room in rooms)
                {
                    int roomsCount = room.LookupParameter(c_roomsCountParameterName).AsInteger();

                    using (Transaction t = new Transaction(doc, "Set room index"))
                    {
                        t.Start();

                        Parameter parameter = room.LookupParameter(c_RoomIndexParameterName);

                        if (isEuro)
                        {
                            parameter.Set(roomsCount.ToString() + " евро");
                        }
                        else if (isStudio)
                        {
                            parameter.Set("Студия");
                        }
                        else
                        {
                            parameter.Set(roomsCount.ToString());
                        }

                        t.Commit();
                    }
                }
            }
        }

        // Главная функция
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Текущий документ
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Получаем все комнаты
            FilteredElementCollector roomFilter = new FilteredElementCollector(doc);
            List<Room> allRooms = roomFilter.OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Room>().ToList<Room>();

            FlatRoomsMap flatRoomsMap = CalcFlatRoomsMap(allRooms);
            CalcAndAddParameters(doc, flatRoomsMap, allRooms);

            SetRoomsStyle(doc, flatRoomsMap);
            SetRoomsIndex(doc, flatRoomsMap);

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}
