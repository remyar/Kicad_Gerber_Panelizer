using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Polygons = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using ClipperLib;

namespace Kicad_gerber_panelizer
{
    public class AngledThing
    {
        public PointF Center = new PointF(); // float for serializer... need to investigate
        public float Angle;
    }

    public class GerberInstance : AngledThing
    {
        public string GerberPath;
        public bool Generated = false;

        [System.Xml.Serialization.XmlIgnore]
        public List<PolyLine> TransformedOutlines = new List<PolyLine>();

        [System.Xml.Serialization.XmlIgnore]
        public List<List<PolyLine>> OffsetOutlines = new List<List<PolyLine>>();

        [System.Xml.Serialization.XmlIgnore]
        internal float LastAngle;
        [System.Xml.Serialization.XmlIgnore]
        internal PointD LastCenter;

        [System.Xml.Serialization.XmlIgnore]
        public List<BreakTab> Tabs = new List<BreakTab>();

        [System.Xml.Serialization.XmlIgnore]
        public Bounds BoundingBox = new Bounds();
        internal void CreateOffsetLines(double extradrilldistance)
        {
            OffsetOutlines = new List<List<PolyLine>>(TransformedOutlines.Count);
            for (int i = 0; i < TransformedOutlines.Count; i++)
            {
                var L = new List<PolyLine>();
                Polygons clips = new Polygons();
                var poly = TransformedOutlines[i].toPolygon();
                bool winding = Clipper.Orientation(poly);

                clips.Add(poly);
                double offset = 0.25 * 100000.0f + extradrilldistance;
                if (winding == false) offset *= -1;
                Polygons clips2 = Clipper.OffsetPolygons(clips, offset, JoinType.jtRound);
                foreach (var a in clips2)
                {
                    PolyLine P = new PolyLine();
                    P.fromPolygon(a);
                    L.Add(P);
                }

                OffsetOutlines.Add(L);
            }

        }

        public void RebuildTransformed(GerberOutline gerberOutline, double extra)
        {
            BoundingBox.Reset();
            LastAngle = Angle;
            LastCenter = new PointD(Center.X, Center.Y);
            TransformedOutlines = new List<PolyLine>();
            var GO = gerberOutline;
            foreach (var b in GO.TheGerber.OutlineShapes)
            {
                PolyLine PL = new PolyLine();
                PL.FillTransformed(b, new PointD(Center), Angle);
                TransformedOutlines.Add(PL);
                BoundingBox.AddPolyLine(PL);
            }
            CreateOffsetLines(extra);



        }
    }

    public class BreakTab : AngledThing
    {
        public float Radius;
        public bool Valid;

        [System.Xml.Serialization.XmlIgnore]
        public List<string> Errors = new List<string>();

        [System.Xml.Serialization.XmlIgnore]
        public int EvenOdd;

    }
}
