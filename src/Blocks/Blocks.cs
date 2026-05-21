using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;
using RayTraceAPI;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class Blocks
{
    public class Data
    {
        public Data
        (
            CBaseProp block,
            string type,
            bool pole,
            string size = "Normal",
            string color = "None",
            string transparency = "100%",
            string team = "Both",
            string effect = "None",
            Property properties = null!

        )
        {
            Entity = block;
            Type = type;
            Pole = pole;
            Size = size;
            Team = team;
            Color = color;
            Transparency = transparency;
            Effect = effect;
            Properties = properties;
        }

        public CBaseProp Entity;
        public string Type { get; set; }
        public bool Pole { get; set; }
        public string Size { get; set; }
        public string Team { get; set; }
        public string Color { get; set; }
        public string Transparency { get; set; }
        public string Effect { get; set; }
        public Property Properties { get; set; }
    }

    public class SaveData
    {
        public string Type { get; set; } = "";
        public bool Pole { get; set; } = false;
        public string Size { get; set; } = "";
        public string Team { get; set; } = "";
        public string Color { get; set; } = "";
        public string Transparency { get; set; } = "";
        public string Effect { get; set; } = "";
        public Property Properties { get; set; } = new();
        public VectorUtils.VectorDTO Position { get; set; } = new();
        public VectorUtils.QAngleDTO Rotation { get; set; } = new();
    }

    public static void Create(CCSPlayerController player)
    {
        var rayTrace = Plugin.RayTraceInterface.Get();
        if (rayTrace == null) return;

        TraceOptions options = new();

        var pawn = player.PlayerPawn.Value!;
        if (pawn == null || pawn.EyeAngles == null) return;

        Vector? eyePosition = new(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z);
        if (eyePosition == null) return;

        if (rayTrace.TraceShape(eyePosition, pawn.EyeAngles, pawn, options, out TraceResult result) && !result.DidHit)
        {
            Utils.PrintToChat(player, $"{ChatColors.Red}Could not find a valid location to create block");
            return;
        }

        var endPos = result.EndPos;
        var BuilderData = Building.Builders[player.Slot];

        try
        {
            CreateBlock(
                player,
                BuilderData.BlockType,
                BuilderData.BlockPole,
                BuilderData.BlockSize,
                new(endPos.X, endPos.Y, endPos.Z),
                new(),
                BuilderData.BlockColor,
                BuilderData.BlockTransparency,
                BuilderData.BlockTeam,
                BuilderData.BlockEffect?.Particle ?? "None"
            );

            if (config.Sounds.Building.Enabled)
                player.EmitSound(config.Sounds.Building.Create);

            Utils.PrintToChat(player, $"Created -" +
                $" type: {ChatColors.White}{BuilderData.BlockType}{ChatColors.Grey}," +
                $" size: {ChatColors.White}{BuilderData.BlockSize}{ChatColors.Grey}," +
                $" color: {ChatColors.White}{BuilderData.BlockColor}{ChatColors.Grey}," +
                $" team: {ChatColors.White}{BuilderData.BlockTeam}{ChatColors.Grey}," +
                $" transparency: {ChatColors.White}{BuilderData.BlockTransparency},"
            );
        }
        catch
        {
            Utils.PrintToChat(player, $"{ChatColors.Red}Failed to create block");
            return;
        }
    }

    public static Dictionary<CBaseEntity, Data> Entities = new();
    public static void CreateBlock(
        CCSPlayerController? player,
        string type,
        bool pole,
        string size,
        Vector position,
        QAngle rotation,
        string color = "None",
        string transparency = "100%",
        string team = "Both",
        string effect = "None",
        Property? properties = null
    )
    {
        var block = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");

        if (block != null && block.IsValid && block.Entity != null)
        {
            block.Entity.Name = "blockmaker_" + type;
            block.EnableUseOutput = true;
            block.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            block.ShadowStrength = config.Settings.Blocks.DisableShadows ? 0.0f : 1.0f;

            string model = Utils.GetModelFromSelectedBlock(type, pole);
            block.SetModel(model);

            block.Teleport(position, rotation);
            block.DispatchSpawn();
            block.AcceptInput("SetScale", block, block, Utils.GetSize(size).ToString());
            block.AcceptInput("DisableMotion");

            var clr = Utils.GetColor(color);
            int alpha = Utils.GetAlpha(transparency);
            block.Render = Color.FromArgb(alpha, clr.R, clr.G, clr.B);
            Utilities.SetStateChanged(block, "CBaseModelEntity", "m_clrRender");

            if (!string.IsNullOrEmpty(effect) && effect != "None")
                CreateParticle(block, effect, size);

            if (properties == null)
            {
                if (Properties.BlockProperties.TryGetValue(type.Split('.')[0], out var defaultProperties))
                {
                    properties = new Property
                    {
                        Cooldown = defaultProperties.Cooldown,
                        Value = defaultProperties.Value,
                        Duration = defaultProperties.Duration,
                        OnTop = defaultProperties.OnTop,
                        Locked = defaultProperties.Locked,
                    };
                    if (player != null) properties.Builder = $"{player.PlayerName} - {DateTime.Now:dd/MMMM/yyyy HH:mm}";
                }
                else properties = new Property();
            }
            else
            {
                properties = new Property
                {
                    Cooldown = properties.Cooldown,
                    Value = properties.Value,
                    Duration = properties.Duration,
                    OnTop = properties.OnTop,
                    Locked = properties.Locked,
                    Builder = properties.Builder,
                };
            }

            Entities[block] = new Data(block, type, pole, size, color, transparency, team, effect, properties);
        }
    }

    public static void Delete(CCSPlayerController player, bool all = false)
    {
        if (all)
        {
            Utils.RemoveEntities();

            Entities.Clear();

            Teleports.Entities.Clear();
            Teleports.isNext.Clear();

            Lights.Entities.Clear();
        }
        else
        {
            var entity = player.GetBlockAim();

            if (entity == null)
            {
                Utils.PrintToChat(player, $"{ChatColors.Red}Could not find a block to delete");
                return;
            }

            if (Entities.TryGetValue(entity, out var block))
            {
                if (Utils.BlockLocked(player, block))
                    return;

                block.Entity.Remove();
                Entities.Remove(block.Entity);

                if (config.Sounds.Building.Enabled)
                    player.EmitSound(config.Sounds.Building.Delete);

                Utils.PrintToChat(player, $"Deleted -" +
                    $" type: {ChatColors.White}{block.Type}{ChatColors.Grey}," +
                    $" size: {ChatColors.White}{block.Size}{ChatColors.Grey}," +
                    $" color: {ChatColors.White}{block.Color}{ChatColors.Grey}," +
                    $" team: {ChatColors.White}{block.Team}{ChatColors.Grey}," +
                    $" transparency: {ChatColors.White}{block.Transparency},"
                );

                return;
            }
        }
    }

    public class Property
    {
        public float Cooldown { get; set; } = 0;
        public float Value { get; set; } = 0;
        public float Duration { get; set; } = 0;
        public bool OnTop { get; set; } = true;
        public bool Locked { get; set; } = false;
        public string Builder { get; set; } = "";
    }

    public static class Properties
    {
        public static readonly Dictionary<string, Property> BlockDefaultProperties = new()
        {
            { Models.Data.Bhop.Title, new Property { Duration = 0.25f, Cooldown = 1.0f } },
            { Models.Data.Health.Title, new Property { Value = 4.0f, Cooldown = 0.75f } },
            { Models.Data.Grenade.Title, new Property { Cooldown = 60.0f } },
            { Models.Data.Gravity.Title, new Property { Duration = 4.0f, Value = 0.4f, Cooldown = 5.0f } },
            { Models.Data.Frost.Title, new Property { Cooldown = 60.0f } },
            { Models.Data.Flash.Title, new Property { Cooldown = 60.0f } },
            { Models.Data.Fire.Title, new Property { Duration = 5.0f, Value = 8.0f, Cooldown = 5.0f } },
            { Models.Data.Delay.Title, new Property { Duration = 1.0f, Cooldown = 1.5f } },
            { Models.Data.Damage.Title, new Property { Value = 8.0f, Cooldown = 0.75f } },
            { Models.Data.Stealth.Title, new Property { Duration = 7.5f, Cooldown = 60.0f } },
            { Models.Data.Speed.Title, new Property { Duration = 3.0f, Value = 2.0f, Cooldown = 60.0f } },
            { Models.Data.SpeedBoost.Title, new Property { Duration = 300.0f, Value = 650.0f } },
            { Models.Data.Camouflage.Title, new Property { Duration = 10.0f, Cooldown = 60.0f } },
            { Models.Data.Slap.Title, new Property { Value = 2.0f } },
            { Models.Data.Random.Title, new Property { Cooldown = 60f } },
            { Models.Data.Invincibility.Title, new Property { Duration = 5.0f, Cooldown = 60.0f } },
            { Models.Data.Trampoline.Title, new Property { Value = 500.0f } },
            { Models.Data.Death.Title, new Property { OnTop = false } },
            { Models.Data.Honey.Title, new Property { Value = 0.3f } },
            { Models.Data.Platform.Title, new Property() },
            { Models.Data.NoFallDmg.Title, new Property { OnTop = false } },
            { Models.Data.Ice.Title, new Property() },
            { Models.Data.Nuke.Title, new Property() },
            { Models.Data.Glass.Title, new Property() },
            { Models.Data.Pistol.Title, new Property{  Value = 1f, Cooldown = 999f } },
            { Models.Data.Rifle.Title, new Property{ Value = 1f, Cooldown = 999f } },
            { Models.Data.Sniper.Title, new Property{ Value = 1f, Cooldown = 999f } },
            { Models.Data.ShotgunHeavy.Title, new Property{ Value = 1f, Cooldown = 999f } },
            { Models.Data.SMG.Title, new Property{ Value = 1f, Cooldown = 999f } },
            { Models.Data.Barrier.Title, new Property{ Duration = 0.01f, Value = 0f, Cooldown = 2.0f } },
        };

        public static Dictionary<string, Property> BlockProperties { get; set; } = new();

        public static void Load()
        {
            string directoryPath = Path.GetDirectoryName(Plugin.Instance.Config.GetConfigPath())!;
            string correctedPath = directoryPath.Replace("/BlockMaker.json", "");

            var propertiesPath = Path.Combine(correctedPath, "default_properties.json");

            if (!string.IsNullOrEmpty(propertiesPath))
            {
                if (!File.Exists(propertiesPath))
                {
                    using (FileStream fs = File.Create(propertiesPath))
                        fs.Close();

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    string jsonContent = JsonSerializer.Serialize(BlockDefaultProperties, options);

                    File.WriteAllText(propertiesPath, jsonContent);
                }

                if (File.Exists(propertiesPath))
                {
                    string jsonContent = File.ReadAllText(propertiesPath);
                    BlockProperties = JsonSerializer.Deserialize<Dictionary<string, Property>>(jsonContent) ?? new();
                }
            }
        }
    }

    public class Effect
    {
        public string Title { get; set; } = "None";
        public string Particle { get; set; } = "";

        public Effect(string title, string particle)
        {
            Title = title;
            Particle = particle;
        }
    }

    private static void CreateParticle(CBaseProp block, string effect, string size)
    {
        var particle = Utilities.CreateEntityByName<CEnvParticleGlow>("env_particle_glow");

        if (particle != null && particle.IsValid && particle.Entity != null)
        {
            particle.Entity.Name = "blockmaker_effect";
            particle.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            particle.StartActive = true;

            particle.EffectName = effect;
            particle.SetModel(block.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
            particle.AcceptInput("FollowEntity", block, particle, "!activator");

            particle.DispatchSpawn();
        }
    }
}
