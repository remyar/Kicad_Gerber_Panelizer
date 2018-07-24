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
        //List<Gerber_utils> GerberUtils = new List<Gerber_utils>();
        Treeview TV = new Treeview();
        //GerberPanel GerberPanel = new GerberPanel();
        public PointD CenterPoint = new PointD(0, 0);
        //Gerber_Parser gerberParser = new Gerber_Parser();

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

            treeView1.Enabled = true;
            treeView1.Refresh();
        }

        private void GerberPanelizerParent_Load(object sender, EventArgs e)
        {
            gerberToolStripMenuItem.Visible = false;
            treeView1.AllowDrop = false;
            treeView1.Enabled = false;
            TV.setTreeView(treeView1);
            ZoomToFit();
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] D = e.Data.GetData(DataFormats.FileDrop) as string[];
           // GerberUtil.OpenDirectory(D);
        }

        PointD MouseToMM(PointD Mouse)
        {
            PointD P = new PointD(Mouse.X, Mouse.Y);
            P.X -= panelizer_display.Width / 2;
            P.Y -= panelizer_display.Height / 2;
            P.Y *= -1;
            P.X /= (float)Zoom;
            P.Y /= (float)Zoom;
            P.X += (float)CenterPoint.X;
            P.Y += (float)CenterPoint.Y;

            return P;

        }
        public GerberPanel ThePanel = new GerberPanel();
        public AngledThing SelectedInstance;

        private void openDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {

                ZoomToFit();

                var DropPoint = MouseToMM(new PointD(200, 200));

                string S = folderBrowserDialog1.SelectedPath;

                var R = ThePanel.AddGerberFolder(S);
                foreach (var s in R)
                {
                    GerberInstance GI = new GerberInstance() { GerberPath = s };
                    GI.Center = DropPoint.ToF();
                    ThePanel.TheSet.Instances.Add(GI);
                    SelectedInstance = GI;

                    TV.BuildTree(this,ThePanel.TheSet);
                    Redraw(true, true);
                }

                ThePanel.MaxRectPack();
                ThePanel.BuildAutoTabs();
                ZoomToFit();
            }

            glControl1_Paint(sender);
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

        public bool SuspendRedraw = false;
        public bool AutoUpdate = false;
        private string BaseName = "Untitled";
        private bool ShapeMarkedForUpdate = true;
        private bool ForceShapeUpdate = false;
        private float DrawingScale;
        public double Zoom = 1;
        enum SnapMode
        {
            Mil100,
            Mil50,
            MM1,
            MM05,
            Off
        }
        SnapMode CurrentSnapMode = SnapMode.Off;

        internal void Redraw(bool refreshshape = true, bool force = false)
        {
            if (SuspendRedraw) return;
            if (force) ForceShapeUpdate = true;
            if (refreshshape)
            {
                ShapeMarkedForUpdate = true;
                //ProcessButton.Enabled = true;

            }
            //glControl1.Invalidate();
        }

        public double SnapDistance()
        {

            switch (CurrentSnapMode)
            {
                case SnapMode.MM1: return 1;
                case SnapMode.MM05: return 0.5;
                case SnapMode.Mil50: return 50 * (25.4 / 1000.0);
                case SnapMode.Mil100: return 100 * (25.4 / 1000.0);
            };
            return -1;

        }

        private void glControl1_Paint(object sender, PaintEventArgs e = null)
        {
            if (ShapeMarkedForUpdate && (AutoUpdate || ForceShapeUpdate))
            {
                ThePanel.UpdateShape(); // check if needed?
                ShapeMarkedForUpdate = false;
                ForceShapeUpdate = false;
            }

            DrawingScale = Math.Min(panelizer_display.Width, panelizer_display.Height) / (float)(Math.Max(ThePanel.TheSet.Height, ThePanel.TheSet.Width) + 10);
 /*           DrawingScale = 1.0f;
            using (Graphics grfx = Graphics.FromImage(panelizer_display.Image))
            {
                grfx.DrawImage(ThePanel.DrawBoardBitmap(1.0f, panelizer_display.Width, panelizer_display.Height, SelectedInstance), SelectedInstance.Center.X, SelectedInstance.Center.Y);
            }

            //panelizer_display.Image = ThePanel.DrawBoardBitmap(1.0f, panelizer_display.Width, panelizer_display.Height, SelectedInstance);
            panelizer_display.Refresh();

            using (Graphics grfx = Graphics.FromImage(panelizer_display.Image))
            {
                grfx.DrawImage(ThePanel.DrawBoardBitmap(1.0f, panelizer_display.Width, panelizer_display.Height, SelectedInstance), SelectedInstance.Center.X, SelectedInstance.Center.Y);
            }

            panelizer_display.Refresh();*/

            using (Graphics grfx = Graphics.FromImage(panelizer_display.Image))
            {
                grfx.ScaleTransform(5, 5); 
                grfx.DrawImage(ThePanel.DrawBoardBitmap(1.0f / DrawingScale, panelizer_display.Width, panelizer_display.Height, SelectedInstance, null, SnapDistance()), new PointF(SelectedInstance.Center.X, SelectedInstance.Center.Y));
          
            }

            panelizer_display.Refresh();
        }

        public void ZoomToFit()
        {
            if (ThePanel.TheSet.Width > 0 && ThePanel.TheSet.Height > 0 && panelizer_display.Width > 0 && panelizer_display.Height > 0)
            {
                double A1 = (ThePanel.TheSet.Width + 8) / (ThePanel.TheSet.Height + 8);
                double A2 = panelizer_display.Width / panelizer_display.Height;

                Zoom = Math.Min(panelizer_display.Width / (ThePanel.TheSet.Width + 8), panelizer_display.Height / (ThePanel.TheSet.Height + 8));

                CenterPoint.X = ThePanel.TheSet.Width / 2;
                CenterPoint.Y = ThePanel.TheSet.Height / 2;
            }
            else
            {
                Zoom = 1;
                CenterPoint = new PointD(0, 0);
            }

            UpdateScrollers();
        }

        private void UpdateScrollers()
        {
            double hratio = panelizer_display.Width / (ThePanel.TheSet.Width * Zoom);
            double vratio = panelizer_display.Height / (ThePanel.TheSet.Height * Zoom);

            /*if (hratio > 1)
            {
                hScrollBar1.Visible = false;
            }
            else
            {
                double scrollablemm = (1 - hratio) * (ThePanel.TheSet.Width + 6);
                // Console.WriteLine("{0} mm in X", scrollablemm);
                hScrollBar1.Maximum = (int)Math.Ceiling(scrollablemm);
                hScrollBar1.LargeChange = 1;
                hScrollBar1.Minimum = 0;
                hScrollBar1.Value = 0;
                hScrollBar1.Update();
                hScrollBar1.Visible = true;
            }


            if (vratio > 1)
            {
                vScrollBar1.Visible = false;
            }
            else
            {
                double scrollablemm = (1 - vratio) * (ThePanel.TheSet.Height + 6);
                //  Console.WriteLine("{0} mm in Y", scrollablemm);
                vScrollBar1.LargeChange = 1;

                vScrollBar1.Visible = true;
                vScrollBar1.Maximum = (int)Math.Ceiling(scrollablemm);
                vScrollBar1.Minimum = 0;
                vScrollBar1.Value = 0;
            }*/
        }

        private void refreshPictureBox()
        {
            //-- on dessine les outline de chaque gerber
          /*  foreach (Gerber_utils gb in GerberUtils)
            {
                foreach (Layer l in gb.layerList)
                {
                    if (l.getBoardLayer() == BoardLayer.Outline)
                    {
                        ParsedGerber TheGerber;
                        var G = gerberParser.ParseGerber274x(l.getLines(), false, true);
                        G.Name = l.getLayerName();

                        TheGerber = G;

                        TheGerber.FixPolygonWindings();
                        foreach (var a in TheGerber.OutlineShapes)
                        {
                            a.CheckIfHole();
                        }

                    }
                }
            }*/
        }
    }
}
