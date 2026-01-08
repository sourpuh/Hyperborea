using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Hyperborea.Screenshot;
using Pictomancy;

namespace Hyperborea.Guides;
internal class CompositeGuide : Guide
{
    public List<Guide> Children = new();

    public override AABB Bounds => AABB.Bounding(Children.Select(x => x.Bounds));

    public override void Draw(PctDrawList drawList)
    {
        foreach(var child in Children)
        {
            child.Draw(drawList);
        }
    }

    public override bool ParameterSelector()
    {
        bool ret = false;

        if (ImGuiComponents.IconButton("circle_guide", FontAwesomeIcon.Bullseye))
        {
            var newGuide = new CircleGuide(1);
            Children.Add(newGuide);
        }

        ImGui.SameLine();

        if (ImGuiComponents.IconButton("rectangle_guide", FontAwesomeIcon.BorderAll))
        {
            var newGuide = new RectangleGuide(1, 1);
            Children.Add(newGuide);
        }

        Guide? toDelete = null;
        foreach (var child in Children)
        {
            ret |= child.ParameterSelector();
            if (ImGui.Button($"Delete##{child}"))
            {
                toDelete = child;
            }
        }
        if (toDelete != null)
            Children.Remove(toDelete);

        return ret;
    }

    public override void AddToMask(Vector3 maskCenter, float[,] mask, float pixelsPerYalm = 50)
    {
        foreach (var child in Children)
        {
            Svc.Log.Info($"guide: {child}");
            child.AddToMask(maskCenter, mask, pixelsPerYalm);
        }
    }
}
