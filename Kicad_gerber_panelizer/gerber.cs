using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Kicad_gerber_panelizer
{
    public static class Gerber
    {
        public static bool ExtremelyVerbose = false;

        public static void DetermineBoardSideAndLayer(string gerberfile, out BoardSide Side, out BoardLayer Layer)
        {
            Side = BoardSide.Unknown;
            Layer = BoardLayer.Unknown;
            string filename = Path.GetFileName(gerberfile);

            if (filename.ToLower().Contains("outline")) { Side = BoardSide.Both; Layer = BoardLayer.Outline; }
            if (filename.ToLower().Contains("-edge.cuts")) { Side = BoardSide.Both; Layer = BoardLayer.Outline; }

            if (filename.ToLower().Contains("-b.cu")) { Side = BoardSide.Bottom; Layer = BoardLayer.Copper; }
            if (filename.ToLower().Contains("-f.cu")) { Side = BoardSide.Top; Layer = BoardLayer.Copper; }
            if (filename.ToLower().Contains("-b.silks")) { Side = BoardSide.Bottom; Layer = BoardLayer.Silk; }
            if (filename.ToLower().Contains("-f.silks")) { Side = BoardSide.Top; Layer = BoardLayer.Silk; }
            if (filename.ToLower().Contains("-b.mask")) { Side = BoardSide.Bottom; Layer = BoardLayer.SolderMask; }
            if (filename.ToLower().Contains("-f.mask")) { Side = BoardSide.Top; Layer = BoardLayer.SolderMask; }
            if (filename.ToLower().Contains("-b.paste")) { Side = BoardSide.Bottom; Layer = BoardLayer.Paste; }
            if (filename.ToLower().Contains("-f.paste")) { Side = BoardSide.Top; Layer = BoardLayer.Paste; }

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
    }
}
