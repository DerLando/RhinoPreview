using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Rhino.FileIO;
using RhinoPreview.Core;

namespace RhinoPreview
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _filepath;
        private File3dm _file;
        private File3dmProperties _properties;
        private PerspectiveCamera _camera;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Btn_OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            // create new open file dialog
            OpenFileDialog openFileDialog = new OpenFileDialog
                {Filter = "Rhino files (*.3dm)|*.3dm|All files (*.*)|*.*"};
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                // Clear viewport
                Viewport.Children.Clear();

                // populate fields
                _filepath = openFileDialog.FileName;
                _file = RhinoFileReader.ReadFile(openFileDialog.FileName);
                _properties = new File3dmProperties(_file);
                _camera = _properties.GetCamera();

                _properties.Name = _filepath.Split('\'').Last();

                lB_FileProperties.ItemsSource = _properties.ToList();
                Viewport.Camera = _camera;

                var modelVisual3d = new ModelVisual3D();
                var modelGroup = new Model3DGroup();

                // Define the lights cast in the scene. Without light, the 3D object cannot 
                // be seen. Note: to illuminate an object from additional directions, create 
                // additional lights.
                //DirectionalLight myDirectionalLight = new DirectionalLight();
                //myDirectionalLight.Color = Colors.White;
                //myDirectionalLight.Direction = new Vector3D(-0.61, -0.5, -0.61);

                //modelGroup.Children.Add(myDirectionalLight);

                // Add ambient light
                //AmbientLight myAmbientLight = new AmbientLight(Colors.White);
                //modelGroup.Children.Add(myAmbientLight);

                // Add lights
                foreach (var light in _properties.Lights)
                {
                    modelGroup.Children.Add(light);
                }

                // Add render meshes
                foreach (var propertiesRenderMesh in _properties.RenderMeshes)
                {
                    modelGroup.Children.Add(propertiesRenderMesh);
                }

                modelVisual3d.Content = modelGroup;
                Viewport.Children.Add(modelVisual3d);

            }

        }

        private void Btn_OpenInRhino_OnClick(object sender, RoutedEventArgs e)
        {
            if (_filepath is null) return;
            Process.Start(_filepath);
        }

        private void Viewport_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var xTrans = new TranslateTransform3D(_camera.LookDirection * e.Delta / 360D);
            _camera.Position = xTrans.Transform(_camera.Position);

        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Left)
            //{
            //    _camera.Pan(Extensions.PanDirection.Left);
            //}

            //if (e.Key == Key.Right)
            //{
            //    _camera.Pan(Extensions.PanDirection.Right);
            //}

            //if (e.Key == Key.Up)
            //{
            //    _camera.Pan(Extensions.PanDirection.Up);
            //}

            //if (e.Key == Key.Down)
            //{
            //    _camera.Pan(Extensions.PanDirection.Down);
            //}

            if (e.Key == Key.LeftShift | e.Key == Key.RightShift)
            {
                shiftPressed = true;
            }
            else
            {
                shiftPressed = false;
            }

        }

        private static double currentPositionX;
        private static double currentPositionY;
        private bool shiftPressed = false;

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            // test rmb pressing
            if (e.RightButton == MouseButtonState.Pressed)
            {

                // test shift holding
                if (shiftPressed)
                {
                    var position = e.GetPosition(this);
                    var deltaX = currentPositionX - position.X;
                    var deltaY = currentPositionY - position.Y;

                    Extensions.PanDirection panDirection = Extensions.PanDirection.None;
                    if (Math.Abs(deltaX) > Math.Abs(deltaY))
                    {
                        if (deltaX > 0) panDirection = Extensions.PanDirection.Right;
                        if (deltaX < 0) panDirection = Extensions.PanDirection.Left;
                    }
                    else
                    {
                        if (deltaY > 0) panDirection = Extensions.PanDirection.Up;
                        if (deltaY < 0) panDirection = Extensions.PanDirection.Down;
                    }

                    _camera.Pan(panDirection);
                }

                else
                {
                    var position = e.GetPosition(this);
                    var deltaX = currentPositionX - position.X;
                    var deltaY = currentPositionY - position.Y;

                    Extensions.PanDirection panDirection = Extensions.PanDirection.None;
                    if (Math.Abs(deltaX) > Math.Abs(deltaY))
                    {
                        if (deltaX > 0) panDirection = Extensions.PanDirection.Right;
                        if (deltaX < 0) panDirection = Extensions.PanDirection.Left;
                    }
                    else
                    {
                        if (deltaY > 0) panDirection = Extensions.PanDirection.Up;
                        if (deltaY < 0) panDirection = Extensions.PanDirection.Down;
                    }

                    //currentPositionX = e.GetPosition(this).X;

                    _camera.Rotate(panDirection, _properties.SceneBbox);
                }
            }


            else
            {
                var position = e.GetPosition(this);
                currentPositionX = position.X;
                currentPositionY = position.Y;
            }

        }

        private void UIElement_OnKeyUp(object sender, KeyEventArgs e)
        {
            shiftPressed = false;
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mouseposition = e.GetPosition(Viewport);
            Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
            Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            //test for a result in the Viewport3D
            VisualTreeHelper.HitTest(Viewport, null, HTResult, pointparams);
        }

        public HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            //MessageBox.Show(rawresult.ToString());
            RayHitTestResult rayResult = rawresult as RayHitTestResult;

            if (rayResult != null)
            {
                RayMeshGeometry3DHitTestResult rayMeshResult = rayResult as RayMeshGeometry3DHitTestResult;

                if (rayMeshResult != null)
                {
                    GeometryModel3D hitgeo = rayMeshResult.ModelHit as GeometryModel3D;
                    var id = (Guid) hitgeo.GetValue(File3dmProperties.IdKey.DependencyProperty);

                    lB_PickedObjectProps.ItemsSource = _file.Objects.FindId(id).GetObjectProperties();

                    //UpdateResultInfo(rayMeshResult);
                    //UpdateMaterial(hitgeo, (side1GeometryModel3D.Material as MaterialGroup));
                }
            }

            return HitTestResultBehavior.Continue;
        }
    }
}
