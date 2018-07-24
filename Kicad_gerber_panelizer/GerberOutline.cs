using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Kicad_gerber_panelizer
{
    public class GerberOutline
    {
        public ParsedGerber TheGerber;
        public GerberOutline(string filename)
        {
            if (filename.Length > 0)
            {
                //TheGerber = LoadGerberFile(filename, true, false, new GerberParserState() { PreCombinePolygons = false });
                TheGerber.FixPolygonWindings();
                foreach (var a in TheGerber.OutlineShapes)
                {
                    a.CheckIfHole();
                }
            }
            else
            {
                TheGerber = new ParsedGerber();
            }
        }

        public GerberOutline(StreamReader sr, string originalfilename)
        {

            //TheGerber = LoadGerberFileFromStream(sr, originalfilename, true, false, new GerberParserState() { PreCombinePolygons = false });
            TheGerber.FixPolygonWindings();
            foreach (var a in TheGerber.OutlineShapes)
            {
                a.CheckIfHole();
            }

        }

        public PointD GetActualCenter()
        {
            return TheGerber.BoundingBox.Middle();

        }

        internal void BuildShapeCache()
        {
            TheGerber.BuildShapeCache();
        }
    }
}
