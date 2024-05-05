using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

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
            //TaskDialog.Show("Заголовок. Количество уровней", level.Count.ToString());

            Guid guidPlaceType = new Guid("15723a20-5ef0-40db-a947-2be11932e630"); //guid Кврт.ТипПомещения(5 тип - нежилое)
            Guid guidNumberApartment = new Guid("77f09e29-8873-4223-bdd9-4c9050cf18a4");// guid Кврт.НомерКвартиры
            int currentNumberApartment = 1;
            const int numberNonResidentialPremises = 5;

                for (int j = 0; j < level.Count; j++)
                {
                    using (Transaction transaction = new Transaction(document))
                    {
                        transaction.Start("Safety transaction");
                        List<FamilyInstance> windows = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Windows).Cast<FamilyInstance>().Where(it => it.LevelId.Equals(level[j].Id) && it.ToRoom != null && it.FromRoom != null).ToList();
                        List<FamilyInstance> doors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Doors).Cast<FamilyInstance>().Where(it => it.LevelId.Equals(level[j].Id) && it.ToRoom != null && it.FromRoom != null).ToList();
                        List<FamilyInstance> opening = new List<FamilyInstance>();
                        opening.AddRange(windows);
                        opening.AddRange(doors);
                        List<SpatialElement> rooms = new FilteredElementCollector(document).OfClass(typeof(SpatialElement)).Cast<SpatialElement>().Where(it => it.Level.Id.Equals(level[j].Id)).ToList();
                        Dictionary<ElementId, Parameter[]> roomsDict = new Dictionary<ElementId, Parameter[]>();

                        for (int i = 0; i < rooms.Count; i++)
                        {
                            ParameterSet parameterSet = rooms[i].Parameters;
                            Parameter roomType = null;
                            Parameter apartmentNumber = null;
                            foreach (Parameter para in parameterSet)
                            {
                                if (para.IsShared && para.GUID == guidNumberApartment) { apartmentNumber = para; }
                                else if (para.IsShared && para.GUID == guidPlaceType) { roomType = para; }
                            }
                            roomsDict.Add(rooms[i].Id, new Parameter[] { roomType, apartmentNumber });
                        }
                        try
                        {

                            for (int u = 0; u < rooms.Count; u++)
                            {
                                if (roomsDict[rooms[u].Id][0].AsInteger() != numberNonResidentialPremises && roomsDict[rooms[u].Id][1].AsString() == String.Empty)
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
                }
            return Result.Succeeded;
        }
    }
}
