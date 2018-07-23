using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace Kicad_gerber_panelizer
{
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
        public PolyLineSet.Bounds BoundingBox = new PolyLineSet.Bounds();
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

    class Gerber_utils
    {
        private PictureBox _pb;
        //List<string> outlinefiles = new List<string>();
        //List<string> millfiles = new List<string>();
        //List<string> copperfiles = new List<string>();
        String name;
        public List<Layer> layerList = new List<Layer>();
        double coordX;
        double coordY;
        String filePath;

        public Gerber_utils(PictureBox pb)
        {
            _pb = pb;
        }

        public void OpenDirectory(String[] FileNames, bool skipoutlines  = false)
        {
            string path = Path.GetDirectoryName(FileNames[0]);

            foreach (var F in FileNames)
            {
                BoardSide BS = BoardSide.Unknown;
                BoardLayer BL = BoardLayer.Unknown;
                String LN;

                if (Gerber.FindFileType(F) == BoardFileType.Gerber)
                {
                    String[] file = Gerber.DetermineBoardSideAndLayer(F, out BS, out BL , out LN);

                    Layer l = new Layer(F , BS, BL , file);

                    l.setCoord(0.0, 0.0);

                    coordX = 0.0;
                    coordY = 0.0;

                    filePath = path;
                    layerList.Add(l);
                }
            }
        }

        

        public void setName(String str )
        {
            name = str;
        }

        public String getName()
        {
            return name;
        }
        public String getProjectPath()
        {
            return filePath;
        }
    }
}
