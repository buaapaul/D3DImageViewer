using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Hywire.D3DWrapper;
using Hywire.D3DImageEx;
using System.Windows.Interop;
using Microsoft.Win32;
using System.IO;

namespace Hywire.ImageViewer
{
    /// <summary>
    /// MainWindowDX11.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindowDX11 : Window
    {
        private ViewModel _ViewModel;
        private D3DImageRenderer _ImageWrapper;
        private D3dImageEx _D3dImageEx;
        private IntPtr _WindHandle;
        public MainWindowDX11()
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
            if (_D3dImageEx.IsFrontBufferAvailable)
            {
                _D3dImageEx.Lock();
                _ImageWrapper.Draw(parameter);
                _D3dImageEx.AddDirtyRect(new Int32Rect(0, 0, _D3dImageEx.PixelWidth, _D3dImageEx.PixelHeight));
                _D3dImageEx.Unlock();
            }
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
                        if (img.Format == PixelFormats.Gray16 || img.Format == PixelFormats.Gray8)
                        {
                            _ViewModel.IsMultiChannelImage = false;
                            _ViewModel.SelectedRedChannelMap = _ViewModel.ColorChannelOptions[0];
                            _ViewModel.SelectedGreenChannelMap = _ViewModel.ColorChannelOptions[0];
                            _ViewModel.SelectedBlueChannelMap = _ViewModel.ColorChannelOptions[0];
                            _ViewModel.SelectedAlphaChannelMap = _ViewModel.ColorChannelOptions[3];
                        }
                        else
                        {
                            _ViewModel.IsMultiChannelImage = true;
                            _ViewModel.SelectedRedChannelMap = _ViewModel.ColorChannelOptions[0];
                            _ViewModel.SelectedGreenChannelMap = _ViewModel.ColorChannelOptions[1];
                            _ViewModel.SelectedBlueChannelMap = _ViewModel.ColorChannelOptions[2];
                            _ViewModel.SelectedAlphaChannelMap = _ViewModel.ColorChannelOptions[3];
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
                    _ViewModel.IsImageLoaded = false;
                }
            }
        }
        private void StartRendering()
        {
            _ImageWrapper.Initialize(_ViewModel.ImageInfo, _WindHandle);
            if (_ImageWrapper.RenderTarget != null)
            {
                _D3dImageEx.Lock();
                _D3dImageEx.SetBackBufferEx(D3DImageEx.D3DResourceType.DX11Texture2D, _ImageWrapper.RenderTarget);
                _D3dImageEx.Unlock();

                if (_D3dImageEx.IsFrontBufferAvailable)
                {
                    _D3dImageEx.Lock();
                    _ImageWrapper.Draw(_ViewModel.DisplayParameters);
                    _D3dImageEx.AddDirtyRect(new Int32Rect(0, 0, _D3dImageEx.PixelWidth, _D3dImageEx.PixelHeight));
                    _D3dImageEx.Unlock();
                }
            }
        }

        private void StopRendering()
        {
            //d3dImg.Lock();
            //d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            //d3dImg.Unlock();
            _ImageWrapper.CleanUp();
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

            if (_D3dImageEx != null)
            {
                _D3dImageEx.Lock();
                _D3dImageEx.SetBackBufferEx(D3DImageEx.D3DResourceType.DX11Texture2D, null);
                _D3dImageEx.Unlock();
            }
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ImageWrapper = new D3DImageRenderer(DirectXVersion.DirectX11);
            _WindHandle = new WindowInteropHelper(this).Handle;

            _D3dImageEx = new D3dImageEx(_WindHandle);
            _D3dImageEx.IsFrontBufferAvailableChanged += _D3dImageEx_IsFrontBufferAvailableChanged;
            imageContainer.Source = _D3dImageEx;
        }

        private void _D3dImageEx_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_D3dImageEx.IsFrontBufferAvailable)
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
            if (_D3dImageEx != null)
            {
                _D3dImageEx.Dispose();
            }
        }
    }
}
