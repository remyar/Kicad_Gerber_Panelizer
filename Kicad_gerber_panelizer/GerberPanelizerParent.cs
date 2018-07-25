using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

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

            this.addInstanceToolStripMenuItem.Click += new System.EventHandler(TV.addInstanceToolStripMenuItem_Click);
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] D = e.Data.GetData(DataFormats.FileDrop) as string[];
           // GerberUtil.OpenDirectory(D);
        }

        PointD MouseToMM(PointD Mouse)
        {
            PointD P = new PointD(Mouse.X, Mouse.Y);
          //  P.X -= panelizer_display.Width / 2;
          //  P.Y -= panelizer_display.Height / 2;
          //  P.Y *= -1;
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

                bool add = true;
                ZoomToFit();

                var DropPoint = MouseToMM(new PointD(250, 250));

                string S = folderBrowserDialog1.SelectedPath;

                foreach (String p in ThePanel.TheSet.LoadedOutlines)
                {
                    if (p == S)
                        add = false;
                }
                var R = ThePanel.AddGerberFolder(S, add);
                foreach (var s in R)
                {
                    GerberInstance GI = new GerberInstance() { GerberPath = s};
                    GI.Center = DropPoint.ToF();
                    ThePanel.TheSet.Instances.Add(GI);
                    SelectedInstance = GI;
                    TV.BuildTree(this,ThePanel.TheSet);
                    Redraw(true, true);
                }

                refreshPictureBox();
              /*  ThePanel.MaxRectPack();
                ThePanel.BuildAutoTabs();
                ZoomToFit();*/
            }

 //           glControl1_Paint(sender);
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
        SnapMode CurrentSnapMode = SnapMode.MM1;

        internal void Redraw(bool refreshshape = true, bool force = false)
        {
            if (SuspendRedraw) return;
            if (force) ForceShapeUpdate = true;
            if (refreshshape)
            {
                ShapeMarkedForUpdate = true;
                //ProcessButton.Enabled = true;
                //glControl1_Paint();
            }
            //glControl1.Invalidate();
            //glControl1_Paint();
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

        private void glControl1_Paint(object sender = null, PaintEventArgs e = null)
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
                grfx.DrawImage(ThePanel.DrawBoardBitmap(1.0f / DrawingScale, panelizer_display.Width, panelizer_display.Height, SelectedInstance, null, SnapDistance()), new PointF(0, 0));
            }

            panelizer_display.Refresh();
        }

        public void ZoomToFit()
        {
          /*  if (ThePanel.TheSet.Width > 0 && ThePanel.TheSet.Height > 0 && panelizer_display.Width > 0 && panelizer_display.Height > 0)
            {
                double A1 = (ThePanel.TheSet.Width + 8) / (ThePanel.TheSet.Height + 8);
                double A2 = panelizer_display.Width / panelizer_display.Height;

                Zoom = Math.Min(panelizer_display.Width / (ThePanel.TheSet.Width + 8), panelizer_display.Height / (ThePanel.TheSet.Height + 8));

                CenterPoint.X = ThePanel.TheSet.Width / 2;
                CenterPoint.Y = ThePanel.TheSet.Height / 2;
            }
            else*/
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
            ThePanel.MaxRectPack();
            ThePanel.BuildAutoTabs();
            ZoomToFit();
            glControl1_Paint();
        }

        bool MouseCapture = false;
        PointD DragStartCoord = new PointD();
        PointD DragInstanceOriginalPosition = new PointD();
        PointD LastMouseMove = new PointD(0, 0);

        internal void SetSelectedInstance(AngledThing gerberInstance)
        {

            SelectedInstance = gerberInstance;

            //UpdateHoverControls();

            //ID.UpdateBoxes(this);
            //Redraw(false);
        }

        private void panelizer_display_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                SelectedInstance = ThePanel.FindOutlineUnderPoint(MouseToMM(new PointD(e.X / 5, e.Y / 5)));
                if (SelectedInstance != null)
                {
                    MouseCapture = true;
                    DragStartCoord = new PointD(e.X/5, e.Y/5);
                    DragInstanceOriginalPosition = new PointD(SelectedInstance.Center);
                }
                SetSelectedInstance(SelectedInstance);
            }
        }

        private void panelizer_display_MouseEnter(object sender, EventArgs e)
        {

        }

        private void panelizer_display_MouseUp(object sender, MouseEventArgs e)
        {
            if (MouseCapture)
            {
                //ID.UpdateBoxes(this);

                MouseCapture = false;
                PointD Delta = new PointD(e.X/5, e.Y/5) - DragStartCoord;
                if (Delta.Length() == 0)
                {
                    if (SelectedInstance != null)
                    {
                        SelectedInstance.Center = DragInstanceOriginalPosition.ToF();

                    }
                    Redraw(false);
                }
                else
                {
                    if (SelectedInstance != null)
                    {
                        var GI = SelectedInstance as GerberInstance;
                      
                        if (GI != null)
                        {
                           // GI.Center.X = e.X - (int)(GI.BoundingBox.Width() / 2.0);
                           // GI.Center.Y = e.Y + (int)(GI.BoundingBox.Height() / 2.0);
                            GI.RebuildTransformed(ThePanel.GerberOutlines[GI.GerberPath], ThePanel.TheSet.ExtraTabDrillDistance);


                        }
                    }
                    Redraw(true);

                    glControl1_Paint(sender);
                }
            }
        }

        public PointD Snap(PointD inp)
        {
            if (CurrentSnapMode == SnapMode.Off)
                return inp;

            double multdiv = 1;

            switch (CurrentSnapMode)
            {
                case SnapMode.MM1: break;
                case SnapMode.MM05: multdiv = 2; break;
                case SnapMode.Mil50: multdiv = 0.254 / 2.0; break;
                case SnapMode.Mil100: multdiv = 0.254; break;
            };

            PointD Res = new PointD();
            Res.X = Math.Floor(inp.X * multdiv) / multdiv;
            Res.Y = Math.Floor(inp.Y * multdiv) / multdiv;
            return Res;

        }

        private void panelizer_display_MouseMove(object sender, MouseEventArgs e)
        {
            LastMouseMove = new PointD(e.X/5, e.Y/5);
            if (MouseCapture && SelectedInstance != null)
            {
                PointD Delta = new PointD(e.X / 5, e.Y / 5) - DragStartCoord;
                Delta.X /= Zoom;
                Delta.Y /= -Zoom;

                PointD newP = new PointD(DragInstanceOriginalPosition.X + Delta.X, DragInstanceOriginalPosition.Y - Delta.Y);
                SelectedInstance.Center = Snap(newP).ToF();
                //UpdateHoverControls();
                //       SelectedInstance.Center.Y = (float)(DragInstanceOriginalPosition.Y + Delta.Y);
                Redraw(false);
                glControl1_Paint(sender);
            }
            else
            {

              /*  var newHoverShape = ThePanel.FindOutlineUnderPoint(MouseToMM(new PointD(e.X, e.Y)));
                if (newHoverShape != HoverShape)
                {
                    HoverShape = newHoverShape;
                    Redraw(false);
                }*/
            }

            
        }

        PointD ContextStartCoord = new PointD();

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                TreeNode node = treeView1.GetNodeAt(e.Location);
                if (node.GetType() == typeof(Treeview.InstanceTreeNode))
                {
                    var P = PointToScreen(new Point(e.X + treeView1.Location.X, e.Y + treeView1.Location.Y));
                    contextMenuStrip1.Show(P);
                    treeView1.SelectedNode = node;
                }
                if (node.GetType() == typeof(Treeview.GerberFileNode))
                {
                    var P = PointToScreen(new Point(e.X, e.Y));
                    contextMenuStrip1.Show(P);
                    treeView1.SelectedNode = node;
                }
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportAllGerbersToolStripMenuItem_Click(null, null);
        }

        String ExportFolder;
        Thread ExportThread;
        Progress ProgressDialog;
        public void exportAllGerbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                folderBrowserDialog2.SelectedPath = ThePanel.TheSet.LastExportFolder;
            }
            catch (Exception)
            {

            }
            if (folderBrowserDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExportFolder = folderBrowserDialog2.SelectedPath;
                ProgressDialog = new Progress(this);
                ProgressDialog.Show();
                Enabled = false;
                //ParentFrame.Enabled = false;
                ExportThread = new Thread(new ThreadStart(ExportThreadFunc));
                ExportThread.Start();

            }
        }

        public void ExportThreadFunc()
        {
            ThePanel.SaveGerbersToFolder(BaseName, ExportFolder, ProgressDialog);
        }

        internal void ProcessDone()
        {
            this.Enabled = true;
            //ParentFrame.Enabled = true;

            ProgressDialog.Close();
            ProgressDialog.Dispose();
            ProgressDialog = null;
        }

        internal void AddInstance(string path, PointD coord)
        {

            SetSelectedInstance(ThePanel.AddInstance(path, MouseToMM(coord)));
            TV.BuildTree(this, ThePanel.TheSet);
            Redraw(true);
            refreshPictureBox();
        }
    }
}
