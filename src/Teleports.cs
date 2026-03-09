using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using FixVectorLeak;
using Microsoft.Extensions.Logging;

public static class Teleports
{
    public class Data
    {
        public Data
        (
            CBaseProp teleport,
            string name
        )
        {
            Entity = teleport;
            Name = name;
        }

        public CBaseProp Entity;
        public string Name { get; set; }
    }

    public class SaveData
    {
        public string Name { get; set; } = "";
        public VectorUtils.VectorDTO Position { get; set; } = new();
        public VectorUtils.QAngleDTO Rotation { get; set; } = new();
    }

    public class Pair
    {
        public Data Entry { get; set; }
        public Data Exit { get; set; }

        public Pair(Data entry, Data exit)
        {
            Entry = entry;
            Exit = exit;
        }
    }

    public class PairSaveData
    {
        public SaveData Entry { get; set; } = new SaveData();
        public SaveData Exit { get; set; } = new SaveData();
    }

    private static Plugin Instance = Plugin.Instance;
    private static Config Config = Instance.Config;

    public static List<Pair> Entities = new List<Pair>();
    public static Dictionary<CCSPlayerController, bool> isNext = new();

    public static void Create(CCSPlayerController player)
    {
        var BuilderData = Building.Builders[player.Slot];
        var playerPawn = player.PlayerPawn.Value!;
        var position = new Vector_t(playerPawn.AbsOrigin!.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z + playerPawn.Collision.Maxs.Z / 2);
        var rotation = playerPawn.AbsRotation!.ToQAngle_t();

        if (!isNext.ContainsKey(player))
            isNext.Add(player, false);

        try
        {
            string Type = isNext[player] ? "Exit" : "Entry";
            var teleportData = CreateEntity(position, rotation, Type);

            if (teleportData != null)
            {
                Utils.PrintToChat(player, $"Created teleport ({Type})");

                if (isNext[player])
                {
                    var incompletePair = Entities.FirstOrDefault(p => p.Exit == null);

                    if (incompletePair != null)
                    {
                        incompletePair.Exit = teleportData;
                        Utils.PrintToChat(player, $"Paired teleports");
                    }
                    else
                    {
                        Entities.Add(new Pair(null!, teleportData));
                        Utils.PrintToChat(player, $"Pairing failed when creating a new exit teleport");
                    }
                }
                else Entities.Add(new Pair(teleportData, null!));

                isNext[player] = !isNext[player];
            }
            else Utils.PrintToChat(player, $"Failed to create {Type} teleport");

        }
        catch (Exception ex)
        {
            Instance.Logger.LogError($"Exception: {ex}");
        }
    }

    public static Data? CreateEntity(Vector_t position, QAngle_t rotation, string name)
    {
        var teleport = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");

        var config = Config.Settings.Teleports;

        var entryModel = config.Entry.Model;
        var exitModel = config.Exit.Model;

        var entryColor = config.Entry.Color;
        var exitColor = config.Exit.Color;

        if (teleport != null && teleport.IsValid && teleport.Entity != null)
        {
            teleport.Entity.Name = "blockmaker_Teleport_" + name;
            teleport.EnableUseOutput = true;

            teleport.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            teleport.ShadowStrength = Config.Settings.Blocks.DisableShadows ? 0.0f : 1.0f;
            teleport.Render = Utils.ParseColor(name == "Entry" ? entryColor : exitColor);

            teleport.SetModel(name == "Entry" ? entryModel : exitModel);
            teleport.Teleport(position, rotation);
            teleport.DispatchSpawn();
            teleport.AcceptInput("DisableMotion");

            // cant grab teleport with trigger group :skull:
            if (Building.BuildMode)
                teleport.CollisionRulesChanged(CollisionGroup.COLLISION_GROUP_WEAPON); //weapons wont go through during buildmode
            else teleport.CollisionRulesChanged(CollisionGroup.COLLISION_GROUP_TRIGGER);

            Data teleportData = new(teleport, name);

            return teleportData;
        }
        else
        {
            Utils.Log("(CreateTeleport) Failed to create teleport");
            return null;
        }
    }

    public static void Delete(CCSPlayerController player)
    {
        var entity = player.GetBlockAim();
        if (entity == null)
        {
            Utils.PrintToChat(player, $"{ChatColors.Red}Could not find a teleport to delete");
            return;
        }

        var teleports = Entities.First(pair => pair.Entry.Entity == entity || pair.Exit.Entity == entity);

        if (teleports != null)
        {
            if (teleports.Entry == null || teleports.Entry.Entity == null ||
                teleports.Exit == null || teleports.Exit.Entity == null)
            {
                Utils.PrintToChat(player, $"{ChatColors.Red}Could not delete unfinished teleport pair");
                return;
            }

            var entryEntity = teleports.Entry.Entity;
            if (entryEntity != null && entryEntity.IsValid)
                entryEntity.Remove();

            var exitEntity = teleports.Exit.Entity;
            if (exitEntity != null && exitEntity.IsValid)
                exitEntity.Remove();

            Entities.Remove(teleports);

            if (Instance.Config.Sounds.Building.Enabled)
                player.EmitSound(Instance.Config.Sounds.Building.Delete);

            Utils.PrintToChat(player, $"Deleted teleport pair");
        }
        else Utils.PrintToChat(player, $"{ChatColors.Red}Could not find a teleport to delete");
    }

    public static void Action(Pair teleport, CBaseEntity caller, CBaseEntity activator)
    {
        if (teleport.Entry?.Entity == null || teleport.Exit?.Entity == null || caller?.Entity == null)
            return;

        if (!caller.Entity.Name?.Contains("Entry", StringComparison.OrdinalIgnoreCase) ?? true || activator.Entity.Name.Contains("blockmaker", StringComparison.OrdinalIgnoreCase))
            return;

        caller.EmitSound(Config.Sounds.Blocks.Teleport);

        var exitEntity = teleport.Exit.Entity;
        var exitPosition = exitEntity.AbsOrigin?.ToVector_t();
        var exitVelocity = Config.Settings.Teleports.Velocity > 0
            ? new Vector_t(activator.AbsVelocity.X, activator.AbsVelocity.Y, Config.Settings.Teleports.Velocity)
            : activator.AbsVelocity.ToVector_t();

        if (activator.DesignerName == "player")
        {
            var pawn = activator.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid) return;

            var angles = Config.Settings.Teleports.ForceAngles
                ? exitEntity.AbsRotation?.ToQAngle_t() ?? pawn.EyeAngles.ToQAngle_t()
                : pawn.EyeAngles.ToQAngle_t();

            pawn.Teleport(exitPosition, angles, exitVelocity);
            exitEntity.EmitSound(Config.Sounds.Blocks.Teleport);
        }
        else
        {
            if (!Instance.Config.Settings.Teleports.AllowEntities)
                return;

            activator.Teleport(
                exitPosition,
                Config.Settings.Teleports.ForceAngles ? exitEntity.AbsRotation?.ToQAngle_t() : null,
                exitVelocity
            );
            exitEntity.EmitSound(Config.Sounds.Blocks.Teleport);
        }
    }
}
