using ECommons.EzIpcManager;
using Hyperborea.Gui;

namespace Hyperborea;
internal class IPCProvider
{

    internal IPCProvider()
    {
        EzIPC.Init(this);
    }

    [EzIPC] public void Load(uint territoryType, Vector3 pos) {
        if (P.Enable())
        {
            UI.a2 = (int)territoryType;
            UI.SpawnOverride = true;
            UI.Position = pos.ToPoint3();
            UI.Load();
        }
    }
}
