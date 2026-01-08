using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Hyperborea.Guides;
using Lumina.Excel.Sheets;
using System.Text.Json.Serialization;

namespace Hyperborea.Screenshot;

public struct MapParameters
{
    // Necessary
    // Territory ID
    // [
    // Nickname
    // Center
    // Weather
    // Effects
    // Arena Mask
    // Death Wall Size?
    // (Optional) BG Camera info
    //   Ortho, position
    // ]
    // Dynamic load
    // other IDs
    // map bg file
    // AABB

    public uint TerritoryId;
    public string NickName = "Default";
    public Vector3 Center = new(100, 0, 100);
    public uint Weather = 0;
    public List<MapEffect> MapEffects = [];
    public List<Guides.Guide> Mask = [];
    internal CompositeGuide Guide = new();

    [JsonIgnore]
    public AABB AABB => Guide.Bounds;
    [JsonIgnore]
    public float LongAxisLength => AABB.LongAxisLength;
    [JsonIgnore]
    public float OrthoHeight => AABB.OverheadSize.Y;

    [JsonIgnore]
    public string Name;
    [JsonIgnore]
    public string Filename => $"{TerritoryId}_{NickName}";
    [JsonIgnore]
    public string Bg;


    public MapParameters(uint TerritoryId)
    {
        this.TerritoryId = TerritoryId;
        Guide.Children = Mask;
    }

    public static MapParameters Init(IDataManager dataManager, uint TerritoryId)
    {
        var sheet = dataManager.GetExcelSheet<TerritoryType>();
        var row = sheet.GetRow(TerritoryId);

        var param = new MapParameters(TerritoryId);
        param.Name = row.GetName();
        param.Bg = row.Bg.ExtractText();
        return param;
    }

    public struct MapEffect
    {
        public uint Index;
        public ushort State;
        public ushort Param;
    }
}