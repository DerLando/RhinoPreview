using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Point = System.Windows.Point;

namespace RhinoPreview.Core
{
    public class File3dmProperties
    {
        // parent file
        public File3dm File3dm { get; set; }

        // relevant properties
        public int LayerCount { get; set; }
        public string Name { get; set; }
        public int ObjectCount { get; set; }
        public int BlockDefinitionCount { get; set; }

        public PerspectiveCamera Camera { get; set; }
        public BoundingBox SceneBbox { get; set; }
        public System.Windows.Media.Media3D.Light[] Lights { get; set; }

        // geometry render meshes
        public GeometryModel3D[] RenderMeshes { get; set; }

        public File3dmProperties(File3dm file3dm)
        {
            File3dm = file3dm;
            CreateRenderMeshes();
            CreateLights();

            Name = File3dm.CreatedBy;

            LayerCount = File3dm.AllLayers.Count;
            ObjectCount = File3dm.Objects.Count;
            BlockDefinitionCount = File3dm.AllInstanceDefinitions.Count;
        }

        public static DependencyPropertyKey IdKey = DependencyProperty.RegisterReadOnly("Guid", typeof(Guid),
            typeof(GeometryModel3D), new PropertyMetadata(Guid.Empty));

        private void CreateRenderMeshes()
        {
            List<GeometryModel3D> geoModels = new List<GeometryModel3D>();
            BoundingBox bbox = BoundingBox.Empty;
            var backFaceMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Black));

            foreach (var file3dmObject in File3dm.Objects)
            {
                // skip if not mesh (until we find out how to extract render meshes)
                var objectType = file3dmObject.Geometry.ObjectType;
                if (objectType != ObjectType.Mesh && objectType != ObjectType.Brep) continue;

                // create new empty geometry model
                var geoModel = new GeometryModel3D();

                // create guid property
                geoModel.SetValue(IdKey, file3dmObject.Id);

                // convert mesh to meshGeo
                MeshGeometry3D mesh = new MeshGeometry3D();

                if (objectType == ObjectType.Mesh)
                {
                    mesh = (file3dmObject.Geometry as Mesh).ToMeshGeometry3D();
                }

                if (objectType == ObjectType.Brep)
                {
                    var tempMesh = new Mesh();
                    foreach (var face in (file3dmObject.Geometry as Brep).Faces)
                    {
                        tempMesh.Append(face.GetMesh(MeshType.Render));
                    }

                    tempMesh.Normals.ComputeNormals();
                    tempMesh.Compact();

                    mesh = tempMesh.ToMeshGeometry3D();
                }
                geoModel.Geometry = mesh;

                // create material
                var material = new DiffuseMaterial(new SolidColorBrush(File3dm.AllLayers
                    .FindIndex(file3dmObject.Attributes.LayerIndex).Color.ToMediaColor()));
                geoModel.Material = material;
                geoModel.BackMaterial = backFaceMaterial;

                geoModels.Add(geoModel);

                // union bbox
                bbox.Union(file3dmObject.Geometry.GetBoundingBox(Plane.WorldXY));
            }

            bbox.Inflate(2);
            SceneBbox = bbox;

            // set up camera from bbox
            //var position = bbox.Corner(true, true, false);
            //Camera = new PerspectiveCamera(position.ToPoint3D(), (bbox.Center - position).ToVector3D(),
            //    new Vector3D(0, 0, 1), 60);

            var myGeometryModel = new GeometryModel3D();

            // The geometry specifes the shape of the 3D plane. In this sample, a flat sheet 
            // is created.
            MeshGeometry3D myMeshGeometry3D = new MeshGeometry3D();

            // Create a collection of normal vectors for the MeshGeometry3D.
            Vector3DCollection myNormalCollection = new Vector3DCollection();
            myNormalCollection.Add(new Vector3D(0, 0, 1));
            myNormalCollection.Add(new Vector3D(0, 0, 1));
            myNormalCollection.Add(new Vector3D(0, 0, 1));
            myNormalCollection.Add(new Vector3D(0, 0, 1));
            myNormalCollection.Add(new Vector3D(0, 0, 1));
            myNormalCollection.Add(new Vector3D(0, 0, 1));
            myMeshGeometry3D.Normals = myNormalCollection;

            // Create a collection of vertex positions for the MeshGeometry3D. 
            Point3DCollection myPositionCollection = new Point3DCollection();
            myPositionCollection.Add(new Point3D(-0.5, -0.5, 0.5));
            myPositionCollection.Add(new Point3D(0.5, -0.5, 0.5));
            myPositionCollection.Add(new Point3D(0.5, 0.5, 0.5));
            myPositionCollection.Add(new Point3D(0.5, 0.5, 0.5));
            myPositionCollection.Add(new Point3D(-0.5, 0.5, 0.5));
            myPositionCollection.Add(new Point3D(-0.5, -0.5, 0.5));
            myMeshGeometry3D.Positions = myPositionCollection;

            // Create a collection of texture coordinates for the MeshGeometry3D.
            PointCollection myTextureCoordinatesCollection = new PointCollection();
            myTextureCoordinatesCollection.Add(new Point(0, 0));
            myTextureCoordinatesCollection.Add(new Point(1, 0));
            myTextureCoordinatesCollection.Add(new Point(1, 1));
            myTextureCoordinatesCollection.Add(new Point(1, 1));
            myTextureCoordinatesCollection.Add(new Point(0, 1));
            myTextureCoordinatesCollection.Add(new Point(0, 0));
            myMeshGeometry3D.TextureCoordinates = myTextureCoordinatesCollection;

            // Create a collection of triangle indices for the MeshGeometry3D.
            Int32Collection myTriangleIndicesCollection = new Int32Collection();
            myTriangleIndicesCollection.Add(0);
            myTriangleIndicesCollection.Add(1);
            myTriangleIndicesCollection.Add(2);
            myTriangleIndicesCollection.Add(3);
            myTriangleIndicesCollection.Add(4);
            myTriangleIndicesCollection.Add(5);
            myMeshGeometry3D.TriangleIndices = myTriangleIndicesCollection;

            // Apply the mesh to the geometry model.
            myGeometryModel.Geometry = myMeshGeometry3D;

            // The material specifies the material applied to the 3D object. In this sample a  
            // linear gradient covers the surface of the 3D object.

            // Create a horizontal linear gradient with four stops.   
            LinearGradientBrush myHorizontalGradient = new LinearGradientBrush();
            myHorizontalGradient.StartPoint = new Point(0, 0.5);
            myHorizontalGradient.EndPoint = new Point(1, 0.5);
            myHorizontalGradient.GradientStops.Add(new GradientStop(Colors.Yellow, 0.0));
            myHorizontalGradient.GradientStops.Add(new GradientStop(Colors.Red, 0.25));
            myHorizontalGradient.GradientStops.Add(new GradientStop(Colors.Blue, 0.75));
            myHorizontalGradient.GradientStops.Add(new GradientStop(Colors.LimeGreen, 1.0));

            // Define material and apply to the mesh geometries.
            DiffuseMaterial myMaterial = new DiffuseMaterial(myHorizontalGradient);
            myGeometryModel.Material = myMaterial;

            // Apply a transform to the object. In this sample, a rotation transform is applied,  
            // rendering the 3D object rotated.
            RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            AxisAngleRotation3D myAxisAngleRotation3d = new AxisAngleRotation3D();
            myAxisAngleRotation3d.Axis = new Vector3D(0, 3, 0);
            myAxisAngleRotation3d.Angle = 40;
            myRotateTransform3D.Rotation = myAxisAngleRotation3d;
            myGeometryModel.Transform = myRotateTransform3D;

            geoModels.Add(myGeometryModel);
            RenderMeshes = geoModels.ToArray();
        }

        private void CreateLights()
        {
            var corners = SceneBbox.GetCorners();
            //var tl = new DirectionalLight(Colors.White, (SceneBbox.Center - corners[4]).ToVector3D());
            //var tr = new DirectionalLight(Colors.White, (SceneBbox.Center - corners[5]).ToVector3D());

            var tl = new PointLight(Colors.White, corners[4].ToPoint3D());
            var tr = new PointLight(Colors.White, corners[5].ToPoint3D());
            var tbr = new PointLight(Colors.White, corners[7].ToPoint3D());
            var tbl = new PointLight(Colors.White, corners[6].ToPoint3D());

            Lights = new[] {tl, tr, tbr, tbl};
        }

        public PerspectiveCamera GetCamera()
        {
            var views = File3dm.AllViews;
            foreach (var view in views)
            {
                if (view.Viewport.IsPerspectiveProjection)
                {
                    return new PerspectiveCamera(view.Viewport.CameraLocation.ToPoint3D(),
                        view.Viewport.CameraDirection.ToVector3D(), view.Viewport.CameraUp.ToVector3D(),
                        view.Viewport.GetFieldOfView());
                }
            }

            if (Camera is null)
            {
                // Defines the camera used to view the 3D object. In order to view the 3D object,
                // the camera must be positioned and pointed such that the object is within view 
                // of the camera.
                PerspectiveCamera myPCamera = new PerspectiveCamera();

                // Specify where in the 3D scene the camera is.
                myPCamera.Position = new Point3D(0, 0, 2);

                // Specify the direction that the camera is pointing.
                myPCamera.LookDirection = new Vector3D(0, 0, -1);

                // Define camera's horizontal field of view in degrees.
                myPCamera.FieldOfView = 60;

                return myPCamera;
            }

            return Camera;
        }

        public string[] ToList()
        {
            string[] properties = new string[4];

            properties[0] = $"Name: {Name}";
            properties[1] = $"Layer Count: {LayerCount.ToString()}";
            properties[2] = $"Object Count: {ObjectCount.ToString()}";
            properties[3] = $"Blockdefinition Count: {BlockDefinitionCount.ToString()}";

            return properties;
        }
    }
}
