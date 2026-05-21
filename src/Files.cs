using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using System.Text.Json;

public static partial class Files
{
    public static string mapsFolder = "";

    public static void Load()
    {
        mapsFolder = Path.Combine(Plugin.Instance.ModuleDirectory, "maps");
        Directory.CreateDirectory(mapsFolder);

        Blocks.Models.Load();

        Blocks.Properties.Load();

        Builders.Load();
    }

    public static class Builders
    {
        public static List<string> steamids = new List<string>();
        private static readonly string filepath = Path.Combine(Plugin.Instance.ModuleDirectory, "builders.txt");
        private static readonly char[] separator = ['#', '/', ' '];

        public static void Load()
        {
            if (!File.Exists(filepath) || String.IsNullOrEmpty(File.ReadAllText(filepath)))
            {
                File.WriteAllText(filepath, "# List of builders\n76561197960287930 // example");
                steamids = new List<string>();
                return;
            }

            var builders = new List<string>();

            foreach (var line in File.ReadAllLines(filepath))
            {
                string? steamId = line.Split(separator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (!string.IsNullOrEmpty(steamId))
                    builders.Add(steamId);
            }

            steamids = builders;
        }

        public static void Save(List<string> builders)
        {
            var lines = File.ReadAllLines(filepath).ToList();
            var updatedLines = new List<string>();
            var steamIdToLineMap = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                string? steamId = line.Split(separator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (!string.IsNullOrEmpty(steamId))
                    steamIdToLineMap[steamId] = line;

                else updatedLines.Add(line);
            }

            foreach (var steamId in builders)
            {
                if (steamIdToLineMap.ContainsKey(steamId))
                    updatedLines.Add(steamIdToLineMap[steamId]);

                else updatedLines.Add($"{steamId} // added by system");
            }

            File.WriteAllLines(filepath, updatedLines);
        }
    }

    public static class EntitiesData
    {
        public static void Save(bool autosave = false)
        {
            if (Utilities.GetPlayers().Count <= 0)
                return;

            if (Blocks.Entities.Count <= 0)
            {
                Utils.Log($"No blocks to save on {Server.MapName}");
                return;
            }

            var blocksPath = Path.Combine(mapsFolder, "blocks.json");
            var teleportsPath = Path.Combine(mapsFolder, "teleports.json");
            var lightsPath = Path.Combine(mapsFolder, "lights.json");

            try
            {
                var blocksList = new List<Blocks.SaveData>();
                var teleportsList = new List<Teleports.PairSaveData>();
                var lightsList = new List<Lights.SaveData>();

                // Save blocks
                foreach (var prop in Blocks.Entities)
                {
                    var block = prop.Key;
                    var data = prop.Value;

                    if (block != null && block.IsValid)
                    {
                        blocksList.Add(new Blocks.SaveData
                        {
                            Type = data.Type,
                            Pole = data.Pole,
                            Size = data.Size,
                            Team = data.Team,
                            Color = data.Color,
                            Transparency = data.Transparency,
                            Effect = data.Effect,
                            Properties = data.Properties,
                            Position = new VectorUtils.VectorDTO(block.AbsOrigin!),
                            Rotation = new VectorUtils.QAngleDTO(block.AbsRotation!)
                        });
                    }
                }

                // Save teleports
                foreach (var teleport in Teleports.Entities)
                {
                    if (teleport.Entry != null && teleport.Exit != null)
                    {
                        teleportsList.Add(new Teleports.PairSaveData
                        {
                            Entry = new Teleports.SaveData
                            {
                                Name = teleport.Entry.Name,
                                Position = new VectorUtils.VectorDTO(teleport.Entry.Entity.AbsOrigin!),
                                Rotation = new VectorUtils.QAngleDTO(teleport.Entry.Entity.AbsRotation!)
                            },
                            Exit = new Teleports.SaveData
                            {
                                Name = teleport.Exit.Name,
                                Position = new VectorUtils.VectorDTO(teleport.Exit.Entity.AbsOrigin!),
                                Rotation = new VectorUtils.QAngleDTO(teleport.Exit.Entity.AbsRotation!)
                            }
                        });
                    }
                }

                // Save lights
                foreach (var prop in Lights.Entities)
                {
                    var entity = prop.Key;
                    var data = prop.Value;

                    if (entity != null && entity.IsValid)
                    {
                        lightsList.Add(new Lights.SaveData
                        {
                            Color = data.Color,
                            Style = data.Style,
                            Brightness = data.Brightness,
                            Distance = data.Distance,
                            Position = new VectorUtils.VectorDTO(entity.AbsOrigin!),
                            Rotation = new VectorUtils.QAngleDTO(entity.AbsRotation!)
                        });
                    }
                }

                int blocksCount = blocksList.Count;
                int teleportsCount = teleportsList.Count;
                int lightsCount = lightsList.Count;

                // Save blocks to blocks.json
                if (blocksCount > 0)
                {
                    string blocksJson = JsonSerializer.Serialize(blocksList, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(blocksPath, blocksJson);
                }

                // Save teleports to teleports.json
                if (teleportsCount > 0)
                {
                    string teleportsJson = JsonSerializer.Serialize(teleportsList, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(teleportsPath, teleportsJson);
                }

                // Save lights to lights.json
                if (lightsCount > 0)
                {
                    string lightsJson = JsonSerializer.Serialize(lightsList, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(lightsPath, lightsJson);
                }

                if (Plugin.Instance.Config.Sounds.Building.Enabled)
                    Utils.PlaySoundAll(Plugin.Instance.Config.Sounds.Building.Save);

                int blocks = Utils.GetPlacedBlocksCount();
                var s = blocks == 1 ? "" : "s";

                Utils.PrintToChatAll($"{(autosave ? "Auto-" : "")}Saved {ChatColors.White}{blocks} {ChatColors.Grey}block{s} on {ChatColors.White}{Server.MapName}");
            }
            catch (Exception ex)
            {
                Utils.Log($"Failed to save data: {ex.Message}");
            }
        }

        public static void Load()
        {
            var blocksPath = Path.Combine(mapsFolder, "blocks.json");
            var teleportsPath = Path.Combine(mapsFolder, "teleports.json");
            var lightsPath = Path.Combine(mapsFolder, "lights.json");

            // spawn blocks
            if (File.Exists(blocksPath) && Utils.IsValidJson(blocksPath))
            {
                var blocksJson = File.ReadAllText(blocksPath);
                var blocksList = JsonSerializer.Deserialize<List<Blocks.SaveData>>(blocksJson);

                if (blocksList != null && blocksList.Count > 0)
                {
                    foreach (var Data in blocksList)
                    {
                        Blocks.CreateBlock(
                            null,
                            Data.Type,
                            Data.Pole,
                            Data.Size,
                            new(Data.Position.X, Data.Position.Y, Data.Position.Z),
                            new(Data.Rotation.Pitch, Data.Rotation.Yaw, Data.Rotation.Roll),
                            Data.Color,
                            Data.Transparency,
                            Data.Team,
                            Data.Effect,
                            Data.Properties
                        );
                    }
                }
            }
            else Utils.Log($"Failed to spawn Blocks. File for {Server.MapName} is empty or invalid");

            // spawn teleports
            if (File.Exists(teleportsPath) && Utils.IsValidJson(teleportsPath))
            {
                var teleportsJson = File.ReadAllText(teleportsPath);
                var teleportsList = JsonSerializer.Deserialize<List<Teleports.PairSaveData>>(teleportsJson);

                if (teleportsList != null && teleportsList.Count > 0)
                {
                    foreach (var teleportPairData in teleportsList)
                    {
                        var entryPosition = new Vector(teleportPairData.Entry.Position.X, teleportPairData.Entry.Position.Y, teleportPairData.Entry.Position.Z);
                        var entryRotation = new QAngle(teleportPairData.Entry.Rotation.Pitch, teleportPairData.Entry.Rotation.Yaw, teleportPairData.Entry.Rotation.Roll);

                        var entryEntity = Teleports.CreateEntity(entryPosition, entryRotation, teleportPairData.Entry.Name);

                        var exitPosition = new Vector(teleportPairData.Exit.Position.X, teleportPairData.Exit.Position.Y, teleportPairData.Exit.Position.Z);
                        var exitRotation = new QAngle(teleportPairData.Exit.Rotation.Pitch, teleportPairData.Exit.Rotation.Yaw, teleportPairData.Exit.Rotation.Roll);

                        var exitEntity = Teleports.CreateEntity(exitPosition, exitRotation, teleportPairData.Exit.Name);

                        if (entryEntity != null && exitEntity != null)
                            Teleports.Entities.Add(new Teleports.Pair(entryEntity, exitEntity));
                    }
                }
            }

            // spawn lights
            if (File.Exists(lightsPath) && Utils.IsValidJson(lightsPath))
            {
                var lightsJson = File.ReadAllText(lightsPath);
                var lightsList = JsonSerializer.Deserialize<List<Lights.SaveData>>(lightsJson);

                if (lightsList != null && lightsList.Count > 0)
                {
                    foreach (var lightData in lightsList)
                    {
                        Lights.CreateEntity(
                            lightData.Color,
                            lightData.Style,
                            lightData.Brightness,
                            lightData.Distance,
                            new Vector(lightData.Position.X, lightData.Position.Y, lightData.Position.Z),
                            new QAngle(lightData.Rotation.Pitch, lightData.Rotation.Yaw, lightData.Rotation.Roll)
                        );
                    }
                }
            }
        }
    }
}