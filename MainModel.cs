using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkyNet
{
    public static class PhysicalConstants
    {
        public const double EARTHRADIUS_METERS = 6371000;
        public const double G_MKGS = 6.67e-11;
        public const double EARTHMASS_KG = 5.972e24;
        public const double GM_EARTH = G_MKGS * EARTHMASS_KG;
        public static Random Rand = new Random();
    }



    //-------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    //-------------------------------------------------------------------------------------
    public class MainModel : BaseModel
    {

        double Altitude_Meters = 1200000;
        double inclination = 57 / 180.0 * Math.PI;
        double beamDivergence = 50 / 180.0 * Math.PI;
        List<SatelliteModel> _mainOrbit;
        public double GlobalTimeSeconds { get; set; }

        /// <summary>
        /// Obervable property: VisualObjects
        /// </summary>
        public ObservableCollection<SatelliteModel> Satellites { get; set; }

        /// <summary>
        /// Obervable property: WindowTitle
        /// </summary>
        public string WindowTitle
        {
            get { return "SkyNet v" + Assembly.GetExecutingAssembly().GetName().Version; }
        }


        /// <summary>
        /// Obervable property: Scale
        /// </summary>
        private double _scale;
        public double Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                RaisePropertyChanged("Scale");
            }
        }


        /// <summary>
        /// Obervable property: CenterX
        /// </summary>
        private double _centerX;
        public double CenterX
        {
            get { return _centerX; }
            set
            {
                _centerX = value;
                RaisePropertyChanged("CenterX");
            }
        }


        /// <summary>
        /// Obervable property: CenterY
        /// </summary>
        private double _centerY;
        public double CenterY
        {
            get { return _centerY; }
            set
            {
                _centerY = value;
                RaisePropertyChanged("CenterY");
            }
        }


        /// <summary>
        /// Obervable property: TimeDilation
        /// </summary>
        private int _timeDilation;
        public int TimeDilation
        {
            get { return _timeDilation; }
            set
            {
                _timeDilation = value;
                RaisePropertyChanged("TimeDilation");
            }
        }


        /// <summary>
        /// Obervable property: PrimeCoverage
        /// </summary>
        private int _primeCoverage;
        public int PrimeCoverage
        {
            get { return _primeCoverage; }
            set
            {
                _primeCoverage = value;
                RaisePropertyChanged("PrimeCoverage");
            }
        }
        
        /// <summary>
        /// Obervable property: HeatmapEnabled
        /// </summary>
        private bool _heatmapEnabled;
        public bool HeatmapEnabled
        {
            get { return _heatmapEnabled; }
            set
            {
                _heatmapEnabled = value;
                RaisePropertyChanged("HeatmapEnabled");
            }
        }


        /// <summary>
        /// Obervable property: SatelliteCount
        /// </summary>
        private int _satelliteCount;
        public int SatelliteCount
        {
            get { return _satelliteCount; }
            set
            {
                _satelliteCount = value;
                RaisePropertyChanged("SatelliteCount");
            }
        }
        
        
        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        //-------------------------------------------------------------------------------------
        public MainModel()
        {
            Satellites = new ObservableCollection<SatelliteModel>();
            HeatmapEnabled = false;
            PrimeCoverage = 20;

            Scale = .00001;
            CenterX = 350;
            CenterY = 150;
            TimeDilation = 10;

            var earthBrush = new ImageBrush(new BitmapImage(new Uri(@"earth.png", UriKind.Relative)));

            _mainOrbit = CalculateOrbitSolutions();

            SatelliteCount = 700;
            RegenerateSatellites();
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// RegenerateSatellites
        /// </summary>
        //-------------------------------------------------------------------------------------
        public void RegenerateSatellites()
        {
            Satellites.Clear();
            int skip = _mainOrbit.Count / SatelliteCount;
            for (int i = 0; i < SatelliteCount; i++)
            {
                var newSatellite = _mainOrbit[i * skip];
                newSatellite.Size = 5;
                Satellites.Add(newSatellite);
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// CalculateOrbitSolutions Precalculate satellite positions for a particular orbit
        /// </summary>
        //-------------------------------------------------------------------------------------
        private List<SatelliteModel> CalculateOrbitSolutions()
        {
            // Calculate the long orbit and divide it up into points. 
            double theta = 0;
            List<SatelliteModel> mainOrbit = new List<SatelliteModel>();
            var testSatellite = new SatelliteModel();
            var satelliteRadius = PhysicalConstants.EARTHRADIUS_METERS + Altitude_Meters;
            SetSatelliteData(theta, testSatellite, satelliteRadius);

            var stepDistanceMeters = 20000;
            mainOrbit.Add(testSatellite.Clone());
            var lastLocation = testSatellite.Location;
            var orbitCheckLocation = testSatellite.Location;
            int count = 0;
            var done = false;
            int thisOrbitCount = 0;
            while (!done)
            {
                while ((testSatellite.Location - lastLocation).Length < stepDistanceMeters)
                {
                    var length = (testSatellite.Location - lastLocation).Length;
                    count++;
                    testSatellite.Move(.1);

                    // Check for orbit completion
                    if (thisOrbitCount > 2)
                    {
                        if ((testSatellite.Location - orbitCheckLocation).Length < stepDistanceMeters)
                        {
                            theta += Math.PI * .05;
                            if (theta > Math.PI * 2)
                            {
                                done = true;
                            }
                            SetSatelliteData(theta, testSatellite, satelliteRadius);
                            orbitCheckLocation = lastLocation = testSatellite.Location;
                            thisOrbitCount = 0;
                            break;
                        }
                    }
                }
                mainOrbit.Add(testSatellite.Clone());
                thisOrbitCount++;
                lastLocation = testSatellite.Location;

            }
            return mainOrbit;
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// SetSatelliteData
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void SetSatelliteData(double theta, SatelliteModel testSatellite, double satelliteRadius)
        {
            testSatellite.Location = new Vector3(
                (satelliteRadius) * Math.Sin(theta),
                0,
                (satelliteRadius) * Math.Cos(theta)
                );
            var stableVelocity = Math.Sqrt(PhysicalConstants.GM_EARTH / testSatellite.Location.Length);
            double prexv = stableVelocity * Math.Cos(inclination);
            double yv = stableVelocity * Math.Sin(inclination);
            double xv = prexv * Math.Cos(theta);
            double zv = -prexv * Math.Sin(theta);
            testSatellite.Velocity = new Vector3(xv, yv, zv);
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Move
        /// </summary>
        //-------------------------------------------------------------------------------------
        internal void Move(double timeStep)
        {
            timeStep *= TimeDilation;
            GlobalTimeSeconds += timeStep;
            foreach (var satellite in Satellites)
            {
                for (int i = 0; i < 20; i++)
                {
                    satellite.Move(timeStep/20);
                }

            }
        }
    }

}
