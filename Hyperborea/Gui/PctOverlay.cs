using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Hyperborea.Guides;
using Pictomancy;

namespace Hyperborea;

/**
 * Pictomancy overlay to draw draft waymarks and guide and provide mouse world position selection.
 */
public class PctOverlay
{
    public object? currentMousePlacementThing;
    // TODO maybe Queue up everything to draw in this list if selection and placeholders should be separate
    internal List<Action<PctDrawList>> list = new();
    //internal bool showGuide = false;
    //internal Guide guide;

    Quaternion? rmbStart;
    Quaternion? lmbStart;

    public PctOverlay()
    {
        PictoService.Initialize(Svc.PluginInterface);
        Svc.PluginInterface.UiBuilder.Draw += OnUpdate;
    }

    public void Dispose()
    {
        PictoService.Dispose();
        Svc.PluginInterface.UiBuilder.Draw -= OnUpdate;
    }

    public void OnTerritoryChange()
    {
        //showGuide = false;
        //guide = new CircleGuide();
    }

    private void DrawCrosshair(PctDrawList drawList, Vector3 worldPos)
    {
        drawList.PathLineTo(worldPos - Vector3.UnitX);
        drawList.PathLineTo(worldPos + Vector3.UnitX);
        drawList.PathStroke(0xFFFFFFFF, new());
        drawList.PathLineTo(worldPos - Vector3.UnitZ);
        drawList.PathLineTo(worldPos + Vector3.UnitZ);
        drawList.PathStroke(0xFFFFFFFF, new());
    }

    internal void DeferDrawDebugRay(RaycastHit hit1, RaycastHit hit2)
    {
        list.Add((PctDrawList drawList) => DrawDebugRay(drawList, hit1, hit2));
    }

    private void DrawDebugRay(PctDrawList drawList, RaycastHit hit1, RaycastHit hit2)
    {
        drawList.PathLineTo(hit1.Point);
        drawList.PathLineTo(hit2.Point);
        drawList.PathStroke(0xFF0000FF, new());
    }

    public Vector3 SnapToGrid(Vector3 input)
    {
        Vector3 gridSnapped = new(MathF.Round(input.X, 1), input.Y, MathF.Round(input.Z, 1));
        return gridSnapped;
    }

    internal void StartMouseWorldPosSelecting(object thing)
    {
        currentMousePlacementThing = thing;
    }

    internal enum SelectionResult
    {
        NotSelecting,
        SelectingValid,
        SelectingInvalid,
        Selected,
        Canceled,
    }

    private unsafe Quaternion CameraRotation => FFXIVClientStructs.FFXIV.Common.Math.Quaternion.CreateFromRotationMatrix(CameraManager.Instance()->CurrentCamera->ViewMatrix);

    private unsafe bool IsClicked(MouseButtonFlags button, ref Quaternion? startingCameraRotation)
    {
        // Use RMB to cancel selection if clicked with negligible drift.
        var isDown = UIInputData.Instance()->UIFilteredCursorInputs.MouseButtonHeldFlags.HasFlag(button);
        if (isDown && startingCameraRotation == null)
        {
            startingCameraRotation = CameraRotation;
        }
        if (!isDown && startingCameraRotation.HasValue)
        {
            var drift = 1 - Quaternion.Dot(CameraRotation, startingCameraRotation.Value);
            startingCameraRotation = null;
            if (drift < 0.001)
            {
                currentMousePlacementThing = null;
                return true;
            }
        }
        return false;
    }

    internal unsafe SelectionResult MouseWorldPosSelection(object thing, ref Vector3 worldPos)
    {
        if (!thing.Equals(currentMousePlacementThing))
        {
            return SelectionResult.NotSelecting;
        }

        var mousePos = ImGui.GetIO().MousePos;
        if (Raycaster.ScreenToWorld(mousePos, out worldPos))
        {
            worldPos = SnapToGrid(worldPos);

            if (!Raycaster.CheckAndSnapY(ref worldPos))
                return SelectionResult.SelectingInvalid;

            if (IsClicked(MouseButtonFlags.RBUTTON, ref rmbStart))
            {
                return SelectionResult.Canceled;
            }

            if (IsClicked(MouseButtonFlags.LBUTTON, ref lmbStart))
            {
                return SelectionResult.Selected;
            }

            var worldPosTemp = worldPos;
            list.Add((PctDrawList drawList) => DrawCrosshair(drawList, worldPosTemp));
            return SelectionResult.SelectingValid;
        }

        return SelectionResult.SelectingInvalid;
    }

    bool once = true;

    private void OnUpdate()
    {
        if (once)
        {
            var shouldDraw = list.Count > 0;
            if (!shouldDraw)
            {
                list.Clear();
                return;
            }
            try
            {
                using (var drawList = PictoService.Draw())
                {

                    if (drawList == null)
                    {
                        list.Clear();
                        return;
                    }
                    foreach (var action in list)
                        action(drawList);
                    list.Clear();
                }
            }
            catch (Exception e)
            {
                //Svc.ReportError($"Drawing Failed Please Report! Restart plugin to re-enable drawing. Caught {e}");
                once = false;
            }
        }
    }
}
