using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;

public static class TouchHooks
{
    private static VirtualFunctionVoid<CBaseEntity, CBaseEntity>? OnStartTouch;
    private static VirtualFunctionVoid<CBaseEntity, CBaseEntity>? OnTouch;
    private static VirtualFunctionVoid<CBaseEntity, CBaseEntity>? OnEndTouch;

    public static void Load()
    {
        Server.NextWorldUpdate(() =>
        {
            var tempEntity = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override")!;

            if (OnStartTouch == null) OnStartTouch = new(tempEntity, GameData.GetOffset("StartTouch"));
            if (OnTouch == null) OnTouch = new(tempEntity, GameData.GetOffset("Touch"));
            if (OnEndTouch == null) OnEndTouch = new(tempEntity, GameData.GetOffset("EndTouch"));

            OnStartTouch?.Hook(Hook_OnStartTouch, HookMode.Pre);
            OnTouch?.Hook(Hook_OnTouch, HookMode.Pre);
            OnEndTouch?.Hook(Hook_OnEndTouch, HookMode.Pre);
        });
    }

    public static void Unload()
    {
        OnStartTouch?.Unhook(Hook_OnStartTouch, HookMode.Pre);
        OnTouch?.Unhook(Hook_OnTouch, HookMode.Pre);
        OnEndTouch?.Unhook(Hook_OnEndTouch, HookMode.Pre);
    }

    private static HookResult Hook_OnStartTouch(DynamicHook hook)
    {
        CBaseEntity caller = hook.GetParam<CBaseEntity>(0);
        CBaseEntity activator = hook.GetParam<CBaseEntity>(1);

        // Teleports (only on start touch)
        var teleport = Teleports.Entities.Where(pair => pair.Entry.Entity == caller || pair.Exit.Entity == caller).FirstOrDefault();
        if (teleport != null)
            Teleports.Action(teleport, caller, activator);

        if (activator.DesignerName != "player")
            return HookResult.Continue;

        //Plugin.Instance.Logger.LogInformation($"[OnStartTouch] {activator.DesignerName} ({activator.Index}) - {caller.DesignerName} ({caller.Index})");

        var pawn = activator.As<CCSPlayerPawn>();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var player = pawn.OriginalController?.Value?.As<CCSPlayerController>();
        if (player == null || player.IsBot)
            return HookResult.Continue;

        if (Blocks.Entities.TryGetValue(caller, out var block))
        {
            if (Building.BuildMode)
            {
                foreach (var kvp in Building.BuilderHolds)
                    if (kvp.Value.Entity == block!.Entity)
                        return HookResult.Continue;
            }

            if (player.PlayerPawn.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return HookResult.Continue;

            if (block.Properties.OnTop && !VectorUtils.CheckOnTop(block, pawn))
                return HookResult.Continue;

            if (Utils.CorrectTeam(player, block.Team))
                Blocks.Actions(player, block.Entity);
        }

        return HookResult.Continue;
    }

    private static HookResult Hook_OnTouch(DynamicHook hook)
    {
        CBaseEntity caller = hook.GetParam<CBaseEntity>(0);
        CBaseEntity activator = hook.GetParam<CBaseEntity>(1);

        if (activator.DesignerName != "player")
            return HookResult.Continue;

        //Plugin.Instance.Logger.LogInformation($"[OnTouch] {activator.DesignerName} ({activator.Index}) - {caller.DesignerName} ({caller.Index})");

        var pawn = activator.As<CCSPlayerPawn>();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var player = pawn.OriginalController?.Value?.As<CCSPlayerController>();
        if (player == null || player.IsBot)
            return HookResult.Continue;

        if (Blocks.Entities.TryGetValue(caller, out var block))
        {
            if (Building.BuildMode)
            {
                foreach (var kvp in Building.BuilderHolds)
                    if (kvp.Value.Entity == block!.Entity)
                        return HookResult.Continue;
            }

            if (player.PlayerPawn.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return HookResult.Continue;

            if (!VectorUtils.CheckOnTop(block, pawn))
                return HookResult.Continue;

            if (Utils.CorrectTeam(player, block.Team))
                Blocks.Actions(player, block.Entity);
        }

        return HookResult.Continue;
    }

    private static HookResult Hook_OnEndTouch(DynamicHook hook)
    {
        CBaseEntity caller = hook.GetParam<CBaseEntity>(0);
        CBaseEntity activator = hook.GetParam<CBaseEntity>(1);

        if (activator.DesignerName != "player")
            return HookResult.Continue;

        //Plugin.Instance.Logger.LogInformation($"[OnEndTouch] {activator.DesignerName} ({activator.Index}) - {caller.DesignerName} ({caller.Index})");

        var pawn = activator.As<CCSPlayerPawn>();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var player = pawn.OriginalController?.Value?.As<CCSPlayerController>();
        if (player == null || player.IsBot)
            return HookResult.Continue;

        return HookResult.Continue;
    }
}