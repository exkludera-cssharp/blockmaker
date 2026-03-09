using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2TraceRay.Class;
using CS2TraceRay.Enum;
using CS2TraceRay.Struct;
using FixVectorLeak;
using System.Drawing;

public partial class Lights
{
    public class Config_Light
    {
        public string Title { get; set; }
        public string RGB { get; set; }

        public Config_Light(string title, string rgb)
        {
            Title = title;
            RGB = rgb;
        }
    }

    public class Data
    {
        public Data
        (
            COmniLight light,
            string color = "White",
            string style = "None",
            string brightness = "1",
            string distance = "1000"
        )
        {
            Entity = light;
            Color = color;
            Style = style;
            Brightness = brightness;
            Distance = distance;
        }

        public COmniLight Entity;
        public string Color { get; set; }
        public string Style { get; set; }
        public string Brightness { get; set; }
        public string Distance { get; set; }
    }

    public class SaveData
    {
        public string Color { get; set; } = "";
        public string Style { get; set; } = "";
        public string Brightness { get; set; } = "";
        public string Distance { get; set; } = "";
        public VectorUtils.VectorDTO Position { get; set; } = new();
        public VectorUtils.QAngleDTO Rotation { get; set; } = new();
    }

    public static Dictionary<CBaseProp, Data> Entities = new();
    public static void Create(CCSPlayerController player)
    {
        var BuilderData = Building.Builders[player.Slot];

        CGameTrace? trace = TraceRay.TraceShape(player.GetEyePosition()!, player.PlayerPawn.Value?.EyeAngles!, TraceMask.MaskShot, player);
        if (trace == null || !trace.HasValue || trace.Value.Position.Length() == 0)
        {
            Utils.PrintToChat(player, $"{ChatColors.Red}Could not find a valid location to create light");
            return;
        }

        var endPos = trace.Value.Position;

        CreateEntity(BuilderData.LightColor, BuilderData.LightStyle, BuilderData.LightBrightness, BuilderData.LightDistance, new(endPos.X, endPos.Y, endPos.Z), null);
        Utils.PrintToChat(player, $"Created Light -" +
            $" color: {ChatColors.White}{BuilderData.LightColor}{ChatColors.Grey}," +
            $" style: {ChatColors.White}{BuilderData.LightStyle}{ChatColors.Grey}," +
            $" brightness: {ChatColors.White}{BuilderData.LightBrightness}{ChatColors.Grey}," +
            $" distance: {ChatColors.White}{BuilderData.LightDistance}"
        );
    }

    public static void CreateEntity(string color = "White", string style = "None", string brightness = "5", string distance = "1000", Vector_t? position = null, QAngle_t? rotation = null)
    {
        var light = Utilities.CreateEntityByName<COmniLight>("light_omni2");
        if (light != null && light.IsValid && light.Entity != null)
        {
            light.Entity.Name = "blockmaker_light";
            light.Enabled = true;
            light.DirectLight = 3;
            light.OuterAngle = 360f;
            light.ColorMode = 0;
            light.Shape = 0;

            light.LightStyleString = style;
            light.Color = Utils.GetColor(color);
            light.Brightness = float.Parse(brightness);
            light.Range = float.Parse(distance);

            light.Teleport(position, rotation);
            light.DispatchSpawn();

            var entity = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
            if (entity != null && entity.IsValid && entity.Entity != null)
            {
                entity.Entity.Name = "blockmaker_light_entity";
                entity.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);

                if (Plugin.Instance.Config.Settings.Lights.HideModel && !Building.BuildMode)
                    entity.Render = Color.Transparent;

                entity.SetModel(Plugin.Instance.Config.Settings.Lights.Model);
                entity.Teleport(position, rotation);
                entity.DispatchSpawn();
                entity.AcceptInput("DisableMotion");

                light.AcceptInput("FollowEntity", entity, light, "!activator");

                Entities[entity] = new(light, color, style, brightness, distance);
            }
        }
    }

    public static bool Delete(CCSPlayerController player, bool message = true, bool replace = false)
    {
        var BuilderData = Building.Builders[player.Slot];

        var entity = player.GetBlockAim();

        if (entity != null && Entities.TryGetValue(entity, out var light))
        {
            if (replace)
                CreateEntity(BuilderData.LightColor, BuilderData.LightStyle, BuilderData.LightBrightness, BuilderData.LightDistance, entity.AbsOrigin?.ToVector_t(), entity.AbsRotation?.ToQAngle_t());

            light.Entity.Remove();
            Entities.Remove(entity);
            entity.Remove();

            if (message)
                Utils.PrintToChat(player, $"Deleted Light -" +
                    $" color: {ChatColors.White}{light.Color}{ChatColors.Grey}," +
                    $" style: {ChatColors.White}{light.Style}{ChatColors.Grey}," +
                    $" brightness: {ChatColors.White}{light.Brightness}{ChatColors.Grey}," +
                    $" distance: {ChatColors.White}{light.Distance}"
                );

            return true;
        }
        else
        {
            if (message)
                Utils.PrintToChat(player, $"{ChatColors.Red}Could not find a light to delete");

            return false;
        }
    }

    public static void Settings(CCSPlayerController player, string type, string input)
    {
        var data = Building.Builders[player.Slot];

        switch (type)
        {
            case "LightBrightness":
                data.LightBrightness = input;
                Utils.PrintToChat(player, $"Light Brightness Value: {ChatColors.White}{input}");
                Delete(player, false, true);
                break;
            case "LightDistance":
                data.LightDistance = input;
                Utils.PrintToChat(player, $"Light Distance Value: {ChatColors.White}{input}");
                Delete(player, false, true);
                break;
            default:
                Utils.PrintToChat(player, $"{ChatColors.Red}Unknown property type: {type}");
                break;
        }

        data.ChatInput = "";
    }

    public static readonly Dictionary<string, string> Styles = new()
    {
        ["None"] = "None",
        ["candle_1"] = "0",
        ["candle_2"] = "1",
        ["candle_3"] = "2",
        ["event_test"] = "3",
        ["fast_strobe"] = "4",
        ["flicker_1"] = "5",
        ["flicker_2"] = "6",
        ["fluorescent_flicker"] = "7",
        ["gentle_pulse"] = "8",
        ["slow_pulse_nofade"] = "9",
        ["slow_strobe"] = "10",
        ["slow_strong_pulse"] = "11"
    };
}
