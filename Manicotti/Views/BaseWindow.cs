using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Manicotti.Views
{
    public class BaseWindow : Window
    {
        public BaseWindow()
        {
            //this.ApplyTemplate();
            InitializeStyle();
            this.Loaded += delegate
            {
                InitializeEvent();
            };
        }
        private void InitializeEvent()
        {
            var resourceDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Manicotti;component/Views/BaseWindowStyle.xaml", UriKind.Absolute)
            };
            ControlTemplate baseTemplate = resourceDict["BaseWindowControlTemplate"] as ControlTemplate;
            
            Button minBtn = this.Template.FindName("MinimizeButton", this) as Button;
            minBtn.Click += delegate
            {
                this.WindowState = WindowState.Minimized;
            };

            Button closeBtn = this.Template.FindName("CloseButton", this) as Button;
            closeBtn.Click += delegate
            {
                this.Close();
            };

            //Button maxBtn = this.Template.FindName("MaximizeButton", this) as Button;
            //maxBtn.Click += delegate
            //{
            //    this.WindowState = (this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal);
            //};
            
            Border mainHeader = this.Template.FindName("MainWindowBorder", this) as Border;
            mainHeader.MouseMove += delegate (object sender, MouseEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };
            //mainHeader.MouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e)
            //{
            //    if (e.ClickCount >= 2)
            //    {
            //        maxBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            //    }
            //};

            TextBlock titleBar = this.Template.FindName("txtTitle", this) as TextBlock;
            titleBar.Text = "Manicotti";
        }

        private void InitializeStyle()
        {
            var resourceDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Manicotti;component/Views/BaseWindowStyle.xaml", UriKind.Absolute)
            };
            this.Style = resourceDict["BaseWindowStyle"] as Style;
        }

    }
}
