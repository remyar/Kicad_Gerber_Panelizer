using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kicad_gerber_panelizer
{
    public class ProgressLog
    {
        List<string> TextlinesToAdd = new List<string>();
        float fProgress = 0;

        public void AddLog(string text, float progress)
        {
            TextlinesToAdd.Add(text);
            Console.WriteLine(text);
            fProgress = progress;
        }

        public void AddString(string text, float progress = 0.0f)
        {
            if (progress == -1) progress = fProgress;
                AddLog(text, progress);
        }
    }
}
