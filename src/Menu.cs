using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Interface;

public partial class Menu
{
    private static Plugin Instance = Plugin.Instance;

    public static IMenu Create(string title, string menuType, IMenu? prevMenu = null)
    {
        IMenu menu = MenuManager.MenuByType(menuType, title, Instance);

        if (prevMenu != null)
            menu.PrevMenu = prevMenu;

        menu.ExitButton = true;

        return menu;
    }

    public static void Open(CCSPlayerController player, string title)
    {
        string menuType = Instance.Config.Settings.MenuType;

        IMenu Menu = Create(title, menuType);

        Menu.AddItem($"Blocks", (player, option) =>
        {
            Menu_Commands(player, menuType, Menu);
        });

        Menu.AddItem("Teleports", (player, option) =>
        {
            Menu_Teleports(player, menuType, Menu);
        });

        Menu.AddItem("Lights", (player, option) =>
        {
            Menu_Lights(player, menuType, Menu);
        });

        Menu.AddItem("Settings", (player, option) =>
        {
            Menu_BuildSettings(player, menuType, Menu);
        });

        Menu.Display(player, 0);
    }

    /* Menu_Commands */

    static void Menu_Commands(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create("Block Menu", menuType, Parent);

        Menu.AddItem($"Settings", (player, option) =>
        {
            Menu_BlockSettings(player, menuType, Menu);
        });

        Menu.AddItem("Create", (player, option) =>
        {
            Commands.CreateBlock(player);
            Menu_Commands(player, menuType, Parent);
        });

        Menu.AddItem("Delete", (player, option) =>
        {
            Commands.DeleteBlock(player);
            Menu_Commands(player, menuType, Parent);
        });

        Menu.AddItem("Rotate", (player, option) =>
        {
            string[] options = { "Reset", "X-", "X+", "Y-", "Y+", "Z-", "Z+" };

            PositionMenuOptions(player, menuType, Parent, options, true);
        });

        Menu.AddItem("Move", (player, option) =>
        {
            string[] options = { "X-", "X+", "Y-", "Y+", "Z-", "Z+" };

            PositionMenuOptions(player, menuType, Parent, options, false);
        });

        Menu.AddItem("Copy", (player, option) =>
        {
            Commands.CopyBlock(player);
            Menu_Commands(player, menuType, Parent);
        });

        Menu.AddItem("Convert", (player, option) =>
        {
            Commands.ConvertBlock(player);
            Menu_Commands(player, menuType, Parent);
        });

        Menu.AddItem("Lock", (player, option) =>
        {
            Commands.LockBlock(player);
            Menu_Commands(player, menuType, Parent);
        });

        Menu.Display(player, 0);
    }

    static void PositionMenuOptions(CCSPlayerController player, string menuType, IMenu Parent, string[] options, bool rotate)
    {
        var BuilderData = Building.Builders[player.Slot];

        float value = rotate ? BuilderData.RotationValue : BuilderData.PositionValue;
        string title = $"{(rotate ? "Rotate" : "Move")} Block ({value} Units)";

        IMenu Menu = Create(title, menuType, Parent);

        Menu.AddItem($"Select Units", (player, option) =>
        {
            BuilderData.ChatInput = rotate ? "Rotation" : "Position";
            Utils.PrintToChat(player, $"Write your desired number in the chat");
            PositionMenuOptions(player, menuType, Parent, options, rotate);
        });

        foreach (string input in options)
        {
            Menu.AddItem(input, (player, option) =>
            {
                if (rotate) Commands.RotateBlock(player, input);
                else Commands.PositionBlock(player, input);
                PositionMenuOptions(player, menuType, Parent, options, rotate);
            });
        }

        Menu.Display(player, 0);
    }

    /* Menu_Commands */

    /* Menu_BlockSettings */
    
    static void Menu_BlockSettings(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create("Block Settings", menuType, Parent);

        Menu.AddItem($"Type: {BuilderData.BlockType}", (player, option) =>
        {
            TypeMenuOptions(player, menuType, Menu);
        });

        Menu.AddItem($"Size: {BuilderData.BlockSize}", (player, option) =>
        {
            var sizeValues = Instance.Config.Settings.Blocks.Sizes.ToArray();
            int currentIndex = Array.FindIndex(sizeValues, s => s.Title == BuilderData.BlockSize);
            int nextIndex = (currentIndex + 1) % sizeValues.Length;
            BuilderData.BlockSize = sizeValues[nextIndex].Title;

            Menu_BlockSettings(player, menuType, Parent);
        });

        Menu.AddItem($"Pole: {(BuilderData.BlockPole ? "ON" : "OFF")}", (player, option) =>
        {
            Commands.Pole(player);

            Menu_BlockSettings(player, menuType, Parent);
        });

        Menu.AddItem($"Team: {BuilderData.BlockTeam}", (player, option) =>
        {
            string[] teamValues = { "Both", "T", "CT" };
            int currentIndex = Array.IndexOf(teamValues, BuilderData.BlockTeam);
            int nextIndex = (currentIndex + 1) % teamValues.Length;
            BuilderData.BlockTeam = teamValues[nextIndex];
            Commands.TeamBlock(player, teamValues[nextIndex]);

            Menu_BlockSettings(player, menuType, Parent);
        });

        Menu.AddItem($"Properties", (player, option) =>
        {
            var entity = player.GetBlockAim();

            if (entity?.Entity == null || string.IsNullOrEmpty(entity.Entity.Name))
            {
                Utils.PrintToChat(player, "Could not find a block to edit properties");
                Menu_BlockSettings(player, menuType, Parent);
                return;
            }

            BuilderData.ChatInput = "";
            BuilderData.PropertyEntity.Clear();

            PropertiesMenuOptions(player, menuType, Menu, entity);
        });

        Menu.AddItem($"Transparency: {BuilderData.BlockTransparency}", (player, option) =>
        {
            TransparencyMenuOptions(player, menuType, Menu);
        });

        Menu.AddItem($"Effect: {BuilderData.BlockEffect.Title}", (player, option) =>
        {
            EffectMenuOptions(player, menuType, Menu);
        });

        Menu.AddItem($"Color: {BuilderData.BlockColor}", (player, option) =>
        {
            ColorMenuOptions(player, menuType, Menu);
        });

        Menu.Display(player, 0);
    }

    static void TypeMenuOptions(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Select Type ({BuilderData.BlockType})", menuType, Parent);

        var blockmodels = Blocks.Models.Data;

        foreach (var block in blockmodels.GetAllBlocks())
        {
            string blockName = block.Title;

            Menu.AddItem(blockName, (player, option) =>
            {
                if (blockName == blockmodels.Pistol.Title ||
                    blockName == blockmodels.Sniper.Title ||
                    blockName == blockmodels.Rifle.Title ||
                    blockName == blockmodels.SMG.Title ||
                    blockName == blockmodels.ShotgunHeavy.Title
                )
                {
                    GunTypeMenu(player, menuType, Parent, block.Title);
                    return;
                }

                Commands.BlockType(player, blockName);

                TypeMenuOptions(player, menuType, Parent);
            });
        }

        Menu.Display(player, 0);
    }

    static void GunTypeMenu(CCSPlayerController player, string menuType, IMenu Parent, string gunType)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Select {gunType}", menuType, Parent);

        if (WeaponList.Categories.ContainsKey(gunType))
        {
            var weaponsInCategory = WeaponList.Categories[gunType];

            foreach (var weaponID in weaponsInCategory)
            {
                var weapon = WeaponList.Weapons.FirstOrDefault(w => w.Designer == weaponID);

                if (weapon != null)
                {
                    Menu.AddItem(weapon.Name, (player, option) =>
                    {
                        var blockModels = Blocks.Models.Data;
                        foreach (var model in blockModels.GetAllBlocks())
                        {
                            if (string.Equals(model.Title, gunType, StringComparison.OrdinalIgnoreCase))
                            {
                                BuilderData.BlockType = $"{model.Title}.{weapon.Name}";
                                Utils.PrintToChat(player, $"Selected Type: {ChatColors.White}{model.Title}.{weapon.Name}");

                                Menu_BlockSettings(player, menuType, Parent);
                                return;
                            }
                        }
                    });
                }
            }
        }

        Menu.Display(player, 0);
    }

    static void TransparencyMenuOptions(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Select Transparency ({BuilderData.BlockTransparency})", menuType, Parent);

        foreach (var value in Utils.AlphaMapping.Keys)
        {
            Menu.AddItem(value, (player, option) =>
            {
                BuilderData.BlockTransparency = value;

                Utils.PrintToChat(player, $"Selected Transparency: {ChatColors.White}{value}");

                Commands.TransparencyBlock(player, value);

                Menu_BlockSettings(player, menuType, Parent);
            });
        }

        Menu.Display(player, 0);
    }

    static void EffectMenuOptions(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Select Effect ({BuilderData.BlockEffect.Title})", menuType, Parent);

        Menu.AddItem("None", (player, option) =>
        {
            BuilderData.BlockEffect = new("None", "");

            Utils.PrintToChat(player, $"Selected Effect: {ChatColors.White}None");

            Commands.EffectBlock(player);

            Menu_BlockSettings(player, menuType, Parent);
        });

        foreach (var value in Instance.Config.Settings.Blocks.Effects)
        {
            Menu.AddItem(value.Title, (player, option) =>
            {
                BuilderData.BlockEffect = value;

                Utils.PrintToChat(player, $"Selected Effect: {ChatColors.White}{value.Title}");

                Commands.EffectBlock(player);

                Menu_BlockSettings(player, menuType, Parent);
            });
        }

        Menu.Display(player, 0);
    }

    static void ColorMenuOptions(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Select Color ({BuilderData.BlockColor})", menuType, Parent);

        foreach (var color in Utils.ColorMapping.Keys)
        {
            Menu.AddItem(color, (player, option) =>
            {
                Commands.BlockColor(player, color);

                Menu_BlockSettings(player, menuType, Parent);
            });
        }

        Menu.Display(player, 0);
    }

    static void PropertiesMenuOptions(CCSPlayerController player, string menuType, IMenu Parent, CBaseEntity entity)
    {
        var BuilderData = Building.Builders[player.Slot];

        if (Blocks.Entities.TryGetValue(entity, out var block))
        {
            IMenu Menu = Create($"Properties ({block.Type})", menuType, Parent);
            var properties = block.Properties;

            if (!string.IsNullOrEmpty(properties.Builder))
            {
                string name = "";
                string date = "unknown";
                var parts = properties.Builder.Split(['-'], StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    name = string.Join("-", parts.Take(parts.Length - 1)).Trim();
                    date = parts[parts.Length - 1].Trim();
                }
                else name = properties.Builder.Trim();

                Menu.AddItem("Builder Info", (player, option) =>
                {
                    Utils.PrintToChat(player, $"{ChatColors.White}{block.Type} {ChatColors.Grey}created by: {ChatColors.White}{name} {ChatColors.Grey}date: {ChatColors.White}{date}");
                    PropertiesMenuOptions(player, menuType, Parent, entity);
                });
            }

            PropertyMenuOption(Menu, menuType, Parent, "Reset", 1, player, entity);
            PropertyMenuOption(Menu, menuType, Parent, "OnTop", 1, player, entity);
            PropertyMenuOption(Menu, menuType, Parent, "Duration", properties.Duration, player, entity);
            PropertyMenuOption(Menu, menuType, Parent, "Value", properties.Value, player, entity);
            PropertyMenuOption(Menu, menuType, Parent, "Cooldown", properties.Cooldown, player, entity);
            PropertyMenuOption(Menu, menuType, Parent, "Locked", 1, player, entity);

            Menu.Display(player, 0);
        }
    }

    static void PropertyMenuOption(IMenu Menu, string menuType, IMenu Parent, string property, float value, CCSPlayerController player, CBaseEntity entity)
    {
        var BuilderData = Building.Builders[player.Slot];

        if (value != 0)
        {
            string title = $": {value}";

            if (property == "Reset")
                title = "";

            if (property == "OnTop")
                title = Blocks.Entities[entity].Properties.OnTop ? ": Enabled" : ": Disabled";

            if (property == "Locked")
                title = Blocks.Entities[entity].Properties.Locked ? ": Enabled" : ": Disabled";

            Menu.AddItem($"{property}{title}", (player, option) =>
            {
                BuilderData.ChatInput = property;
                BuilderData.PropertyEntity[property] = entity;

                if (property == "Reset" || property == "OnTop" || property == "Locked")
                {
                    Commands.Properties(player, property, property);
                    PropertiesMenuOptions(player, menuType, Parent, entity);
                }
                else
                {
                    Utils.PrintToChat(player, $"Write your desired number in the chat");
                    PropertiesMenuOptions(player, menuType, Parent, entity);
                }
            });
        }
    }

    /* Menu_BlockSettings */

    /* Menu_Teleports */

    static void Menu_Teleports(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create("Teleports Menu", menuType, Parent);

        Menu.AddItem($"Create", (player, option) =>
        {
            Teleports.Create(player);

            Menu_Teleports(player, menuType, Parent);
        });

        Menu.AddItem($"Delete", (player, option) =>
        {
            Teleports.Delete(player);

            Menu_Teleports(player, menuType, Parent);
        });

        Menu.Display(player, 0);
    }
    /* Menu_Teleports */

    /* Menu_Lights */

    static void Menu_Lights(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create("Lights Menu", menuType, Parent);

        Menu.AddItem($"Create", (player, option) =>
        {
            Lights.Create(player);

            Menu_Lights(player, menuType, Parent);
        });

        Menu.AddItem($"Delete", (player, option) =>
        {
            Lights.Delete(player);

            Menu_Lights(player, menuType, Parent);
        });

        Menu.AddItem($"Color: " + BuilderData.LightColor, (player, option) =>
        {
            IMenu ColorsMenu = Create($"Select Color ({BuilderData.LightColor})", menuType, Menu);

            foreach (var color in Utils.ColorMapping.Keys)
            {
                ColorsMenu.AddItem(color, (player, option) =>
                {
                    BuilderData.LightColor = color;

                    Lights.Delete(player, false, true);

                    Menu_Lights(player, menuType, Parent);
                });
            }

            ColorsMenu.Display(player, 0);
        });

        Menu.AddItem($"Style: " + BuilderData.LightStyle, (player, option) =>
        {
            IMenu StylesMenu = Create($"Select Style ({BuilderData.LightStyle})", menuType, Menu);

            foreach (var style in Lights.Styles.Keys)
            {
                StylesMenu.AddItem(style, (player, option) =>
                {
                    BuilderData.LightStyle = style;

                    Lights.Delete(player, false, true);

                    Menu_Lights(player, menuType, Parent);
                });
            }

            StylesMenu.Display(player, 0);
        });

        Menu.AddItem($"Brightness: " + BuilderData.LightBrightness, (player, option) =>
        {
            BuilderData.ChatInput = "LightBrightness";

            Utils.PrintToChat(player, $"Write your desired number in the chat");
            Menu_Lights(player, menuType, Parent);
        });

        Menu.AddItem($"Distance: " + BuilderData.LightDistance, (player, option) =>
        {
            BuilderData.ChatInput = "LightDistance";

            Utils.PrintToChat(player, $"Write your desired number in the chat");
            Menu_Lights(player, menuType, Parent);
        });

        Menu.Display(player, 0);
    }
    /* Menu_Lights */

    /* Menu_Settings */

    static void Menu_BuildSettings(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Build Settings", menuType, Parent);

        Menu.AddItem("Build Mode: " + (Building.BuildMode ? "ON" : "OFF"), (player, option) =>
        {
            Commands.BuildMode(player);

            Menu_BuildSettings(player, menuType, Parent);
        });

        Menu.AddItem("Godmode: " + (BuilderData.Godmode ? "ON" : "OFF"), (player, option) =>
        {
            Commands.Godmode(player);

            Menu_BuildSettings(player, menuType, Parent);
        });

        Menu.AddItem("Noclip: " + (BuilderData.Noclip ? "ON" : "OFF"), (player, option) =>
        {
            Commands.Noclip(player);

            Menu_BuildSettings(player, menuType, Parent);
        });

        Menu.AddItem($"Grid Settings", (player, option) =>
        {
            GridMenuOptions(player, menuType, Menu);
        });

        Menu.AddItem($"Snap Settings", (player, option) =>
        {
            SnapMenuOptions(player, menuType, Menu);
        });

        Menu.AddItem("Save Blocks", (player, option) =>
        {
            Commands.SaveBlocks(player);

            Menu_BuildSettings(player, menuType, Parent);
        });

        Menu.AddItem("Clear Blocks", (player, option) =>
        {
            IMenu ConfirmMenu = Create($"Confirm", menuType, Menu);

            ConfirmMenu.AddItem("NO - keep blocks", (player, option) =>
            {
                Menu_BuildSettings(player, menuType, Parent);
            });

            ConfirmMenu.AddItem("YES - remove blocks", (player, option) =>
            {
                Commands.ClearBlocks(player);

                Menu_BuildSettings(player, menuType, Parent);
            });

            ConfirmMenu.Display(player, 0);
        });

        Menu.AddItem("Manage Builders", (player, option) =>
        {
            IMenu BuildersMenu = Create($"Manage Builders", menuType, Menu);

            var targets = Utilities.GetPlayers().Where(x => x != player && !x.IsBot);
            if (targets.Count() <= 0)
            {
                Utils.PrintToChat(player, $"{ChatColors.Red}No players available");
                Menu.Display(player, 0);
                return;
            }

            foreach (var target in targets)
            {
                BuildersMenu.AddItem(target.PlayerName, (player, option) =>
                {
                    Commands.ManageBuilder(player, target.SteamID.ToString());
                });
            }

            BuildersMenu.Display(player, 0);
        });

        Menu.Display(player, 0);
    }

    static void GridMenuOptions(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Grid Options ({BuilderData.GridValue} Units)", menuType, Parent);

        Menu.AddItem($"Select Units", (player, option) =>
        {
            BuilderData.ChatInput = "Grid";

            Utils.PrintToChat(player, $"Write your desired number in the chat");
            GridMenuOptions(player, menuType, Parent);
        });

        Menu.AddItem($"Grid: " + (BuilderData.Grid ? "ON" : "OFF"), (player, option) =>
        {
            Commands.Grid(player, "");

            GridMenuOptions(player, menuType, Parent);
        });

        Menu.Display(player, 0);
    }

    static void SnapMenuOptions(CCSPlayerController player, string menuType, IMenu Parent)
    {
        var BuilderData = Building.Builders[player.Slot];

        IMenu Menu = Create($"Snap Options ({BuilderData.SnapValue} Units)", menuType, Parent);

        Menu.AddItem($"Select Units", (player, option) =>
        {
            BuilderData.ChatInput = "Snap";

            Utils.PrintToChat(player, $"Write your desired number in the chat");
            SnapMenuOptions(player, menuType, Parent);
        });

        Menu.AddItem($"Snap: " + (BuilderData.Snapping ? "ON" : "OFF"), (player, option) =>
        {
            Commands.Snapping(player);

            SnapMenuOptions(player, menuType, Parent);
        });

        Menu.Display(player, 0);
    }

    /* Menu_Settings */
}