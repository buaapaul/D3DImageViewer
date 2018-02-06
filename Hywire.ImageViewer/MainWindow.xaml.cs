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
using SlimDX;

namespace Hywire.ImageViewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel _ViewModel = null;

        private D3DImageRenderer _ImageWrapper = null;
        private ImageInfo _ImageInfo = null;
        public MainWindow()
        {
            InitializeComponent();
            _ViewModel = new ViewModel();
            _ViewModel.OnUpdateImage += _ViewModel_OnUpdateImage;
            DataContext = _ViewModel;
        }

        private void _ViewModel_OnUpdateImage(ImageDisplayParameters parameter)
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
            _ImageWrapper = new D3DImageRenderer();
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
                        _ImageInfo = new ImageInfo(img);
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
            _ImageWrapper.Initialize(_ImageInfo, new WindowInteropHelper(this).Handle);
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
            }
        }

        public void StopRendering()
        {
            d3dImg.Lock();
            d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            d3dImg.Unlock();
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
                //StartRendering();
            }
            else
            {
                //StopRendering();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRendering();
        }

        private void menuClose_Click(object sender, RoutedEventArgs e)
        {
            if (_ImageInfo != null)
            {
                _ImageInfo.Dispose();
                _ImageInfo = null;
            }
            StopRendering();
        }

        private void imageContainer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Point pt = e.GetPosition(imageGrid);
            _ViewModel.LookAtX = (float)(pt.X / imageGrid.ActualWidth * 2 - 1);
            _ViewModel.LookAtY = (float)(1 - pt.Y / imageGrid.ActualHeight * 2);
            _ViewModel.ViewScale += e.Delta / 1000.0f;
        }
    }
}
