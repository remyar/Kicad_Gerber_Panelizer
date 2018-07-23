using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;

namespace Kicad_gerber_panelizer
{
    public static class Gerber
    {
        class QuadR
        {
            public double CX;
            public double CY;
            public double D1 = 0;
            public double D2 = 0;
            public double Diff;
            public double DRat = 0;
            public double E;
            public double S;
            internal void Calc(double LastX, double LastY, double X, double Y)
            {
                double CX1 = LastX - CX;
                double CX2 = X - CX;
                double CY1 = LastY - CY;
                double CY2 = Y - CY;

                D1 = Math.Sqrt(CX1 * CX1 + CY1 * CY1);
                D2 = Math.Sqrt(CX2 * CX2 + CY2 * CY2);
                if (D2 != 0) DRat = D1 / D2;

                S = Math.Atan2(LastY - CY, LastX - CX);
                E = Math.Atan2(Y - CY, X - CX);

            }

            internal void FixClockwise()
            {
                //       while (S < E) S += Math.PI * 2;
                Diff = S - E;
                while (Diff > Math.PI) Diff -= Math.PI * 2;
                //              Console.WriteLine("clock: {0:N2}", Gerber.RadToDeg(Diff));
            }

            internal void FixCounterClockwise()
            {
                while (S > E) S -= Math.PI * 2;
                while (S < 0)
                {
                    S += Math.PI * 2.0;
                    E += Math.PI * 2.0;
                }
                Diff = E - S;

                // while (Diff < 0) Diff += Math.PI * 2.0;
                //                Console.WriteLine("counterclock: {0:N2}", Gerber.RadToDeg(Diff));

            }
        }

        public static double ArcQualityScaleFactor = 15;

        public static bool DirectlyShowGeneratedBoardImages = true;
        public static bool DumpSanitizedOutput = false;
        public static string EOF = "M02*";
        public static bool ExtremelyVerbose = false;
        public static bool GerberRenderBumpMapOutput = true;
        public static string INCH = "%MOIN*%";
        public static string LinearInterpolation = "G01*";
        public static string LineEnding = "\n";
        public static string MM = "%MOMM*%";
        public static bool SaveDebugImageOutput = false;
        public static bool SaveIntermediateImages = false;
        public static bool SaveOutlineImages = false;
        public static bool ShowProgress = false;
        public static string StartRegion = "G36*";
        public static string StopRegion = "G37*";
        public static bool WaitForKey = false;
        public static bool WriteSanitized = false;

        public static String[] DetermineBoardSideAndLayer(string gerberfile, out BoardSide Side, out BoardLayer Layer , out String layerName)
        {
            Side = BoardSide.Unknown;
            Layer = BoardLayer.Unknown;
            layerName = "";

            string filename = Path.GetFileName(gerberfile);

            if (filename.ToLower().Contains("outline")) { Side = BoardSide.Both; Layer = BoardLayer.Outline; layerName = "outline"; }
            if (filename.ToLower().Contains("-edge.cuts")) { Side = BoardSide.Both; Layer = BoardLayer.Outline; layerName = "outline";}

            if (filename.ToLower().Contains("-b.cu")) { Side = BoardSide.Bottom; Layer = BoardLayer.Copper; layerName = "Bottom copper"; }
            if (filename.ToLower().Contains("-f.cu")) { Side = BoardSide.Top; Layer = BoardLayer.Copper; layerName = "Top copper"; }
            if (filename.ToLower().Contains("-b.silks")) { Side = BoardSide.Bottom; Layer = BoardLayer.Silk; layerName = "Bottom Silk"; }
            if (filename.ToLower().Contains("-f.silks")) { Side = BoardSide.Top; Layer = BoardLayer.Silk; layerName = "Top Silk"; }
            if (filename.ToLower().Contains("-b.mask")) { Side = BoardSide.Bottom; Layer = BoardLayer.SolderMask; layerName = "Bottom SolderMask"; }
            if (filename.ToLower().Contains("-f.mask")) { Side = BoardSide.Top; Layer = BoardLayer.SolderMask; layerName = "Top SolderMask"; }
            if (filename.ToLower().Contains("-b.paste")) { Side = BoardSide.Bottom; Layer = BoardLayer.Paste; layerName = "Bottom Paste"; }
            if (filename.ToLower().Contains("-f.paste")) { Side = BoardSide.Top; Layer = BoardLayer.Paste; layerName = "Top Paste"; }

            if (Side != BoardSide.Unknown && Layer != BoardLayer.Unknown)
                return File.ReadAllLines(gerberfile);
            else
                return null;
        }

        public static BoardFileType FindFileType(string filename)
        {
            //filename = filename.ToLower();
            List<string> unsupported = new List<string>() { "config", "exe", "dll", "png", "zip", "gif", "jpeg", "doc", "docx", "jpg", "bmp" };
            string[] filesplit = filename.Split('.');
            string ext = filesplit[filesplit.Count() - 1].ToLower();
            foreach (var s in unsupported)
            {
                if (ext == s)
                {

                    return BoardFileType.Unsupported;
                }
            }
            try
            {
                // var F = File.OpenText(a);
                var F = File.ReadAllLines(filename);
                for (int i = 0; i < F.Count(); i++)
                {
                    string L = F[i];
                    if (L.Contains("%FS")) return BoardFileType.Gerber;
                    if (L.Contains("M48")) return BoardFileType.Drill;
                };


            }
            catch (Exception E)
            {
                if (Gerber.ExtremelyVerbose)
                {
                    Console.WriteLine("Exception determining filetype: {0}", E.Message);
                }
                return BoardFileType.Unsupported;
            }

            return BoardFileType.Unsupported;


        }

        internal static bool TryParseDouble(string inp, out double N)
        {
            inp = inp.Replace("*", "");
            return double.TryParse(inp, NumberStyles.Any, CultureInfo.InvariantCulture, out N);
        }

        public static string ToFloatingPointString(double value)
        {
            return ToFloatingPointString(value, NumberFormatInfo.CurrentInfo);
        }

        public static double RadToDeg(double inp)
        {
            return inp * 360.0 / (Math.PI * 2.0);
        }

        private static readonly Regex rxScientific = new Regex(@"^(?<sign>-?)(?<head>\d+)(\.(?<tail>\d*?)0*)?E(?<exponent>[+\-]\d+)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static bool SkipEagleDrillFix = false;

        public static List<PointD> CreateCurvePoints(double LastX, double LastY, double X, double Y, double I, double J, InterpolationMode mode, GerberQuadrantMode qmode)
        {
            //   Console.WriteLine("Current curve mode: {0}", qmode);
            List<PointD> R = new List<PointD>();

            double Radius = Math.Sqrt(I * I + J * J);
            double CX = LastX + I;
            double CY = LastY + J;

            Quadrant Q = Quadrant.xposypos;

            double HS = Math.Atan2(LastY - CY, LastX - CX);
            double HE = Math.Atan2(Y - CY, X - CX);
            if (qmode == GerberQuadrantMode.Multi)
            {

                if (mode == InterpolationMode.ClockWise)
                {
                    while (HS <= HE) HS += Math.PI * 2;
                }
                else
                {
                    while (HS >= HE) HS -= Math.PI * 2;
                }

            }
            else
            {
                double LastDiff = Math.PI * 2;
                List<QuadR> qR = new List<QuadR>();
                qR.Add(new QuadR() { CX = LastX + I, CY = LastY + J });
                qR.Add(new QuadR() { CX = LastX - I, CY = LastY + J });
                qR.Add(new QuadR() { CX = LastX - I, CY = LastY - J });
                qR.Add(new QuadR() { CX = LastX + I, CY = LastY - J });

                foreach (var a in qR) a.Calc(LastX, LastY, X, Y);
                int candidates = 0;


                if (Gerber.ExtremelyVerbose)
                {
                    var DX = LastX - X;

                    var DY = LastY - Y;
                    var L = Math.Sqrt(DX * DX + DY * DY);
                    if (L < 1.0)
                    {


                        R.Add(new PointD(X, Y));

                        return R;
                    }

                    Console.WriteLine("length: {0}", L);
                }
                if (mode == InterpolationMode.CounterClockwise)
                {

                    double LastRat = 10;
                    foreach (var a in qR) a.FixCounterClockwise();
                    foreach (var a in qR)
                    {
                        if (a.Diff <= Math.PI / 2.0)
                        {
                            candidates++;
                            if (Math.Abs(1 - a.DRat) < LastRat)
                            {
                                CX = a.CX;
                                CY = a.CY;
                                HS = a.S;
                                HE = a.E;
                                LastRat = Math.Abs(1 - a.DRat);
                            }

                            if (Gerber.ExtremelyVerbose) Console.WriteLine("candidate: {0:N1} - {1:N1} - {2:N1}", RadToDeg(a.S), RadToDeg(a.E), RadToDeg(a.Diff));
                        }
                    }
                    /*
                                        HS = qR[3].S;
                                        CX = qR[3].CX;
                                        CY = qR[3].CY;
                                        HE = qR[3].E;
                                        */

                }
                else
                {

                    foreach (var a in qR) a.FixClockwise();

                    foreach (var a in qR)
                    {
                        if (a.Diff >= 0 && a.Diff <= Math.PI / 2.0 + 0.00001)
                        {
                            candidates++;
                            if (Math.Abs(a.Diff) < LastDiff)
                            {
                                CX = a.CX;
                                CY = a.CY;
                                HS = a.S;
                                HE = a.E;
                                LastDiff = Math.Abs(a.Diff);
                            }

                            if (Gerber.ExtremelyVerbose) Console.WriteLine("candidate: {0} - {1} - {2}", a.S, a.E, a.Diff);
                        }
                        if (Gerber.ExtremelyVerbose) Console.WriteLine("selected : {0} - {1} - {2}", HS, HE, LastDiff);

                    }

                }

                if (candidates == 0 && Gerber.ExtremelyVerbose)
                {
                    foreach (var a in qR)
                    {
                        Console.WriteLine("no candidate: {0} - {1} - {2}  ( should be smaller than {3}) ", a.S, a.E, a.Diff, Math.PI / 2.0);
                    }
                }

            }
            if (Gerber.ExtremelyVerbose)
            {
                Console.WriteLine("HS {0:N1}  HE {1:N1} DIFF {2:N1} QUAD {3} CX {4} CY {5}", RadToDeg(HS), RadToDeg(HE), RadToDeg(HE - HS), Q, CX, CY);


            }
            int segs = (int)(Gerber.ArcQualityScaleFactor * Math.Max(2.0, Radius) * Math.Abs(HS - HE) / (Math.PI * 2));

            if (segs < 10) segs = 10;

            double HEdeg = RadToDeg(HE);

            double HSdeg = RadToDeg(HS);
            for (int i = 0; i <= segs; i++)
            {
                double P = ((double)i / (double)segs) * (HE - HS) + HS;
                double nx = Math.Cos(P) * Radius + CX;
                double ny = Math.Sin(P) * Radius + CY;
                R.Add(new PointD(nx, ny));
            }

            //    R.Add(new PointD(X, Y));

            return R;
        }

        public static string ToFloatingPointString(double value, NumberFormatInfo formatInfo)
        {
            string result = value.ToString("r", NumberFormatInfo.InvariantInfo);
            Match match = rxScientific.Match(result);
            if (match.Success)
            {
                Debug.WriteLine("Found scientific format: {0} => [{1}] [{2}] [{3}] [{4}]", result, match.Groups["sign"], match.Groups["head"], match.Groups["tail"], match.Groups["exponent"]);
                int exponent = int.Parse(match.Groups["exponent"].Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
                StringBuilder builder = new StringBuilder(result.Length + Math.Abs(exponent));
                builder.Append(match.Groups["sign"].Value);
                if (exponent >= 0)
                {
                    builder.Append(match.Groups["head"].Value);
                    string tail = match.Groups["tail"].Value;
                    if (exponent < tail.Length)
                    {
                        builder.Append(tail, 0, exponent);
                        builder.Append(formatInfo.NumberDecimalSeparator);
                        builder.Append(tail, exponent, tail.Length - exponent);
                    }
                    else
                    {
                        builder.Append(tail);
                        builder.Append('0', exponent - tail.Length);
                    }
                }
                else
                {
                    builder.Append('0');
                    builder.Append(formatInfo.NumberDecimalSeparator);
                    builder.Append('0', (-exponent) - 1);
                    builder.Append(match.Groups["head"].Value);
                    builder.Append(match.Groups["tail"].Value);
                }
                result = builder.ToString();
            }
            return result;
        }

        public static string BuildOutlineApertureMacro(string name, List<PointD> Vertices, GerberNumberFormat format)
        {
            string res = "%AM" + name + "*" + Gerber.LineEnding;
            res += String.Format("4,1,{0}," + Gerber.LineEnding, (Vertices.Count - 2));
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                res += String.Format("{0},{1}," + Gerber.LineEnding, Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].X)).Replace(',', '.'), Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].Y)).Replace(',', '.'));
            }

            res += "0*" + Gerber.LineEnding + "%" + Gerber.LineEnding;
            return res;
        }

        public static string WriteMacroStart(string name)
        {
            return "%AM" + name + "*" + Gerber.LineEnding;
        }
        public static string WriteMacroEnd()
        {
            return "" + Gerber.LineEnding + "%" + Gerber.LineEnding;

        }

        public static string WriteMacroPartVertices(List<PointD> Vertices, GerberNumberFormat format)
        {
            string res = "";
            res += String.Format("4,1,{0}," + Gerber.LineEnding, (Vertices.Count - 2));
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                res += String.Format("{0},{1}," + Gerber.LineEnding, Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].X)).Replace(',', '.'), Gerber.ToFloatingPointString(format._ScaleMMToFile(Vertices[i].Y)).Replace(',', '.'));
            }
            res += "0*";
            return res;
        }

        internal static double ParseDouble(string inp)
        {
            return double.Parse(inp, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public class GerberBlock
        {
            public bool Header;
            public List<string> Lines = new List<string>();
        }

        public static List<string> SanitizeInputLines(List<String> inputlines, string SanitizedFile = "")
        {
            List<GerberBlock> Blocks = new List<GerberBlock>();
            bool HeaderActive = false;
            GerberBlock CurrentBlock = null;
            GerberBlock PreviousBlock = null;
            List<String> lines = new List<string>();
            string currentline = "";
            bool LastWasStar = false;

            CurrentBlock = new GerberBlock();
            CurrentBlock.Header = false;
            Blocks.Add(CurrentBlock);
            bool AddToPrevious = false;
            foreach (var ll in inputlines)
            {

                List<String> CadenceSplit = new List<string>();
                string T = ll.Trim();
                if (T.StartsWith("%") && T.EndsWith("%") && T.Contains("*") && (T.StartsWith("%AM") == false))
                {
                    T = T.Substring(1, T.Length - 2);
                    var s = T.Split('*');
                    if (s.Count() > 1)
                    {
                        foreach (var sl in s)
                        {
                            if (sl.Length > 0)
                            {
                                CadenceSplit.Add("%" + sl + "*%");
                            }

                        }
                        if (Gerber.ExtremelyVerbose)
                        {
                            if (CadenceSplit.Count > 1)
                            {
                                Console.WriteLine("Cadence format fixing:");
                                Console.WriteLine("original: {0}", ll);
                                Console.WriteLine("split:");
                                foreach (var sss in CadenceSplit)
                                {
                                    Console.WriteLine("     {0}", sss);
                                }
                            }
                        }
                    }
                    else
                    {
                        CadenceSplit.Add(ll);
                    }
                }
                else
                {
                    CadenceSplit.Add(ll);

                }

                foreach (var l in CadenceSplit)
                {

                    foreach (var c in l)
                    {
                        AddToPrevious = false;
                        GerberBlock Last = CurrentBlock;
                        if (c == '%')
                        {
                            if (HeaderActive == false)
                            {
                                HeaderActive = true;

                            }
                            else
                            {
                                if (CurrentBlock.Lines.Count == 0)
                                {
                                    CurrentBlock.Lines.Add("");
                                }
                                CurrentBlock.Lines[CurrentBlock.Lines.Count - 1] += '%';
                                AddToPrevious = true;
                                HeaderActive = false;
                            }
                            PreviousBlock = CurrentBlock;
                            CurrentBlock = null;
                        }
                        //     if (LastWasStar && c == '%')
                        //    {
                        //       Last.Lines[Last.Lines.Count -1] += '%';
                        //      lines[lines.Count - 1] += '%';
                        //  }
                        //  else
                        //  {

                        if (AddToPrevious == false)
                        {
                            if (CurrentBlock == null)
                            {
                                CurrentBlock = new GerberBlock();
                                CurrentBlock.Header = HeaderActive;
                                Blocks.Add(CurrentBlock);

                            }
                            currentline += c;
                            //  }
                            if (HeaderActive && c == '*')
                            {
                                LastWasStar = true;
                                CurrentBlock.Lines.Add(currentline);
                                lines.Add(currentline);
                                currentline = "";
                            }
                            else
                            {
                                if (c == '*')
                                {
                                    LastWasStar = true;
                                    CurrentBlock.Lines.Add(currentline);
                                    lines.Add(currentline);
                                    currentline = "";

                                }
                                else
                                {
                                    LastWasStar = false;

                                }
                            }
                        }

                    }
                    if (HeaderActive)
                    {

                    }
                    else
                    {
                        if (currentline.Length > 0)
                        {
                            if (CurrentBlock == null)
                            {
                                CurrentBlock = new GerberBlock();
                                CurrentBlock.Header = HeaderActive;
                                Blocks.Add(CurrentBlock);

                            }
                            CurrentBlock.Lines.Add(currentline);
                            lines.Add(currentline);
                            currentline = "";
                        }
                    }
                }
            }
            if (currentline.Length > 0)
            {
                CurrentBlock.Lines.Add(currentline);
                lines.Add(currentline);
            }

            List<String> reslines = new List<string>();
            //  foreach ( var a in lines)
            // {
            //     if (a.Length > 0) reslines.Add(a);

            // }

            foreach (var b in Blocks)
            {
                foreach (var a in b.Lines)
                {
                    string B = a.Trim().Replace(Gerber.LineEnding + Gerber.LineEnding, Gerber.LineEnding);
                    if (B.Trim().Length > 0)
                    {
                        if (b.Header)
                        {
                            //                        string FinalLine = a.Replace("%", "").Replace("*", "").Trim();

                            reslines.Add(a.Trim());
                        }
                        else
                        {
                            reslines.Add(a.Trim());
                        }
                    }
                }
            }
            DumpSanitizedFileToLog(SanitizedFile, Blocks, reslines);

            return reslines;
        }

        private static void DumpSanitizedFileToLog(string SanitizedFile, List<GerberBlock> Blocks, List<String> lines)
        {
            if (SanitizedFile.Length > 0)
            {
                Gerber.WriteAllLines(SanitizedFile, lines);
                List<string> l2 = new List<string>();
                foreach (var b in Blocks)
                {

                    foreach (var l in b.Lines)
                    {
                        if (b.Header == true)
                        {
                            l2.Add("HEAD " + l);
                        }
                        else
                        {
                            l2.Add("BODY " + l);
                        }
                    }

                }
                Gerber.WriteAllLines(SanitizedFile + ".txt", l2);
            }
        }

        public static void WriteAllLines(string filename, List<string> lines)
        {

            File.WriteAllText(filename, string.Join(Gerber.LineEnding, lines));

        }
    }
}
