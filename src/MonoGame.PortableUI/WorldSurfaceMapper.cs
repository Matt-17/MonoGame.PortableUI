using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI
{
    public static class WorldSurfaceMapper
    {
        public static Ray GetMouseRay(Viewport viewport, Matrix view, Matrix projection, PointF mousePosition)
        {
            var near = viewport.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0), projection, view, Matrix.Identity);
            var far = viewport.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 1), projection, view, Matrix.Identity);
            var direction = Vector3.Normalize(far - near);
            return new Ray(near, direction);
        }

        public static bool TryMapRayToSurface(
            Ray ray,
            Matrix quadWorld,
            Vector2 quadSize,
            int surfaceWidth,
            int surfaceHeight,
            out PointF uiPoint)
        {
            uiPoint = new PointF();
            if (quadSize.X <= 0 || quadSize.Y <= 0 || surfaceWidth <= 0 || surfaceHeight <= 0)
                return false;

            Matrix.Invert(ref quadWorld, out var inverse);
            var localOrigin = Vector3.Transform(ray.Position, inverse);
            var localDirection = Vector3.TransformNormal(ray.Direction, inverse);
            if (System.Math.Abs(localDirection.Z) < 0.00001f)
                return false;

            var distance = -localOrigin.Z / localDirection.Z;
            if (distance < 0)
                return false;

            var hit = localOrigin + localDirection * distance;
            var u = hit.X / quadSize.X + 0.5f;
            var v = 0.5f - hit.Y / quadSize.Y;
            if (u < 0 || u > 1 || v < 0 || v > 1)
                return false;

            uiPoint = new PointF(u * surfaceWidth, v * surfaceHeight);
            return true;
        }

        public static bool TryMapPointToSurface(
            PointF screenPoint,
            Matrix spriteTransform,
            Vector2 spriteSize,
            int surfaceWidth,
            int surfaceHeight,
            out PointF uiPoint)
        {
            uiPoint = new PointF();
            if (spriteSize.X <= 0 || spriteSize.Y <= 0 || surfaceWidth <= 0 || surfaceHeight <= 0)
                return false;

            Matrix.Invert(ref spriteTransform, out var inverse);
            var local = Vector2.Transform(new Vector2(screenPoint.X, screenPoint.Y), inverse);
            var u = local.X / spriteSize.X;
            var v = local.Y / spriteSize.Y;
            if (u < 0 || u > 1 || v < 0 || v > 1)
                return false;

            uiPoint = new PointF(u * surfaceWidth, v * surfaceHeight);
            return true;
        }
    }
}
