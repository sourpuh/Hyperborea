using System.Linq;
using Transformation = Lumina.Data.Parsing.Common.Transformation;

namespace Hyperborea.Screenshot;
public class AABB
{
    public Vector3 Min { get; set; }
    public Vector3 Max { get; set; }
    internal Vector3 Center => (Min + Max) / 2;
    internal float LongAxisLength => MathF.Max((Max - Min).X, (Max - Min).Z);

    public static AABB Bounding(IEnumerable<AABB> boxes)
    {
        var minPoints = boxes.Select(x => x.Min);
        var maxPoints = boxes.Select(x => x.Max);
        return Bounding(minPoints.Concat(maxPoints));
    }

    public static AABB Bounding(IEnumerable<Vector3> points)
    {
        var min = float.MaxValue * Vector3.One;
        var max = float.MinValue * Vector3.One;
        foreach (var point in points)
        {
            min = Vector3.MinNumber(min, point);
            max = Vector3.MaxNumber(max, point);
        }
        return new AABB(min, max);
    }
    public AABB()
    {
    }

    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public AABB(Transformation t)
    {
        Min = Convert(t.Translation) - Convert(t.Scale);
        Max = Convert(t.Translation) + Convert(t.Scale);
    }

    public bool Contains(Vector3 v)
    {
        return v.X > Min.X &&
               v.Y > Min.Y &&
               v.Z > Min.Z &&
               v.X < Max.X &&
               v.Y < Max.Y &&
               v.Z < Max.Z;
    }

    private static Vector3 Convert(Lumina.Data.Parsing.Common.Vector3 v)
    {
        return new(v.X, v.Y, v.Z);
    }

    internal Vector2 OverheadSize => Max.XZ() - Min.XZ();
}
