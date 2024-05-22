using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    internal class LevelsPicker : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            bool res = false;
            if (elem is Level) { res = true; }
            return res;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
