using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace ClassLibrary1
{
    [Transaction(TransactionMode.Manual)]
    internal class ApartmentNumberer : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            return Result.Succeeded;
        }
        public Curve[] ExtractBoundary(string roomName, Level lvl, Document doc)
        {
            SpatialElement room = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).Cast<SpatialElement>().Where(t => t.Level.Id.Equals(lvl.Id) && t.Name == roomName && t.SpatialElementType == SpatialElementType.Room).ToList()[0];
            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;
            Curve[] curves = new Curve[room.GetBoundarySegments(options).ToArray().Count()];
            BoundarySegment[] segments = room.GetBoundarySegments(options)[0].ToArray();
            for (int i = 0; i < segments.Length; i++)
            {
                curves[i] = segments[i].GetCurve();
            }
            return curves;
        }
    }
    internal class ClosedCurve
    {
        private Curve[] _curves = null;
        private double _length = new double();
        public ClosedCurve(Curve[] curves)
        {
            this._curves = curves;
            _length = GetLength();
        }
        private double GetLength() 
        { 
            double length = 0;
            foreach (var crv in _curves)
            {
                length += crv.Length;
            }
            return length;
        }

    }
}
