using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Hywire.D3DWrapper;
using Microsoft.Win32;


namespace Hywire.ImageViewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel _ViewModel = null;

        private Hywire.D3DWrapper.D3DImageRenderer _ImageWrapper = null;
        public MainWindow()
        {
            InitializeComponent();
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string ProductVersion;
            ProductVersion = string.Format("{0}.{1}.{2}.{3}",
                version.Major,
                version.Minor,
                version.Build,
                version.Revision);
            this.Title += " V" + ProductVersion;
            _ViewModel = new ViewModel();
            _ViewModel.OnUpdateImage += _ViewModel_OnUpdateImage;
            DataContext = _ViewModel;
        }

        private void _ViewModel_OnUpdateImage(ImageDisplayParameterStruct parameter)
        {
            if (!_ImageWrapper.IsInitialized)
            {
                return;
            }
            if (d3dImg.IsFrontBufferAvailable)
            {
                d3dImg.Lock();
                _ImageWrapper.Draw(parameter);
                d3dImg.AddDirtyRect(new Int32Rect(0, 0, d3dImg.PixelWidth, d3dImg.PixelHeight));
                d3dImg.Unlock();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ImageWrapper = new D3DWrapper.D3DImageRenderer();
            //test.Initialize(new WindowInteropHelper(this).Handle, System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("C:\\users\\paul\\desktop\\2500x1000.tif"));
        }

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opDlg = new OpenFileDialog();
            opDlg.Filter = "(*.jpg,*.tiff,*.png)|*.jpg;*.tif;*.png";
            if (opDlg.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fs = File.OpenRead(opDlg.FileName))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.CreateOptions = BitmapCreateOptions.None | BitmapCreateOptions.PreservePixelFormat;
                        img.StreamSource = fs;
                        img.EndInit();
                        if (img.Format == PixelFormats.Gray16 || img.Format == PixelFormats.Rgb48 || img.Format == PixelFormats.Rgba64)
                        {
                            _ViewModel.DisplayLimitHigh = 65535;
                        }
                        else
                        {
                            _ViewModel.DisplayLimitHigh = 255;
                        }
                        _ViewModel.DisplayRangeHigh = _ViewModel.DisplayLimitHigh;
                        _ViewModel.DisplayRangeLow = 0;
                        _ViewModel.ImageInfo = new ImageInfo(img);
                        _ViewModel.IsImageLoaded = true;
                    }
                    StartRendering();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    _ImageWrapper.CleanUp();
                }
            }
        }

        public void StartRendering()
        {
            _ImageWrapper.Initialize(_ViewModel.ImageInfo, new WindowInteropHelper(this).Handle);
            IntPtr pSurface = _ImageWrapper.BackBuffer;
            if (pSurface != IntPtr.Zero)
            {
                d3dImg.Lock();
                d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pSurface);
                d3dImg.Unlock();

                if (d3dImg.IsFrontBufferAvailable)
                {
                    d3dImg.Lock();
                    _ImageWrapper.Draw(_ViewModel.DisplayParameters);
                    d3dImg.AddDirtyRect(new Int32Rect(0, 0, d3dImg.PixelWidth, d3dImg.PixelHeight));
                    d3dImg.Unlock();
                }

                //_ImageInfo.Dispose();
            }
        }

        public void StopRendering()
        {
            //d3dImg.Lock();
            //d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            //d3dImg.Unlock();
            _ImageWrapper.CleanUp();
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void d3dImg_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (d3dImg.IsFrontBufferAvailable)
            {
                if (_ViewModel.ImageInfo.IsLoaded)
                {
                    StartRendering();
                }
            }
            else
            {
                StopRendering();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRendering();
        }

        private void menuClose_Click(object sender, RoutedEventArgs e)
        {
            if (_ViewModel.ImageInfo != null)
            {
                _ViewModel.ImageInfo.Dispose();
                _ViewModel.ImageInfo = null;
                _ViewModel.IsImageLoaded = false;
            }
            StopRendering();

            d3dImg.Lock();
            d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            d3dImg.Unlock();
        }

        private void imageContainer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Point pt = e.GetPosition(imageGrid);
            float xCoord = (float)(pt.X / imageGrid.ActualWidth * 2 - 1);
            float yCoord = (float)(1 - pt.Y / imageGrid.ActualHeight * 2);
            _ViewModel.LookAtX += xCoord / _ViewModel.ViewScale;
            _ViewModel.LookAtY += yCoord / _ViewModel.ViewScale;
            //_ViewModel.LookAtX = (float)(pt.X / imageGrid.ActualWidth * 2 - 1);
            //_ViewModel.LookAtY = (float)(1 - pt.Y / imageGrid.ActualHeight * 2);
            _ViewModel.ViewScale += e.Delta / 1000.0f;
        }
    }
}
