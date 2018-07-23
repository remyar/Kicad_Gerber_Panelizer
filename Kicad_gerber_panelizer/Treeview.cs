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

        private TreeNode Gerbers;
        private TreeNode BreakTabs;
        private TreeNode RootNode;

        private List<String> projectList  = new List<String>();

        public Treeview(TreeView tv)
        {
            _tv = tv;

            update();
        }

        public void addProject(Gerber_utils project)
        {
            project.setName(Path.GetFileNameWithoutExtension(project.getProjectPath()) + "(" + (projectList.Count + 1).ToString() + ")");

            projectList.Add(project.getName());

            update();
        }

        public void update()
        {
            _tv.Nodes.Clear();
            TreeNode[] projectNode = new TreeNode[projectList.Count];
            int idx = 0;
            foreach (string pl in projectList)
            {
                projectNode[idx] = new TreeNode(pl);
                idx++;
            }

            Gerbers = new TreeNode("Gerbers", projectNode);

            BreakTabs = new TreeNode("Breaktabs");

            TreeNode[] array = new TreeNode[] { Gerbers, BreakTabs };

            RootNode = new TreeNode("Board", array);
            _tv.Nodes.Add(RootNode);
            _tv.ExpandAll();
        }
    }
}
