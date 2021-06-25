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

namespace Manicotti.Views
{
    /// <summary>
    /// WPF_FindElement.xaml 的交互逻辑
    /// </summary>
    public partial class FindElement : BaseWindow
    {
        List<Tuple<string, int, string, string>> Tuples = new List<Tuple<string, int, string, string>>();
        public FindElement(List<Tuple<string, int, string, string>> tuples)
        {
            Tuples = tuples;
            InitializeComponent();
        }
        private void FindElement_Loaded(object sender, RoutedEventArgs e)
        {
            List<FindFamilyInstaceClass> findFamilyInstaceClasses = new List<FindFamilyInstaceClass>();
            foreach (var item in Tuples)
            {
                FindFamilyInstaceClass findFamilyInstaceClass = new FindFamilyInstaceClass();
                findFamilyInstaceClass.Name = item.Item1;
                findFamilyInstaceClass.Number = item.Item2.ToString();
                if (item.Item3 == "族名称")
                {
                    findFamilyInstaceClass.BackGround = FindFamilyInstaceClass.BlackWord;
                    findFamilyInstaceClass.ForeGround = FindFamilyInstaceClass.WhiteWord;
                }
                findFamilyInstaceClass.Manufacturies = item.Item4;
                findFamilyInstaceClasses.Add(findFamilyInstaceClass);
            }
            DgFindElement.ItemsSource = findFamilyInstaceClasses;
        }
    }
}
