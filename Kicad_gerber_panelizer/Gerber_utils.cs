using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Kicad_gerber_panelizer
{
    class Gerber_utils
    {
        List<string> outlinefiles = new List<string>();
        List<string> millfiles = new List<string>();
        List<string> copperfiles = new List<string>();

        public Gerber_utils()
        {

        }

        public void OpenDirectory(String[] FileNames, bool skipoutlines  = false)
        {
            string path = Path.GetDirectoryName(FileNames[0]);

            foreach (var F in FileNames)
            {
                BoardSide BS = BoardSide.Unknown;
                BoardLayer BL = BoardLayer.Unknown;

                if (Gerber.FindFileType(F) == BoardFileType.Gerber)
                {
                    Gerber.DetermineBoardSideAndLayer(F, out BS, out BL);

                    if (BS == BoardSide.Both && BL == BoardLayer.Outline)
                    {
                        outlinefiles.Add(F);
                    }
                    else
                    {
                        if (BS == BoardSide.Both && BL == BoardLayer.Mill)
                        {
                            millfiles.Add(F);
                        }
                        else
                        {
                            if (BL == BoardLayer.Copper)
                            {
                                copperfiles.Add(F);
                            }
                        }
                    }
                }
            }
        }
    }
}
