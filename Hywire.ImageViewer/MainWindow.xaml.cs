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
using Hywire.D3DImageRenderer;
using Microsoft.Win32;

namespace Hywire.ImageViewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private D3DWrapper _ImageRenderer = null;
        private BitmapImage _Image = null;
        private string _ImagePath;
        private ImageDisplayParameters _DisplayParameters = new ImageDisplayParameters()
        {
            DisplayLimitHigh = 0.0001f,
            DisplayLimitLow = 0.0f,
        };
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ImageRenderer = new D3DWrapper();
        }

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opDlg = new OpenFileDialog();
            opDlg.Filter = "(*.jpg,*.tiff,*.png)|*.jpg;*.tif;*.png";
//            opDlg.FilterIndex = -1;
            if (opDlg.ShowDialog() == true)
            {
                _Image = new BitmapImage();
                using (FileStream fs = File.OpenRead(opDlg.FileName))
                {
                    _Image.BeginInit();
                    _Image.StreamSource = fs;
                    _Image.CacheOption = BitmapCacheOption.Default;
                    _Image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    _Image.EndInit();
                }
                _ImagePath = opDlg.FileName;
                StartRendering();
            }
        }

        private void StartRendering()
        {
            if (!d3dImg.IsFrontBufferAvailable)
            {
                return;
            }
            if (_Image == null)
            {
                return;
            }
            _ImageRenderer.Initialize(_ImagePath, _Image.PixelWidth, _Image.PixelHeight, new WindowInteropHelper(this).Handle);
            IntPtr pSurface = _ImageRenderer.BackBuffer;
            if (pSurface != IntPtr.Zero)
            {
                d3dImg.Lock();
                d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pSurface);
                d3dImg.Unlock();
                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        private void StopRendering()
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            _ImageRenderer.CleanUp();
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (d3dImg.IsFrontBufferAvailable)
            {
                d3dImg.Lock();
                _ImageRenderer.Draw(_DisplayParameters);
                d3dImg.AddDirtyRect(new Int32Rect(0, 0, d3dImg.PixelWidth, d3dImg.PixelHeight));
                d3dImg.Unlock();
            }
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void d3dImg_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (d3dImg.IsFrontBufferAvailable)
            {
                StartRendering();
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
    }
}
