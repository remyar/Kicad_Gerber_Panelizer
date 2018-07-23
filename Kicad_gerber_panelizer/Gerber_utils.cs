using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace Kicad_gerber_panelizer
{
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
