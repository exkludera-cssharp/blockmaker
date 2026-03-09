using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

public static class Transmit
{
    public static void Load()
    {
        Plugin.Instance.RegisterListener<Listeners.CheckTransmit>(CheckTransmit);
        Plugin.Instance.HookUserMessage(208, CMsgSosStartSoundEvent, HookMode.Pre);
    }

    public static void Unload()
    {
        Plugin.Instance.RemoveListener<Listeners.CheckTransmit>(CheckTransmit);
        Plugin.Instance.UnhookUserMessage(208, CMsgSosStartSoundEvent, HookMode.Pre);
    }

    private static void CheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null || !Building.Builders.ContainsKey(player.Slot))
                continue;

            foreach (var hidden in Blocks.HiddenPlayers)
            {
                if (player == hidden || player.Pawn.Value?.As<CCSPlayerPawnBase>().PlayerState == CSPlayerState.STATE_OBSERVER_MODE)
                    continue;

                var remove = hidden.Pawn.Value;
                if (remove == null) continue;

                info.TransmitEntities.Remove(remove);
            }
        }
    }

    private static HookResult CMsgSosStartSoundEvent(UserMessage um)
    {
        int entIndex = um.ReadInt("source_entity_index");
        var entHandle = NativeAPI.GetEntityFromIndex(entIndex);

        var pawn = new CBasePlayerPawn(entHandle);
        if (pawn == null || !pawn.IsValid || pawn.DesignerName != "player") return HookResult.Continue;

        var player = pawn.Controller?.Value?.As<CCSPlayerController>();
        if (player == null || !player.IsValid) return HookResult.Continue;
   
        if (Blocks.HiddenPlayers.Contains(player))
        {
            foreach (var target in Utilities.GetPlayers())
            {
                if (target.NotValid()) continue;
                if (target == player) continue;

                um.Recipients.Remove(target);
            }
        }

        return HookResult.Continue;
    }
}