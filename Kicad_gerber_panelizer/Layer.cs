using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kicad_gerber_panelizer
{
    class Layer
    {
        private String _layerName;
        private BoardSide _side;
        private BoardLayer _layer;
        private String[] _file;

        private double coordX;
        private double coordY;

        public Layer(String ln , BoardSide bs , BoardLayer bl ,String[] file)
        {
            _layerName = ln;
            _side = bs;
            _layer = bl;
            _file = file;
        }

        public void setCoord(double X , double Y )
        {
            coordX = X;
            coordY = Y;
        }

        public String getLayerName()
        {
            return _layerName;
        }

        public BoardSide getBoardSide()
        {
            return _side;
        }

        public BoardLayer getBoardLayer()
        {
            return _layer;
        }

        public String[] getFile()
        {
            return _file;
        }

        public List<String> getLines()
        {
            List<String> ls = new List<String>();

            foreach (String f in _file)
            {
                ls.Add(f);
            }

            return ls;
        }
    }
}
