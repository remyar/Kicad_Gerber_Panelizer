using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Kicad_gerber_panelizer
{
    public partial class GerberPanelizerParent : Form
    {
        Gerber_utils GerberUtils = new Gerber_utils();

        public GerberPanelizerParent()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Gerber Set Files (*.gerberset)|*.gerberset|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
              /*  GerberPanelize childForm = new GerberPanelize(this, TV, ID);
                childForm.MdiParent = this;
                childForm.Show();
                childForm.LoadFile(FileName);
                childForm.ZoomToFit();
                ActivePanelizeInstance = childForm;*/
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //-- creation d'un nouveau panel
            Panelizer_view Panelizer_view = new Panelizer_view(panelizer_display);
            Treeview Treeview_view = new Treeview(treeView1);

            gerberToolStripMenuItem.Visible = true;
           // treeView1.AllowDrop = true;

            this.treeView1.AllowDrop = true;


            treeView1.Refresh();
        }

        private void GerberPanelizerParent_Load(object sender, EventArgs e)
        {
            gerberToolStripMenuItem.Visible = false;
            treeView1.AllowDrop = false;
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] D = e.Data.GetData(DataFormats.FileDrop) as string[];
            GerberUtils.OpenDirectory(D);
        }

        private void openDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                string S = folderBrowserDialog1.SelectedPath;
                string[] D = Directory.GetFiles(S);
                GerberUtils.OpenDirectory(D);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void treeView1_DragLeave(object sender, EventArgs e)
        {

        }
    }
}
