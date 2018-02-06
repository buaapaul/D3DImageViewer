using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.WPF.Framework;
using Hywire.D3DWrapper;
using SlimDX;
using System.Windows.Interop;

namespace Hywire.ImageViewer
{
    class ViewModel : ViewModelBase
    {
        public delegate void UpdateImageHandler(ImageDisplayParameters parameter);
        public event UpdateImageHandler OnUpdateImage;
        #region Private Fields
        private int _DisplayRangeHigh;
        private int _DisplayRangeLow;
        private float _LookAtX;
        private float _LookAtY;
        private float _ViewScale;
        private ImageDisplayParameters _DisplayParameters = new ImageDisplayParameters()
        {
            DisplayLimitHigh = 1.0f,
            DisplayLimitLow = 0.0f,
            ViewerPosition = new Vector3(0.0f, 0.0f, -1.0f),
        };
        #endregion Private Fields

        public ViewModel()
        {
            _DisplayRangeHigh = 65535;
            _DisplayRangeLow = 0;
            _LookAtX = 0.0f;
            _LookAtY = 0.0f;
            _ViewScale = 1.0f;
        }
        #region Public Properties
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
                    _DisplayParameters.DisplayLimitHigh = _DisplayRangeHigh / 65535.0f;
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
                    _DisplayParameters.DisplayLimitLow = _DisplayRangeLow / 65535.0f;
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

        public ImageDisplayParameters DisplayParameters
        {
            get
            {
                return _DisplayParameters;
            }
        }
        #endregion Public Properties
    }
}
