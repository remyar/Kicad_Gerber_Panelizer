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
        List<Gerber_utils> GerberUtils = new List<Gerber_utils>();
        Treeview Treeview_view;
        Gerber_Parser gerberParser = new Gerber_Parser();

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
            

            gerberToolStripMenuItem.Visible = true;
           // treeView1.AllowDrop = true;

            treeView1.AllowDrop = true;


            treeView1.Refresh();
        }

        private void GerberPanelizerParent_Load(object sender, EventArgs e)
        {
            gerberToolStripMenuItem.Visible = false;
            treeView1.AllowDrop = false;

            Treeview_view = new Treeview(treeView1);
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] D = e.Data.GetData(DataFormats.FileDrop) as string[];
           // GerberUtil.OpenDirectory(D);
        }

        private void openDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Gerber_utils GerberUtil = new Gerber_utils(panelizer_display);
                string S = folderBrowserDialog1.SelectedPath;
                string[] D = Directory.GetFiles(S);
                GerberUtil.OpenDirectory(D);

                GerberUtils.Add(GerberUtil);

                Treeview_view.addProject(GerberUtil);

                refreshPictureBox();
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


        private void refreshPictureBox()
        {
            //-- on dessine les outline de chaque gerber
            foreach (Gerber_utils gb in GerberUtils)
            {
                foreach (Layer l in gb.layerList)
                {
                    if (l.getBoardLayer() == BoardLayer.Outline)
                    {
                        gerberParser.ParseGerber274x(l.getLines(), true, true);
                    }
                }
            }
        }
    }
}
