using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Pol = ClassLibrary2;
using Autodesk.Revit.Exceptions;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    internal class ApartamentMaker : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document document = uIDocument.Document;
            LevelsPicker levelsPicker = new LevelsPicker();
            IList<Reference> levelsRefs = new List<Reference>();
            try
            {
                levelsRefs = uIDocument.Selection.PickObjects(ObjectType.Element, levelsPicker, "Выберите уровни");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException e) { return Result.Failed; }
            List<Level> levels = new List<Level>();
            for (int j = 0; j < levelsRefs.Count; j++)
            {
                levels.Add(document.GetElement(levelsRefs[j]) as Level);
            }
            //List<Level> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).OfCategory(BuiltInCategory.OST_Levels).Cast<Level>().ToList();
            Dictionary<Level, int> lvlMinApartNumb = new Dictionary<Level, int>();
            TaskDialog.Show("Заголовок", "Начало команды");
            string nameApartmentPlaceType = "Кврт.ТипПомещения";
            string nameApartmentNumber = "Кврт.НомерКвартиры";
            string namePublicСorridor = "Межквартирный коридор";
            string nameHall = "Лифтовой холл";
            SpatialElement hall = null;
            SpatialElement publicСorridor = null;
            int currentNumberApartment = 1;
            const int numberNonResidentialPremises = 5;

            foreach (Level lvl in levels)
            {
                lvlMinApartNumb[lvl] = currentNumberApartment;
                Dictionary<string, List<SpatialElement>> ApartmentsRooms = new Dictionary<string, List<SpatialElement>>();
                Dictionary<string, nUV> Apartments = new Dictionary<string, nUV>(); // номер кв. и координата центра
                Dictionary<string, Dictionary<nUV, double>> ApartmentsPreliminary = new Dictionary<string, Dictionary<nUV, double>>(); // номер кв. и коорд<->площадь
                List<SpatialElement> rooms = new FilteredElementCollector(document).OfClass(typeof(SpatialElement))
                                                                                   .Cast<SpatialElement>()
                                                                                   .Where(it => it.Level.Id
                                                                                   .Equals(lvl.Id) && it.SpatialElementType == SpatialElementType.Room)
                                                                                   .ToList();
                List<FamilyInstance> windows = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance))
                                                                                     .OfCategory(BuiltInCategory.OST_Windows)
                                                                                     .Cast<FamilyInstance>()
                                                                                     .Where(it => it.LevelId.Equals(lvl.Id) && it.ToRoom != null && it.FromRoom != null)
                                                                                     .ToList();
                List<FamilyInstance> doors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance))
                                                                                   .OfCategory(BuiltInCategory.OST_Doors)
                                                                                   .Cast<FamilyInstance>().Where(it => it.LevelId
                                                                                   .Equals(lvl.Id) && it.ToRoom != null && it.FromRoom != null)
                                                                                   .ToList();
                List<FamilyInstance> opening = new List<FamilyInstance>();
                Dictionary<ElementId, Parameter[]> roomsDict = new Dictionary<ElementId, Parameter[]>();
                List<SpatialElement> nonResidentialPremisesRooms = new List<SpatialElement>();
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
                            else if (para.AsString() == nameHall) { hall = rooms[i]; }
                            else if (para.AsString() == namePublicСorridor) { publicСorridor = rooms[i]; }
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

                foreach (var roomm in rooms)
                {
                    Room room = roomm as Room;
                    double area = room.Area;
                    IEnumerator<GeometryObject> geometryEl = room.ClosedShell.GetEnumerator();
                    geometryEl.MoveNext();
                    Solid solid = geometryEl.Current as Solid;
                    nUV center = nUV.Zero;
                    if (solid != null && roomsDict[room.Id][1].AsString() != null && roomsDict[room.Id][0].AsInteger() != numberNonResidentialPremises)
                    {
                        center = new nUV(solid.ComputeCentroid().X, solid.ComputeCentroid().Y);
                    }
                    else continue;
                    if (!ApartmentsPreliminary.ContainsKey(roomsDict[room.Id][1].AsString()))
                    {
                        Dictionary<nUV, double> keyValuePairs = new Dictionary<nUV, double> { { center, room.Area } };
                        ApartmentsPreliminary.Add(roomsDict[room.Id][1].AsString(), keyValuePairs);
                    }
                    else
                    {
                        ApartmentsPreliminary[roomsDict[room.Id][1].AsString()].Add(center, area);
                    }
                }
                foreach (var apart in ApartmentsPreliminary)
                {
                    nUV numerator = new nUV(0, 0);
                    double denominator = 0;
                    foreach (var room in apart.Value)
                    {
                        numerator += room.Key * room.Value;
                        denominator += room.Value;
                    }
                    nUV center = numerator / denominator;
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
                nUV[] orderedPoints = TransformationFromPointToUV(Pol.PolygonCreator.Assembly(points));

                //выбор первой квартиры для нумерации:
                //находим координаты холла и коридора МОП
                UV hallPoint = new UV((hall.Location as LocationPoint).Point.X, (hall.Location as LocationPoint).Point.Y);
                UV publicCorridorPoint = new UV((publicСorridor.Location as LocationPoint).Point.X, (publicСorridor.Location as LocationPoint).Point.Y);
                //вектор холл -> коридор
                UV vectorHallToCorridor = publicCorridorPoint - hallPoint;
                UV normalLeftVector = new UV(-1 * vectorHallToCorridor.V, vectorHallToCorridor.U);
                //знак точки слева от вектора
                bool sign = AboveLine(hallPoint, publicCorridorPoint, hallPoint + normalLeftVector);
                List<UV> uvs = new List<UV>(); //точки слева от выхода из хола в коридор
                foreach (var t in orderedPoints)
                {
                    if (AboveLine(hallPoint, publicCorridorPoint, t) == sign) { uvs.Add(t); }
                }
                UV firstApartmentPoint = Closest(hallPoint, uvs);
                for (int i = 0; i < orderedPoints.Length; i++)
                {
                    if (orderedPoints[i] == firstApartmentPoint)
                    {
                        LeftShift(orderedPoints, i);
                    }
                }
                for (int i = 0; i < rooms.Count; i++)
                {
                    ParameterSet parSet = rooms[i].Parameters;
                    foreach (Parameter para in parSet)
                    {
                        if (para.IsShared && para.Definition.Name == nameApartmentNumber && para.AsString() != null && para.AsString().Contains("Кв."))
                        {
                            if (ApartmentsRooms.ContainsKey(para.AsString()))
                            {
                                ApartmentsRooms[para.AsString()].Add(rooms[i]);
                                continue;
                            }
                            ApartmentsRooms.Add(para.AsString(), new List<SpatialElement> { rooms[i] });
                        }
                    }
                }
                using (Transaction transaction1 = new Transaction(document))
                {
                    transaction1.Start("Safety transaction");
                    for (int i = lvlMinApartNumb[lvl]; i < lvlMinApartNumb[lvl] + Apartments.Count; i++)
                    {
                        int index = Array.IndexOf(orderedPoints, Apartments["Кв." + i.ToString()]);
                        foreach (var v in ApartmentsRooms["Кв." + i.ToString()])
                        {
                            roomsDict[v.Id][1].Set("Кв." + (index + lvlMinApartNumb[lvl]).ToString());
                        }
                    }
                    transaction1.Commit();
                }
                    
            }
            return Result.Succeeded;
        }
        public static bool AboveLine(UV p1, UV p2, UV p3)
        {
            return p3.V > p1.V + (p2.V - p1.V) / (p2.U - p1.U) * (p3.U - p1.U);
        }
        public static nUV TransformationFromPointToUV(Pol.Point point)
        {
            nUV uv = new nUV(point.X, point.Y);
            return uv;
        }
        public static nUV[] TransformationFromPointToUV(Pol.Point[] points)
        {
            nUV[] res = new nUV[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                res[i] = TransformationFromPointToUV(points[i]);
            }
            return res;
        }
        public static UV MostPerpendicular(UV p1, UV p2, List<UV> arr)
        {
            UV vector1 = p2 - p1;
            double minCos = 1;
            UV res = null;
            foreach (var p in arr)
            {
                UV vector2 = p - p1;
                double currentCos = Math.Abs(vector1.DotProduct(vector2) / (vector1.GetLength() * vector2.GetLength()));
                if (currentCos < minCos)
                {
                    minCos = currentCos;
                    res = p;
                }
            }
            return res;
        }
        public static UV Closest(UV p1, List<UV> arr)
        {
            double[] distance = new double[arr.Count];
            for (int i = 0; i < distance.Length; i++)
            {
                distance[i] = p1.DistanceTo(arr[i]);
            }
            return arr[Array.IndexOf(distance, distance.Min())];
        }
        public static void LeftShift<T>(T[] array, int positions)
        {
            int length = array.Length;
            positions = positions % length; // Handle positions greater than array length

            T[] temp = new T[positions];
            Array.Copy(array, temp, positions); // Copy the elements to a temporary array

            Array.Copy(array, positions, array, 0, length - positions); // Move remaining elements to the left
            Array.Copy(temp, 0, array, length - positions, positions); // Move elements from the temporary array to the end
        }
    }
}