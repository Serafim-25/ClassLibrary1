using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Pol = ClassLibrary2;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    internal class ApartamentMaker : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document document = uIDocument.Document;

            List<Level> level = new FilteredElementCollector(document).OfClass(typeof(Level)).OfCategory(BuiltInCategory.OST_Levels).Cast<Level>().ToList();
            TaskDialog.Show("Заголовок", "Начало команды");

            Guid guidPlaceType = new Guid("15723a20-5ef0-40db-a947-2be11932e630"); //guid Кврт.ТипПомещения(5 тип - нежилое)
            Guid guidNumberApartment = new Guid("77f09e29-8873-4223-bdd9-4c9050cf18a4");// guid Кврт.НомерКвартиры

            string nameApartmentPlaceType = "Кврт.ТипПомещения";
            string nameApartmentNumber = "Кврт.НомерКвартиры";
            string nameApartmentArea = "Кврт.ПлощадьКвартиры";
            string nameApartmentAreaResidential = "Кврт.ПлощадьКвартирыЖилая";
            string nameApartmentAreaGeneral = "Кврт.ПлощадьКвартирыОбщая";
            string nameApartmentNumberRooms = "Кврт.ЧислоКомнат";
            string nameApartmentRoomIndex = "Кврт.ИндексПомещения";

            int currentNumberApartment = 1;
            const int numberNonResidentialPremises = 5;

            
            for (int j = 0; j < level.Count; j++)
            {
                Dictionary<string, UV> Apartments = new Dictionary<string, UV>(); // номер кв. и координата центра
                Dictionary<string, Dictionary<UV, double>> ApartmentsPreliminary = new Dictionary<string, Dictionary<UV, double>>(); // номер кв. и коорд<->площадь
                List<SpatialElement> rooms = new FilteredElementCollector(document).OfClass(typeof(SpatialElement)).Cast<SpatialElement>().Where(it => it.Level.Id.Equals(level[j].Id)).ToList();
                List<FamilyInstance> windows = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Windows).Cast<FamilyInstance>().Where(it => it.LevelId.Equals(level[j].Id) && it.ToRoom != null && it.FromRoom != null).ToList();
                List<FamilyInstance> doors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Doors).Cast<FamilyInstance>().Where(it => it.LevelId.Equals(level[j].Id) && it.ToRoom != null && it.FromRoom != null).ToList();
                List<FamilyInstance> opening = new List<FamilyInstance>();
                Dictionary<ElementId, Parameter[]> roomsDict = new Dictionary<ElementId, Parameter[]>();
                using (Transaction transaction = new Transaction(document))
                {
                    transaction.Start("Safety transaction");
                    opening.AddRange(windows);
                    opening.AddRange(doors);
 
                    for (int i = 0; i < rooms.Count; i++)
                    {
                        ParameterSet parameterSet = rooms[i].Parameters;
                        Parameter roomType = null;
                        Parameter apartmentNumber = null;
                        foreach (Parameter para in parameterSet)
                        {
                            if (para.IsShared && para.Definition.Name == nameApartmentNumber) { apartmentNumber = para; }
                            else if (para.IsShared && para.Definition.Name == nameApartmentPlaceType) { roomType = para; }
                        }
                        roomsDict.Add(rooms[i].Id, new Parameter[] { roomType, apartmentNumber });
                    }
                    try
                    {

                        for (int u = 0; u < rooms.Count; u++)
                        {
                            if (roomsDict[rooms[u].Id][0].AsInteger() != numberNonResidentialPremises && roomsDict[rooms[u].Id][1].AsString() == string.Empty)
                            {
                                roomsDict[rooms[u].Id][1].Set("Кв." + currentNumberApartment.ToString());

                                for (int t = 0; t < opening.Count; t++)
                                {
                                    if (roomsDict[opening[t].ToRoom.Id][1].AsString() == "Кв." + currentNumberApartment.ToString())
                                    {
                                        if (roomsDict[opening[t].FromRoom.Id][0].AsInteger() != numberNonResidentialPremises && (roomsDict[opening[t].FromRoom.Id][1].AsString() != "Кв." + currentNumberApartment.ToString()))
                                        {
                                            roomsDict[opening[t].FromRoom.Id][1].Set("Кв." + currentNumberApartment.ToString());
                                            t = 0;
                                        }
                                    }
                                    else if (roomsDict[opening[t].FromRoom.Id][1].AsString() == "Кв." + currentNumberApartment.ToString())
                                    {
                                        if (roomsDict[opening[t].ToRoom.Id][0].AsInteger() != numberNonResidentialPremises && (roomsDict[opening[t].ToRoom.Id][1].AsString() != "Кв." + currentNumberApartment.ToString()))
                                        {
                                            roomsDict[opening[t].ToRoom.Id][1].Set("Кв." + currentNumberApartment.ToString());
                                            t = 0;
                                        }
                                    }
                                }
                                currentNumberApartment += 1;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TaskDialog.Show("Исключение", e.Message);
                        continue;
                    }
                    transaction.Commit();
                }
                //TaskDialog.Show("Заголовок", "Конец транзакции");
                foreach (var roomm in rooms)
                {
                    Room room = roomm as Room;
                    double area = room.Area;
                    IEnumerator<GeometryObject> geometryEl = room.ClosedShell.GetEnumerator();
                    geometryEl.MoveNext();
                    Solid solid = geometryEl.Current as Solid;
                    UV center = UV.Zero;
                    if (solid != null && roomsDict[room.Id][1].AsString() != null && roomsDict[room.Id][0].AsInteger() != numberNonResidentialPremises)
                    {
                        center = new UV(solid.ComputeCentroid().X, solid.ComputeCentroid().Y);
                    }
                    else continue;
                    if (!ApartmentsPreliminary.ContainsKey(roomsDict[room.Id][1].AsString()))
                    {
                        Dictionary<UV, double> keyValuePairs = new Dictionary<UV, double> { { center, room.Area } };
                        ApartmentsPreliminary.Add(roomsDict[room.Id][1].AsString(), keyValuePairs);
                    }
                    else
                    {
                        ApartmentsPreliminary[roomsDict[room.Id][1].AsString()].Add(center, area);
                    }
                }
                foreach (var apart in ApartmentsPreliminary)
                {
                    UV numerator = new UV(0, 0);
                    double denominator = 0;
                    foreach (var room in apart.Value)
                    {
                        numerator += room.Key * room.Value;
                        denominator += room.Value;
                    }
                    UV center = numerator / denominator;
                    Apartments.Add(apart.Key, center);
                }
                Pol.Point[] points = new Pol.Point[Apartments.Count];
                var apartEnumerator = Apartments.GetEnumerator();
                for (int i = 0; i < points.Length; i++)
                {
                    apartEnumerator.MoveNext();
                    var apart = apartEnumerator.Current;
                    points[i] = new Pol.Point(apart.Value.U, apart.Value.V);
                }
                Pol.Point[] orderedPoints = Pol.PolygonCreator.Assembly(points);
            }
            return Result.Succeeded;
        }
    }
}
