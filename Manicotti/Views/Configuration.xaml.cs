#region Namespaces
using System;
using System.IO;
using System.Xml;
using System.Windows;
using System.Windows.Input;

using Autodesk.Revit.DB;
#endregion

namespace Manicotti.Views
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : BaseWindow
    {
        // field
        public Document document;
        // constructor
        public Configuration(Document doc)
        {
            InitializeComponent();
        }


        private void apply_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.url_column = url_column.Text;
            Properties.Settings.Default.url_door = url_door.Text;
            Properties.Settings.Default.url_window = url_window.Text;
            Properties.Settings.Default.url_columnFamily = url_columnFamily.Text;
            Properties.Settings.Default.floorHeight = double.Parse(floorHeight.Text);
            Properties.Settings.Default.sillHeight = double.Parse(sillHeight.Text);
            Properties.Settings.Default.wallThickness = double.Parse(wallThickness.Text);
            Properties.Settings.Default.minLength = double.Parse(minLength.Text);
            Properties.Settings.Default.jointRadius = double.Parse(jointRadius.Text);
            Properties.Settings.Default.layerColumn = layerColumn.Text;
            Properties.Settings.Default.layerWall = layerWall.Text;
            Properties.Settings.Default.layerWindow = layerWindow.Text;
            Properties.Settings.Default.layerSpace = layerSpace.Text;
            Properties.Settings.Default.layerFrame = layerFrame.Text;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            // Properties.Settings.Default.Save();
            this.Close();
        }

        private void reset_Click(object sender, RoutedEventArgs e)
        {
            string thisAssemblyFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            Properties.Settings.Default.url_column = thisAssemblyFolderPath + @"\Resources\rfa\M_Rectangular Column.rfa";
            Properties.Settings.Default.url_door = thisAssemblyFolderPath + @"\Resources\rfa\M_Door-Single-Panel.rfa";
            Properties.Settings.Default.url_window = thisAssemblyFolderPath + @"\Resources\rfa\M_Window-Fixed.rfa";
            Properties.Settings.Default.url_columnFamily = @"C:\ProgramData\Autodesk\RVT 2020\Family Templates\English\Metric Column.rft";
            Properties.Settings.Default.floorHeight = 4000;
            Properties.Settings.Default.sillHeight = 1200;
            Properties.Settings.Default.wallThickness = 200;
            Properties.Settings.Default.minLength = 0.001;
            Properties.Settings.Default.jointRadius = 50;
            Properties.Settings.Default.layerColumn = "COLUMN";
            Properties.Settings.Default.layerWall = "WALL";
            Properties.Settings.Default.layerWindow = "WINDOW";
            Properties.Settings.Default.layerSpace = "SPACE";
            Properties.Settings.Default.layerFrame = "FRAME";
        }


        private void BtnOpen1_Click(object sender, RoutedEventArgs e)
        {
            string strA = strrRfaName();
            if (!string.IsNullOrEmpty(strA))
            {
                url_door.Text = strA;
            }
        }

        public string strrRfaName()
        {
            string strName = string.Empty;
            System.Windows.Forms.OpenFileDialog ofd = null;
            ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Title = "Revit文件";
            ofd.FileName = "";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.Filter = "Revit文件(*.rfa)|*.rfa";
            ofd.ValidateNames = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.FilterIndex = 1;
            //string strName = string.Empty;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                strName = ofd.FileName;
            }
            return strName;
        }

        private void BtnOpen2_Click(object sender, RoutedEventArgs e)
        {
            string strA = strrRfaName();
            if (!string.IsNullOrEmpty(strA))
            {
                url_window.Text = strA;
            }
        }

        private void BtnOpen3_Click(object sender, RoutedEventArgs e)
        {
            string strA = strrRfaName();
            if (!string.IsNullOrEmpty(strA))
            {
                url_column.Text = strA;
            }
        }

        private void BtnOpen4_Click(object sender, RoutedEventArgs e)
        {
            string strName = string.Empty;
            System.Windows.Forms.OpenFileDialog ofd = null;
            ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Title = "Revit文件";
            ofd.FileName = "";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.Filter = "Revit文件(*.rft)|*.rft";
            ofd.ValidateNames = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.FilterIndex = 1;
            //string strName = string.Empty;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                strName = ofd.FileName;
            }
            if (!string.IsNullOrEmpty(strName))
            {
                url_column.Text = strName;
            }
        }
    }
}
