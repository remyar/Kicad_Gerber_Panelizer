using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Kicad_gerber_panelizer
{
    class Treeview
    {
        TreeView _tv;

        TreeNode Gerbers;
        TreeNode BreakTabs;
        TreeNode RootNode;

        public Treeview(TreeView tv)
        {
            _tv = tv;

            Gerbers = new TreeNode("Gerbers");
            BreakTabs = new TreeNode("Breaktabs");

            TreeNode[] array = new TreeNode[] { Gerbers, BreakTabs };

            RootNode = new TreeNode("Board", array);
            _tv.Nodes.Add(RootNode);
            _tv.ExpandAll();
        }
    }
}
