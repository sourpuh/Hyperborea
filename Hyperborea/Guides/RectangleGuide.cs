using Dalamud.Interface.Components;
using ECommons.ExcelServices.TerritoryEnumeration;
using Hyperborea.Screenshot;
using Pictomancy;

namespace Hyperborea.Guides;
public class RectangleGuide(float halfWidth = 1, float halfDepth = 1) : Guide
{
    internal bool visible = false;
    public Vector3 center;
    public float HalfWidth = halfWidth;
    public float HalfDepth = halfDepth;
    private float RotationRadians = 0;

    public override void Draw(PctDrawList drawList)
    {
        if (visible)
        {
            drawList.AddText(PointAtOffset(0, HalfDepth + 0.1f) + Vector3.UnitY * 0.1f, 0xFFFFFFFF, "N", 5f);

            drawList.PathLineTo(NorthEast);
            drawList.PathLineTo(SouthEast);
            drawList.PathLineTo(SouthWest);
            drawList.PathLineTo(NorthWest);
            drawList.PathStroke(0xFFFFFFFF, PctStrokeFlags.Closed);
        }
    }

    private Vector3 PointAtOffset(float x, float z)
    {
        var radius = new Vector3(x, 0, z).Length();
        var radians = MathF.Atan2(z, x);
        Vector3 offset = new(MathF.Cos(RotationRadians - radians), 0, MathF.Sin(RotationRadians - radians));
        return center + offset * radius;
    }

    public override bool ParameterSelector()
    {
        bool ret = false;
        ImGui.TextUnformatted("Position:");
        ImGui.SetNextItemWidth(125f);
        ImGui.SameLine();
        ret |= ImGui.InputFloat3($"##position {GUID}", ref center, format: "%.1f");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton($"start_guide_selection {GUID}", FontAwesomeIcon.MousePointer))
        {
            visible = true;
            P.Overlay.StartMouseWorldPosSelecting(GUID);
        }
        switch (P.Overlay.MouseWorldPosSelection(GUID, ref center))
        {
            case PctOverlay.SelectionResult.Selected:
                //AlignToGuide();
                break;
            case PctOverlay.SelectionResult.Canceled:
                visible = false;
                break;
        }

        ImGui.TextUnformatted("Width:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ret |= IntySliderFloat($"##width {GUID}", ref HalfWidth, 1, 50);

        ImGui.TextUnformatted("Depth:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ret |= IntySliderFloat($"##depth {GUID}", ref HalfDepth, 1, 50);

        return ret;
    }

    public Vector3 North => PointAtOffset(0, HalfDepth);
    public Vector3 NorthEast => PointAtOffset(HalfWidth, HalfDepth);
    public Vector3 SouthEast => PointAtOffset(HalfWidth, -HalfDepth);
    public Vector3 SouthWest => PointAtOffset(-HalfWidth, -HalfDepth);
    public Vector3 NorthWest => PointAtOffset(-HalfWidth, HalfDepth);

    public override AABB Bounds => new(center - new Vector3(HalfWidth, HalfWidth, HalfDepth), center + new Vector3(HalfWidth, HalfWidth, HalfDepth));

    public override void AddToMask(Vector3 center3, float[,] mask, float pixelsPerYalm = 50)
    {
        int width = mask.GetLength(0);
        int height = mask.GetLength(1);
        Svc.Log.Info($"rect {width}, {mask}");
        // world coordinates of the NW corner of the mask
        Vector2 maskNW = center3.XZ() - new Vector2(width, height) / pixelsPerYalm / 2;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 offset = new Vector2(i, j) / pixelsPerYalm;
                Vector2 worldPos = maskNW + offset;

                bool containsX = MathF.Abs(center.X - worldPos.X) < HalfWidth;
                bool containsZ = MathF.Abs(center.Z - worldPos.Y) < HalfDepth;
                if (containsX && containsZ)
                {
                    mask[i, j] = 1f;
                }
            }
        }
    }
}
