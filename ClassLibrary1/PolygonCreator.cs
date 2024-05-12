using System.Collections.Generic;
using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ClassLibrary2
{
    public static class PolygonCreator
    {
        static Random random = new Random();
        static Random random2 = new Random();
        static public Point[] Assembly(Point[] points)
        {
            Polygon myPolygon = new Polygon(points);
            shufflePolygon(ref myPolygon);
            randomTestPolygon(ref myPolygon, 250);
            Point[] res = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                res[i] = myPolygon.Edges[i].P1;
            }
            return res;
        }
        public static void randomTestPolygon(ref Polygon p, int t = 20)
        {
            int hasbeenbestfor = 0;
            double bestscore = p.Perimeter();
            Polygon pbest = p.Clone() as Polygon;
            while (hasbeenbestfor < t)
            {
                shufflePolygon(ref p);
                unWindPolygon(ref p);
                double currentScore = p.Perimeter();
                //Console.WriteLine("{0} {1} {2}", hasbeenbestfor, bestscore, currentScore);
                if (currentScore < bestscore)
                {
                    bestscore = currentScore;
                    pbest = p.Clone() as Polygon;
                    hasbeenbestfor = 0;
                }
                else
                {
                    hasbeenbestfor++;
                }
                p.Vertices = pbest.Vertices;
                p.Edges = pbest.Edges;
            }
        }
        public static bool isInInterval(double x0, double x1, double x2)
        {
            double eps = Math.Pow(10, -5);
            if (((x1 + eps) < x0 && x0 < (x2 - eps)) || ((x2 + eps) < x0 && x0 < (x1 - eps)))
            {
                return true;
            }
            else return false;
        }
        public static bool isIntersect(Line line1, Line line2)
        {
            Line l1 = line1.Clone() as Line;
            Line l2 = line2.Clone() as Line;
            if (l1.Length == 0 || l2.Length == 0) return false;
            while ((l1.P1.X == l1.P2.X) || (l2.P1.X == l2.P2.X))
            {
                l1.rotate(45 * Math.PI / 180);
                l2.rotate(45 * Math.PI / 180);
            }
            double a1 = (l1.P2.Y - l1.P1.Y) / (l1.P2.X - l1.P1.X);
            double a2 = (l2.P2.Y - l2.P1.Y) / (l2.P2.X - l2.P1.X);
            double b1 = l1.P1.Y - a1 * l1.P1.X;
            double b2 = l2.P1.Y - a2 * l2.P1.X;
            if (a1 == a2)
            {
                if (b1 == b2)
                {
                    if (isInInterval(l2.P1.X, l1.P1.X, l1.P2.X) ||
                        isInInterval(l2.P2.X, l1.P1.X, l1.P2.X) ||
                        isInInterval(l1.P1.X, l2.P1.X, l2.P2.X) ||
                        isInInterval(l1.P2.X, l2.P1.X, l2.P2.X))
                    {
                        return true;
                    }
                    else return false;
                }
                else return false;
            }
            else
            {
                double x = -(b2 - b1) / (a2 - a1);
                if (isInInterval(x, l1.P1.X, l1.P2.X) &&
                    isInInterval(x, l2.P1.X, l2.P2.X))
                {
                    return true;
                }
                else return false;
            }
        }
        public static bool anyIntersect(Polygon p)
        {
            bool[,] bools = new bool[p.N, p.N];
            for (int j = 0; j < p.N; j++)
            {
                for (int k = 0; k < p.N; k++)
                {
                    bools[j, k] = isIntersect(p.Edges[j], p.Edges[k]);
                }
            }
            foreach (var h in bools)
            {
                if (h == true) return true;
            }
            return false;
        }
        public static void unWindPolygon(ref Polygon p, int bailout = 100)
        {
            int t = 0;
            while (anyIntersect(p) && t < bailout)
            {
                t += 1;
                bool breakingFlag = false;
                for (int j = 0; j < p.N; j++)
                {
                    for (int k = 0; k < p.N; k++)
                    {
                        if (isIntersect(p.Edges[j], p.Edges[k]))
                        {
                            Point p12;
                            Point p22;
                            if (j < p.N - 1)
                            {
                                p12 = p.Vertices[j + 1];
                            }
                            else { p12 = p.Vertices[0]; }
                            if (k < p.N - 1)
                            {
                                p22 = p.Vertices[k + 1];
                                p.Vertices[k + 1] = p12;
                            }
                            else
                            {
                                p22 = p.Vertices[0];
                                p.Vertices[0] = p12;
                            }
                            if (j < p.N - 1) { p.Vertices[j + 1] = p22; }
                            else { p.Vertices[0] = p22; }
                            breakingFlag = true;
                            break;
                        }
                    }
                    if (breakingFlag) break;
                }
                p.Edges = new Polygon(p.Vertices).Edges;
            }
        }
        public static void shufflePolygon(ref Polygon p)
        {
            for (int i = p.Vertices.Length - 1; i >= 1; i--)
            {
                int j = random.Next(i + 1);
                // обменять значения data[j] и data[i]
                var temp = p.Vertices[j];
                (p.Vertices[j], p.Vertices[i]) = (p.Vertices[i], temp);
            }
            p.Edges = new Polygon(p.Vertices).Edges;
        }
    }
    public class Point : ICloneable
    {
        private double _X, _Y;
        public Point(double x, double y)
        {
            _X = x;
            _Y = y;
        }
        public double X
        {
            get { return _X; }
        }
        public double Y
        {
            get { return _Y; }
        }

        public object Clone()
        {
            return new Point(X, Y);
        }

        internal void rotate(double theta)
        {

            Matrix<double> A = DenseMatrix.OfArray(new double[,] {
                                {Math.Cos(theta),-Math.Sin(theta)},
                                {Math.Sin(theta),Math.Cos(theta)}});
            Vector<double> B = CreateVector.DenseOfArray(new double[] { X, Y });
            _X = A.Multiply(B)[0];
            _Y = A.Multiply(B)[1];
        }
    }
    public class Line : ICloneable
    {
        private Point _P1, _P2;
        public Line(Point p1, Point p2)
        {
            _P1 = p1;
            _P2 = p2;
        }
        public Point P1
        {
            get { return _P1; }
        }
        public Point P2
        {
            get { return _P2; }
        }
        public double Length
        {
            get
            {
                return Math.Sqrt(Math.Pow((P1.X - P2.X), 2) + Math.Pow((P1.Y - P2.Y), 2));
            }
        }

        public object Clone()
        {
            return new Line(P1.Clone() as Point, P2.Clone() as Point);
        }



        public void rotate(double theta)
        {
            _P1.rotate(theta);
            _P2.rotate(theta);
        }
    }
    public class Polygon : ICloneable
    {
        private int _N;
        private double _Perimeter;
        public int N
        {
            get { return _N; }
        }
        public Point[] Vertices;
        public List<Line> Edges = new List<Line>();

        public double Perimeter()
        {
            double per = 0;
            for (int i = 0; i < Edges.Count; i++)
            {
                per += Edges[i].Length;
            }
            _Perimeter = per;
            return per;
        }
        public Polygon(Point[] points)
        {
            _N = points.Length;
            Vertices = points;
            int k = 0;
            for (; k < N - 1; k++)
            {
                Edges.Add(new Line(points[k], points[k + 1]));
            }
            Edges.Add(new Line(points[N - 1], points[0]));
            double per = 0;
            for (int i = 0; i < Edges.Count; i++)
            {
                per += Edges[i].Length;
            }
            _Perimeter = per;
        }
        public void rotate(double theta)
        {
            for (int i = 0; i < N; i++)
            {
                Vertices[i].rotate(theta);
                Edges[i].rotate(theta);
            }
        }

        public object Clone() => new Polygon(Vertices);
        public void Print()
        {
            for (int i = 0; i < Edges.Count; i++)
            {
                Console.WriteLine("({0} {1})", Edges[i].P1.X, Edges[i].P1.Y);
            }
        }
    }
}
