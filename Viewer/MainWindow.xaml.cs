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
        // 当前显示图片引用
        BitmapSource CurrentDisplayImage;
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
      // 灰度化
        // 可以进行灰度化的格式
        List<PixelFormat> graySupported = new List<PixelFormat> {
            PixelFormats.Bgr24,
            PixelFormats.Bgr32,
            PixelFormats.Bgra32
        };

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
                                        Image CurrentDisplayImage = new Image();

                                        CurrentDisplayImage.Source = new BitmapImage(new Uri(iterator.FullName));
                                        CurrentDisplayImage.Width = 128;
                                        CurrentDisplayImage.Height = 128;
                                        CurrentDisplayImage.Margin = new Thickness(4);

                                        CurrentDisplayImage.Stretch = Stretch.UniformToFill;

                                        CurrentDisplayImage.Tag = iterator.FullName;
                                        CurrentDisplayImage.MouseLeftButtonUp += 
                                            new MouseButtonEventHandler(
                                                delegate (object sender, MouseButtonEventArgs e)
                                                {
                                                    DisplayImage((string)CurrentDisplayImage.Tag);
                                                    e.Handled = true;
                                                    DismissImageGrid();
                                                }
                                            );
                                            // end_Handler
                                        ImageGrid.Children.Add(CurrentDisplayImage);
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
        public void DisplayImage(BitmapSource image)
        {
            CurrentDisplayImage = image;

            // 计算初始缩放
            double factor = 1.0;
            if (CurrentDisplayImage.Width*factor > MainWin.Width)
                factor = MainWin.Width/CurrentDisplayImage.Width;
            if (CurrentDisplayImage.Height*factor > MainWin.Height)
                factor = MainWin.Height/CurrentDisplayImage.Height;

            // 设定图像控件
            ImagePanel.Width = CurrentDisplayImage.Width*factor;
            ImagePanel.Height = CurrentDisplayImage.Height*factor;
            ImagePanel.Source = CurrentDisplayImage;

            // 校正图像位置大小
            RestoreImage();

            // 图像信息
            StringBuilder str = new StringBuilder();
            str.Append("Pixel Format: " + CurrentDisplayImage.Format + "\n");
            str.Append("Height: " + CurrentDisplayImage.PixelHeight + " px\n");
            str.Append("Width: " + CurrentDisplayImage.PixelWidth + " px\n");
            LblInfo.Content = str.ToString();
        }

        public void DisplayImage(string filename)
        {
            CurrentDisplayFilename = filename;
            try
            {
                DisplayImage(new BitmapImage(new Uri(filename)));
            }
            catch (Exception ex)
            {
                ImagePanel.Source = null;
                LblInfo.Content = "文件 " + filename + " 无法解析";
            }

        }

        private void DisplayTransformInfo()
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("Translate: {0:F3}, {1:F3}\n", TransTrans.X, TransTrans.Y);
            str.AppendFormat("Scale: {0:F3}, {1:F3}\n", ScaleTrans.ScaleX, ScaleTrans.ScaleY);
            str.AppendFormat("Rotate: {0:F3}", RotateTrans.Angle);
            LblTransform.Content = str.ToString();
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

        private void Gray(int method)
        {
            // 格式制御
            if (!graySupported.Contains(CurrentDisplayImage.Format))
            {
                LblTransform.Content = "像素格式 " + CurrentDisplayImage.Format + " 不能被灰度化";
                return;
            }

            int pixelCount = CurrentDisplayImage.PixelHeight 
                * CurrentDisplayImage.PixelWidth;
            int bytePerPixel = CurrentDisplayImage.Format.BitsPerPixel / 8;
            byte[] pixels = new byte[pixelCount * 4];
            IList<PixelFormatChannelMask> msaks = CurrentDisplayImage.Format.Masks;

            // 提取像素格式
            CurrentDisplayImage.CopyPixels(pixels,
                CurrentDisplayImage.PixelWidth * bytePerPixel,
                0);

            // 构造新图像
            // 填充像素
            byte[] grayPixels = new byte[pixelCount];
            for (int i=0; i<pixelCount; i++)
                switch (method)
                {
                    case 1:
                        grayPixels[i] =(byte)
                            ( pixels[i*bytePerPixel+0]*0.333
                            + pixels[i*bytePerPixel+1]*0.333
                            + pixels[i*bytePerPixel+2]*0.333 );
                        break;
                    case 2:
                        grayPixels[i] =(byte)
                            ( pixels[i*bytePerPixel+0]*0.299
                            + pixels[i*bytePerPixel+1]*0.587
                            + pixels[i*bytePerPixel+2]*0.114 );
                        break;
                    case 3:
                        grayPixels[i] =(byte)(
                            Math.Pow(
                                Math.Pow(pixels[i*bytePerPixel+0], 2.2)*0.2973
                                + Math.Pow(pixels[i*bytePerPixel+1], 2.2)*0.6274
                                + Math.Pow(pixels[i*bytePerPixel+2], 2.2)*0.0753,
                                1/2.2)
                            );
                        break;
                }
          
            // 生成 & 显示
            DisplayImage( 
                BitmapSource.Create(
                    CurrentDisplayImage.PixelWidth,
                    CurrentDisplayImage.PixelHeight,
                    CurrentDisplayImage.DpiX,
                    CurrentDisplayImage.DpiY,
                    PixelFormats.Gray8,
                    null,
                    grayPixels,
                    CurrentDisplayImage.PixelWidth
                )
            );
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

            DisplayTransformInfo();
        }

        private void TranslateImageTo(double abslouteX, double abslouteY)
        {
            TransTrans.X = abslouteX;
            TransTrans.Y = abslouteY;

            DisplayTransformInfo();
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

            DisplayTransformInfo();

        }

        private void DragImage()
        {
            Point mouseOnWin = Mouse.GetPosition(MainWin);

            TranslateImageTo(mouseOnWin.X - LButtonStart.X, mouseOnWin.Y - LButtonStart.Y);
        }

      // GUI 事件侦听
      // 主窗体
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
            // ESC 退出(无) 重置模式
            if (e.Key == Key.Escape)
            {
                if (ControlMode == 0)
                    ExitApp();
                if (ControlMode != 0)
                    ControlMode = 0;
            }
            // Z 重置图像
            if (e.Key == Key.Z)
                RestoreImage();
            // VSR 模式转换
            if (e.Key == Key.V)
                ControlMode = (ControlMode * CompoundMode) ^ DragMode;
            if (e.Key == Key.S)
                ControlMode = (ControlMode * CompoundMode) ^ ScaleMode;
            if (e.Key == Key.R)
                ControlMode = (ControlMode * CompoundMode) ^ RotateMode;
            if (e.Key == Key.X)
            {
                if (ControlPane.Visibility == Visibility.Visible)
                    ControlPane.Visibility = Visibility.Hidden;
                else
                    ControlPane.Visibility = Visibility.Visible;
            }
            // L 列表
            if (e.Key == Key.L)
            {
                DisplayImageGrid();
                ImageGridPanel.Focus();
            }
            // 方向键 移动图像(V) 导航(无)
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
            // JK/PgUp/PgDn 导航
            if (e.Key == Key.J)
                DisplayNextImage();
            if (e.Key == Key.K)
                DisplayPrevImage();
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
      // 关闭按钮
        private void OnClickXButton(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }
      // 图像阵列
        private void ImageGridPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if  (e.Key == Key.Escape)
                DismissImageGrid();
            e.Handled = true;
        }
      // 控制板
        private void ControlPane_KeyDown(object sender, KeyEventArgs e)
        {
            //e.Handled = true;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            DisplayPrevImage();
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            DisplayImage(CurrentDisplayFilename);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            DisplayNextImage();
        }

        private void BtnMove_Click(object sender, RoutedEventArgs e)
        {
            ControlMode = (ControlMode * CompoundMode) ^ DragMode;
        }

        private void BtnScale_Click(object sender, RoutedEventArgs e)
        {
            ControlMode = (ControlMode * CompoundMode) ^ ScaleMode;
        }

        private void BtnRotate_Click(object sender, RoutedEventArgs e)
        {
            ControlMode = (ControlMode * CompoundMode) ^ RotateMode;
        }

        private void BtnGray1_Click(object sender, RoutedEventArgs e)
        {
            Gray(1);
        }

        private void BtnGray2_Click(object sender, RoutedEventArgs e)
        {
            Gray(2);
        }
        private void BtnGray3_Click(object sender, RoutedEventArgs e)
        {
            Gray(3);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            RestoreImage();
        }
    }
}
