using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using FixVectorLeak;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

public static class Events
{
    private static Plugin Instance = Plugin.Instance;
    private static Config Config = Instance.Config;

    public static void Register()
    {
        Instance.RegisterListener<Listeners.OnTick>(Building.OnTick);
        Instance.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        Instance.RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        Instance.RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        Instance.RegisterListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);

        Instance.RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFull);
        Instance.RegisterEventHandler<EventRoundStart>(EventRoundStart);
        Instance.RegisterEventHandler<EventRoundEnd>(EventRoundEnd);
        Instance.RegisterEventHandler<EventPlayerDeath>(EventPlayerDeath);

        Instance.AddCommandListener("say", OnCommandSay, HookMode.Pre);
        Instance.AddCommandListener("say_team", OnCommandSay, HookMode.Pre);

        Transmit.Load();

        TouchHooks.Load();
    }

    public static void Deregister()
    {
        Instance.RemoveListener<Listeners.OnTick>(Building.OnTick);
        Instance.RemoveListener<Listeners.OnMapStart>(OnMapStart);
        Instance.RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
        Instance.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        Instance.RemoveListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);

        Instance.DeregisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFull);
        Instance.DeregisterEventHandler<EventRoundStart>(EventRoundStart);
        Instance.DeregisterEventHandler<EventRoundEnd>(EventRoundEnd);
        Instance.DeregisterEventHandler<EventPlayerDeath>(EventPlayerDeath);

        Instance.RemoveCommandListener("say", OnCommandSay, HookMode.Pre);
        Instance.RemoveCommandListener("say_team", OnCommandSay, HookMode.Pre);

        Transmit.Unload();

        TouchHooks.Unload();
    }

    public static Timer? AutoSaveTimer;
    private static void OnMapStart(string mapname)
    {
        Files.mapsFolder = Path.Combine(Instance.ModuleDirectory, "maps", Server.MapName);
        Directory.CreateDirectory(Files.mapsFolder);

        if (Config.Settings.Building.AutoSave.Enable)
        {
            AutoSaveTimer?.Kill();

            AutoSaveTimer = Instance.AddTimer(Config.Settings.Building.AutoSave.Timer, () =>
            {
                if (!Building.BuildMode)
                    return;

                Files.EntitiesData.Save(true);
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        if (Config.Settings.Building.BuildMode.Config)
        {
            List<string> commands =
            [
                "sv_cheats 1", "mp_join_grace_time 3600", "mp_timelimit 60",
                "mp_roundtime 60", "mp_freezetime 0", "mp_warmuptime 0", "mp_maxrounds 99"
            ];

            foreach (string command in commands)
                Server.ExecuteCommand(command);
        }
    }

    private static void OnMapEnd()
    {
        Utils.Clear();
    }

    private static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<string> resources =
        [
            Config.Sounds.SoundEvents,
            Config.Settings.Teleports.Entry.Model,
            Config.Settings.Teleports.Exit.Model,
            Config.Settings.Blocks.CamouflageT,
            Config.Settings.Blocks.CamouflageCT,
            Config.Settings.Blocks.FireParticle,
            Config.Settings.Lights.Model,
        ];

        foreach (var effect in Config.Settings.Blocks.Effects)
            resources.Add(effect.Particle);

        foreach (var model in Blocks.Models.Data.GetAllBlocks())
        {
            resources.Add(model.Block);
            resources.Add(model.Pole);
        }

        foreach (var resource in resources)
        {
            if (!string.IsNullOrEmpty(resource))
                manifest.AddResource(resource);
        }
    }

    private static HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || player.NotValid())
            return HookResult.Continue;

        if (Building.BuildMode)
        {
            Files.Builders.Load();

            if (Utils.HasPermission(player) || Files.Builders.steamids.Contains(player.SteamID.ToString()))
                Building.Builders[player.Slot] = new Building.BuilderData { BlockType = Blocks.Models.Data.Platform.Title };
        }

        return HookResult.Continue;
    }

    private static HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Utils.Clear();
        Files.EntitiesData.Load();

        return HookResult.Continue;
    }

    private static HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (Building.BuildMode && Config.Settings.Building.AutoSave.Enable)
            Files.EntitiesData.Save();

        return HookResult.Continue;
    }

    private static HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || player.NotValid())
            return HookResult.Continue;

        if (Blocks.PlayerCooldowns.TryGetValue(player.Slot, out var playerCooldowns))
            playerCooldowns.Clear();

        if (Blocks.CooldownsTimers.TryGetValue(player.Slot, out var playerTimers))
        {
            foreach (var timer in playerTimers)
                timer.Kill();

            playerTimers.Clear();
        }

        if (Blocks.HiddenPlayers.TryGetValue(player, out var hiddenPlayer))
            Blocks.HiddenPlayers.Remove(player);

        return HookResult.Continue;
    }

    private static HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || player.NotValid())
            return HookResult.Continue;

        if (Building.Builders.TryGetValue(player.Slot, out var pData))
        {
            var type = pData.ChatInput;

            if (!string.IsNullOrEmpty(type))
            {
                var input = info.ArgString.Replace("\"", "");

                if (!float.TryParse(input, out float number) || (number <= 0 && type != "Snap"))
                {
                    Utils.PrintToChat(player, $"{ChatColors.Red}Invalid input value: {ChatColors.White}{input}");
                    return HookResult.Handled;
                }

                switch (type)
                {
                    case "Grid":
                        pData.GridValue = number;
                        Utils.PrintToChat(player, $"Grid Value: {ChatColors.White}{number}");
                        break;
                    case "Snap":
                        pData.SnapValue = number;
                        Utils.PrintToChat(player, $"Snap Value: {ChatColors.White}{number}");
                        break;
                    case "Rotation":
                        pData.RotationValue = number;
                        Utils.PrintToChat(player, $"Rotation Value: {ChatColors.White}{number}");
                        break;
                    case "Position":
                        pData.PositionValue = number;
                        Utils.PrintToChat(player, $"Position Value: {ChatColors.White}{number}");
                        break;
                    case "LightBrightness":
                        Commands.LightSettings(player, type, input);
                        break;
                    case "LightDistance":
                        Commands.LightSettings(player, type, input);
                        break;
                    case "Reset":
                    default:
                        Commands.Properties(player, type, input);
                        break;
                }

                pData.ChatInput = "";

                return HookResult.Handled;
            }
        }
    
        return HookResult.Continue;
    }

    private static HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
    {
        if (pawn.DesignerName == "player" && info.Attacker.Value?.DesignerName == "player")
            return HookResult.Continue;

        var blockModels = Blocks.Models.Data;
        string NoFallDmg = blockModels.NoFallDmg.Title;
        string Trampoline = blockModels.Trampoline.Title;

        foreach (var blocktarget in Blocks.Entities.Where(x => x.Value.Type.Equals(NoFallDmg) || x.Value.Type.Equals(Trampoline)))
        {
            var block = blocktarget.Key;

            if (pawn.AbsOrigin == null || block.AbsOrigin == null)
                return HookResult.Continue;

            Vector_t playerMaxs = pawn.Collision.Maxs.ToVector_t() * 2;
            Vector_t blockMaxs = block.Collision!.Maxs.ToVector_t() * Utils.GetSize(blocktarget.Value.Size) * 2;

            if (VectorUtils.IsWithinBounds(block.AbsOrigin.ToVector_t(), pawn.AbsOrigin.ToVector_t(), blockMaxs, playerMaxs))
            {
                if (blocktarget.Value.Properties.OnTop)
                {
                    Vector_t blockOrigin = block.AbsOrigin!.ToVector_t();
                    Vector_t pawnOrigin = pawn.AbsOrigin!.ToVector_t();
                    QAngle_t blockRotation = block.AbsRotation!.ToQAngle_t();

                    if (VectorUtils.IsTopOnly(blockOrigin, pawnOrigin, blockMaxs, playerMaxs, blockRotation))
                        return HookResult.Handled;
                }
                else return HookResult.Handled;
            }
        }

        return HookResult.Continue;
    }
}