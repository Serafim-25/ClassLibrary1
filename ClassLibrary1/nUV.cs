using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class nUV : UV, IEquatable<nUV>
    {
        public nUV(double p1, double p2) : base(p1, p2)
        { }
        public bool Equals(nUV other)
        {
            bool res = false;
            if (other.U == this.U && other.V == this.V) { res = true; }
            return res;
        }
        public static nUV operator +(nUV left, nUV right)
        {
            UV res = (UV)left + (UV)right;
            return new nUV(res.U, res.V);
        }
        public static nUV operator *(nUV left, double value)
        {
            UV res = (UV)left * value;
            return new nUV(res.U, res.V);
        }
        public static nUV operator /(nUV left, double value)
        {
            UV res = (UV)left / value;
            return new nUV(res.U, res.V);
        }
        public static new nUV Zero
        {
            get
            {
                return new nUV(0, 0);
            }
        }
    }
}
