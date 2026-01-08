using ECommons.ExcelServices.TerritoryEnumeration;
using Hyperborea.Screenshot;
using Pictomancy;
using System.Text.Json.Serialization;

namespace Hyperborea.Guides;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CircleGuide), typeDiscriminator: "circle")]
[JsonDerivedType(typeof(RectangleGuide), typeDiscriminator: "rectangle")]
public abstract class Guide
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();

    public Vector3 Center => Bounds.Center;

    public abstract void Draw(PctDrawList drawList);
    //public abstract Vector3 North { get; }
    //public abstract float RotationRadians { get; set; }
    public abstract AABB Bounds { get; }

    public abstract bool ParameterSelector();

    internal bool IntySliderFloat(string id, ref float val, int min, int max)
    {
        string fmt = (val - (int)val) > 0.05f ? "%.1f" : "%.0f";
        return ImGui.SliderFloat(id, ref val, min, max, fmt);
    }

    internal bool ImguiRotationInput(ref int rotationDegrees)
    {
        return ImGui.DragInt("##rotation", ref rotationDegrees, 15, -180, 180);
    }
    public abstract void AddToMask(Vector3 center3, float[,] mask, float pixelsPerYalm = 50);
}
