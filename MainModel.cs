using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    public class SatelliteModel : BaseModel
    {
        public Vector3 Location { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
        public UIElement VisualElement { get; set; }


        public double Size { get; set; }
        public double VisualSize
        {
            get
            {
                var output = Size * ParentModel.Scale;
                if (output < 4) output = 4;
                return output;
            }
        }

        public Brush FillColor { get; set; }

        public MainModel ParentModel {get; set;}

        public double X { get { return ParentModel.CenterX + Location.X * ParentModel.Scale - VisualSize / 2; } }
        public double Y { get { return ParentModel.CenterY + Location.Y * ParentModel.Scale - VisualSize / 2; } }

        public SatelliteModel()
        {
            FillColor = Brushes.White;
        }

        internal void Move(double timeStep)
        {
            Location += Velocity * timeStep;

            var distanceSquared = Location.LengthSquared;
            if (distanceSquared > 0)
            {
                Velocity += (-PhysicalConstants.GM_EARTH * Location.Normalized) / (distanceSquared) * timeStep;
            }
        }

        public SatelliteModel Clone()
        {
            var newModel = new SatelliteModel();
            newModel.Location = Location;
            newModel.Velocity = Velocity;
            newModel.Acceleration = Acceleration;
            newModel.Size = Size;
            newModel.ParentModel = ParentModel;

            return newModel;

        }

        public System.Windows.Media.Media3D.GeometryModel3D Model { get; set; }
    }


    public class MainModel : BaseModel
    {

        double Altitude_Meters = 1200000;
        double inclination = 57 / 180.0 * Math.PI;
        double beamDivergence = 50 / 180.0 * Math.PI;

        public ObservableCollection<SatelliteModel> VisualObjects { get; set; }


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
        

        public MainModel()
        {
            VisualObjects = new ObservableCollection<SatelliteModel>();

            Scale = .00001;
            CenterX = 350;
            CenterY = 150;
            TimeDilation = 10;

            var earthBrush = new ImageBrush(new BitmapImage(new Uri(@"earth.png", UriKind.Relative)));

            //VisualObjects.Add(new SatelliteModel());
            //VisualObjects[0].Size = PhysicalConstants.EARTHRADIUS_METERS * 2;
            //VisualObjects[0].ParentModel = this;
            //VisualObjects[0].FillColor = earthBrush;

            //VisualObjects.Add(new SatelliteModel());
            //VisualObjects[1].Size = 200;
            //VisualObjects[1].ParentModel = this;
            //VisualObjects[1].FillColor = Brushes.Red;

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
            while(!done)
            {
                while((testSatellite.Location - lastLocation).Length < stepDistanceMeters)
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


            //double thetaSkip = .4117;
            //double secondsToAdvance = 0;
            //double secondsToAdvanceSkip = 17;
            int satelliteCount = 700;
            int skip = mainOrbit.Count / satelliteCount;
            for(int i = 0; i < satelliteCount; i++)
            {
                var newSatellite = mainOrbit[i * skip];
                newSatellite.Size = 5;
                VisualObjects.Add(newSatellite);
            }

            //for(int i = 0; i < 2000; i++)
            //{
            //    //var theta = PhysicalConstants.Rand.NextDouble() * Math.PI * 2;
                
            //    var newSatellite = new SatelliteModel();
            //    newSatellite.Size = 5;
            //    var satelliteRadius = PhysicalConstants.EARTHRADIUS_METERS + Altitude_Meters + PhysicalConstants.Rand.NextDouble() * 300;
            //    newSatellite.Size = (satelliteRadius - PhysicalConstants.EARTHRADIUS_METERS) * Math.Tan(beamDivergence);
                
            //    newSatellite.Location = new Vector3(
            //        (satelliteRadius) * Math.Sin(theta),
            //        0,
            //        (satelliteRadius) * Math.Cos(theta)
            //        );

            //    var stableVelocity = Math.Sqrt(PhysicalConstants.GM_EARTH / newSatellite.Location.Length);

            //    double prexv = stableVelocity * Math.Cos(inclination);
            //    double yv = stableVelocity * Math.Sin(inclination);
            //    double xv = prexv * Math.Cos(theta);
            //    double zv = -prexv * Math.Sin(theta);
            //    newSatellite.Velocity = new Vector3(xv, yv, zv);

            //    var orbitalPeriod = Math.PI * satelliteRadius * 2 / stableVelocity;
            //    //var secondsToAdvance = PhysicalConstants.Rand.Next((int)orbitalPeriod);
            //    for (int a = 0; a < secondsToAdvance; a++)
            //    {
            //        newSatellite.Move(1);
            //    }

            //    newSatellite.ParentModel = this;
            //    newSatellite.FillColor = new SolidColorBrush(Color.FromArgb(60, 255, 255, 0));
            //    VisualObjects.Add(newSatellite);
            //    theta += thetaSkip;
            //    secondsToAdvance += secondsToAdvanceSkip;
            //}
        }

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

        public double GlobalTimeSeconds { get; set; }

        internal void Move(double timeStep)
        {
            timeStep *= TimeDilation;
            GlobalTimeSeconds += timeStep;
            foreach (var satellite in VisualObjects)
            {
                for (int i = 0; i < 20; i++)
                {
                    satellite.Move(timeStep/20);
                }

            }
        }
    }

}
