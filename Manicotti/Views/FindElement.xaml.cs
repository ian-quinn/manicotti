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
        List<Tuple<string, int, string, string, string, ElementId>> Tuples = new List<Tuple<string, int, string, string, string, ElementId>>();
        List<Tuple<string, int, string, string, string, ElementId>> tuples1 = new List<Tuple<string, int, string, string, string, ElementId>>();
        List<string> lstCategory = new List<string>();
        public FindElement(List<Tuple<string, int, string, string, string, ElementId>> tuples, List<string> strCategory)
        {
            Tuples = tuples;
            lstCategory = strCategory;
            InitializeComponent();
        }
        private void FindElement_Loaded(object sender, RoutedEventArgs e)
        {
            lstCategory.Insert(0, "全部");
            lstCategory = lstCategory.Distinct().ToList();
            CmdCategory.ItemsSource = lstCategory;
            CmdCategory.SelectedIndex = 0;
            List<FindFamilyInstaceClass> findFamilyInstaceClasses = new List<FindFamilyInstaceClass>();
            findFamilyInstaceClasses = lstFindInstanceClass(Tuples);
            DgFindElement.ItemsSource = findFamilyInstaceClasses;
            tuples1 = new List<Tuple<string, int, string, string, string, ElementId>>();
            tuples1.AddRange(Tuples);
        }

        private void CmdCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmdCategory.SelectedItem != null)
            {
                if (CmdCategory.SelectedItem.ToString() != "全部")
                {
                    List<Tuple<string, int, string, string, string, ElementId>> tuplesCat = new List<Tuple<string, int, string, string, string, ElementId>>();
                    foreach (var item in Tuples)
                    {
                        if (item.Item5 == CmdCategory.SelectedItem.ToString())
                        {
                            tuplesCat.Add(item);
                        }
                    }
                    List<FindFamilyInstaceClass> findFamilyInstaceClasses = new List<FindFamilyInstaceClass>();
                    findFamilyInstaceClasses = lstFindInstanceClass(tuplesCat);
                    DgFindElement.ItemsSource = findFamilyInstaceClasses;
                    tuples1 = new List<Tuple<string, int, string, string, string, ElementId>>();
                    tuples1.AddRange(tuplesCat);
                }
                else
                {
                    List<FindFamilyInstaceClass> findFamilyInstaceClasses = new List<FindFamilyInstaceClass>();
                    findFamilyInstaceClasses = lstFindInstanceClass(Tuples);
                    DgFindElement.ItemsSource = findFamilyInstaceClasses;
                    tuples1 = new List<Tuple<string, int, string, string, string, ElementId>>();
                    tuples1.AddRange(Tuples);
                }
            }
        }

        public List<FindFamilyInstaceClass> lstFindInstanceClass(List<Tuple<string, int, string, string, string, ElementId>> TupleCat)
        {
            List<FindFamilyInstaceClass> findFamilyInstaceClasses = new List<FindFamilyInstaceClass>();
            foreach (var item in TupleCat)
            {
                FindFamilyInstaceClass findFamilyInstaceClass = new FindFamilyInstaceClass();
                findFamilyInstaceClass.Name = item.Item1;
                findFamilyInstaceClass.Number = item.Item2.ToString();
                findFamilyInstaceClass.StrElemId = item.Item6.ToString();
                if (item.Item3 == "族名称")
                {
                    findFamilyInstaceClass.IsEna = true;
                    findFamilyInstaceClass.Check = "-";
                    findFamilyInstaceClass.BackGround = FindFamilyInstaceClass.BlackWord;
                    findFamilyInstaceClass.ForeGround = FindFamilyInstaceClass.WhiteWord;
                }
                findFamilyInstaceClass.Manufacturies = item.Item4;
                findFamilyInstaceClasses.Add(findFamilyInstaceClass);

            }
            return findFamilyInstaceClasses;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if ((DgFindElement.SelectedItem as FindFamilyInstaceClass).Check == "-")
            {
                List<Tuple<string, int, string, string, string, ElementId>> tuples2 = new List<Tuple<string, int, string, string, string, ElementId>>();
                string strElemID = (DgFindElement.SelectedItem as FindFamilyInstaceClass).StrElemId;

                tuples2 = tuples1.FindAll(t => t.Item6.ToString() == strElemID && t.Item3 != "族名称");
                foreach (var item in tuples2)
                {
                    tuples1.Remove(item);

                }
                List<FindFamilyInstaceClass> findFamilyInstaceClasses = new List<FindFamilyInstaceClass>();
                findFamilyInstaceClasses = lstFindInstanceClass(tuples1);
                findFamilyInstaceClasses.Find(t => t.StrElemId == strElemID).Check = "+";
                //(DgFindElement.SelectedItem as FindFamilyInstaceClass).Check = "-";
                DgFindElement.ItemsSource = findFamilyInstaceClasses;
            }
            else if ((DgFindElement.SelectedItem as FindFamilyInstaceClass).Check == "+")
            {
                List<Tuple<string, int, string, string, string, ElementId>> tuples2 = new List<Tuple<string, int, string, string, string, ElementId>>();
                string strElemID = (DgFindElement.SelectedItem as FindFamilyInstaceClass).StrElemId;
                tuples2 = Tuples.FindAll(t => t.Item6.ToString() == strElemID && t.Item3 != "族名称");

                for (int i = 0; i < tuples2.Count; i++)
                {
                    tuples1.Insert(DgFindElement.SelectedIndex + 1, tuples2[i]);
                }
                List<FindFamilyInstaceClass> findFamilyInstaceClasses = new List<FindFamilyInstaceClass>();
                findFamilyInstaceClasses = lstFindInstanceClass(tuples1);
                findFamilyInstaceClasses.Find(t => t.StrElemId == strElemID).Check = "-";
                //(DgFindElement.SelectedItem as FindFamilyInstaceClass).Check = "+";
                DgFindElement.ItemsSource = findFamilyInstaceClasses;
            }
        }
    }
}
