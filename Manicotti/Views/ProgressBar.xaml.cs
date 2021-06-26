#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
#endregion

namespace Manicotti.Views
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : BaseWindow
    {
        string _format;
        int _max;
        public bool ProcessCancelled { get; set; }

        /// <summary>
        /// Set up progress bar form and immediately display it modelessly.
        /// </summary>
        /// <param name="caption">Form caption</param>
        /// <param name="format">Progress message string</param>
        /// <param name="max">Number of elements to process</param>
        public ProgressBar(string caption, string format, int max)
        {
            _format = format;
            _max = max;
            ProcessCancelled = false;
            InitializeComponent();
            //txtTitle.Text = caption;
            txtStatus.Text = (null == format) ? caption : string.Format(format, 0);
            progress.Minimum = 0;
            progress.Maximum = max;
            progress.Value = 0;
            Show();
            System.Windows.Forms.Application.DoEvents();
        }

        public void Increment()
        {
            ++progress.Value;
            if (null != _format)
            {
                txtStatus.Text = string.Format("{0} {1}/{2}", _format, progress.Value, _max);
            }
            System.Windows.Forms.Application.DoEvents();
        }

        public void JobCompleted()
        {
            btnOk.Visibility = Visibility.Visible;
            btnCancel.Visibility = Visibility.Collapsed;

            if (ProcessCancelled)
                txtStatus.Text = string.Format("Aborted. {1}/{2} completed.", _format, progress.Value, _max);
            else
                txtStatus.Text = string.Format("Complete. {1}/{2}", _format, progress.Value, _max);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ProcessCancelled = true;
            JobCompleted();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ProcessCancelled = true;
        }

        private void progress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

    }
}
