using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using FixVectorLeak;
using System.Drawing;

using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

public partial class Blocks
{
    private static Plugin instance = Plugin.Instance;
    private static Config config = instance.Config;
    private static Config_Sounds.Sounds_Blocks sounds = instance.Config.Sounds.Blocks;
    private static Models blockModels = Models.Data;

    private static Dictionary<string, Action<CCSPlayerController, Data>> blockActions = null!;

    public static void LoadTitles()
    {
        blockActions = new()
        {
            { blockModels.Random.Title, Action_Random },
            { blockModels.Bhop.Title, Action_BhopDelay },
            { blockModels.Gravity.Title, Action_Gravity },
            { blockModels.Health.Title, Action_Health },
            { blockModels.Grenade.Title, Action_Nades },
            { blockModels.Frost.Title, Action_Nades },
            { blockModels.Flash.Title, Action_Nades },
            { blockModels.Fire.Title, Action_Fire },
            { blockModels.Delay.Title, Action_BhopDelay },
            { blockModels.Death.Title, Action_Death },
            { blockModels.Damage.Title, Action_Damage },
            { blockModels.Speed.Title, Action_Speed },
            { blockModels.SpeedBoost.Title, Action_SpeedBoost },
            { blockModels.Slap.Title, Action_Slap },
            { blockModels.Nuke.Title, Action_Nuke },
            { blockModels.Stealth.Title, Action_Stealth },
            { blockModels.Invincibility.Title, Action_Invincibility },
            { blockModels.Camouflage.Title, Action_Camouflage },
            { blockModels.Trampoline.Title, Action_Trampoline },
            { blockModels.Honey.Title, Action_Honey },
            { blockModels.Pistol.Title, Action_Weapons },
            { blockModels.Rifle.Title, Action_Weapons },
            { blockModels.Sniper.Title, Action_Weapons },
            { blockModels.SMG.Title, Action_Weapons },
            { blockModels.ShotgunHeavy.Title, Action_Weapons },
            { blockModels.Barrier.Title, Action_Barrier },
        };
    }

    public static void Actions(CCSPlayerController player, CBaseEntity entity)
    {
        if (entity == null || entity.Entity == null)
            return;

        if (Entities.TryGetValue(entity, out var block))
        {
            if (!PlayerCooldowns.ContainsKey(player.Slot))
                PlayerCooldowns[player.Slot] = new();

            if (!CooldownsTimers.ContainsKey(player.Slot))
                CooldownsTimers[player.Slot] = new();

            if (BlockCooldown(player, block.Entity) || TempTimers.Contains(block.Entity))
                return;

            var type = block.Type;

            if (type.Contains('.'))
            {
                foreach (var weaponType in WeaponList.Categories.Keys)
                {
                    if (type.Split('.')[0] == weaponType)
                    {
                        type = weaponType;
                        break;
                    }
                }
            }

            var CustomBlock = blockModels.CustomBlocks.FirstOrDefault(cb => cb.Title.Equals(type, StringComparison.OrdinalIgnoreCase));
            if (CustomBlock != null)
            {
                Action_Custom(player, block, CustomBlock);
                return;
            }

            if (blockActions.TryGetValue(type, out var action))
                action(player, block);

            //else Utils.PrintToChat(player, $"{ChatColors.Red}Error: No action found for {type}");
        }
    }

    private static void ActivatedMessage(CCSPlayerController player, string blocktitle)
    {
        if (!player.NotValid() && player.IsAlive())
            Server.NextFrame(() => Utils.PrintToChat(player, $"{ChatColors.White}{blocktitle} {ChatColors.Grey}activated"));
    }
    private static void DeactivatedMessage(CCSPlayerController player, string blocktitle)
    {
        if (!player.NotValid() && player.IsAlive())
            Server.NextFrame(() => Utils.PrintToChat(player, $"{ChatColors.White}{blocktitle} {ChatColors.Grey}has worn off"));
    }

    public static Dictionary<int, List<CBaseProp>> PlayerCooldowns = new();
    public static Dictionary<int, List<Timer>> CooldownsTimers = new();
    private static void BlockCooldownTimer(CCSPlayerController player, CBaseProp block, float timer = 0, bool message = false)
    {
        if (timer <= 0 || block == null || block.Entity == null)
            return;

        var cooldown = PlayerCooldowns[player.Slot];

        if (!BlockCooldown(player, block))
            cooldown.Add(block);

        var cdtimer = instance.AddTimer(timer, () =>
        {
            if (cooldown.Contains(block))
            {
                cooldown.Remove(block);

                if (message)
                    Utils.PrintToChat(player, $"{ChatColors.White}{block.Entity.Name} {ChatColors.Grey}block is no longer on cooldown");
            }

            //else Utils.PrintToChat(player, $"{ChatColors.Red}Error: could not reset cooldown for {block} block");
        });

        CooldownsTimers[player.Slot].Add(cdtimer);
    }
    private static bool BlockCooldown(CCSPlayerController player, CBaseProp block)
    {
        return PlayerCooldowns.TryGetValue(player.Slot, out var blockList) && blockList.Contains(block);
    }

    private static void Action_Custom(CCSPlayerController player, Data data, CustomBlockModel customBlock)
    {
        var block = data.Entity;
        var settings = data.Properties;

        string playerName = player.PlayerName;
        string steamId = player.AuthorizedSteamID?.SteamId2.ToString() ?? "0";
        string steamId3 = player.AuthorizedSteamID?.SteamId3.ToString() ?? "0";
        string steamId64 = player.AuthorizedSteamID?.SteamId64.ToString() ?? "0";
        string userId = player.UserId?.ToString() ?? "-1";
        string slot = player.Slot.ToString();
        string value = settings.Value.ToString();

        foreach (string command in customBlock.Command)
        {
            if (string.IsNullOrEmpty(command))
            {
                Utils.PrintToChat(player, $"{ChatColors.Red}Error: empty command string for custom block {customBlock.Title}");
                continue;
            }

            string replacedcommand = command
                .Replace("{NAME}", playerName)
                .Replace("{STEAMID}", steamId)
                .Replace("{STEAMID3}", steamId3)
                .Replace("{STEAMID64}", steamId64)
                .Replace("{USERID}", userId)
                .Replace("{SLOT}", slot)
                .Replace("{VALUE}", value);

            Server.ExecuteCommand(replacedcommand);
            Utils.Log($"(CustomBlock: {customBlock.Title}) executed command: {replacedcommand}");
        }

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Random(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var settings = data.Properties;

        var availableActions = Entities.Where(x =>
            x.Value.Type != blockModels.Random.Title &&
            x.Value.Type != blockModels.Bhop.Title &&
            x.Value.Type != blockModels.Delay.Title
        ).ToList();

        var randomAction = availableActions[new Random().Next(availableActions.Count)];
        var randomdata = randomAction.Value;
        var type = randomAction.Value.Type;

        Server.NextFrame(() =>
        {
            Utils.PrintToChat(player, $"You got {ChatColors.White}{type} {ChatColors.Grey}from the {ChatColors.White}{blockModels.Random.Title} {ChatColors.Grey}block");
            if (blockActions.TryGetValue(type, out var action))
                action(player, randomdata);
        });

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    public static List<CBaseEntity> TempTimers = new();
    private static void Action_BhopDelay(CCSPlayerController player, Data data)
    {
        if (TempTimers.Contains(data.Entity))
            return;

        var block = data.Entity;
        var settings = data.Properties;
        var duration = settings.Duration;
        var cooldown = settings.Cooldown;
        var render = block.Render;

        TempTimers.Add(block);

        instance.AddTimer(duration, () =>
        {
            block.CollisionRulesChanged(CollisionGroup.COLLISION_GROUP_TRIGGER);

            var clr = Utils.GetColor(data.Color);
            int alpha = Utils.GetAlpha(data.Transparency);
            block.Render = Color.FromArgb(alpha / 2, clr.R, clr.G, clr.B);
            Utilities.SetStateChanged(block, "CBaseModelEntity", "m_clrRender");

            instance.AddTimer(cooldown, () =>
            {
                block.CollisionRulesChanged(CollisionGroup.COLLISION_GROUP_NONE);

                block.Render = render;
                Utilities.SetStateChanged(block, "CBaseModelEntity", "m_clrRender");

                if (TempTimers.Contains(block))
                    TempTimers.Remove(block);
            });
        });
    }

    private static void Action_Gravity(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var title = blockModels.Gravity.Title;
        var settings = data.Properties;
        var gravity = player.GravityScale;

        player.SetGravity(settings.Value);

        ActivatedMessage(player, title);

        instance.AddTimer(settings.Duration, () =>
        {
            player.SetGravity(gravity);

            DeactivatedMessage(player, title);
        });

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Health(CCSPlayerController player, Data data)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        if (pawn.Health >= pawn.MaxHealth)
            return;

        var block = data.Entity;
        var settings = data.Properties;

        player.Health((int)+settings.Value);

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Nades(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var settings = data.Properties;
        var type = data.Type;

        string designer = "";

        var grenades = new Dictionary<string, string>
        {
            { blockModels.Grenade.Title, "weapon_hegrenade" },
            { blockModels.Frost.Title, "weapon_smokegrenade" },
            { blockModels.Flash.Title, "weapon_flashbang" }
        };

        if (grenades.TryGetValue(type, out var mappedDesigner))
            designer = mappedDesigner;

        else Utils.PrintToChat(player, "Couldn't find nade based on block type");

        var check = player.FindWeapon(designer);
        if (check != null) return;

        player.GiveWeapon(designer);

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Fire(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var settings = data.Properties;
        var fire = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;

        fire.EffectName = "particles/burning_fx/env_fire_medium.vpcf";
        fire.DispatchSpawn();
        fire.AcceptInput("Start");
        fire.AcceptInput("FollowEntity", player.Pawn(), player.Pawn(), "!activator");

        var damagetimer = instance.AddTimer(1.0f, () =>
        {
            player.EmitSound(sounds.Fire);
            instance.AddTimer(0.33f, () => player.Health((int)-settings.Value));
        }, TimerFlags.REPEAT);

        instance.AddTimer(settings.Duration, () =>
        {
            if (damagetimer != null)
                damagetimer.Kill();

            if (fire.IsValid)
            {
                fire.AcceptInput("Stop");
                fire.Remove();
            }
        });

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Death(CCSPlayerController player, Data data)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        var block = data.Entity;

        BlockCooldownTimer(player, block, 1.0f);

        if (pawn.TakesDamage == false)
            return;

        player.CommitSuicide(false, true);
    }

    private static void Action_Damage(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var settings = data.Properties;

        player.Health((int)-settings.Value);
  
        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Weapons(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var weapon = data.Type.Substring(data.Type.LastIndexOf('.') + 1);
        var settings = data.Properties;

        var gun = WeaponList.Weapons.FirstOrDefault(w => w.Name.Equals(weapon, StringComparison.OrdinalIgnoreCase));

        if (gun != null)
        {
            var weaponCategory = WeaponList.Categories.FirstOrDefault(category =>
                category.Value.Contains(gun.Designer)).Key;

            if (!string.IsNullOrEmpty(weaponCategory))
            {
                int weaponGroup = weaponCategory == Models.Data.Pistol.Title ? 2 : 1;

                var hasGroupWeapon = player.PlayerPawn.Value?.WeaponServices?.MyWeapons
                    .Any(w => WeaponList.Categories
                    .Where(cat => (cat.Key == Models.Data.Pistol.Title ? 2 : 1) == weaponGroup)
                    .SelectMany(cat => cat.Value)
                    .Contains(w.Value?.DesignerName)) ?? false;

                if (hasGroupWeapon)
                    return;
            }

            var designer = gun.Designer;

            player.GiveWeapon(designer);

            if (WeaponList.SpecialWeapons.TryGetValue(designer, out var special))
                designer = special;

            player.FindWeapon(designer).SetAmmo((int)settings.Value, 0);

            Utils.PrintToChatAll($"{ChatColors.LightPurple}{player.PlayerName} {ChatColors.Grey}equipped a {ChatColors.White}{weapon}");

            BlockCooldownTimer(player, block, settings.Cooldown);
        }

        else Utils.PrintToChatAll($"{ChatColors.Red}The weapon '{weapon}' does not exist in the weapon list.");
    }

    private static void Action_Speed(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var velocity = player.PlayerPawn.Value!.VelocityModifier;
        var title = blockModels.Speed.Title;
        var settings = data.Properties;

        player.SetVelocity(settings.Value);
        player.EmitSound(sounds.Speed);
        ActivatedMessage(player, title);

        instance.AddTimer(settings.Duration, () =>
        {
            player.SetVelocity(velocity);
            DeactivatedMessage(player, title);
        });

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_SpeedBoost(CCSPlayerController player, Data data)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        var block = data.Entity;
        var settings = data.Properties;

        float angleYaw = pawn.EyeAngles.Y * (float)Math.PI / 180f;
        float boost = settings.Value;

        pawn.AbsVelocity.X = (float)Math.Cos(angleYaw) * boost;
        pawn.AbsVelocity.Y = (float)Math.Sin(angleYaw) * boost;
        pawn.AbsVelocity.Z = settings.Duration;

        pawn.Teleport(null, null, pawn.AbsVelocity);

        BlockCooldownTimer(player, block, 0.25f);
    }

    private static void Action_Slap(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var settings = data.Properties;

        player.Slap((int)settings.Value);

        BlockCooldownTimer(player, block, 0.25f);
    }

    public static bool nuked;
    private static void Action_Nuke(CCSPlayerController player, Data data)
    {
        if (nuked)
            return;

        CsTeam teamToNuke = 0;
        string teamName = "";

        if (player.IsT())
        {
            teamToNuke = CsTeam.CounterTerrorist;
            teamName = "Counter-Terrorist";
        }
        else if (player.IsCT())
        {
            teamToNuke = CsTeam.Terrorist;
            teamName = "Terrorist";
        }

        var playersToNuke = Utilities.GetPlayers().Where(p => p.Team == teamToNuke);

        foreach (var playerToNuke in playersToNuke)
            playerToNuke.CommitSuicide(false, true);

        Utils.PrintToChatAll($"{ChatColors.LightPurple}{player.PlayerName} {ChatColors.Grey}has nuked the {teamName} team");

        Utils.PlaySoundAll(sounds.Nuke);

        nuked = true;
    }

    public static HashSet<CCSPlayerController> HiddenPlayers = new();
    private static void Action_Stealth(CCSPlayerController player, Data data)
    {
        if (HiddenPlayers.Contains(player))
            return;

        var block = data.Entity;
        var settings = data.Properties;
        var title = blockModels.Stealth.Title;

        HiddenPlayers.Add(player);

        player.EmitSound(sounds.Stealth);
        player.ColorScreen(Color.FromArgb(150, 75, 75, 75), settings.Duration / 2, settings.Duration, EntityExtends.FadeFlags.FADE_OUT);
        ActivatedMessage(player, title);

        instance.AddTimer(settings.Duration, () =>
        {
            HiddenPlayers.Remove(player);
            DeactivatedMessage(player, title);
        });

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Invincibility(CCSPlayerController player, Data data)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        var block = data.Entity;
        var settings = data.Properties;
        var title = blockModels.Invincibility.Title;
        var render = pawn.Render;

        if (pawn.TakesDamage == false)
            Server.NextFrame(() => Utils.PrintToChat(player, "You are already invincible"));

        else
        {
            pawn.TakesDamage = false;
            pawn.Render = Color.FromArgb(255, 200, 0, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            player.EmitSound(sounds.Invincibility);
            player.ColorScreen(Color.FromArgb(50, 255, 0, 255), settings.Duration / 2, settings.Duration, EntityExtends.FadeFlags.FADE_OUT);
            ActivatedMessage(player, title);

            instance.AddTimer(settings.Duration, () =>
            {
                pawn.TakesDamage = true;
                pawn.Render = render;
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

                DeactivatedMessage(player, title);
            });
        }

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Camouflage(CCSPlayerController player, Data data)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        var block = data.Entity;
        var settings = data.Properties;
        var title = blockModels.Camouflage.Title;

        var model = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName!;

        if (player.IsT()) player.SetModel(config.Settings.Blocks.CamouflageT);
        else if (player.IsCT()) player.SetModel(config.Settings.Blocks.CamouflageCT);

        player.EmitSound(sounds.Camouflage);
        player.ColorScreen(Color.FromArgb(50, 0, 255, 0), settings.Duration / 2, settings.Duration, EntityExtends.FadeFlags.FADE_OUT);
        ActivatedMessage(player, title);

        instance.AddTimer(settings.Duration, () =>
        {
            player.SetModel(model);
            DeactivatedMessage(player, title);
        });

        BlockCooldownTimer(player, block, settings.Cooldown);
    }

    private static void Action_Trampoline(CCSPlayerController player, Data data)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        var block = data.Entity;
        var settings = data.Properties;

        pawn.AbsVelocity.Z = settings.Value;

        pawn.Teleport(null, null, pawn.AbsVelocity);

        BlockCooldownTimer(player, block);
    }

    private static void Action_Honey(CCSPlayerController player, Data data)
    {
        var block = data.Entity;
        var settings = data.Properties;

        player.SetVelocity(settings.Value);

        BlockCooldownTimer(player, block);
    }

    private static void Action_Barrier(CCSPlayerController player, Data data)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        var block = data.Entity;
        var settings = data.Properties;

        if ((data.Team == "CT" && player.Team == CsTeam.Terrorist) ||
            (data.Team == "T" && player.Team == CsTeam.CounterTerrorist))
        {
            pawn.Teleport(pawn.AbsOrigin!.ToVector_t(), velocity: new());
            return;
        }

        Action_BhopDelay(player, data);

        BlockCooldownTimer(player, block);
    }

    public static void Test(CCSPlayerController player)
    {
        var block = player.GetBlockAim();

        if (block == null)
        {
            Utils.PrintToChat(player, $"{ChatColors.Red}Could not find a block to test");
            return;
        }

        if (block.Entity == null || !block.Entity.Name.StartsWith("blockmaker"))
            return;

        Utils.PrintToChat(player, $"Testing - {ChatColors.White}{block.Entity!.Name.Replace("blockmaker_", "")}");
        Actions(player, block);
    }
}