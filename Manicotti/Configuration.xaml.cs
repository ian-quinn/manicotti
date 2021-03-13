using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manicotti
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : Window
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
            Properties.Settings.Default.Save();
        }

        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void reset_Click(object sender, RoutedEventArgs e)
        {
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
            Properties.Settings.Default.Save();
        }
    }
}
