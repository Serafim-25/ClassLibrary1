using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    internal class Tests : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIDocument = commandData.Application.ActiveUIDocument;
            Document document = uIDocument.Document;
            Point p1 = Point.Create(new XYZ(1, 2, 3));
            Point p2 = Point.Create(new XYZ(1, 2, 3));
            bool check = p1.Equals(p2);
            TaskDialog.Show("ПРОВЕРКА", check.ToString());
            return Result.Succeeded;
        }
    }
}
