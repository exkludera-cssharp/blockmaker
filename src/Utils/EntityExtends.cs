using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

public static class EntityExtends
{
    public static bool NotValid(this CCSPlayerController? player)
    {
        return player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.Connected != PlayerConnectedState.Connected || player.IsBot || player.IsHLTV;
    }

    public static bool IsPlayer(this CCSPlayerController? player)
    {
        return player is { IsValid: true, IsHLTV: false, IsBot: false, UserId: not null, SteamID: > 0 };
    }

    public static void SetGravity(this CCSPlayerController? player, float value)
    {
        CCSPlayerPawn? pawn = player.Pawn();

        if (pawn != null)
            pawn.ActualGravityScale = value;
    }

    public static void SetVelocity(this CCSPlayerController? player, float value)
    {
        CCSPlayerPawn? pawn = player.Pawn();

        if (pawn != null)
            pawn.VelocityModifier = value;
    }

    public static void SetArmour(this CCSPlayerController? player, int hp)
    {
        CCSPlayerPawn? pawn = player.Pawn();

        if (pawn != null)
            pawn.ArmorValue = hp;
    }

    public static void SetModel(this CCSPlayerController? player, string model)
    {
        CCSPlayerPawn? pawn = player.Pawn();

        if (pawn != null)
            pawn.SetModel(model);
    }

    public static void GiveWeapon(this CCSPlayerController? player, String weaponName)
    {
        if (player.IsAlive())
            player.GiveNamedItem(weaponName);
    }

    public static CCSPlayerPawn? Pawn(this CCSPlayerController? player)
    {
        if (!player.IsLegal() && !player.IsAlive())
            return null;

        CCSPlayerPawn? pawn = player.PlayerPawn.Value;

        return pawn;
    }

    public static bool IsLegal([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.PlayerPawn.IsValid && player.PlayerPawn.Value?.IsValid == true && !player.IsBot;
    }

    public static bool IsT([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return IsLegal(player) && player.Team == CsTeam.Terrorist;
    }

    public static bool IsCT([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return IsLegal(player) && player.Team == CsTeam.CounterTerrorist;
    }

    public static bool IsAlive([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return player!.PawnIsAlive && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    static public CBasePlayerWeapon? FindWeapon(this CCSPlayerController? player, string designername)
    {
        CCSPlayerPawn? pawn = player.Pawn();

        if (pawn == null)
            return null;

        var matchedWeapon = pawn.WeaponServices?.MyWeapons.Where(x => x.Value?.DesignerName == designername).FirstOrDefault();

        if (matchedWeapon != null && matchedWeapon.IsValid)
            return matchedWeapon.Value;

        return null;
    }

    static public void SetAmmo(this CBasePlayerWeapon? weapon, int clip, int reserve)
    {
        if (weapon == null || !weapon.IsValid)
            return;

        weapon.Clip1 = clip;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");

        weapon.ReserveAmmo[0] = reserve;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
    }

    public enum FadeFlags
    {
        FADE_IN,
        FADE_OUT,
        FADE_STAYOUT
    }
    public static void ColorScreen(this CCSPlayerController player, Color color, float hold = 0.1f, float fade = 0.2f, FadeFlags flags = FadeFlags.FADE_IN, bool withPurge = true)
    {
        var fadeMsg = UserMessage.FromPartialName("Fade");

        fadeMsg.SetInt("duration", Convert.ToInt32(fade * 512));
        fadeMsg.SetInt("hold_time", Convert.ToInt32(hold * 512));

        var flag = flags switch
        {
            FadeFlags.FADE_OUT => 0x0001,
            FadeFlags.FADE_IN => 0x0002,
            FadeFlags.FADE_STAYOUT => 0x0008,
            _ => (0x0001 | 0x0010),
        };

        if (withPurge)
            flag |= 0x0010;

        fadeMsg.SetInt("flags", flag);
        fadeMsg.SetInt("color", color.R | color.G << 8 | color.B << 16 | color.A << 24);
        fadeMsg.Send(player);
    }

    public static void Slap(this CCSPlayerController player, int damage)
    {
        var pawn = player.Pawn();
        if (pawn == null) return;

        var random = new Random();

        pawn.AbsVelocity.X += (random.Next(180) + 50) * ((random.Next(2) == 1) ? -1 : 1);
        pawn.AbsVelocity.Y += (random.Next(180) + 50) * ((random.Next(2) == 1) ? -1 : 1);
        pawn.AbsVelocity.Z += random.Next(200) + 100;

        player.Health(-damage);
    }

    public static void Health(this CCSPlayerController player, int hp)
    {
        var pawn = player.Pawn();
        if (pawn == null)
            return;

        if (pawn.TakesDamage == false && hp <= 0)
            return;

        int newHealth = pawn.Health + hp;
        if (hp > 0) newHealth = Math.Min(newHealth, pawn.MaxHealth);
        else if (hp < 0) newHealth = Math.Max(newHealth, 0);

        pawn.Health = newHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        player.ColorScreen(Color.FromArgb(100, 255, 0, 0), 0.25f, 0.5f, FadeFlags.FADE_OUT);

        var sounds = Plugin.Instance.Config.Sounds.Blocks;
        if (hp > 0) player.EmitSound(sounds.Health);
        else if (hp < 0) player.EmitSound(sounds.Damage);

        if (pawn.Health <= 0)
            pawn.CommitSuicide(true, true);
    }

    public static void CollisionRulesChanged(this CBaseEntity entity, CollisionGroup group)
    {
        if (entity.Collision == null)
        {
            Utils.Log("(CollisionRulesChanged) Collision is null");
            return;
        }
 
        entity.Collision.CollisionGroup = (byte)group;
        entity.Collision.CollisionAttribute.CollisionGroup = (byte)group;

        VirtualFunctionVoid<nint> collisionRulesChanged = new VirtualFunctionVoid<nint>(entity.Handle, GameData.GetOffset("CBaseEntity_CollisionRulesChanged"));
        collisionRulesChanged.Invoke(entity.Handle);
    }
}