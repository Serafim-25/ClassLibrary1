using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    public class RoomPlacerTest : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document document = uIDocument.Document;

            Level roomLevel = new FilteredElementCollector(document).OfClass(typeof(Level)).FirstElement() as Level;
            IEnumerable<Element> walls = new FilteredElementCollector(document).OfClass(typeof(Wall)).ToElements();


            // предполагаем, что эта стена - прямая
            foreach (var wallt in walls)
            {
                Wall wall = wallt as Wall;
                Line wline = (wall.Location as LocationCurve).Curve as Line;
                XYZ directionLine = wline.Direction;
                UV vector = new UV(directionLine.Y, directionLine.X);

                Curve wallCurve = (wall.Location as LocationCurve).Curve;
                double width = wall.Width;

                for (int i = 0; i < wallCurve.Length; i++)
                {
                    UV currentWallPoint = new UV(wallCurve.Evaluate(i, false)[0], wallCurve.Evaluate(i, false)[1]);

                    UV roomPos1 = currentWallPoint + 0.5 * width * vector;
                    UV roomPos2 = currentWallPoint - 0.5 * width * vector;


                    using (Transaction transaction = new Transaction(document))
                    {
                        transaction.Start("Safety transaction");

                        Room room1 = document.Create.NewRoom(roomLevel, roomPos1);
                        ElementId room1Id = room1.Id;
                        try
                        {
                            if (room1.Area == 0)
                            {
                                throw new Exception();
                            }
                        }
                        catch
                        {
                            document.Delete(room1Id);
                        }
                        Room room2 = document.Create.NewRoom(roomLevel, roomPos2);
                        ElementId room2Id = room2.Id;
                        try
                        {
                            if (room2.Area == 0)
                            {
                                throw new Exception();
                            }
                        }
                        catch
                        {
                            document.Delete(room2Id);
                        }

                        transaction.Commit();
                    }
                }
            }
            return Result.Succeeded;
        }
    }
}
