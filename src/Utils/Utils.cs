using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using FixVectorLeak;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Text.Json;

public static class Utils
{
    private static Plugin instance = Plugin.Instance;
    private static Config config = instance.Config;

    public static bool BuildMode(CCSPlayerController player)
    {
        bool isBuilder = Building.Builders.TryGetValue(player.Slot, out var BuilderData);

        if (Building.BuildMode && (isBuilder || HasPermission(player)))
            return true;

        else if (!Building.BuildMode && (isBuilder || HasPermission(player)))
        {
            PrintToChat(player, $"{ChatColors.Red}Build Mode is disabled");
            return false;
        }
        else
        {
            PrintToChat(player, $"{ChatColors.Red}You don't have access to Build Mode");
            return false;
        }
    }

    public static bool HasPermission(CCSPlayerController player)
    {
        if (config.Commands.Admin.Permission.Count == 0)
            return true;

        foreach (string perm in config.Commands.Admin.Permission)
        {
            if (perm.StartsWith("@") && AdminManager.PlayerHasPermissions(player, perm))
                return true;
            if (perm.StartsWith("#") && AdminManager.PlayerInGroup(player, perm))
                return true;
        }
        return false;
    }

    public static bool CorrectTeam(CCSPlayerController player, string team)
    {
        team = (team ?? string.Empty).ToLower();
        bool isTeamValid =
            ((team == "t" || team == "terrorist") && player.Team == CsTeam.Terrorist) ||
            ((team == "ct" || team == "counterterrorist") && player.Team == CsTeam.CounterTerrorist) ||
            string.IsNullOrEmpty(team) || team == "both" || team == "all";

        return isTeamValid;
    }

    public static void Log(string message)
    {
        instance.Logger.LogInformation(message);
    }

    public static void PrintToChat(CCSPlayerController player, string message)
    {
        player.PrintToChat($" {config.Settings.Prefix} {message}");
    }

    public static void PrintToChatAll(string message)
    {
        Server.PrintToChatAll($" {config.Settings.Prefix} {message}");
    }

    public static void PlaySoundAll(string sound)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            RecipientFilter filter = [player];
            player.EmitSound(sound, filter);
        }
    }

    public static bool IsValidJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log($"JSON Check: file does not exist ({filePath})");
            return false;
        }

        string fileContent = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            Log($"JSON Check: file is empty ({filePath})");
            return false;
        }

        try
        {
            JsonDocument.Parse(fileContent);
            return true;
        }
        catch (JsonException)
        {
            Log($"JSON Check: invalid content ({filePath})");
            return false;
        }
    }

    public static string GetModelFromSelectedBlock(string blockType, bool pole)
    {
        int hyphenIndex = blockType.IndexOf('.');
        if (hyphenIndex >= 0)
            blockType = blockType.Substring(0, hyphenIndex);
        var blockModels = Blocks.Models.Data;

        foreach (var model in blockModels.GetAllBlocks())
        {
            if (model.Title.Equals(blockType, StringComparison.OrdinalIgnoreCase))
                return pole ? model.Pole : model.Block;
        }

        return string.Empty;
    }

    public static CBaseProp? GetBlockAim(this CCSPlayerController player)
    {
        var GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;

        if (GameRules is null)
            return null;

        VirtualFunctionWithReturn<CCSGameRules, CBasePlayerController, nint, CBaseEntity?> CCSGameRules_FindPickerEntity = new(GameRules.Handle, GameData.GetOffset("CCSGameRules_FindPickerEntity"));
        CBaseEntity? entity = CCSGameRules_FindPickerEntity.Invoke(GameRules, player, nint.Zero);

        if (entity != null &&
            entity.IsValid &&
            entity.Entity != null &&
            entity.DesignerName.Contains("prop_physics_override") &&
            entity.Entity.Name.StartsWith("blockmaker")
        )
            return entity.As<CBaseProp>();

        return null;
    }

    public static CBaseProp? GetClosestBlock(Vector_t endPos, CBaseProp excludeBlock, double threshold)
    {
        CBaseProp? closestBlock = null;

        foreach (var target in Utilities.GetAllEntities().Where(e => e.DesignerName.Contains("prop_physics_override") && e.Entity!.Name.StartsWith("blockmaker")))
        {
            CBaseProp prop = target.As<CBaseProp>();

            if (prop == excludeBlock)
                continue;

            double distance = VectorUtils.CalculateDistance(endPos, prop.AbsOrigin!.ToVector_t());
            if (distance < threshold)
                closestBlock = prop;
        }

        return closestBlock;
    }

    public static int GetPlacedBlocksCount()
    {
        int blockCount = 0;

        foreach (var block in Utilities.GetAllEntities().Where(b => b.DesignerName == "prop_physics_override"))
        {
            if (block == null || !block.IsValid || block.Entity == null)
                continue;

            if (!String.IsNullOrEmpty(block.Entity.Name) && block.Entity.Name.StartsWith("blockmaker"))
                blockCount++;
        }

        return blockCount;
    }

    public static Color ParseColor(string input)
    {
        var colorParts = input.Split(',');
        if (colorParts.Length == 4 &&
            int.TryParse(colorParts[0], out var r) &&
            int.TryParse(colorParts[1], out var g) &&
            int.TryParse(colorParts[2], out var b) &&
            int.TryParse(colorParts[3], out var a))
        {
            return Color.FromArgb(a, r, g, b);
        }
        return Color.FromArgb(255, 255, 255, 255);
    }

    public static readonly Dictionary<string, Color> ColorMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "None", Color.White },
        { "White", Color.White },
        { "Red", Color.Red },
        { "Green", Color.Green },
        { "Blue", Color.Blue },
        { "Yellow", Color.Yellow },
        { "Orange", Color.Orange },
        { "Lime", Color.Lime },
        { "Aqua", Color.Aqua },
        { "Lightblue", Color.LightBlue },
        { "Darkblue", Color.DarkBlue },
        { "Purple", Color.Purple },
        { "Pink", Color.LightPink},
        { "Hotpink", Color.HotPink},
        { "Gray", Color.Gray },
        { "Silver", Color.Silver },
    };
    public static Color GetColor(string input)
    {
        return ColorMapping.TryGetValue(input.ToLower(), out var color) ? color : ColorMapping["None"];
    }

    public static readonly Dictionary<string, int> AlphaMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "100%", 255 },
        { "75%", 191 },
        { "50%", 128 },
        { "25%", 50 },
        { "0%", 0 },
    };
    public static int GetAlpha(string input)
    {
        if (AlphaMapping.TryGetValue(input, out int value))
            return value;

        if (int.TryParse(input.TrimEnd('%'), out int percentage) && percentage >= 0 && percentage <= 100)
            return (int)Math.Round(percentage / 100.0 * 255);

        throw new ArgumentException("Invalid percentage format or value.", nameof(input));
    }

    public static float GetSize(string input)
    {
        var blockSize = config.Settings.Blocks.Sizes
            .FirstOrDefault(bs => bs.Title.Equals(input, StringComparison.OrdinalIgnoreCase));

        return blockSize?.Size ?? config.Settings.Blocks.Sizes.First(bs => bs.Size == 1.0f).Size;
    }

    public static CBeam DrawBeam(Vector_t startPos, Vector_t endPos, Color color, float width = 0.25f)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam")!;

        beam.Entity!.Name = "blockmaker_beam";
        beam.Render = color;
        beam.Width = width;

        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;

        beam.Teleport(startPos);
        beam.DispatchSpawn();

        return beam;
    }

    public static void DrawBeamsAroundBlock(CCSPlayerController player, CBaseEntity block, Color color)
    {
        Vector_t pos = block.AbsOrigin!.ToVector_t();
        QAngle_t rotation = block.AbsRotation!.ToQAngle_t();

        float scale = Blocks.Entities.ContainsKey(block) ? Utils.GetSize(Blocks.Entities[block].Size) : 1;

        var max = block.Collision!.Maxs * scale;
        var min = block.Collision!.Mins * scale;

        Vector_t forward = new(
            (float)Math.Cos(rotation.Y * Math.PI / 180) * (float)Math.Cos(rotation.X * Math.PI / 180),
            (float)Math.Sin(rotation.Y * Math.PI / 180) * (float)Math.Cos(rotation.X * Math.PI / 180),
            (float)-Math.Sin(rotation.X * Math.PI / 180)
        );
        Vector_t right = new(
            (float)Math.Cos((rotation.Y + 90) * Math.PI / 180),
            (float)Math.Sin((rotation.Y + 90) * Math.PI / 180),
            0
        );
        Vector_t up = VectorUtils.Cross(forward, right);

        Vector_t[] localCorners =
        {
            new(min.X, min.Y, min.Z), // Bottom-back-left
            new(max.X, min.Y, min.Z), // Bottom-back-right
            new(max.X, max.Y, min.Z), // Bottom-front-right
            new(min.X, max.Y, min.Z), // Bottom-front-left
            new(min.X, min.Y, max.Z), // Top-back-left
            new(max.X, min.Y, max.Z), // Top-back-right
            new(max.X, max.Y, max.Z), // Top-front-right
            new(min.X, max.Y, max.Z)  // Top-front-left
        };

        Vector_t[] corners = new Vector_t[8];
        for (int i = 0; i < localCorners.Length; i++)
        {
            Vector_t localCorner = localCorners[i];
            corners[i] =
                pos +
                forward * localCorner.X +
                right * localCorner.Y +
                up * localCorner.Z;
        }

        var beams = new List<Vector_t[]>
        {
            new[] { corners[0], corners[1] }, new[] { corners[1], corners[2] }, new[] { corners[2], corners[3] }, new[] { corners[3], corners[0] }, // Bottom
            new[] { corners[4], corners[5] }, new[] { corners[5], corners[6] }, new[] { corners[6], corners[7] }, new[] { corners[7], corners[4] }, // Top
            new[] { corners[0], corners[4] }, new[] { corners[1], corners[5] }, new[] { corners[2], corners[6] }, new[] { corners[3], corners[7] }  // Sides
        };

        var playerBeams = Building.BuilderHolds[player].Beams;

        // Remove invalid or excess beams
        for (int i = playerBeams.Count - 1; i >= 0; i--)
        {
            if (playerBeams[i] == null || !playerBeams[i].IsValid)
            {
                playerBeams.RemoveAt(i);
            }
        }

        // Update or create beams
        for (int i = 0; i < beams.Count; i++)
        {
            if (i < playerBeams.Count)
            {
                // Update existing beam
                var beam = playerBeams[i];
                if (beam != null && beam.IsValid)
                {
                    beam.Render = color;
                    beam.Width = 0.25f;
                    beam.Teleport(beams[i][0]);
                    beam.EndPos.X = beams[i][1].X;
                    beam.EndPos.Y = beams[i][1].Y;
                    beam.EndPos.Z = beams[i][1].Z;
                    Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
                    Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
                }
                else
                {
                    // Replace invalid beam
                    playerBeams[i] = DrawBeam(beams[i][0], beams[i][1], color);
                }
            }
            else
            {
                // Create new beam if needed
                var newBeam = DrawBeam(beams[i][0], beams[i][1], color);
                playerBeams.Add(newBeam);
            }
        }

        // Remove any extra beams
        while (playerBeams.Count > beams.Count)
        {
            var beam = playerBeams[playerBeams.Count - 1];
            if (beam != null && beam.IsValid)
                beam.Remove();
            playerBeams.RemoveAt(playerBeams.Count - 1);
        }
    }

    public static bool BlockLocked(CCSPlayerController player, Blocks.Data block)
    {
        if (block.Properties.Locked)
        {
            PrintToChat(player, "Block is locked");
            return true;
        }
        return false;
    }

    public static void RemoveEntities()
    {
        var entityNames = new HashSet<string>
        {
            "prop_physics_override",
            "trigger_multiple",
            "light_omni2",
            "env_particle_glow"
        };
        foreach (var entity in Utilities.GetAllEntities().Where(x => entityNames.Contains(x.DesignerName)))
        {
            if (entity.Entity == null || string.IsNullOrEmpty(entity.Entity.Name))
                continue;

            if (entity.Entity.Name.StartsWith("blockmaker"))
                entity.Remove();
        }
    }

    public static void Clear()
    {
        RemoveEntities();

        foreach (var timer in Plugin.Instance.Timers)
        {
            if (timer == Events.AutoSaveTimer)
                continue;

            timer?.Kill();
        }

        Blocks.Entities.Clear();

        Teleports.Entities.Clear();
        Teleports.isNext.Clear();

        Lights.Entities.Clear();

        Blocks.PlayerCooldowns.Clear();
        Blocks.CooldownsTimers.Clear();
        Blocks.TempTimers.Clear();

        Blocks.HiddenPlayers.Clear();
        Blocks.nuked = false;

        Building.BuilderHolds.Clear();
    }
}