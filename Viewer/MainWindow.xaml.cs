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
        // 模式
        int ControlMode = 0;
        int CompoundMode = 0;
        const int DragMode = 1;
        const int ScaleMode = 2;
        const int RotateMode = 4;
        // 线性变换
        Point DragStartPos;
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
            //LabelTranslate.Content = "Translate " + TransTrans.X + "," + TransTrans.Y;
        }

        private void TranslateImageTo(double abslouteX, double abslouteY)
        {
            TransTrans.X = abslouteX;
            TransTrans.Y = abslouteY;
        }

        private void RotateImage(double deltaAngle)
        {
            Point mouse = Mouse.GetPosition(ImagePanel);
            Point origin = TransGroup.Transform(mouse);
            
            RotateTrans.Angle += deltaAngle;
            // 调试
            LabelRotate.Content = "Rotate " + RotateTrans.Angle;

            // 位移补偿
            Point alter = TransGroup.Transform(mouse);
            TranslateImage(origin.X - alter.X, origin.Y - alter.Y);
        }

        private void ScaleImage(double deltaMultiple)
        {
            Point mouse = Mouse.GetPosition(ImagePanel);
            Point origin = TransGroup.Transform(mouse);

            ScaleTrans.ScaleX += deltaMultiple;
            ScaleTrans.ScaleY += deltaMultiple;
            // 调试
            //LabelScale.Content = "Scale " + ScaleTrans.ScaleX + "," + ScaleTrans.ScaleY;
           
            // 位移补偿
            Point alter = TransGroup.Transform(mouse);
            TranslateImage(origin.X - alter.X, origin.Y - alter.Y);
        }

        private void RestoreImage()
        {
            ScaleTrans.ScaleX = ScaleTrans.ScaleY = 1;

            TransTrans.X = (MainWin.Width - ImagePanel.Width)/2;
            TransTrans.Y = (MainWin.Height - ImagePanel.Height)/2;

            RotateTrans.Angle = 0;
        }

        private void DragImage()
        {
            Point mouseOnWin = Mouse.GetPosition(MainWin);
            //Point mouseOnImage = TransGroup.Transform(DragStartPos);

            TranslateImageTo(mouseOnWin.X - DragStartPos.X, mouseOnWin.Y - DragStartPos.Y);
        }

    // Direct Event Listener
        private void OnLeftClickImage(object sender, MouseButtonEventArgs e)
        {
            if ((ControlMode & DragMode)!=0)
                DragStartPos = ScaleTrans.Transform(RotateTrans.Transform(Mouse.GetPosition(ImagePanel)));
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (((ControlMode & DragMode) != 0) && (Mouse.LeftButton == MouseButtonState.Pressed))
                DragImage();
        }

        private void OnKeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ControlMode == 0)
                    Application.Current.Shutdown();
                if (ControlMode != 0)
                    ControlMode = 0;
            }
            if (e.Key == Key.Z)
                RestoreImage();
            if (e.Key == Key.V)
                ControlMode = (ControlMode * CompoundMode) ^ DragMode;
            if (e.Key == Key.S)
                ControlMode = (ControlMode * CompoundMode) ^ ScaleMode;
            if (e.Key == Key.R)
                ControlMode = (ControlMode * CompoundMode) ^ RotateMode;
        }

        private void OnKeyRelease(object sender, KeyEventArgs e)
        {
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((ControlMode & RotateMode) != 0)
                RotateImage(e.Delta * wheelFactorRotate);
            if ((ControlMode & ScaleMode) != 0)
                ScaleImage(e.Delta * wheelFactorScale);
        }

    }
}
