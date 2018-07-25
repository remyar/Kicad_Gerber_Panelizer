using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Kicad_gerber_panelizer
{
    class Treeview
    {
        private TreeView _tv;
        GerberPanelizerParent TargetHost;
        private TreeNode Gerbers;
        private TreeNode BreakTabs;
        private TreeNode RootNode;

        public Treeview()
        {
            Gerbers = new TreeNode("Gerbers");
            BreakTabs = new TreeNode("Breaktabs");

            TreeNode[] array = new TreeNode[] { Gerbers, BreakTabs };

            RootNode = new TreeNode("Board", array);

        }

        public void setTreeView(TreeView tv)
        {
            _tv = tv;

            _tv.Nodes.Add(RootNode);
            _tv.ExpandAll();
            _tv.Enabled = false;

        }

        class InstanceTreeNode : TreeNode
        {
            public AngledThing TargetInstance;

            public InstanceTreeNode(AngledThing GI)
                : base("instance")
            {
                TargetInstance = GI;
                Text = ToString();
            }
            public override string ToString()
            {
                if (TargetInstance.GetType() == typeof(GerberInstance))
                {
                    return String.Format("Instance: {0} {1},{2} {3}", Path.GetFileNameWithoutExtension((TargetInstance as GerberInstance).GerberPath), TargetInstance.Center.X, TargetInstance.Center.Y, TargetInstance.Angle);
                }
                else
                {
                    return "tab";
                }
            }
        }

        class GerberFileNode : TreeNode
        {
            public string pPath;
            public GerberFileNode(string path)
                : base("gerber")
            {
                pPath = path;
                Text = ToString();
            }
            public override string ToString()
            {
                return Path.GetFileNameWithoutExtension(pPath);
            }
        }

        public void BuildTree(GerberPanelizerParent Parent, GerberLayoutSet S)
        {
            TargetHost = Parent;
            if (TargetHost == null) { _tv.Enabled = false; return; } else { _tv.Enabled = true; };
            while (Gerbers.Nodes.Count > 0)
            {
                Gerbers.Nodes[0].Remove();
            }
            while (BreakTabs.Nodes.Count > 0)
            {
                BreakTabs.Nodes[0].Remove();
            }
            foreach (var a in S.LoadedOutlines)
            {
                Gerbers.Nodes.Add(new GerberFileNode(a));
            }

            foreach (var a in S.Instances)
            {
                foreach (GerberFileNode t in Gerbers.Nodes)
                {
                    if (t.pPath == a.GerberPath)
                    {
                        t.Nodes.Add(new InstanceTreeNode(a));
                    }
                }

            }


            foreach (var t in S.Tabs)
            {
                BreakTabs.Nodes.Add(new InstanceTreeNode(t));
            }

            _tv.ExpandAll();
        }

        public void addInstanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = "";
            if (_tv.SelectedNode.GetType() == typeof(GerberFileNode))
            {
                path = (_tv.SelectedNode as GerberFileNode).pPath;
            }
            if (_tv.SelectedNode.GetType() == typeof(InstanceTreeNode))
            {
                if ((_tv.SelectedNode as InstanceTreeNode).TargetInstance.GetType() == typeof(GerberInstance))
                {
                    path = ((_tv.SelectedNode as InstanceTreeNode).TargetInstance as GerberInstance).GerberPath;
                }
            }
            if (path.Length > 0)
            {
              //  TargetHost.AddInstance(path, new PointD(0, 0));
            }
        }
    }
}
