using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace RhinoPreview.Core
{
    public static class Extensions
    {
        #region Conversions

        public static Point3D ToPoint3D(this Point3d pt)
        {
            return new Point3D(pt.X, pt.Y, pt.Z);
        }

        public static Point3D ToPoint3D(this Point3f pt)
        {
            return new Point3D(Convert.ToDouble(pt.X), Convert.ToDouble(pt.Y), Convert.ToDouble(pt.Z));
        }

        public static Vector3D ToVector3D(this Vector3d vec)
        {
            return new Vector3D(vec.X, vec.Y, vec.Z);
        }

        public static Vector3D ToVector3D(this Vector3f vec)
        {
            return new Vector3D(Convert.ToDouble(vec.X), Convert.ToDouble(vec.Y), Convert.ToDouble(vec.Z));
        }

        public static double ToDegrees(this double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static Rect3D ToRect3D(this BoundingBox bbox)
        {
            var box = new Box(bbox);
            return new Rect3D(bbox.Center.ToPoint3D(), new Size3D(box.X.Length, box.Y.Length, box.Z.Length));
        }

        public static MeshGeometry3D ToMeshGeometry3D(this Mesh mesh)
        {
            // empty mesh geo
            var meshGeo = new MeshGeometry3D();

            // triangulate input mesh
            var cBefore = mesh.Faces.Count;
            mesh.Faces.ConvertQuadsToTriangles();
            var cAfter = mesh.Faces.Count;

            // Destroy topology
            mesh.Compact();

            // re-calculate normals
            mesh.Normals.ComputeNormals();

            // get converted normal list
            //var normals = from normal in mesh.Normals select normal.ToVector3D();
            //meshGeo.Normals = new Vector3DCollection(normals);

            // get converted vertex list
            var vertices = new Point3DCollection();
            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                mesh.Faces.GetFaceVertices(i, out var a, out var b, out var c, out _);
                vertices.Add(a.ToPoint3D());
                vertices.Add(b.ToPoint3D());
                vertices.Add(c.ToPoint3D());
            }
            meshGeo.Positions = new Point3DCollection(vertices);

            // get faces?
            //var faceIndices = Enumerable.Range(0, mesh.Faces.Count);
            //meshGeo.TriangleIndices = new Int32Collection(faceIndices);

            // get bounding box for clipping
            //var bbox = mesh.GetBoundingBox(Plane.WorldXY).ToRect3D();
            //meshGeo.Bounds = bbox;

            return meshGeo;

        }

        #endregion

        public static string[] GetObjectProperties(this File3dmObject obj)
        {
            var props = new string[5];

            props[0] = $"Name: {obj.Name}";
            props[1] = $"Layer: {obj.Attributes.LayerIndex}";
            props[2] = $"Type: {obj.Geometry.ObjectType}";
            props[3] = $"Id: {obj.Id}";
            props[4] = $"Material: {obj.Attributes.MaterialIndex}";

            return props;
        }

        public static double GetFieldOfView(this ViewportInfo vpi)
        {
            //vpi.GetCameraAngles(out var halfDiagonal, out _, out _);
            //return halfDiagonal.ToDegrees();

            return 60;
        }

        public enum PanDirection
        {
            None,
            Left,
            Right,
            Up,
            Down
        }

        private static double panFactor = 0.1;
        public static void Pan(this PerspectiveCamera camera, PanDirection direction)
        {
            var vecLeft = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection);
            TranslateTransform3D xTrans;

            switch (direction)
            {
                case PanDirection.Left:
                    xTrans = new TranslateTransform3D(vecLeft * panFactor);
                    camera.Position = xTrans.Transform(camera.Position);
                    break;
                case PanDirection.Right:
                    xTrans = new TranslateTransform3D(-vecLeft * panFactor);
                    camera.Position = xTrans.Transform(camera.Position);
                    break;
                case PanDirection.Up:
                    xTrans = new TranslateTransform3D(-camera.UpDirection * panFactor);
                    camera.Position = xTrans.Transform(camera.Position);
                    break;
                case PanDirection.Down:
                    xTrans = new TranslateTransform3D(camera.UpDirection * panFactor);
                    camera.Position = xTrans.Transform(camera.Position);
                    break;
                case PanDirection.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static Point3D GetTarget(this PerspectiveCamera camera)
        {
            TranslateTransform3D xTrans = new TranslateTransform3D(camera.LookDirection * 5);
            return xTrans.Transform(camera.Position);
            //return new Point3D(0, 0, 0);
        }

        public static void Rotate(this PerspectiveCamera camera, PanDirection direction, BoundingBox bbox)
        {
            RotateTransform3D xRot;
            var angle = (Math.PI / 64).ToDegrees();
            var target = bbox.Center.ToPoint3D();
            Point3D upPoint;


            switch (direction)
            {
                case PanDirection.Left:
                    upPoint = new TranslateTransform3D(camera.UpDirection).Transform(camera.Position);

                    xRot = new RotateTransform3D(new AxisAngleRotation3D(camera.UpDirection, angle), target);
                    camera.Position = xRot.Transform(camera.Position);
                    camera.LookDirection = target - camera.Position;

                    camera.UpDirection = xRot.Transform(upPoint) - camera.Position;
                    break;
                case PanDirection.Right:
                    upPoint = new TranslateTransform3D(camera.UpDirection).Transform(camera.Position);

                    xRot = new RotateTransform3D(new AxisAngleRotation3D(camera.UpDirection, -angle), target);
                    camera.Position = xRot.Transform(camera.Position);
                    camera.LookDirection = target - camera.Position;

                    camera.UpDirection = xRot.Transform(upPoint) -camera.Position;
                    break;
                case PanDirection.Up:
                    upPoint = new TranslateTransform3D(camera.UpDirection).Transform(camera.Position);

                    xRot = new RotateTransform3D(
                        new AxisAngleRotation3D(Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection), -angle),
                        target);
                    camera.Position = xRot.Transform(camera.Position);
                    camera.LookDirection = target - camera.Position;

                    camera.UpDirection = xRot.Transform(upPoint) - camera.Position;
                    break;
                case PanDirection.Down:
                    upPoint = new TranslateTransform3D(camera.UpDirection).Transform(camera.Position);

                    xRot = new RotateTransform3D(
                        new AxisAngleRotation3D(Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection), angle),
                        target);
                    camera.Position = xRot.Transform(camera.Position);
                    camera.LookDirection = target - camera.Position;

                    camera.UpDirection = xRot.Transform(upPoint) - camera.Position;

                    break;
                case PanDirection.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
