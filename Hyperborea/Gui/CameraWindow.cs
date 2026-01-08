using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons.Configuration;
using ECommons.Hooks;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Hyperborea.Guides;
using Hyperborea.Screenshot;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace Hyperborea.Gui;

public class CameraWindow : Window
{
    // TODO SIG FROM https://github.com/UnknownX7/Cammy/blob/master/Game.cs#L17
    public static readonly AsmPatch cameraNoClippyReplacer = new("E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? F3 0F 10 44 24 ?? 41 B7 01", [0x30, 0xC0, 0x90, 0x90, 0x90]);
    int index = 0;
    //List<MapParameters> maps;
    internal CameraWindow() : base("Sour Hyperborea", ImGuiWindowFlags.NoScrollbar)
    {
        EzConfigGui.WindowSystem.AddWindow(this);

        Size = new(400, 500);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(250, 330),
        };

        //using (FileStream stream = File.OpenRead($"{Svc.PluginInterface.ConfigDirectory}\\custom_map_parameters.json"))
        //{
        //    var options = new JsonSerializerOptions { IncludeFields = true };
        //    maps = JsonSerializer.Deserialize<List<MapParameters>>(stream, options);
        //}

        //    ZoneData myZoneData = new();
        //    foreach (var map in maps)
        //    {
        //        ZoneInfo info;
        //        if (!myZoneData.Data.TryGetValue(map.Bg, out info))
        //        {
        //            info = new();
        //            info.Name = map.Name;
        //            info.Spawn = map.Center.ToPoint3();
        //            myZoneData.Data.Add(map.Bg, info);
        //        }
        //        PhaseInfo phase = new();
        //        phase.Name = map.Name;
        //        phase.Weather = map.Weather;
        //        phase.MapEffects.AddRange(map.MapEffects.Select(x => new MapEffectInfo() { a1 = (int)x.Index, a2 = x.State, a3 = x.Param}));
        //        phase.Spawn = map.Center.ToPoint3();
        //        info.Phases.Add(phase);
        //    }
        //    EzConfig.SaveConfiguration(myZoneData, $"{Svc.PluginInterface.ConfigDirectory}\\new_data.yaml");
    }

    MapParameters currentMap;
    CompositeGuide guide => currentMap.Guide;
    const int maxMapEffects = 200;
    float padding = 0;
    bool[] activeEffects = new bool[maxMapEffects];
    ushort[] param3s = new ushort[maxMapEffects];
    InstanceObject?[] instanceObjects = new InstanceObject?[maxMapEffects];
    Stopwatch stopwatch = new();
    int mapEffectApplications = 0;

    public void OnTerritoryChange(ushort territoryId)
    {
        currentMap = MapParameters.Init(Svc.Data, territoryId);
        for (int i = 0; i < activeEffects.Length; i++)
        {
            activeEffects[i] = false;
            param3s[i] = 0;
            instanceObjects[i] = null;
        }
        stopwatch.Restart();
        mapEffectApplications = 2;
    }

    public unsafe override void Draw()
    {
        P.Overlay.list.Add(guide.Draw);
        bool align = false;
        var gameCamera = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera();
        var renderCamera = gameCamera->SceneCamera.RenderCamera;

        if (ImGui.BeginTable("columns", 2))
        {
            ImGui.TableSetupColumn("Primary", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Secondary", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            //ImGui.Text($"{index}/{maps.Count}");
            //bool load = false;
            //if (ImGuiComponents.IconButton("goLeft", FontAwesomeIcon.ArrowLeft))
            //{
            //    index--;
            //    if (index < 0)
            //        index = maps.Count - 1;
            //    load = true;
            //}

            //ImGui.SameLine();

            //if (ImGuiComponents.IconButton("goRight", FontAwesomeIcon.ArrowRight))
            //{
            //    index = (index + 1) % maps.Count;
            //    load = true;
            //}

            var map = currentMap;

            //ImGui.SameLine();
            //if (ImGuiComponents.IconButton("skip", FontAwesomeIcon.Times))
            //{
            //    map.Skip = true;
            //}
            //ImGui.SameLine();
            //if (load || ImGuiComponents.IconButton("load", FontAwesomeIcon.Upload))
            //{
            //    UI.a2 = (int)map.TerritoryId;
            //    UI.SpawnOverride = true;
            //    UI.Position = map.Center.ToPoint3();
            //    UI.Load();

            //    var halfSize = map.AABB.Center - map.AABB.Min;
            //    //Guide newguide;
            //    //if (map.Shape == "rectangle")
            //    //{
            //    //    var newrguide = new RectangleGuide();
            //    //    newrguide.HalfWidth = halfSize.X;
            //    //    newrguide.HalfDepth = halfSize.Z;
            //    //    newguide = newrguide;
            //    //}
            //    //else
            //    //{
            //    //    var newcguide = new CircleGuide();
            //    //    newcguide.Radius = map.AABB.LongAxisLength / 2;
            //    //    newguide = newcguide;
            //    //}
            //    //newguide.Center = map.Center;
            //    ////newguide.RotationRadians = map.Rotation;
            //    //Raycaster.CheckAndSnapY(ref newguide.center, castHeight: map.OrthoHeight * 2);
            //    //map.ForegroundMask.Add(newguide);
            //    //P.Overlay.guide = newguide;
            //    //P.Overlay.showGuide = true;
            //    //align = true;

            //    stopwatch.Restart();
            //    mapEffectApplications = 2;
            //}

            if (stopwatch.ElapsedMilliseconds > 500)
            {
                foreach (var mapEffect in map.MapEffects)
                {
                    MapEffect.Delegate(Utils.GetMapEffectModule(), mapEffect.Index, mapEffect.State, mapEffect.Param);
                    param3s[mapEffect.Index] = mapEffect.Param;
                }
                if (mapEffectApplications-- > 0)
                    stopwatch.Restart();
                else
                    stopwatch.Reset();
            }

            var e = EnvManager.Instance();
            //var guide = P.Overlay.guide;
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("ss", FontAwesomeIcon.CameraRetro))
            {
                map.Center = guide.Center;
                //map.CamOffset = map.Center - Svc.ClientState.LocalPlayer.Position;
                //map.OrthoHeight = renderCamera->OrthoHeight - padding;
                // guides no longer have rotation
                //map.Rotation = guide.RotationRadians;
                //map.AABB = guide.Bounds;
                //map.ForegroundMask = guide.Children;

                List<MapParameters.MapEffect> activeMapEffects = new();
                var director = EventFramework.Instance()->DirectorModule.ActiveContentDirector;

                var mapEffects = director->MapEffects;
                if (mapEffects != null)
                {
                    for (uint i = 0; i < mapEffects->ItemCount; i++)
                    {
                        var effect = mapEffects->Items[(int)i];
                        var state = effect.State;
                        var param = param3s[i];
                        if (param > 0)
                        {
                            activeMapEffects.Add(new()
                            {
                                Index = i,
                                State = state,
                                Param = param,
                            });
                        }
                    }
                }
                map.MapEffects = activeMapEffects;
                map.Weather = e->ActiveWeather;


                var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
                using (FileStream createStream = File.Create($"{Svc.PluginInterface.ConfigDirectory}\\params\\{map.Filename}.json"))
                {
                    JsonSerializer.Serialize(createStream, map, options);
                }

                const float pixelsPerYalm = 50;
                var size = (map.AABB.OverheadSize + new Vector2(padding)) * pixelsPerYalm;
                P.Screenshotter.TakeScreenshot((uint)size.X, (uint)size.Y, map.Filename, guide);
            }
            ImGui.Text($"{map.Filename} {map.TerritoryId} {map.Name} {e->DayTimeSeconds}");
            ImGui.Separator();
            ImGui.SliderFloat("Time of Day", ref e->DayTimeSeconds, 0, 60 * 60 * 24 - 1);
            ImGui.Separator();
            DrawMaskSelector();
            ImGui.Separator();
            var noclip = cameraNoClippyReplacer.IsEnabled;
            if (ImGui.Checkbox("No Clip", ref noclip))
            {
                if (noclip)
                    cameraNoClippyReplacer.Enable();
                else
                    cameraNoClippyReplacer.Disable();
            }

            float maxNearPlane = renderCamera->FiniteFarPlane ? renderCamera->FarPlane : 100;
            ImGui.SliderFloat("Near Plane", ref renderCamera->NearPlane, 0.1f, maxNearPlane);
            ImGui.SliderFloat("Far Plane", ref renderCamera->FarPlane, renderCamera->NearPlane, 2000);
            ImGui.Checkbox("Finite Far Plane", ref renderCamera->FiniteFarPlane);

            ImGui.Separator();

            ImGui.SliderFloat("FoV", ref gameCamera->FoV, gameCamera->MinFoV, gameCamera->MaxFoV);
            ImGui.SliderFloat("Min FoV", ref gameCamera->MinFoV, 0.5f, gameCamera->MaxFoV - 0.1f);
            ImGui.SliderFloat("Max FoV", ref gameCamera->MaxFoV, gameCamera->MinFoV + 0.1f, 0.9f);

            ImGui.Separator();

            float zoom = gameCamera->Distance - padding;
            if (ImGui.SliderFloat("Zoom", ref zoom, gameCamera->MinDistance, gameCamera->MaxDistance))
            {
                gameCamera->Distance = zoom + padding;
            }
            ImGui.SliderFloat("Min Zoom", ref gameCamera->MinDistance, 1, gameCamera->MaxDistance - 0.1f);
            ImGui.SliderFloat("Max Zoom", ref gameCamera->MaxDistance, gameCamera->MinDistance + 0.1f, 120);

            ImGui.Separator();

            ImGui.Checkbox("Orthographic", ref renderCamera->IsOrtho);
            float orthoHeight = renderCamera->OrthoHeight - padding;
            if (ImGui.SliderFloat("Ortho Height", ref orthoHeight, 1, 120))
            {
                renderCamera->OrthoHeight = MathF.Round(orthoHeight + padding);
            }

            ImGui.Separator();

            var angle = new Vector2(gameCamera->DirH, gameCamera->DirV);
            //if (ImGui.Button("Align North"))
            //{
            //    gameCamera->minVRotation = -MathF.PI / 2 + 0.002f;
            //    angle.X = 0;
            //    angle.Y = gameCamera->minVRotation;
            //    gameCamera->Angle = angle;
            //}
            if (ImGui.DragFloat2("CameraAngle", ref angle, 0.1f))
            {
                gameCamera->DirH = angle.X;
                gameCamera->DirV = angle.Y;
            }
            ImGui.SliderFloat("MinVRotation", ref gameCamera->DirVMin, -MathF.PI / 2 + 0.002f, 0);
            ImGui.SliderFloat("MaxVRotation", ref gameCamera->DirVMax, gameCamera->DirVMin, MathF.PI / 2 - 0.001f);

            ImGui.Separator();

            using (ImRaii.Disabled(guide == null))
            {
                if (ImGui.Button("Align To Guide") || align)
                {
                    ResetCamera();
                    AlignToGuide();
                }
                ImGui.SameLine();
                if (ImGui.SliderFloat("padding", ref padding, 0, 10))
                {
                    padding = MathF.Round(padding);
                    AlignToGuide();
                }
            }

            if (Svc.ClientState.LocalPlayer != null)
            {
                var gameObject = (GameObject*)Svc.ClientState.LocalPlayer.Address;
                const VisibilityFlags Invisible = VisibilityFlags.Model | VisibilityFlags.Nameplate;
                var visible = (gameObject->RenderFlags & Invisible) > 0;
                if (ImGui.Checkbox("Hide local player", ref visible))
                {
                    if (visible)
                        gameObject->RenderFlags |= Invisible;
                    else
                        gameObject->RenderFlags &= ~Invisible;
                }
                ImGui.DragFloat("X", ref gameObject->Position.X);
                ImGui.DragFloat("Y", ref gameObject->Position.Y);
                ImGui.DragFloat("Z", ref gameObject->Position.Z);
            }

            ImGui.TableNextColumn();
            DrawMapEffect();

            ImGui.EndTable();
        }
    }

    internal bool InputBitmask(string id, ref ushort val)
    {
        //ImGui.Text($"{val} / ");
        bool isTouched = false;
        for (int i = 0; i < 16; i++)
        {
            if(i>0)
                ImGui.SameLine();
            bool set = (val & (1 << i)) != 0;
            if (ImGui.Checkbox($"##{id}#{i}", ref set))
            {
                val = (ushort)(set ? (val | (1 << i)) : (val & ~(1 << i)));
                isTouched = true;
            }
        }
        return isTouched;
    }

    internal unsafe void DrawMapEffect()
    {
        var map = currentMap;
        var director = EventFramework.Instance()->DirectorModule.ActiveContentDirector;

        if (director != null)
        {
            var mapEffects = director->MapEffects;
            if (mapEffects != null)
            {
                for (int i = 0; i < mapEffects->ItemCount; i++)
                {
                    var effect = mapEffects->Items[i];
                    if (instanceObjects[i] == null && map.Bg != "")
                    {
                        var objs = GetObjectsById(map.Bg, effect.LayoutId);
                        instanceObjects[i] = objs.FirstOrDefault();
                    }
                    //if (ImGui.Checkbox($"##{i}", ref activeEffects[i]))
                    //{
                    //    // serialize
                    //}
                    bool update = false;
                    ImGui.Text($"{i}"); ImGui.SameLine();

                    ImGui.Text("State: "); ImGui.SameLine();
                    int i2 = effect.State;
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt("##State" + i, ref i2))
                    {
                        update = true;
                    }
                    ImGui.SameLine();
                    ImGui.Text($"param3: "); ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (InputBitmask("##Flags" + i, ref param3s[i]))
                    {
                        update = true;
                    }
                    //ImGui.SameLine();
                    //ImGui.Text($"Flags: " + effect.Flags);

                    var maybeobj = instanceObjects[i];
                    if (maybeobj != null)
                    {
                        var obj = maybeobj.Value;
                        Vector3 pos = new(
                            obj.Transform.Translation.X,
                            obj.Transform.Translation.Y,
                            obj.Transform.Translation.Z
                        );
                        var x = i;
                        P.Overlay.list.Add((drawList) => drawList.AddText(pos + new Vector3(0, x * 0.5f, 0), 0xFFFFFFFF, $"{x}", 1));

                        if (obj.AssetType == LayerEntryType.SharedGroup)
                        {
                            var sharedGroup = (SharedGroupInstanceObject)obj.Object;
                            ImGui.SameLine();
                            ImGui.Text($"{sharedGroup.AssetPath}");
                            var sgb = GetSharedGroup(sharedGroup.AssetPath);
                        }
                    }
                    else
                    {
                        ImGui.SameLine();
                        ImGui.Text("not found");
                    }

                    if (update)
                    {
                        if (i2 == 0) i2 = 1;
                        MapEffect.Delegate(Utils.GetMapEffectModule(), (uint)i, (ushort)i2, param3s[i]);
                    }
                }
            }
        }
    }

    internal void DrawMaskSelector()
    {
        guide.ParameterSelector();
    }

    //internal void DrawGuideSelector(Guide g)
    //{
    //    var map = maps[index];
    //    if (P.Overlay.showGuide && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.EyeSlash, "Hide Guide"))
    //    {
    //        P.Overlay.showGuide = false;
    //    }
    //    else if (!P.Overlay.showGuide)
    //    {
    //        if (g.Center == Vector3.Zero)
    //        {
    //            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.LocationCrosshairs, "Place Guide"))
    //            {
    //                P.Overlay.showGuide = true;
    //                P.Overlay.StartMouseWorldPosSelecting("guide");
    //            }
    //        }
    //        else if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Eye, "Show Guide"))
    //        {
    //            P.Overlay.showGuide = true;
    //        }
    //    }
    //    ImGui.SameLine();

    //    using (ImRaii.Disabled(g is CircleGuide))
    //    {
    //        if (ImGuiComponents.IconButton("circle_guide", FontAwesomeIcon.Bullseye))
    //        {
    //            var oldGuide = g;
    //            if (oldGuide is RectangleGuide oldRectangleGuide)
    //            {
    //                var newGuide = new CircleGuide(Math.Max(oldRectangleGuide.HalfWidth, oldRectangleGuide.HalfDepth));
    //                newGuide.center = oldRectangleGuide.center;
    //                newGuide.RotationDegrees = oldRectangleGuide.RotationDegrees;
    //                var i = map.ForegroundMask.IndexOf(g);
    //                map.ForegroundMask[i] = newGuide;                    
    //            }
    //        }
    //    }
    //    ImGui.SameLine();
    //    using (ImRaii.Disabled(g is RectangleGuide))
    //    {
    //        if (ImGuiComponents.IconButton("rectangle_guide", FontAwesomeIcon.BorderAll))
    //        {
    //            var oldGuide = g;
    //            if (oldGuide is CircleGuide oldCircleGuide)
    //            {
    //                var newGuide = new RectangleGuide(oldCircleGuide.Radius, oldCircleGuide.Radius);
    //                newGuide.center = oldCircleGuide.center;
    //                newGuide.RotationDegrees = oldCircleGuide.RotationDegrees;
    //                var i = map.ForegroundMask.IndexOf(g);
    //                map.ForegroundMask[i] = newGuide;
    //            }
    //        }
    //    }

    //    ImGui.TextUnformatted("Position:");
    //    ImGui.SetNextItemWidth(125f);
    //    ImGui.SameLine();
    //    //ImGui.InputFloat3("##position", ref g.center, "%.1f");
    //    //ImGui.SameLine();
    //    //if (ImGuiComponents.IconButton("start_guide_selection", FontAwesomeIcon.MousePointer))
    //    //{
    //    //    P.Overlay.showGuide = true;
    //    //    P.Overlay.StartMouseWorldPosSelecting("guide");
    //    //}
    //    //switch (P.Overlay.MouseWorldPosSelection("guide", ref g.center))
    //    //{
    //    //    case PctOverlay.SelectionResult.Selected:
    //    //        AlignToGuide();
    //    //        break;
    //    //    case PctOverlay.SelectionResult.Canceled:
    //    //        P.Overlay.showGuide = false;
    //    //        break;
    //    //}

    //    if (g is CircleGuide circleGuide)
    //    {
    //        ImGui.TextUnformatted("Radius:");
    //        ImGui.SetNextItemWidth(120f);
    //        ImGui.SameLine();
    //        if (IntySliderFloat("##radius", ref circleGuide.Radius, 1, 50)) AlignToGuide();

    //        ImGui.TextUnformatted("Rotation:");
    //        ImGui.SetNextItemWidth(120f);
    //        ImGui.SameLine();
    //        if (ImguiRotationInput(ref circleGuide.RotationDegrees)) AlignToGuide();
    //    }
    //    if (g is RectangleGuide rectangleGuide)
    //    {
    //        ImGui.TextUnformatted("Width:");
    //        ImGui.SetNextItemWidth(120f);
    //        ImGui.SameLine();
    //        if (IntySliderFloat("##width", ref rectangleGuide.HalfWidth, 1, 50)) AlignToGuide();

    //        ImGui.TextUnformatted("Depth:");
    //        ImGui.SetNextItemWidth(120f);
    //        ImGui.SameLine();
    //        if (IntySliderFloat("##depth", ref rectangleGuide.HalfDepth, 1, 50)) AlignToGuide();

    //        ImGui.TextUnformatted("Rotation:");
    //        ImGui.SetNextItemWidth(120f);
    //        ImGui.SameLine();
    //        if (ImguiRotationInput(ref rectangleGuide.RotationDegrees)) AlignToGuide();
    //    }
    //}

    internal bool IntySliderFloat(string id, ref float val, int min, int max)
    {
        string fmt = (val - (int)val) > 0.05f ? "%.1f" : "%.0f";
        return ImGui.SliderFloat(id, ref val, min, max, fmt);
    }

    internal bool ImguiRotationInput(ref int rotationDegrees)
    {
        return ImGui.DragInt("##rotation", ref rotationDegrees, 15, -180, 180);
    }

    internal unsafe void AlignToGuide()
    {
        var gameCamera = (GameCamera*)FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera();
        var renderCamera = gameCamera->SceneCamera.RenderCamera;

        renderCamera->OrthoHeight = guide.Bounds.OverheadSize.Y + padding;
        gameCamera->currentFoV = 0.9f;
        gameCamera->currentZoom = renderCamera->OrthoHeight;
        gameCamera->minVRotation = -MathF.PI / 2;// + 0.002f;
        var angle = gameCamera->angle;
        angle.X = 0; //guide.RotationRadians;
        angle.Y = gameCamera->minVRotation;
        gameCamera->angle = angle;
        gameCamera->position = guide.Center;

        var gameObject = (GameObject*)Svc.ClientState.LocalPlayer.Address;
        gameObject->Position.X = guide.Center.X;
        gameObject->Position.Y = guide.Center.Y;
        gameObject->Position.Z = guide.Center.Z;
    }

    internal unsafe void ResetCamera()
    {
        var gameCamera = (GameCamera*)FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera();
        var renderCamera = gameCamera->SceneCamera.RenderCamera;

        cameraNoClippyReplacer.Enable();

        var map = currentMap;
        renderCamera->IsOrtho = true;
        renderCamera->OrthoHeight = map.OrthoHeight;
        gameCamera->currentFoV = 0.9f;
        gameCamera->currentZoom = map.OrthoHeight;
    }

    private static Dictionary<uint, IEnumerable<InstanceObject>> IdToInstanceObjectCache = new();

    private static IEnumerable<InstanceObject> GetObjectsById(string bg, uint id)
    {
        IEnumerable<InstanceObject>? objects;
        if (!IdToInstanceObjectCache.TryGetValue(id, out objects))
        {
            var objList = new List<InstanceObject>();
            var slashIndex = bg.LastIndexOf('/');

            Svc.Log.Info($"GetObjectsById {id} {bg} {slashIndex}");

            if (slashIndex == -1) return objList;

            List<string> files = ["planmap", "bg", "planevent", "vfx", "planlive", "sound"];
            foreach (var file in files)
            {
                var lgbPath = $"bg/{bg[..slashIndex]}/{file}.lgb";
                var lgb = Svc.Data.GetFile<LgbFile>(lgbPath);
                if (lgb != null)
                {
                    foreach (var layer in lgb.Layers)
                    {
                        foreach (var obj in layer.InstanceObjects.Where(obj => obj.InstanceId == id))
                        {
                            //Svc.Log.Info($"Found {id} {file} {layer.Name} {obj.Name} {obj.InstanceId} {obj.Transform.Translation.ToString()}");
                            objList.Add(obj);
                        }
                        //foreach (var obj in layer.InstanceObjects)
                        //{
                        //    if (obj.Object is VFXInstanceObject)
                        //    {
                        //        var vfx = (VFXInstanceObject)obj.Object;
                        //        Svc.Log.Info($"Found {id} {file} {layer.Name} {obj.Name} {vfx.AssetPath}");
                        //    }
                        //}
                    }
                }
            }

            //{
            //    var lvb = Svc.Data.GetFile<LvbFile>(bg + ".lvb");
            //    if (lvb != null)
            //    {
            //        foreach (var layer in lvb.Layers)
            //        {
            //            foreach (var obj in layer.InstanceObjects.Where(obj => obj.InstanceId == id))
            //            {
            //                Svc.Log.Info($"Found {id} {file} {layer.Name} {obj.Name} {obj.InstanceId} {obj.Transform.Translation.ToString()}");
            //                objList.Add(obj);
            //            }
            //        }
            //    }
            //}

            IdToInstanceObjectCache.Add(id, objList);
            objects = objList;
            if (objList.Count == 0)
            {
                Svc.Log.Info("Not found");
            }
        }

        return objects;
    }

    private static Dictionary<string, SgbFile> sgbCache = new();

    private static SgbFile GetSharedGroup(string sgbPath)
    {
        if (!sgbCache.TryGetValue(sgbPath, out var sgb))
        {
            sgb = Svc.Data.GetFile<SgbFile>(sgbPath);
            if (sgb != null)
            {
                //Svc.Log.Info($"{sgb.ChunkHeader.}");
                var ret = sgb.ChunkHeader;
                Svc.Log.Info($"{sgbPath}");
                Svc.Log.Info($"Unknown10 {ret.Unknown10:X8}");
                Svc.Log.Info($"Unknown14 {ret.Unknown14:X8}");
                Svc.Log.Info($"Unknown18 {ret.Unknown18:X8}");
                Svc.Log.Info($"Unknown1C {ret.Unknown1C:X8}");
                Svc.Log.Info($"Unknown28 {ret.Unknown28:X8}");
            }
            sgbCache.Add(sgbPath, sgb);
        }
        return sgb;
    }
}
