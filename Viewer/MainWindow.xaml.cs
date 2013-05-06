using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Math;
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

namespace Viewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 文件名交换
        string initalFilename;
        string currentDisplayFilename;
        // 拖动
        Point dragPosDelta;
        bool dragStatus;
        // 旋转
        double wheelFactorRotate=0.04;
        // 缩放
        double wheelFactorScale=0.0008;

        public MainWindow()
        {
            InitializeComponent();
            MainWin.Show();
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                initalFilename = Environment.GetCommandLineArgs()[1];
                DisplayImage(initalFilename);
            }
        }

    // Functions
        public static double deg2rad(double degree)
        {
            return(degree/180.0*Math.PI);
        }

        public void DisplayImage(string filename)
        {
            currentDisplayFilename = filename;
            BitmapImage img = new BitmapImage(new Uri(filename));

            double factor = 1.0;
            if (img.Width*factor > MainWin.Width)
                factor = MainWin.Width/img.Width;
            if (img.Height*factor > MainWin.Height)
                factor = MainWin.Height/img.Height;

            ImagePanel.Width = img.Width*factor;
            ImagePanel.Height = img.Height*factor;
            ImagePanel.Source = img;
            
            RestoreImage();
        }

    // Functional Event Listener
        private void TranslateImage(double deltaX, double deltaY)
        {
            TransTrans.X += deltaX;
            TransTrans.Y += deltaY;
            LabelTranslate.Content = "Translate " + TransTrans.X + "," + TransTrans.Y;
        }

        private void RotateImage(double deltaAngle)
        {
            Point center = Mouse.GetPosition(ImagePanel);
            GeneralTransform inverseTrans = TransGroup.Inverse;
            center = inverseTrans.Transform(center);
            
            RotateTrans.Angle += deltaAngle;
            LabelRotate.Content = "Rotate " + RotateTrans.Angle;
        }

        private void ScaleImage(double deltaMultiple)
        {
            Point mouse = Mouse.GetPosition(ImagePanel);

            ScaleTrans.ScaleX += deltaMultiple;
            ScaleTrans.ScaleY += deltaMultiple;
            LabelScale.Content = "Scale " + ScaleTrans.ScaleX + "," + ScaleTrans.ScaleY;
           
            // 位移补偿
            TranslateImage(
                -((mouse.X*Math.Cos(deg2rad(RotateTrans.Angle)) + Mouse.Y*Math.Cos(deg2rad(RotateTrans.Angle)+Math.PI/2))*deltaMultiple,
                -((mouse.X*Math.Sin(deg2rad(RotateTrans.Angle)) + Mouse.Y*Math.Cos(deg2rad(RotateTrans.Angle)+Math.PI/2))*deltaMultiple,
            );
        }

        private void RestoreImage()
        {
            ScaleTrans.ScaleX = ScaleTrans.ScaleY = 1;

            TransTrans.X = (MainWin.Width - ImagePanel.Width)/2;
            TransTrans.Y = (MainWin.Height - ImagePanel.Height)/2;

            RotateTrans.Angle = 0;
        }

        private void DragImageStart(object sender, MouseButtonEventArgs e)
        {
            dragPosDelta = e.GetPosition(ImagePanel);
            dragStatus = true;
        }

        private void DragImageEnd(object sender, MouseButtonEventArgs e)
        {
            dragStatus = false;
        }

        private void DragImage(object sender, MouseEventArgs e)
        {
            if (dragStatus)
            {
                Point currentMouseAt = Mouse.GetPosition(MainWin);
                //ImagePanel.Left = currentMouseAt.X - dragPosDelta.X;
                //ImagePanel.Top = currentMouseAt.Y - dragPosDelta.Y;
            }
        }

    // Direct Event Listener
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            LabelMousePosWin.Content = "WinPos " + Mouse.GetPosition(MainWin);
            LabelMousePosImage.Content = "ImagePos" + Mouse.GetPosition(ImagePanel);
            DragImage(sender, e);
        }

        private void OnKeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();
            if (e.Key == Key.Z)
                RestoreImage();
            //if (e.Key == Key.R)
            //    Cursor = Cursors.Cross;
        }

        private void OnKeyRelease(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.R)
            //    Cursor = Cursors.Arrow;
        }

        private void OnMouseWheelReact(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.R))
                RotateImage(e.Delta * wheelFactorRotate);
            if (Keyboard.IsKeyDown(Key.S))
                ScaleImage(e.Delta * wheelFactorScale);
        }
    }
}
