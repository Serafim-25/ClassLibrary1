﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Document = Autodesk.Revit.DB.Document;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal class RoomName : IExternalCommand
    {
        public const string Stove = "GS_600_Плита кухонная";
        public const string Bed = "Кровать";
        public const string Sofa = "Диван";
        public const string Shield = "GS_700_Щит квартирный ЩК";
        public const string Conditioner = "GS_500_Кондиционер";
        public const string Bath = "GS_620_Ванна стальная эмалированная";
        public const string Toilet = "GS_620_Унитаз керамический компакт";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //задать наименования и типы помещений сразу добавить коэффициенты к площадям лоджий/балконов
            //сортировка по этажам снчала пробегаемся по одному этажу, и так по всем по циклу
            //1 - жилые в кв
            //2 - нежилые в кв
            //3 - лоджии
            //4
            //5 - МОП

            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            // создаем список уровней
            List<Level> level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfCategory(BuiltInCategory.OST_Levels)
                .Cast<Level>()
                .ToList();

            for (int j = 0; j < level.Count; j++)
            {
                //создаем список помещений без имени на уровне j
               
                var rooms = new FilteredElementCollector(doc)
                .OfClass(typeof(SpatialElement)).Cast<SpatialElement>()
                .Where(it => it.SpatialElementType == SpatialElementType.Room)
                .Cast<Room>()
                .ToList();

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

                var dict = new Dictionary<string, ElementId>();
                foreach (var id in listKeyElementIds)
                {
                    dict.Add(doc.GetElement(id).Name, id);
                }

                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Safety transaction");

                    foreach (Room x in rooms)
                    {
                        //создаем список мебели в комнате и проверяем принадлежит ли она данной комнате
                        BoundingBoxXYZ bb = x.get_BoundingBox(null);

                        Outline outline = new Outline(bb.Min, bb.Max);

                        BoundingBoxIntersectsFilter filter
                          = new BoundingBoxIntersectsFilter(outline);

                        var furniture = new FilteredElementCollector(doc)
                            .OfClass(typeof(FamilyInstance))
                            .WherePasses(filter)
                            .ToList();

                        List<string> famnames = new List<string>(); //список имен семейств

                        //задаем наименования помещений
                        foreach (var f in furniture)
                        {
                            FamilyInstance finstance = f as FamilyInstance;
                            FamilySymbol ftype = finstance.Symbol;
                            string famname = ftype.FamilyName;
                            famnames.Add(famname);

                            if (famnames.Contains(Bath) && famnames.Contains(Toilet))
                            {
                                x.LookupParameter("Стиль помещений").Set(dict["Санузел"]);
                                x.LookupParameter("Кврт.ТипПомещения").Set(2);
                                x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                            }
                            else
                            {
                                switch (famname)
                                {
                                    case Stove:
                                        x.LookupParameter("Стиль помещений").Set(dict["Кухня"]);
                                        x.LookupParameter("Кврт.ТипПомещения").Set(2);
                                        x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                                        break;
                                    case Bed:
                                        x.LookupParameter("Стиль помещений").Set(dict["Спальня"]);
                                        x.LookupParameter("Кврт.ТипПомещения").Set(1);
                                        x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                                        break;
                                    case Sofa:
                                        x.LookupParameter("Стиль помещений").Set(dict["Гостиная"]);
                                        x.LookupParameter("Кврт.ТипПомещения").Set(1);
                                        x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                                        break;
                                    case Shield:
                                        x.LookupParameter("Стиль помещений").Set(dict["Холл"]);
                                        x.LookupParameter("Кврт.ТипПомещения").Set(2);
                                        x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                                        break;
                                    case Conditioner:
                                        x.LookupParameter("Стиль помещений").Set(dict["Лоджия"]);
                                        x.LookupParameter("Кврт.ТипПомещения").Set(3);
                                        x.LookupParameter("Кврт.КоэффициентПлощади").Set(0.5);
                                        break;
                                    case Bath:
                                        x.LookupParameter("Стиль помещений").Set(dict["Ванная"]);
                                        x.LookupParameter("Кврт.ТипПомещения").Set(2);
                                        x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                                        break;
                                    case Toilet:
                                        x.LookupParameter("Стиль помещений").Set(dict["Туалет1"]);
                                        x.LookupParameter("Кврт.ТипПомещения").Set(2);
                                        x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                                        break;
                                }
                            }
                            if (x.Name.Contains("Помещение"))
                            {
                                x.LookupParameter("Стиль помещений").Set(dict["Коридор"]);
                                x.LookupParameter("Кврт.ТипПомещения").Set(2);
                                x.LookupParameter("Кврт.КоэффициентПлощади").Set(1);
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
            return Result.Succeeded;
        }
    }
}