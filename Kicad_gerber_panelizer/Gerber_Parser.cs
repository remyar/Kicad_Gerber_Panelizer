using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClipperLib;
using System.Text.RegularExpressions;
using Polygon = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Polygons = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

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

    class Gerber_Parser
    {


        public ParsedGerber ParseGerber274x(List<String> inputlines, bool parseonly, bool forcezerowidth = false, GerberParserState State = null)
        {
            if (State == null) State = new GerberParserState();

            State.CurrentAperture.ShapeType = GerberApertureShape.Empty;
            State.Apertures.Clear();
            State.ApertureMacros.Clear();

            State.CoordinateFormat = new GerberNumberFormat();
            State.CoordinateFormat.SetImperialMode();

            List<String> lines = Gerber.SanitizeInputLines(inputlines, State.SanitizedFile);

            ParsedGerber Gerb = new ParsedGerber();

            Gerb.State = State;
            ParseGerber274_Lines(forcezerowidth, State, lines);
            
            if (parseonly) return Gerb;

            if (State.PreCombinePolygons)
            {
                Polygons solution = new Polygons();
                {


                    Polygons clips = new Polygons();
                    for (int i = 0; i < State.NewShapes.Count; i++)
                    {

                        clips.Add(State.NewShapes[i].toPolygon());
                    }
                    Clipper cp = new Clipper();
                    cp.AddPolygons(solution, PolyType.ptSubject);
                    cp.AddPolygons(clips, PolyType.ptClip);

                    cp.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

                }
                for (int i = 0; i < solution.Count; i++)
                {
                    PolyLine PL = new PolyLine();

                    PL.fromPolygon(solution[i]);
                    PL.MyColor = Color.FromArgb(255, 200, 128, 0);
                    Gerb.DisplayShapes.Add(PL);
                }
            }
            else
            {
                for (int i = 0; i < State.NewShapes.Count; i++)
                {

                    PolyLine PL = new PolyLine();
                    PL.ClearanceMode = State.NewShapes[i].ClearanceMode;
                    PL.fromPolygon(State.NewShapes[i].toPolygon());
                    Gerb.DisplayShapes.Add(PL);
                }
            }

            List<PathDefWithClosed> shapelist = new List<PathDefWithClosed>();
            for (int i = 0; i < State.NewThinShapes.Count; i++)
            {
                //     DisplayShapes.Add(NewThinShapes[i]);
                shapelist.Add(new PathDefWithClosed() { Vertices = State.NewThinShapes[i].Vertices, Width = State.NewThinShapes[i].Width });
            }

            var shapeslinked = Helpers.LineSegmentsToPolygons(shapelist);

            foreach (var a in shapeslinked)
            {
                PolyLine PL = new PolyLine() { ID = State.LastShapeID++ };
                PL.Vertices = a.Vertices;
                PL.Thin = true;
                Gerb.DisplayShapes.Add(PL);
                Gerb.Shapes.Add(PL);
            }

            if (State.PreCombinePolygons)
            {
                //Console.WriteLine("Combining polygons - this may take some time..");
                Polygons solution2 = new Polygons();

                for (int i = 0; i < Gerb.DisplayShapes.Count; i++)
                {
                    //Progress("Executing polygon merge", i, Gerb.DisplayShapes.Count);
                    Polygons clips = new Polygons();

                    clips.Add(Gerb.DisplayShapes[i].toPolygon());
                    Clipper cp = new Clipper();
                    cp.AddPolygons(solution2, PolyType.ptSubject);
                    cp.AddPolygons(clips, PolyType.ptClip);

                    cp.Execute(ClipType.ctUnion, solution2, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                }

                //    DisplayShapes.Clear();
                for (int i = 0; i < solution2.Count; i++)
                {
                    //Progress("Converting back to gerber", i, Gerb.DisplayShapes.Count);

                    PolyLine PL = new PolyLine() { ID = State.LastShapeID++ };
                    PL.fromPolygon(solution2[i]);
                    PL.MyColor = Color.FromArgb(255, 200, 128, 0);
                    Gerb.OutlineShapes.Add(PL);
                }
            }
            else
            {

                for (int i = 0; i < Gerb.DisplayShapes.Count; i++)
                {
                    PolyLine PL = new PolyLine() { ID = State.LastShapeID++ };
                    PL.fromPolygon(Gerb.DisplayShapes[i].toPolygon());
                    PL.ClearanceMode = Gerb.DisplayShapes[i].ClearanceMode;

                    Gerb.OutlineShapes.Add(Gerb.DisplayShapes[i]);

                }
            }
            Gerb.CalcPathBounds();
            Gerb.State = State;
            return Gerb;
        }

        private static bool BasicLineCommands(string Line, GerberParserState State)
        {
            string FinalLine = Line.Replace("%", "").Replace("*", "").Trim();
            switch (FinalLine)
            {
                case "G90": State.CoordinateFormat.Relativemode = false; break;
                case "G91": State.CoordinateFormat.Relativemode = true; break;
                case "G71": State.CoordinateFormat.SetMetricMode(); break;
                case "G70": State.CoordinateFormat.SetImperialMode(); break;
                case "G74": State.CoordinateFormat.SetSingleQuadrantMode(); break;
                case "G75":
                    State.CoordinateFormat.SetMultiQuadrantMode();
                    break;
                case "G36":
                    State.PolygonMode = true;
                    State.PolygonPoints.Clear();
                    break;
                case "G37":
                    {
                        PolyLine PL = new PolyLine() { ID = State.LastShapeID++ };
                        foreach (var a in State.PolygonPoints)
                        {
                            PL.Add(a.X, a.Y);
                        }
                        PL.Close();
                        State.NewShapes.Add(PL);
                        State.PolygonPoints.Clear();

                        State.PolygonMode = false;
                    }
                    break;
                case "LPC": State.ClearanceMode = true; break;
                case "LPD": State.ClearanceMode = false; break;
                case "MOIN": State.CoordinateFormat.SetImperialMode(); break;
                case "MOMM": State.CoordinateFormat.SetMetricMode(); break;
                case "G01":
                    State.MoveInterpolation = InterpolationMode.Linear;
                    break;
                case "G02":
                    State.MoveInterpolation = InterpolationMode.ClockWise;
                    break;
                case "G03":
                    State.MoveInterpolation = InterpolationMode.CounterClockwise;
                    break;

                default:
                    return false;
            }

            return true;
        }

        private static void AddExtrudedCurveSegment(ref double LastX, ref double LastY, List<PolyLine> NewShapes, GerberApertureType CurrentAperture, bool ClearanceMode, double X, double Y, int ShapeID)
        {
            PolyLine PL = new PolyLine() { ID = ShapeID };
            PL.ClearanceMode = ClearanceMode;

            // TODO: use CreatePolyLineSet and extrude that!
            var PolySet = CurrentAperture.CreatePolyLineSet(0, 0);

            foreach (var currpoly in PolySet)
            {
                Polygons Combined = new Polygons();

                PolyLine A = new PolyLine() { ID = ShapeID };
                PolyLine B = new PolyLine() { ID = ShapeID };

                PointD start = new PointD(LastX, LastY);
                PointD end = new PointD(X, Y);
                PointD dir = end - start;

                dir.Normalize();
                //  dir.Rotate(180);
                dir = dir.Rotate(90);
                PointD LeftMost = new PointD();
                double maxdot = -10000000;
                double mindot = 10000000;
                PointD RightMost = new PointD();
                {
                    for (int i = 0; i < currpoly.Count(); i++)
                    {
                        PointD V = currpoly.Vertices[i];
                        //    PointD V2 = new PointD(V.X, V.Y);
                        // V2.Normalize();
                        double dot = V.Dot(dir);
                        //      Console.WriteLine("dot: {0}  {1}, {2}", dot.ToString("n2"), V, dir);
                        if (dot > maxdot)
                        {
                            RightMost = V; maxdot = dot;
                        }
                        if (dot < mindot)
                        {
                            LeftMost = V; mindot = dot;
                        }

                        A.Add(V.X + LastX, V.Y + LastY);
                        B.Add(V.X + X, V.Y + Y);
                    }

                    A.Close();
                    B.Close();

                    PolyLine C = new PolyLine() { ID = ShapeID }; ;
                    C.Add(RightMost.X + LastX, RightMost.Y + LastY);
                    C.Add(LeftMost.X + LastX, LeftMost.Y + LastY);
                    C.Add(LeftMost.X + X, LeftMost.Y + Y);
                    C.Add(RightMost.X + X, RightMost.Y + Y);
                    C.Vertices.Reverse();
                    C.Close();

                    Clipper cp = new Clipper();
                    cp.AddPolygons(Combined, PolyType.ptSubject);
                    cp.AddPolygon(A.toPolygon(), PolyType.ptClip);
                    cp.Execute(ClipType.ctUnion, Combined, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

                    Clipper cp2 = new Clipper();
                    cp2.AddPolygons(Combined, PolyType.ptSubject);
                    cp2.AddPolygon(B.toPolygon(), PolyType.ptClip);
                    cp2.Execute(ClipType.ctUnion, Combined, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

                    Clipper cp3 = new Clipper();
                    cp3.AddPolygons(Combined, PolyType.ptSubject);
                    cp3.AddPolygon(C.toPolygon(), PolyType.ptClip);
                    cp3.Execute(ClipType.ctUnion, Combined, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

                    foreach (var a in Combined)
                    {
                        PolyLine PP = new PolyLine() { ID = ShapeID }; ;
                        PP.fromPolygon(a);
                        PP.Close();
                        NewShapes.Add(PP);
                    }
                }
            }

            LastX = X;
            LastY = Y;
        }

        private static void DoRepeating(GerberParserState State)
        {
            int LastThin = State.NewThinShapes.Count();
            int LastShape = State.NewShapes.Count();
            for (int x = 0; x < State.RepeatXCount; x++)
            {
                for (int y = 0; y < State.RepeatYCount; y++)
                {
                    if (!(x == 0 && y == 0))
                    {
                        double xoff = State.RepeatXOff * x;
                        double yoff = State.RepeatYOff * y;
                        int LastShapeID = -1;
                        for (int i = State.RepeatStartThinShapeIdx; i < LastThin; i++)
                        {
                            var C = State.NewThinShapes[i];
                            if (LastShapeID != C.ID)
                            {
                                State.LastShapeID++;
                                LastShapeID = C.ID;
                            }
                            PolyLine P = new PolyLine() { ID = State.LastShapeID };
                            foreach (var a in C.Vertices)
                            {
                                P.Vertices.Add(new PointD(a.X + xoff, a.Y + yoff));
                            }
                            State.NewThinShapes.Add(P);
                        }
                        for (int i = State.RepeatStartShapeIdx; i < LastShape; i++)
                        {
                            var C = State.NewShapes[i];
                            if (LastShapeID != C.ID)
                            {
                                State.LastShapeID++;
                                LastShapeID = C.ID;
                            }
                            PolyLine P = new PolyLine() { ID = State.LastShapeID };
                            P.Width = C.Width;
                            foreach (var a in C.Vertices)
                            {
                                P.Vertices.Add(new PointD(a.X + xoff, a.Y + yoff));

                            }
                            State.NewShapes.Add(P);

                        }

                    }
                }
            }
            State.Repeater = false;
        }

        private static void SetupRepeater(GerberParserState State, int Xcount, int Ycount, double Xoff, double Yoff)
        {
            if (State.Repeater == true) DoRepeating(State);

            State.Repeater = (Xcount * Ycount > 1) ? true : false;
            State.RepeatXCount = Xcount;
            State.RepeatYCount = Ycount;
            State.RepeatXOff = Xoff;
            State.RepeatYOff = Yoff;

            State.RepeatStartThinShapeIdx = State.NewThinShapes.Count();
            State.RepeatStartShapeIdx = State.NewShapes.Count();
        }

        private static void ParseGerber274_Lines(bool forcezerowidth, GerberParserState State, List<String> lines)
        {
            while (State.CurrentLineIndex < lines.Count)
            {
                GCodeCommand GCC = new GCodeCommand();
                GCC.Decode(lines[State.CurrentLineIndex], State.CoordinateFormat);
                string Line = lines[State.CurrentLineIndex];


                if (BasicLineCommands(Line, State) == false)
                {

                    if (GCC.charcommands.Count > 0 && GCC.errors == 0)
                    {
                        switch (GCC.charcommands[0])
                        {
                            case '%':
                                if (GCC.charcommands.Count > 1)
                                {
                                    switch (GCC.charcommands[1])
                                    {
                                        case 'D':
                                            {
                                                Console.WriteLine(" D in %... ERROR but tolerated..");
                                                if (State.Apertures.TryGetValue((int)GCC.numbercommands[0], out State.CurrentAperture) == false)
                                                {
                                                    // Console.WriteLine("Failed to get aperture {0} ({1})", GCC.numbercommands[0], GCC.originalline);
                                                    State.CurrentAperture = new GerberApertureType();
                                                }
                                                else
                                                {
                                                    // Console.WriteLine("Switched to aperture {0}", GCC.numbercommands[0]);
                                                }


                                            }

                                            break;
                                        case 'S':
                                            if (GCC.charcommands[2] == 'R')
                                            {
                                                if (Gerber.ShowProgress) Console.Write("Setting up step and repeat ");
                                                GerberSplitter GS2 = new GerberSplitter();
                                                GS2.Split(GCC.originalline, State.CoordinateFormat);

                                                int Xcount = (int)GCC.numbercommands[0];
                                                int Ycount = (int)GCC.numbercommands[1];
                                                double Xoff = State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[2]);
                                                double Yoff = State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[3]);

                                                SetupRepeater(State, Xcount, Ycount, Xoff, Yoff);
                                            }
                                            break;
                                        case 'F':
                                            State.CoordinateFormat.Parse(Line);

                                            break;
                                        case 'A':
                                            if (GCC.charcommands.Count > 2)
                                            {
                                                switch (GCC.charcommands[2])
                                                {
                                                    case 'D': // aperture definition
                                                        {
                                                            // Console.WriteLine("Aperture definition: {0}", GCC.originalline);
                                                            GerberApertureType AT = new GerberApertureType();
                                                            AT.SourceLine = GCC.originalline;
                                                            if (GCC.numbercommands.Count < 1)
                                                            {
                                                                Console.WriteLine("Invalid aperture definition! No ID specified!");
                                                            }
                                                            else
                                                            {


                                                                int ATID = (int)GCC.numbercommands[0];
                                                                if (Gerber.ShowProgress) Console.Write("Aperture definition {0}:", ATID);
                                                                bool ismacro = false;

                                                                foreach (var a in State.ApertureMacros)
                                                                {
                                                                    if (GCC.originalline.Substring(6).Contains(a.Value.Name))
                                                                    {
                                                                        ismacro = true;
                                                                        if (Gerber.ShowProgress) Console.WriteLine(" macro");

                                                                    }
                                                                }
                                                                if (ismacro == false)
                                                                {
                                                                    switch (GCC.charcommands[4])
                                                                    {
                                                                        case 'C': // circle aperture

                                                                            if (Gerber.ShowProgress) Console.WriteLine(" circle: {0} (in mm: {1})", GCC.numbercommands[1], State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[1])); // command is diameter

                                                                            double radius = (State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[1]) / 2);
                                                                            if (radius < State.MinimumApertureRadius)
                                                                            {
                                                                                radius = State.MinimumApertureRadius;
                                                                                if (Gerber.ShowProgress) Console.WriteLine(" -- grew aperture radius to minimum radius: {0}", State.MinimumApertureRadius);
                                                                            }

                                                                            AT.SetCircle(radius); // hole ignored for now!
                                                                            // TODO: Add Hole Support
                                                                            if (AT.CircleRadius == 0)
                                                                            {
                                                                                AT.ZeroWidth = true;
                                                                                if (Gerber.ShowProgress) Console.WriteLine(" -- Zero width aperture found!");
                                                                            }
                                                                            break;
                                                                        case 'P': // polygon aperture
                                                                            {
                                                                                //   Console.WriteLine("      ngon: {0}", GCC.numbercommands[1]);
                                                                                if (Gerber.ShowProgress) Console.WriteLine("\tpolygon");
                                                                                AT.NGonDiameter = State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[1]);
                                                                                AT.NGonXoff = 0;
                                                                                AT.NGonYoff = 0;
                                                                                double Rotation = 0;
                                                                                if (GCC.numbercommands.Count > 3) Rotation = GCC.numbercommands[3];
                                                                                AT.NGon((int)GCC.numbercommands[2], State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[1]) / 2, 0, 0, Rotation); // hole ignored for now!
                                                                                // TODO: Add Hole Support
                                                                            }
                                                                            break;
                                                                        case 'R': // rectangle aperture
                                                                            {
                                                                                if (Gerber.ShowProgress) Console.WriteLine(" rectangle");
                                                                                double W = Math.Abs(State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[1]));
                                                                                double H = Math.Abs(State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[2]));
                                                                                //  Console.WriteLine("      rectangle: {0},{1} (in mm: {2},{3})",GCC.numbercommands[1],GCC.numbercommands[2], W,H);
                                                                                AT.SetRectangle(W, H); // hole ignored for now!
                                                                                // TODO: Add Hole Support
                                                                            }


                                                                            break;
                                                                        case 'O': // obround aperture
                                                                            {
                                                                                if (Gerber.ShowProgress) Console.WriteLine(" obround");

                                                                                double W = State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[1]);
                                                                                double H = State.CoordinateFormat.ScaleFileToMM(GCC.numbercommands[2]);
                                                                                AT.SetObround(W, H);
                                                                                if (GCC.numbercommands.Count() > 3)
                                                                                {
                                                                                    // TODO: Add Hole Support
                                                                                }
                                                                            }
                                                                            break;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    {
                                                                        int idx = 4;
                                                                        while (char.IsDigit(lines[State.CurrentLineIndex][idx]) == true) idx++;
                                                                        string mname = lines[State.CurrentLineIndex].Substring(idx).Split('*')[0];
                                                                        string[] macrostrings = mname.Split(',');

                                                                        List<string> macroparamstrings = new List<string>();
                                                                        for (int i = 1; i < macrostrings.Count(); i++)
                                                                        {
                                                                            string a = macrostrings[i];
                                                                            string[] macrosubstrings = a.Split('X');
                                                                            foreach (var aa in macrosubstrings)
                                                                            {
                                                                                macroparamstrings.Add(aa.Trim());
                                                                            }

                                                                        }
                                                                        //   Console.WriteLine("macro aperture type: {0}", macrostrings[0]);
                                                                        double[] paramlist = new double[macroparamstrings.Count];
                                                                        for (int i = 0; i < macroparamstrings.Count; i++)
                                                                        {
                                                                            double R;
                                                                            if (Gerber.TryParseDouble(macroparamstrings[i], out R))
                                                                            {
                                                                                paramlist[i] = R;
                                                                            }
                                                                            else
                                                                            {
                                                                                paramlist[i] = 1;
                                                                            }
                                                                        }
                                                                        AT = State.ApertureMacros[macrostrings[0]].BuildAperture(paramlist.ToList(), State.CoordinateFormat);
                                                                        //  GCC.Print();
                                                                    }
                                                                }

                                                                AT.ID = ATID;
                                                                if (Gerber.ShowProgress)
                                                                {
                                                                    if (State.Apertures.ContainsKey(ATID))
                                                                    {
                                                                        Console.WriteLine("redefining aperture {0}", ATID);
                                                                    }
                                                                }
                                                                State.Apertures[ATID] = AT;

                                                            }
                                                        }
                                                        break;
                                                    case 'M': // aperture macro
                                                        string name = GCC.originalline.Substring(3).Split('*')[0];
                                                        if (Gerber.ShowProgress) Console.WriteLine("Aperture macro: {0} ({1})", name, GCC.originalline);
                                                        GerberApertureMacro AM = new GerberApertureMacro();
                                                        AM.Name = name;

                                                        State.CurrentLineIndex++;
                                                        int macroline = State.CurrentLineIndex;
                                                        string macrostring = "";
                                                        List<string> MacroLines = new List<string>();
                                                        while (lines[macroline].Contains('%') == false)
                                                        {
                                                            MacroLines.Add(lines[macroline]);
                                                            macrostring += lines[macroline];
                                                            //     Console.WriteLine("macro def: {0}", lines[macroline]);
                                                            macroline++;
                                                        }

                                                        macrostring += lines[macroline];

                                                        //macroline++;
                                                        var macroparts = macrostring.Split('*');
                                                        List<string> trimmedparts = new List<string>();
                                                        foreach (var p in macroparts)
                                                        {
                                                            string pt = p.Trim();
                                                            if (pt.Length > 0)
                                                            {
                                                                if (pt[0] != '%')
                                                                {
                                                                    trimmedparts.Add(pt);
                                                                    var spl = pt.Split(',');

                                                                    // if (Gerber.Verbose) Console.WriteLine("macro part: {0}", spl[0]);
                                                                }
                                                            }
                                                        }
                                                        State.CurrentLineIndex = macroline;


                                                        //while (lines[currentline][lines[currentline].Length - 1] != '%')
                                                        foreach (var a in trimmedparts)
                                                        {
                                                            GCodeCommand GCC2 = new GCodeCommand();
                                                            GCC2.Decode(a, State.CoordinateFormat);
                                                            if (GCC2.numbercommands.Count() > 0)
                                                                switch ((int)GCC2.numbercommands[0])
                                                                {
                                                                    case 4: // outline
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: outline");
                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Outline;
                                                                            AMP.DecodeOutline(a, State.CoordinateFormat);
                                                                            AM.Parts.Add(AMP);

                                                                        }
                                                                        break;
                                                                    case 5: // polygon
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: polygon");
                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.Decode(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Polygon;
                                                                            AM.Parts.Add(AMP);
                                                                            //ApertureMacros[name] = AM;
                                                                        }
                                                                        break;
                                                                    case 6: // MOIRE
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: moiré");
                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.Decode(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Moire;
                                                                            AM.Parts.Add(AMP);
                                                                            //ApertureMacros[name] = AM;
                                                                        }
                                                                        break;
                                                                    case 7: // THERMAL
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: thermal");
                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.Decode(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Thermal;
                                                                            AM.Parts.Add(AMP);
                                                                            //ApertureMacros[name] = AM;
                                                                        }
                                                                        break;
                                                                    case 1:
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: circle");

                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.DecodeCircle(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Circle;
                                                                            AM.Parts.Add(AMP);

                                                                        }
                                                                        break;

                                                                    case 20: // line
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: line");

                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.DecodeLine(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Line_2;
                                                                            AM.Parts.Add(AMP);

                                                                        }
                                                                        break;
                                                                    case 2: // line
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: line");

                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.DecodeLine(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Line;
                                                                            AM.Parts.Add(AMP);

                                                                        }
                                                                        break;
                                                                    case 21: // centerline
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: center line");

                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.DecodeCenterLine(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.CenterLine;
                                                                            AM.Parts.Add(AMP);

                                                                        }
                                                                        break;

                                                                    case 22: // lowerlef3line
                                                                        {
                                                                            if (Gerber.ShowProgress) Console.WriteLine("\tMacro part: lower left line");

                                                                            GerberApertureMacroPart AMP = new GerberApertureMacroPart();

                                                                            AMP.DecodeLowerLeftLine(a, State.CoordinateFormat);
                                                                            AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.LowerLeftLine;
                                                                            AM.Parts.Add(AMP);

                                                                        }
                                                                        break;
                                                                    default:
                                                                        {
                                                                            Regex R = new Regex(@"(?<normal>\s*(?<dec>\$\d+)\s*\=\s*(?<rightside>.*))");
                                                                            var M = R.Match(GCC2.originalline);

                                                                            if (M.Length > 0)
                                                                            {
                                                                                Console.WriteLine("Found equation! {0}", GCC2.originalline);
                                                                                GerberApertureMacroPart AMP = new GerberApertureMacroPart();
                                                                                AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Equation;
                                                                                AMP.EquationTarget = M.Groups["dec"].Value;
                                                                                AMP.EquationSource = M.Groups["rightside"].Value;
                                                                                AM.Parts.Add(AMP);
                                                                            }
                                                                            else

                                                                                if (GCC2.numbercommands[0] == 0)
                                                                            {
                                                                                Console.WriteLine("Macro comment? {0}", GCC2.originalline);
                                                                            }
                                                                            else
                                                                            {
                                                                                Console.WriteLine("Unhandled macro part type: {0} in macro {1}: {2}", GCC2.originalline, AM.Name, GCC2.numbercommands[0]);
                                                                                Console.WriteLine("\t{0}", a);
                                                                            }

                                                                        }
                                                                        break;
                                                                }
                                                            else
                                                            {
                                                                Regex R = new Regex(@"(?<normal>\s*(?<dec>\$\d+)\s*\=\s*(?<rightside>.*))");
                                                                var M = R.Match(GCC2.originalline);

                                                                if (M.Length > 0)
                                                                {
                                                                    Console.WriteLine("Found equation! {0}", GCC2.originalline);
                                                                    GerberApertureMacroPart AMP = new GerberApertureMacroPart();
                                                                    AMP.Type = GerberApertureMacroPart.ApertureMacroTypes.Equation;
                                                                    AMP.EquationTarget = M.Groups["dec"].Value;
                                                                    AMP.EquationSource = M.Groups["rightside"].Value;
                                                                    AM.Parts.Add(AMP);
                                                                }
                                                            }

                                                            //         currentline++;
                                                        }

                                                        State.ApertureMacros[name] = AM;

                                                        break;
                                                }
                                            }

                                            break;
                                    };
                                }

                                break;

                            default:
                                GerberSplitter GS = new GerberSplitter();
                                GS.Split(GCC.originalline, State.CoordinateFormat);
                                if (GS.Has("D") && GS.Get("D") >= 10)
                                {
                                    if (State.Apertures.TryGetValue((int)GS.Get("D"), out State.CurrentAperture) == false)
                                    {
                                        //Console.WriteLine("Failed to get aperture {0} ({1})", GCC.numbercommands[0], GCC.originalline);
                                        State.CurrentAperture = new GerberApertureType();
                                        State.CurrentAperture.ShapeType = GerberApertureShape.Empty;
                                    }
                                    else
                                    {
                                        // Console.WriteLine("Switched to aperture {0}", GCC.numbercommands[0]);
                                    }

                                }
                                else
                                {
                                    if (GS.Has("G") && GS.Get("G") < 4)
                                    {
                                        switch ((int)GS.Get("G"))
                                        {
                                            case 2:
                                                State.MoveInterpolation = InterpolationMode.ClockWise;
                                                break;
                                            case 3:
                                                State.MoveInterpolation = InterpolationMode.CounterClockwise;
                                                break;
                                            case 1:
                                                State.MoveInterpolation = InterpolationMode.Linear;
                                                break;
                                        }

                                    }

                                    if (GS.Has("X") || GS.Has("Y") || GS.Has("D"))
                                    {
                                        double X = State.LastX;
                                        double Y = State.LastY;
                                        double I = 0;
                                        double J = 0;
                                        if (State.CoordinateFormat.Relativemode)
                                        {
                                            X = 0;
                                            Y = 0;
                                        }
                                        if (State.MoveInterpolation != InterpolationMode.Linear)
                                        {
                                            if (GS.Has("I")) I = State.CoordinateFormat.ScaleFileToMM(GS.Get("I"));
                                            if (GS.Has("J")) J = State.CoordinateFormat.ScaleFileToMM(GS.Get("J"));
                                        }

                                        if (GS.Has("X"))
                                        {
                                            X = State.CoordinateFormat.ScaleFileToMM(GS.Get("X"));
                                        }

                                        if (GS.Has("Y"))
                                        {
                                            Y = State.CoordinateFormat.ScaleFileToMM(GS.Get("Y"));
                                        }

                                        if (State.CoordinateFormat.Relativemode)
                                        {
                                            X += State.LastX;
                                            Y += State.LastY;
                                        }
                                        int ActualD = State.LastD;
                                        if (GS.Has("D")) ActualD = (int)GS.Get("D");
                                        State.LastD = ActualD;
                                        if (Gerber.ExtremelyVerbose)
                                        {
                                            //      Console.WriteLine("{0} to {1} - {2} ({3} - {4})", State.MoveInterpolation, X, Y, I, J);
                                        }
                                        if (State.PolygonMode)
                                        {

                                            switch (ActualD)
                                            {
                                                case 1:
                                                    switch (State.MoveInterpolation)
                                                    {
                                                        case InterpolationMode.Linear:
                                                            State.PolygonPoints.Add(new PointD(X, Y));
                                                            break;
                                                        default:
                                                            //todo!
                                                            List<PointD> CurvePoints = Gerber.CreateCurvePoints(State.LastX, State.LastY, X, Y, I, J, State.MoveInterpolation, State.CoordinateFormat.CurrentQuadrantMode);
                                                            foreach (var D in CurvePoints)
                                                            {
                                                                State.PolygonPoints.Add(new PointD(D.X, D.Y));

                                                            }
                                                            break;
                                                    }

                                                    //Move
                                                    break;
                                                case 2:
                                                    if (State.PolygonPoints.Count > 0)
                                                    {
                                                        PolyLine PL = new PolyLine() { ID = State.LastShapeID++ };
                                                        PL.ClearanceMode = State.ClearanceMode;
                                                        foreach (var a in State.PolygonPoints)
                                                        {
                                                            PL.Add(a.X, a.Y);
                                                        }
                                                        PL.Close();
                                                        State.NewShapes.Add(PL);
                                                        State.PolygonPoints.Clear();
                                                    }
                                                    State.PolygonPoints.Add(new PointD(X, Y));

                                                    //         PolygonMode = false;
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            if (State.CurrentAperture.ZeroWidth && State.IgnoreZeroWidth || State.CurrentAperture.ShapeType == GerberApertureShape.Empty)
                                            {
                                                if (Gerber.ShowProgress)
                                                {
                                                    Console.WriteLine("ignoring moves with zero width or empty aperture");
                                                }
                                            }
                                            else
                                                switch (ActualD)
                                                {
                                                    case 1:
                                                        // TODO: EXTRUDE A BOOLEAN UNION OF THE COMPOUND SHAPE   

                                                        if (State.CurrentAperture != null)
                                                        {
                                                            if (Gerber.ShowProgress && State.CurrentAperture.ShapeType == GerberApertureShape.Compound)
                                                            {
                                                                Console.WriteLine("Warning: compound aperture used for interpolated move! undefined behaviour for outline generation");

                                                            }
                                                            if (State.CurrentAperture.Shape.Count() > 0 && forcezerowidth == false)
                                                            {

                                                                switch (State.MoveInterpolation)
                                                                {
                                                                    case InterpolationMode.Linear:
                                                                        AddExtrudedCurveSegment(ref State.LastX, ref State.LastY, State.NewShapes, State.CurrentAperture, State.ClearanceMode, X, Y,  State.LastShapeID++ );
                                                                        break;
                                                                    default:

                                                                        List<PointD> CurvePoints = Gerber.CreateCurvePoints(State.LastX, State.LastY, X, Y, I, J, State.MoveInterpolation, State.CoordinateFormat.CurrentQuadrantMode);
                                                                        foreach (var D in CurvePoints)
                                                                        {
                                                                            //   AddExtrudedCurveSegment(ref LastX, ref LastY, NewShapes, CurrentAperture, ClearanceMode, LastX + I, LastY + J);
                                                                            AddExtrudedCurveSegment(ref State.LastX, ref State.LastY, State.NewShapes, State.CurrentAperture, State.ClearanceMode, D.X, D.Y, State.LastShapeID);
                                                                        }
                                                                        State.LastShapeID++;
                                                                        break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (State.ThinLine == null)
                                                                {
                                                                    State.ThinLine = new PolyLine() { ID = State.LastShapeID ++};
                                                                    State.ThinLine.ClearanceMode = State.ClearanceMode;
                                                                    State.ThinLine.Width = State.CurrentAperture.CircleRadius;
                                                                    //Console.WriteLine("Start: {0:N2} , {1:N2} - {2}", State.LastX, State.LastY, Line);
                                                                    State.ThinLine.Add(State.LastX, State.LastY);
                                                                }
                                                                switch (State.MoveInterpolation)
                                                                {
                                                                    case InterpolationMode.Linear:

                                                                        State.ThinLine.Add(X, Y);
                                                                   //     Console.WriteLine("{0:N2} , {1:N2} - {2}", X,Y, Line);
                                                                        break;

                                                                    default:

                                                                        List<PointD> CurvePoints = Gerber.CreateCurvePoints(State.LastX, State.LastY, X, Y, I, J, State.MoveInterpolation, State.CoordinateFormat.CurrentQuadrantMode);
                                                                        foreach (var D in CurvePoints)
                                                                        {
                                                                            State.ThinLine.Add(D.X, D.Y);

                                                                        }
                                                                        break;
                                                                }         // PL.Close();

                                                            }

                                                        }
                                                        break;
                                                    //    CurrentPL.Add(X, Y); break; // move while drawing exposure
                                                    case 2:
                                                        {
                                                            // move only. 
                                                            if (State.ThinLine != null)
                                                            {
                                                                State.NewThinShapes.Add(State.ThinLine);
                                                                State.ThinLine = null;
                                                            }
                                                        }
                                                        break;
                                                    case 3: // stamp 1 aperture
                                                        {
                                                            if (State.CurrentAperture != null)
                                                            {
                                                                List<PolyLine> PL = State.CurrentAperture.CreatePolyLineSet(X, Y);
                                                                foreach (var p in PL)
                                                                {
                                                                    p.ClearanceMode = State.ClearanceMode;
                                                                    State.NewShapes.Add(p);

                                                                }

                                                            }

                                                        }
                                                        break;
                                                }
                                        }
                                        State.LastX = X;
                                        State.LastY = Y;
                                        break;
                                    }

                                    else
                                    {
                                        if (GCC.originalline.Contains("G04"))
                                        {
                                            // probably comment..
                                        }
                                        else
                                        {
                                            if (Gerber.ShowProgress) Console.WriteLine("...? {0}", GCC.originalline);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
                State.CurrentLineIndex++;
            }

            SetupRepeater(State, 1, 1, 0, 0);
            if (State.ThinLine != null)
            {
                State.NewThinShapes.Add(State.ThinLine);
                State.ThinLine = null;
            }

        }
    }
}
