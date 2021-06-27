#region Namespaces
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
#endregion

namespace Manicotti.Views
{
    public class FindFamilyInstaceClass
    {

        public static Brush BlackWord = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#333333"));
        public static Brush GrayWord = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#666666"));
        public static Brush WhiteWord = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFFFFF"));
        public static Brush BorderBrushWhite = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#D7D7D7"));
        public static Brush BorderBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#979797"));

        private Brush foreground = BlackWord;
        public Brush ForeGround
        {
            get
            {
                return foreground;
            }
            set
            {
                foreground = value;
                NotifyPropertyChanged("ForeGround");
            }
        }

        private Brush background = WhiteWord;
        public Brush BackGround
        {
            get
            {
                return background;
            }
            set
            {
                background = value;
                NotifyPropertyChanged("BackGround");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private string name = "";
        public string Name { get { return name; } set { name = value; NotifyPropertyChanged("Name"); } }
        private string number = "";
        public string Number { get { return number; } set { number = value; NotifyPropertyChanged("Number"); } }

        private string manufacturies = "";
        public string Manufacturies { get { return manufacturies; } set { manufacturies = value; NotifyPropertyChanged("Manufacturies"); } }

        private string check = "";
        public string Check { get { return check; } set { check = value; NotifyPropertyChanged("Check"); } }


        private bool isEna = false;
        public bool IsEna { get { return isEna; } set { isEna = value; NotifyPropertyChanged("IsEna"); } }

        private string strElemID = "";
        public string StrElemId { get { return strElemID; } set { strElemID = value; NotifyPropertyChanged("StrElemId"); } }

        protected void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
