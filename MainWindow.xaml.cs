using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkyNet
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainModel _mainModel = new MainModel();
        Thread moverThread;
        bool _running = true;
        Heatmap _heatMap;
        int _heatmapWidth = 400;
        int _heatmapHeight = 200;
        const double SECONDSPERDAY = 86164;
        Model3DGroup mainGroup;
        Transform3DGroup _worldTransform = new Transform3DGroup();
        Transform3DGroup _earthTransform = new Transform3DGroup();
        Transform3DGroup _coverageTransform = new Transform3DGroup();
        double _globalTimeSeconds = 0;
        double rotation;

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        //-------------------------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _mainModel;
            SetupWorld();
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// SetupWorld
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void SetupWorld()
        {
            CompositionTarget.Rendering += DrawFrame;
            moverThread = new Thread(Mover);
            moverThread.Start();

            _heatMap = new Heatmap(_heatmapWidth, _heatmapHeight, 0);
            _heatMap.Render();

            mainGroup = new Model3DGroup();

            var earthModel = ModelHelper.CreateSphereModel(1);
            var coverageModel = ModelHelper.CreateSphereModel(1.004);

            var image = new BitmapImage(new Uri("earthmap10k_reduced.jpg", UriKind.Relative));
            var imageBrush = new ImageBrush(image) { ViewportUnits = BrushMappingMode.Absolute };
            earthModel.Material = new DiffuseMaterial(imageBrush); //new DiffuseMaterial(Brushes.Black)
            mainGroup.Children.Add(earthModel);
            earthModel.Transform = _earthTransform;

            var heatMapBrush = new ImageBrush(_heatMap.Bitmap) { ViewportUnits = BrushMappingMode.Absolute };
            coverageModel.Material = new DiffuseMaterial(heatMapBrush);
            coverageModel.Transform = _coverageTransform;
            mainGroup.Children.Add(coverageModel);

            EarthScene.Content = mainGroup;

            mainGroup.Transform = _worldTransform;
        }


        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Mover
        /// </summary>
        //-------------------------------------------------------------------------------------
        void Mover(object state)
        {
            var timer = new Stopwatch();
            timer.Restart();
            double lastTimeSeconds = 0;

            while (_running)
            {
                double newTimeSeconds = timer.Elapsed.TotalSeconds;
                double deltaSeconds = newTimeSeconds - lastTimeSeconds;
                _mainModel.Move(deltaSeconds);
                lastTimeSeconds = newTimeSeconds;
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// DrawFrame
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void DrawFrame(object sender, EventArgs e)
        {
            _heatMap.Clear();

            _earthTransform.Children.Clear();
            double r = PhysicalConstants.EARTHRADIUS_METERS / 1000;
            _earthTransform.Children.Add(new ScaleTransform3D(r, r, r));
            _earthTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), _mainModel.GlobalTimeSeconds / SECONDSPERDAY * 360)));

            _coverageTransform.Children.Clear();
            _coverageTransform.Children.Add(new ScaleTransform3D(r, r, r));

            _worldTransform.Children.Clear();
            _worldTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), _rotationZ)));
            _worldTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), _rotationX)));
            rotation += .003;
            RenderingEventArgs renderArgs = (RenderingEventArgs)e;

            var coveragePerSatellite = .85 / _mainModel.PrimeCoverage;
            foreach(var item in _mainModel.Satellites)
            {
                var location = item.Location;

                if (_mainModel.HeatmapEnabled)
                {
                    // Back out the correct mapping of this satellite onto the heatmap
                    var h = Math.Sqrt(location.X * location.X + location.Z * location.Z);
                    var sinTheta = -location.Z / h;
                    var theta = -Math.Asin(sinTheta);
                    if (h == 0) theta = Math.PI / 2;
                    if (location.X > 0) theta = Math.PI / 2 + (Math.PI / 2 - theta);
                    var h2 = location.Length;
                    var sinPhi = location.Y / h2;
                    var phi = Math.Asin(sinPhi);
                    if (h2 == 0) phi = Math.PI / 2;

                    var heatx = (theta - Math.PI) / (Math.PI * 2) * _heatmapWidth;
                    var heaty = (Math.PI / 2 - phi) / Math.PI * _heatmapHeight;

                    _heatMap.DrawSpot(heatx, heaty, 20, coveragePerSatellite);
                }

                // Make sure the satellite has a 3D model and render it
                if(item.Model == null)
                {
                    item.Model = ModelHelper.CreateSatelliteModel(mainGroup);
                    item.Model.Material = new DiffuseMaterial(Brushes.Red);
                    mainGroup.Children.Add(item.Model);
                }
                var itemTransform = new Transform3DGroup();
                itemTransform.Children.Add(new ScaleTransform3D(1000, 1000, 1000));
                itemTransform.Children.Add(new TranslateTransform3D(item.Location.X/1000, item.Location.Y/1000, item.Location.Z/1000));
                item.Model.Transform = itemTransform;                
            }

            _heatMap.Render();

        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _running = false;
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void PlanetDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _mainModel.Scale *= ((e.Delta / 1000.0) + 1);
        }

        bool _dragging = false;
        Point _lastSpot;
        double _rotationX = 0;
        double _rotationZ = 0;
        //-------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void Planet3DDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Planet3DDisaply.CaptureMouse();
            _dragging = true;
            _lastSpot = Mouse.GetPosition(sender as IInputElement);
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void Planet3DDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                var spot = Mouse.GetPosition(sender as IInputElement);
                var deltaX = spot.X - _lastSpot.X;
                var deltaY = spot.Y - _lastSpot.Y;
                _rotationZ += deltaX/10.0;
                _rotationX -= deltaY/10.0;
                _lastSpot = spot;
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void Planet3DDisplay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;
            Planet3DDisaply.ReleaseMouseCapture();
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void Planet3DDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        //-------------------------------------------------------------------------------------
        private void Regenerate_Click(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= DrawFrame;

            _running = false;
            Thread.Sleep(100);
            _mainModel.RegenerateSatellites();
            SetupWorld();

            _running = true;
            CompositionTarget.Rendering += DrawFrame;
            moverThread = new Thread(Mover);
            moverThread.Start();

        }
    }
}
