using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    public class RoomPlacerTest : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document document = uIDocument.Document;

            //для теста берем любую 1 стену(даллее предполагаем, что эта стена - прямая)
            Wall wall = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().First();

            LocationCurve wallLine = wall.Location as LocationCurve;
            Curve wallCurve = wallLine.Curve;
            Line wline = wallLine.Curve as Line;
            XYZ directionLine = wline.Direction;
            UV vector = new UV(directionLine.Y, directionLine.X);
            IList<XYZ> wallPoints = wallCurve.Tessellate();
            

            XYZ point1 = wallPoints[0];
            XYZ point2 = wallPoints[1];
            UV uvRoom1 = new UV(((point1.X+point2.X)/2),((point1.Y+point2.Y/2)));
            double width = wall.Width;
            uvRoom1 = uvRoom1 + width*vector;


            Level levelRoom1 = new FilteredElementCollector(document).OfClass(typeof(Level)).FirstElement() as Level;

            using (Transaction transaction = new Transaction(document))
            {
                transaction.Start("Safety transaction");
                Room room = document.Create.NewRoom(levelRoom1, uvRoom1);
                if (null == room)
                {
                    transaction.Commit();
                    return Result.Failed;
                }
                transaction.Commit();
            }
            return Result.Succeeded;
        }
    }
}
