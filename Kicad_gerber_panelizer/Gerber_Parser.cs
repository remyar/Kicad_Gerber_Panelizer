using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kicad_gerber_panelizer
{
    public class PathDefWithClosed
    {
        public bool Closed = false;
        public List<PointD> Vertices = new List<PointD>();
        public double Width;

        public override string ToString()
        {
            string r = "";
            foreach (var a in Vertices)
                r += a.ToString() + "  ";
            return string.Format("closed: {0} verts: ({1} {2}) ({3} {4})", Closed, Vertices[0], Vertices[1], Vertices[Vertices.Count() - 2], Vertices[Vertices.Count() - 1]);
        }
    }

    class Gerber_Parser
    {
        public class GerberParserState
        {
            public Dictionary<string, GerberApertureMacro> ApertureMacros = new Dictionary<string, GerberApertureMacro>();
            public Dictionary<int, GerberApertureType> Apertures = new Dictionary<int, GerberApertureType>();
            public PolyLine Boundary;
            public bool ClearanceMode = false;
            public GerberNumberFormat CoordinateFormat = new GerberNumberFormat();
            public GerberApertureType CurrentAperture = new GerberApertureType() { };
            public int CurrentLineIndex = 0;
            public bool IgnoreZeroWidth = false;
            public int LastD;
            public int LastShapeID = 0;
            // blank default
            public double LastX = 0;

            public double LastY = 0;
            public BoardLayer Layer;
            public double MinimumApertureRadius = -1;
            public InterpolationMode MoveInterpolation = InterpolationMode.Linear;
            public List<PolyLine> NewShapes = new List<PolyLine>();
            public List<PolyLine> NewThinShapes = new List<PolyLine>();
            public bool PolygonMode = false;
            public List<PointD> PolygonPoints = new List<PointD>();
            public bool PreCombinePolygons = true;
            public bool Repeater;
            public int RepeatStartShapeIdx;
            public int RepeatStartThinShapeIdx;
            public int RepeatXCount;
            public double RepeatXOff;
            public int RepeatYCount;
            public double RepeatYOff;
            public string SanitizedFile = "";
            public BoardSide Side;
            public PolyLine ThinLine;
        }

        public void ParseGerber274x(List<String> inputlines, bool parseonly, bool forcezerowidth = false, GerberParserState State = null)
        {
            if (State == null) State = new GerberParserState();

            State.CurrentAperture.ShapeType = GerberApertureShape.Empty;
            State.Apertures.Clear();
            State.ApertureMacros.Clear();

            State.CoordinateFormat = new GerberNumberFormat();
            State.CoordinateFormat.SetImperialMode();

            List<String> lines = Gerber.SanitizeInputLines(inputlines, State.SanitizedFile);

          /*  ParsedGerber Gerb = new ParsedGerber();

            Gerb.State = State;*/
            ParseGerber274_Lines(forcezerowidth, State, lines);
            
            //if (parseonly) return Gerb;
        }

        private static void ParseGerber274_Lines(bool forcezerowidth, GerberParserState State, List<String> lines)
        {
            while (State.CurrentLineIndex < lines.Count)
            {
                GCodeCommand GCC = new GCodeCommand();
                GCC.Decode(lines[State.CurrentLineIndex], State.CoordinateFormat);
            }
        }
    }
}
