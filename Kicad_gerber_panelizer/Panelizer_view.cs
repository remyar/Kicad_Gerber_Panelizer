using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Kicad_gerber_panelizer
{
    class Panelizer_view
    {
        private Bitmap _render;
        private PictureBox _output;

        public Panelizer_view(PictureBox pb )
        {
            _output = pb;
            _render = new Bitmap(pb.Size.Width, pb.Size.Height);

            using (Graphics gfx = Graphics.FromImage(_render))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            {
                gfx.FillRectangle(brush, 0, 0, _render.Width, _render.Height);
            }

            _output.Image = _render;
        }


        public void render()
        {
            _output.Refresh();
        }

    }
}
