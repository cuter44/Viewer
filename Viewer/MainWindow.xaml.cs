﻿using System;
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
using System.IO;
using System.Threading;

namespace Viewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
      // 文件夹及文件名
        // 初始打开的文件名
        string InitalFilename;
        // 正在显示的文件名
        string CurrentDisplayFilename;
        // 当前显示文件索引
        int CurrentDisplayIndex = -1;
        // 文件夹内图片文件列表
        List<string> FileList;
      // 模式
        // 当前控制模式 
        int ControlMode = 0;
        // 控制模式叠加, 未实现
        int CompoundMode = 0;
        // 控制模式常数
        const int DragMode = 1;
        const int ScaleMode = 2;
        const int RotateMode = 4;
      // 鼠标按下记录
        // 左右鼠标按下时坐标, 含义根据当前模式不同
        Point LButtonStart;
        Point RButtonStart;
      // 鼠标手势
        // 判定移动的阀值
        double MouseGestureThreshold = 128.0;
      // 平移
        double KeyMoveFactor = 2.5;
      // 旋转
        // 滚轮旋转因子
        double WheelRotateFactor=0.04;
      // 缩放
        // 滚轮缩放因子
        double WheelScaleFactor=0.0008;

        public MainWindow()
        {
            InitializeComponent();

            // 强制提前显示以校正窗体大小
            MainWin.Show();
            // 初始显示图像
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                InitalFilename = Environment.GetCommandLineArgs()[1];
                DisplayImage(InitalFilename);
            }
            
            // 检出当前文件夹全部图片
            Thread threadScanFolder = new Thread(()=>
                {
                    FileInfo fi = new FileInfo(InitalFilename);
                    DirectoryInfo di = fi.Directory;
                    List<string> list = new List<string>();

                    foreach (FileInfo iterator in di.GetFiles())
                    {
                        string cacheFn = iterator.Name.ToLower();
                        if (cacheFn.EndsWith(".jpg") ||
                        cacheFn.EndsWith(".jpeg") ||
                        cacheFn.EndsWith(".png") ||
                        cacheFn.EndsWith(".bmp") ||
                        cacheFn.EndsWith(".gif")
                        )
                        {
                            list.Add(iterator.FullName);

                            ImageGrid.Dispatcher.Invoke(
                                new Action(
                                    delegate()
                                    {
                                        Image img = new Image();

                                        img.Source = new BitmapImage(new Uri(iterator.FullName));
                                        img.Width = 128;
                                        img.Height = 128;
                                        img.Margin = new Thickness(4);

                                        img.Stretch = Stretch.UniformToFill;

                                        img.Tag = iterator.FullName;
                                        img.MouseLeftButtonUp += 
                                            new MouseButtonEventHandler(
                                                delegate (object sender, MouseButtonEventArgs e)
                                                {
                                                    DisplayImage((string)img.Tag);
                                                    e.Handled = true;
                                                    DismissImageGrid();
                                                }
                                            );
                                            // end_Handler
                                        ImageGrid.Children.Add(img);
                                    }
                                    // end_delegate
                                )
                                //end_Action
                            );
                            // end_Invoke
                        }
                        // end_if
                    }
                    // end_for
                    FileList = list;
                    CurrentDisplayIndex = FileList.IndexOf(CurrentDisplayFilename);
                }
            );
            // 经验: STA的话就不能调Dispatcher
            // MTA的话就不能创建Image对象
            // 最后只好将创建Image的部分放到Dispatcher里.
            //threadScanFolder.SetApartmentState(ApartmentState.STA);
            threadScanFolder.Start();
        }

      // 机能
        public void DisplayImage(string filename)
        {
            CurrentDisplayFilename = filename;
            BitmapImage img = new BitmapImage(new Uri(filename));

            // 计算初始缩放
            double factor = 1.0;
            if (img.Width*factor > MainWin.Width)
                factor = MainWin.Width/img.Width;
            if (img.Height*factor > MainWin.Height)
                factor = MainWin.Height/img.Height;

            // 设定图像控件
            ImagePanel.Width = img.Width*factor;
            ImagePanel.Height = img.Height*factor;
            ImagePanel.Source = img;
            
            // 复位图像
            RestoreImage();
        }

        public void DisplayNextImage()
        {
            if (CurrentDisplayIndex == -1)
                return;

            if (CurrentDisplayIndex == FileList.Count - 1)
                CurrentDisplayIndex = 0;
            else
                CurrentDisplayIndex++;

            DisplayImage(FileList[CurrentDisplayIndex]);
        }

        public void DisplayPrevImage()
        {
            if (CurrentDisplayIndex == -1)
                return;

            if (CurrentDisplayIndex == 0)
                CurrentDisplayIndex = FileList.Count - 1;
            else
                CurrentDisplayIndex--;

            DisplayImage(FileList[CurrentDisplayIndex]);
        }

        private void ExitApp()
        {
            Environment.Exit(0);
        }

      // GUI 交互处理
        private void DismissImageGrid()
        {
            ImageGridPanel.Visibility = Visibility.Collapsed;
        }

        private void DisplayImageGrid()
        {
            ImageGridPanel.Visibility = Visibility.Visible;
        }

        private void TranslateImage(double deltaX, double deltaY)
        {
            TransTrans.X += deltaX;
            TransTrans.Y += deltaY;
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

            TranslateImageTo(mouseOnWin.X - LButtonStart.X, mouseOnWin.Y - LButtonStart.Y);
        }

    // GUI 事件侦听
        private void MainWin_OnLButtonPressed(object sender, MouseButtonEventArgs e)
        {
            if ((ControlMode & DragMode)!=0)
                LButtonStart = ScaleTrans.Transform(RotateTrans.Transform(Mouse.GetPosition(ImagePanel)));
        }

        private void MainWin_OnRButtonPressed(object sender, MouseButtonEventArgs e)
        {
            RButtonStart = Mouse.GetPosition(MainWin);            
        }

        private void MainWin_OnRButtonReleased(object sendeer, MouseButtonEventArgs e)
        {
            Point mouse = Mouse.GetPosition(MainWin);
            
            if ((mouse.X - RButtonStart.X > MouseGestureThreshold) && (Math.Abs(mouse.Y - RButtonStart.Y) < MouseGestureThreshold))
                DisplayNextImage();

            if ((RButtonStart.X - mouse.X > MouseGestureThreshold) && (Math.Abs(mouse.Y - RButtonStart.Y) < MouseGestureThreshold))
                DisplayPrevImage();
        }

        private void MainWin_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (((ControlMode & DragMode) != 0) && (Mouse.LeftButton == MouseButtonState.Pressed))
                DragImage();
        }

        private void MainWin_OnKeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ControlMode == 0)
                    ExitApp();
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
            if (e.Key == Key.L)
            {
                DisplayImageGrid();
                ImageGridPanel.Focus();
            }
            if (e.Key == Key.Left)
            {
                if ((ControlMode & DragMode)!=0)
                    TranslateImage(-KeyMoveFactor, 0);
                else
                    DisplayPrevImage();
            }
            if (e.Key == Key.Right)
            {
                if ((ControlMode & DragMode)!=0)
                    TranslateImage(KeyMoveFactor, 0);
                else
                    DisplayNextImage();
            }
            if (e.Key == Key.Up)
                if ((ControlMode & DragMode)!=0)
                    TranslateImage(0, -KeyMoveFactor);
            if (e.Key == Key.Down)
                if ((ControlMode & DragMode)!=0)
                    TranslateImage(0, KeyMoveFactor);
            if (e.Key == Key.J)
                DisplayPrevImage();
            if (e.Key == Key.K)
                DisplayNextImage();
            if (e.Key == Key.PageUp)
                DisplayPrevImage();
            if (e.Key == Key.PageDown)
                DisplayNextImage();
        }

        private void MainWin_OnKeyRelease(object sender, KeyEventArgs e)
        {

        }

        private void MainWin_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((ControlMode & RotateMode) != 0)
                RotateImage(e.Delta * WheelRotateFactor);
            if ((ControlMode & ScaleMode) != 0)
                ScaleImage(e.Delta * WheelScaleFactor);
        }

        private void OnClickXButton(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }

        private void ImageGridPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if  (e.Key == Key.Escape)
                DismissImageGrid();
            e.Handled = true;
        }
    }
}
