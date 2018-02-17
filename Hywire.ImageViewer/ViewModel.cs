using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.WPF.Framework;
using Hywire.D3DWrapper;
using SlimDX;
using System.Windows.Interop;
using System.Collections.ObjectModel;

namespace Hywire.ImageViewer
{
    class ViewModel : ViewModelBase
    {
        public delegate void UpdateImageHandler(ImageDisplayParameterStruct parameter);
        public event UpdateImageHandler OnUpdateImage;
        #region Private Fields
        private int _DisplayLimitHigh;
        private int _DisplayRangeHigh;
        private int _DisplayRangeLow;
        private float _LookAtX;
        private float _LookAtY;
        private float _ViewScale;
        private bool _IsImageLoaded;
        private ImageDisplayParameterStruct _DisplayParameters = new ImageDisplayParameterStruct()
        {
            DisplayLimitHigh = 1.0f,
            DisplayLimitLow = 0.0f,
            ViewerPosition = new Vector3(0.0f, 0.0f, -1.0f),
        };

        public class ColorChannelType
        {
            public string DisplayName { get; set; }
            public int Value { get; set; }
        }
        public ObservableCollection<ColorChannelType> ColorChannelOptions
        {
            get; private set;
        }

        private ColorChannelType _SelectedRedChannelMap;
        private ColorChannelType _SelectedGreenChannelMap;
        private ColorChannelType _SelectedBlueChannelMap;
        private ColorChannelType _SelectedAlphaChannelMap;
        #endregion Private Fields

        public ViewModel()
        {
            _DisplayRangeHigh = 255;
            _DisplayRangeLow = 0;
            _LookAtX = 0.0f;
            _LookAtY = 0.0f;
            _ViewScale = 1.0f;

            ColorChannelOptions = new ObservableCollection<ColorChannelType>()
            {
                new ColorChannelType {DisplayName="Red", Value=0 },
                new ColorChannelType {DisplayName="Green", Value=1 },
                new ColorChannelType {DisplayName="Blue", Value=2 },
                new ColorChannelType {DisplayName="Alpha", Value=3 },
            };
            SelectedRedChannelMap = ColorChannelOptions[0];
            SelectedGreenChannelMap = ColorChannelOptions[1];
            SelectedBlueChannelMap = ColorChannelOptions[2];
            SelectedAlphaChannelMap = ColorChannelOptions[3];
            DisplayLimitHigh = 255;
        }
        #region Public Properties
        public int DisplayLimitHigh
        {
            get { return _DisplayLimitHigh; }
            set
            {
                if (_DisplayLimitHigh != value)
                {
                    _DisplayLimitHigh = value;
                    RaisePropertyChanged("DisplayLimitHigh");
                }
            }
        }
        public int DisplayRangeHigh
        {
            get { return _DisplayRangeHigh; }
            set
            {
                if (_DisplayRangeHigh != value)
                {
                    _DisplayRangeHigh = value;
                    if (_DisplayRangeHigh <= _DisplayRangeLow)
                    {
                        _DisplayRangeHigh = _DisplayRangeLow + 1;
                    }
                    RaisePropertyChanged("DisplayRangeHigh");
                    _DisplayParameters.DisplayLimitHigh = _DisplayRangeHigh / (float)DisplayLimitHigh;
                    OnUpdateImage(_DisplayParameters);
                }
            }
        }

        public int DisplayRangeLow
        {
            get
            {
                return _DisplayRangeLow;
            }

            set
            {
                if (_DisplayRangeLow != value)
                {
                    _DisplayRangeLow = value;
                    if (_DisplayRangeLow >= _DisplayRangeHigh)
                    {
                        _DisplayRangeLow = _DisplayRangeHigh - 1;
                    }
                    RaisePropertyChanged("DisplayRangeLow");
                    _DisplayParameters.DisplayLimitLow = _DisplayRangeLow / (float)DisplayLimitHigh;
                    OnUpdateImage(_DisplayParameters);
                }
            }
        }

        public float LookAtX
        {
            get
            {
                return _LookAtX;
            }

            set
            {
                if (_LookAtX != value)
                {
                    _LookAtX = value;
                    RaisePropertyChanged("LookAtX");
                }
            }
        }

        public float LookAtY
        {
            get
            {
                return _LookAtY;
            }

            set
            {
                if (_LookAtY != value)
                {
                    _LookAtY = value;
                    RaisePropertyChanged("LookAtY");
                }
            }
        }

        public float ViewScale
        {
            get
            {
                return _ViewScale;
            }

            set
            {
                if (_ViewScale != value)
                {
                    _ViewScale = value;
                    if (_ViewScale < 1.0f)
                    {
                        _ViewScale = 1.0f;
                        _LookAtX = 0.0f;
                        _LookAtY = 0.0f;
                    }
                    if (_ViewScale > 25.0f)
                    {
                        _ViewScale = 25.0f;
                    }
                    RaisePropertyChanged("ViewScale");
                    _DisplayParameters.ViewerPosition = new Vector3(_LookAtX, _LookAtY, -1.0f / _ViewScale);
                    OnUpdateImage(_DisplayParameters);
                }
            }
        }
        public ColorChannelType SelectedRedChannelMap
        {
            get { return _SelectedRedChannelMap; }
            set
            {
                _SelectedRedChannelMap = value;
                RaisePropertyChanged("SelectedRedChannelMap");
                _DisplayParameters.RedChannelMap = _SelectedRedChannelMap.Value;
                OnUpdateImage?.Invoke(_DisplayParameters);
            }
        }
        public ColorChannelType SelectedGreenChannelMap
        {
            get { return _SelectedGreenChannelMap; }
            set
            {
                _SelectedGreenChannelMap = value;
                RaisePropertyChanged("SelectedGreenChannelMap");
                _DisplayParameters.GreenChannelMap = _SelectedGreenChannelMap.Value;
                OnUpdateImage?.Invoke(_DisplayParameters);
            }
        }
        public ColorChannelType SelectedBlueChannelMap
        {
            get { return _SelectedBlueChannelMap; }
            set
            {
                _SelectedBlueChannelMap = value;
                RaisePropertyChanged("SelectedBlueChannelMap");
                _DisplayParameters.BlueChannelMap = _SelectedBlueChannelMap.Value;
                OnUpdateImage?.Invoke(_DisplayParameters);
            }
        }
        public ColorChannelType SelectedAlphaChannelMap
        {
            get { return _SelectedAlphaChannelMap; }
            set
            {
                _SelectedAlphaChannelMap = value;
                RaisePropertyChanged("SelectedAlphaChannelMap");
                _DisplayParameters.AlphaChannelMap = _SelectedAlphaChannelMap.Value;
                OnUpdateImage?.Invoke(_DisplayParameters);
            }
        }

        public ImageDisplayParameterStruct DisplayParameters
        {
            get
            {
                return _DisplayParameters;
            }
        }

        public ImageInfo ImageInfo { get; set; }
        public bool IsImageLoaded
        {
            get { return _IsImageLoaded; }
            set
            {
                if (_IsImageLoaded != value)
                {
                    _IsImageLoaded = value;
                    RaisePropertyChanged("IsImageLoaded");
                }
            }
        }

        #endregion Public Properties
    }
}
