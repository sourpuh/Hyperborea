using Dalamud.Interface.Components;
using Hyperborea.Screenshot;
using Pictomancy;
using TerraFX.Interop.Windows;

namespace Hyperborea.Guides;
public class CircleGuide(float radius = 1) : Guide
{
    internal bool visible = true;
    public Vector3 center;
    public float Radius = radius;

    public override void Draw(PctDrawList drawList)
    {
        drawList.AddCircle(
            center,
            Radius,
            0xFFFFFFFF,
            thickness: 2);
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

        ImGui.TextUnformatted("Radius:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ret |= IntySliderFloat($"##radius {GUID}", ref Radius, 1, 50);

        return ret;
    }

    public override AABB Bounds => new(center - new Vector3(Radius), center + new Vector3(Radius));

    //public bool[] Mask(Vector3 center3, int width, int height)
    //{
    //    const float pixelsPerYalm = 50;
    //    Vector2 maskNW = center3.XZ() - new Vector2(width, height) * pixelsPerYalm / 2;
    //    var mask = new bool[width * height];
    //    for (int i = 0; i < width; i++)
    //    {
    //        for (int j = 0; j < height; j++)
    //        {
    //            Vector2 offset = new Vector2(i, j) * pixelsPerYalm;
    //            Vector2 worldPos = maskNW + offset;
    //            if (Vector2.Distance(center.XZ(), worldPos) < Radius)
    //            {
    //                mask[i * width + j] = true;
    //            }
    //        }
    //    }
    //    return mask;
    //}

    public override void AddToMask(Vector3 center3, float[,] mask, float pixelsPerYalm = 50)
    {
        int width = mask.GetLength(0);
        int height = mask.GetLength(1);
        Svc.Log.Info($"circle {width}, {mask}");

        // world coordinates of the NW corner of the mask
        Vector2 maskNW = center3.XZ() - new Vector2(width, height) / pixelsPerYalm / 2;
        //Svc.Log.Debug($"{center3} {maskNW}");
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 offset = new Vector2(i, j) / pixelsPerYalm;
                Vector2 worldPos = maskNW + offset;
                //Svc.Log.Debug($"{i} {j} {offset}");
                if (Vector2.Distance(center.XZ(), worldPos) < Radius)
                {
                    mask[i, j] = 1f;
                }
            }
        }
    }
}
