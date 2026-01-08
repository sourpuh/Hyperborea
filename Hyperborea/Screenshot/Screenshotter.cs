using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Hyperborea.Guides;
using ImGuiScene;
using Lumina.Data.Parsing;
using System.IO;
using System.Reflection;
using static FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Resource.Delegates;

namespace Hyperborea.Screenshot;

//https://github.com/Caraxi/SimpleTweaksPlugin/blob/15a3ac835ece1f54e41af24d133aba9fef476e30/Tweaks/HighResScreenshots.cs#L22
//https://github.com/Caraxi/SimpleTweaksPlugin/blob/15a3ac835ece1f54e41af24d133aba9fef476e30/Tweaks/ScreenshotFileName.cs#L16
public unsafe class Screenshotter : IDisposable
{
    private nint copyrightShaderAddress;

    private delegate byte IsInputIDClickedDelegate(nint a1, int a2);
    private Hook<IsInputIDClickedDelegate> isInputIDClickedHook;

    private delegate char* GetPathDelegate(char* destination, byte* p);
    private Hook<GetPathDelegate> getPathHook;
    public Screenshotter()
    {
        if (!Svc.SigScanner.TryScanText("48 8B 57 ?? 45 33 C9 ?? ?? ?? 45 33 C0", out copyrightShaderAddress))
        {
            copyrightShaderAddress = 0;
        }

        isInputIDClickedHook ??= Svc.Hook.HookFromSignature<IsInputIDClickedDelegate>("E9 ?? ?? ?? ?? 83 7F ?? ?? 0F 8F ?? ?? ?? ?? BA ?? ?? ?? ?? 48 8B CB", IsInputIDClickedDetour);
        isInputIDClickedHook?.Enable();

        getPathHook ??= Svc.Hook.HookFromSignature<GetPathDelegate>("E8 ?? ?? ?? ?? 48 8B C7 49 8D 9E", GetPathDetour);
        getPathHook?.Enable();
    }

    public void Dispose()
    {
        isInputIDClickedHook.Dispose();
        getPathHook.Dispose();
    }

    public void TakeScreenshot(uint width, uint height, string name, Guide guide)
    {
        // TODO
        this.width = Math.Max(width, height);
        this.height = this.width;
        this.name = name;
        this.guide = guide;
        takeScreenshot = true;
    }

    private bool takeScreenshot;
    private uint width, height;
    private string name;
    private Guide guide;
    private bool shouldPress;
    private uint oldWidth;
    private uint oldHeight;
    private bool isRunning;
    const int ScreenshotButton = (int)InputId.KEY_SCREENSHOT;

    public bool originalUiVisibility;
    byte[] originalCopyrightBytes;
    // IsInputIDClicked is called from Client::UI::UIInputModule.CheckScreenshotState, which is polled
    // We change the res when the button is pressed and tell it to take a screenshot the next time it is polled
    private byte IsInputIDClickedDetour(nint uiInputData, int key)
    {
        var orig = isInputIDClickedHook.Original(uiInputData, key);
        if (AgentModule.Instance()->GetAgentByInternalId(AgentId.Configkey)->IsAgentActive()) return orig;

        if (takeScreenshot)
        {
            orig = 1;
            takeScreenshot = false;
        }

        if (orig == 1 && key == ScreenshotButton && !shouldPress && !isRunning)
        {
            isRunning = true;
            var device = Device.Instance();
            oldWidth = device->Width;
            oldHeight = device->Height;

            var w = Math.Clamp(width, 1280, ushort.MaxValue);
            var h = Math.Clamp(height, 720, ushort.MaxValue);
            if (device->Width != w || device->Height != h)
            {
                device->NewWidth = w;
                device->NewHeight = h;
                device->RequestResolutionChange = 1;
            }

            var raptureAtkModule = Framework.Instance()->GetUIModule()->GetRaptureAtkModule();
            originalUiVisibility = !raptureAtkModule->RaptureAtkUnitManager.Flags.HasFlag(AtkUnitManagerFlags.UiHidden);
            if (originalUiVisibility)
            {
                raptureAtkModule->SetUiVisibility(false);
            }

            Svc.Framework.RunOnTick(() => {
                SetExclusiveDraw(() => { });
                shouldPress = true;
            }, delay: TimeSpan.FromSeconds(1));

            return 0;
        }

        if (key == ScreenshotButton && shouldPress)
        {
            shouldPress = false;

            if (copyrightShaderAddress != 0 && originalCopyrightBytes == null)
            {
                originalCopyrightBytes = ReplaceRaw(copyrightShaderAddress, new byte[] { 0xEB, 0x54 });
            }

            // Reset the res back to normal after the screenshot is taken
            Svc.Framework.RunOnTick(() => {
                FreeExclusiveDraw();

                var raptureAtkModule = Framework.Instance()->GetUIModule()->GetRaptureAtkModule();
                if (originalUiVisibility && raptureAtkModule->RaptureAtkUnitManager.Flags.HasFlag(AtkUnitManagerFlags.UiHidden))
                {
                    raptureAtkModule->SetUiVisibility(true);
                }

                var device = Device.Instance();
                if (device->Width != oldWidth || device->Height != oldHeight)
                {
                    device->NewWidth = oldWidth;
                    device->NewHeight = oldHeight;
                    device->RequestResolutionChange = 1;
                }
            }, delayTicks: 1);

            Svc.Framework.RunOnTick(() => {
                if (originalCopyrightBytes != null)
                {
                    ReplaceRaw(copyrightShaderAddress, originalCopyrightBytes);
                    originalCopyrightBytes = null;
                }
                isRunning = false;
            }, delayTicks: 60);

            Svc.Framework.RunOnTick(async () =>
            {
                ImageMasker.ApplyMaskAsync(
                    $"~\\Documents\\My Games\\FINAL FANTASY XIV - A Realm Reborn\\screenshots\\maps\\{name}.png",
                    guide,
                    $"~\\Documents\\My Games\\FINAL FANTASY XIV - A Realm Reborn\\screenshots\\maps\\{name}_masked.png");
            }, delayTicks: 600);

            return 1;
        }

        if (isRunning && key == ScreenshotButton) return 0;
        return orig;
    }

    private static byte[] ReplaceRaw(nint address, byte[] data)
    {
        var originalBytes = MemoryHelper.ReadRaw(address, data.Length);
        var oldProtection = MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, oldProtection);
        return originalBytes;
    }

    //private static RawDX11Scene.BuildUIDelegate originalHandler;
    internal static bool SetExclusiveDraw(Action action)
    {
        //// Possibly the most cursed shit I've ever done.
        //if (originalHandler != null) return false;
        //try
        //{
        //    var dalamudAssembly = Svc.PluginInterface.GetType().Assembly;
        //    var service1T = dalamudAssembly.GetType("Dalamud.Service`1");
        //    var interfaceManagerT = dalamudAssembly.GetType("Dalamud.Interface.Internal.InterfaceManager");
        //    if (service1T == null) return false;
        //    if (interfaceManagerT == null) return false;
        //    var serviceInterfaceManager = service1T.MakeGenericType(interfaceManagerT);
        //    var getter = serviceInterfaceManager.GetMethod("Get", BindingFlags.Static | BindingFlags.Public);
        //    if (getter == null) return false;
        //    var interfaceManager = getter.Invoke(null, null);
        //    if (interfaceManager == null) return false;
        //    var ef = interfaceManagerT.GetField("Draw", BindingFlags.Instance | BindingFlags.NonPublic);
        //    if (ef == null) return false;
        //    var handler = (RawDX11Scene.BuildUIDelegate)ef.GetValue(interfaceManager);
        //    if (handler == null) return false;
        //    originalHandler = handler;
        //    ef.SetValue(interfaceManager, new RawDX11Scene.BuildUIDelegate(action));
        //    return true;
        //}
        //catch (Exception ex)
        //{
        //    Svc.Log.Fatal(ex.ToString());
        //    Svc.Log.Fatal("This could be messy...");
        //}

        return false;
    }

    internal static bool FreeExclusiveDraw()
    {
        //if (originalHandler == null) return true;
        //try
        //{
        //    var dalamudAssembly = Svc.PluginInterface.GetType().Assembly;
        //    var service1T = dalamudAssembly.GetType("Dalamud.Service`1");
        //    var interfaceManagerT = dalamudAssembly.GetType("Dalamud.Interface.Internal.InterfaceManager");
        //    if (service1T == null) return false;
        //    if (interfaceManagerT == null) return false;
        //    var serviceInterfaceManager = service1T.MakeGenericType(interfaceManagerT);
        //    var getter = serviceInterfaceManager.GetMethod("Get", BindingFlags.Static | BindingFlags.Public);
        //    if (getter == null) return false;
        //    var interfaceManager = getter.Invoke(null, null);
        //    if (interfaceManager == null) return false;
        //    var ef = interfaceManagerT.GetField("Draw", BindingFlags.Instance | BindingFlags.NonPublic);
        //    if (ef == null) return false;
        //    ef.SetValue(interfaceManager, originalHandler);
        //    originalHandler = null;
        //    return true;
        //}
        //catch (Exception ex)
        //{
        //    Svc.Log.Fatal(ex.ToString());
        //    Svc.Log.Fatal("This could be messy...");
        //}

        return false;
    }

    private char* GetPathDetour(char* destination, byte* p)
    {
        try
        {
            var pStr = MemoryHelper.ReadString((nint)p, 64);
            if (pStr.StartsWith("ffxiv_") && (pStr.EndsWith(".png") || pStr.EndsWith(".jpg") || pStr.EndsWith(".bmp")))
            {
                var newName = $"maps/{name}.{pStr.Split('.').Last()}";

                var bytes = Encoding.UTF8.GetBytes(newName);
                var b = stackalloc byte[bytes.Length + 1];
                for (var byteIndex = 0; byteIndex < bytes.Length; byteIndex++) b[byteIndex] = bytes[byteIndex];
                b[bytes.Length] = 0;
                var o = getPathHook.Original(destination, b);
                var str = string.Empty;
                var i = 0;
                while (o[i] != '\0')
                {
                    str += o[i++];
                }

                var fileInfo = new FileInfo(str);
                if (fileInfo.Exists)
                {
                    //Svc.Chat.PrintError($"Screenshot Already Exists: {str}");
                    fileInfo.Delete();
                }

                fileInfo.Directory?.Create();

                return o;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex.ToString());
        }

        return getPathHook.Original(destination, p);
    }
}
