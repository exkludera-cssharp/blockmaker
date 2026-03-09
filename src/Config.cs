using CounterStrikeSharp.API.Core;

public class Config : BasePluginConfig
{
    public Config_Settings Settings { get; set; } = new();
    public Config_Commands Commands { get; set; } = new();
    public Config_Sounds Sounds { get; set; } = new();
}

public class Config_Settings
{
    public string Prefix { get; set; } = "{purple}BlockMaker {grey}|";
    public string MenuType { get; set; } = "CenterHtmlMenu";
    public class Settings_Building
    {
        public class Settings_BuildMode
        {
            public bool Enable { get; set; } = true;
            public bool Config { get; set; } = false;
        };
        public Settings_BuildMode BuildMode { get; set; } = new();

        public class Settings_AutoSave
        {
            public bool Enable { get; set; } = true;
            public float Timer { get; set; } = 300;
        };
        public Settings_AutoSave AutoSave { get; set; } = new();

        public class Settings_BlockGrab
        {
            public bool Render { get; set; } = true;
            public string RenderColor { get; set; } = "255,255,255,128";
            public bool Beams { get; set; }  = true;
            public string BeamsColor { get; set; } = "255,255,255,255";
        };
        public Settings_BlockGrab Grab { get; set; } = new();
    }
    public Settings_Building Building { get; set; } = new();

    public class Settings_Blocks
    {
        public bool DisableShadows { get; set; } = true;
        public string CamouflageT { get; set; } = "characters/models/ctm_fbi/ctm_fbi.vmdl";
        public string CamouflageCT { get; set; } = "characters/models/tm_leet/tm_leet_variantb.vmdl";
        public string FireParticle { get; set; } = "particles/burning_fx/env_fire_medium.vpcf";
        public List<Blocks.BlockSize> Sizes { get; set; } = new()
        {
            new("Small", 0.5f),
            new("Normal", 1.0f),
            new("Large", 2.0f),
            new("X-Large", 3.0f)
        };

        public List<Blocks.Effect> Effects { get; set; } = new()
        {
            new("Fire", "particles/burning_fx/env_fire_small.vpcf"),
            new("Smoke", "particles/burning_fx/smoke_gib_01.vpcf"),
            new("Money", "particles/money_fx/moneybag_trail.vpcf"),
        };
    }
    public Settings_Blocks Blocks { get; set; } = new();

    public class Settings_Teleports
    {
        public bool ForceAngles { get; set; } = false;
        public bool AllowEntities { get; set; } = true;
        public float Velocity { get; set; } = 300;

        public class Settings_TeleportEntry
        {
            public string Model { get; set; } = "models/blockmaker/teleport/model.vmdl";
            public string Color { get; set; } = "0,255,0,255";
        }
        public Settings_TeleportEntry Entry { get; set; } = new();

        public class Settings_TeleportExit
        {
            public string Model { get; set; } = "models/blockmaker/teleport/model.vmdl";
            public string Color { get; set; } = "255,0,0,255";
        }
        public Settings_TeleportExit Exit { get; set; } = new();
    }
    public Settings_Teleports Teleports { get; set; } = new();

    public class Settings_Lights
    {
        public string Model { get; set; } = "models/generic/interior_lamp_kit_01/ilk01_lamp_01_bulb.vmdl";
        public bool HideModel { get; set; } = true;
    };
    public Settings_Lights Lights { get; set; } = new();
}

public class Config_Commands
{
    public class Commands_Admin
    {
        public List<string> Permission { get; set; } = [ "@css/root" ];
        public List<string> BuildMode { get; set; } = [ "buildmode" ];
        public List<string> ManageBuilder { get; set; } = [ "builder", "builders" ];
        public List<string> ResetProperties { get; set; } = ["resetproperties"];
    }
    public Commands_Admin Admin { get; set; } = new();

    public class Commands_Building
    {
        public List<string> BuildMenu { get; set; } = [ "bm", "buildmenu" ];
        public List<string> CreateBlock { get; set; } = [ "create" ];
        public List<string> DeleteBlock { get; set; } = [ "delete" ];
        public List<string> RotateBlock { get; set; } = [ "rotate" ];
        public List<string> PositionBlock { get; set; } = [ "position" ];
        public List<string> BlockType { get; set; } = [ "type"];
        public List<string> BlockColor { get; set; } = [ "color" ];
        public List<string> CopyBlock { get; set; } = [ "copy" ];
        public List<string> ConvertBlock { get; set; } = [ "convert" ];
        public List<string> LockBlock { get; set; } = [ "lock" ];
        public List<string> LockAll { get; set; } = ["lockall"];
        public List<string> SaveBlocks { get; set; } = [ "save" ];
        public List<string> Snapping { get; set; } = [ "snap" ];
        public List<string> Grid { get; set; } = [ "grid" ];
        public List<string> Noclip { get; set; } = [ "nc" ];
        public List<string> Godmode { get; set; } = [ "god" ];
        public List<string> TestBlock { get; set; } = [ "testblock" ];
    }
    public Commands_Building Building { get; set; } = new();
}

public class Config_Sounds
{
    public string SoundEvents { get; set; } = "soundevents/blockmaker.vsndevts";

    public class Sounds_Blocks
    {
        public string Speed { get; set; } = "bm_speed";
        public string Camouflage { get; set; } = "bm_camouflage";
        public string Damage { get; set; } = "bm_damage";
        public string Fire { get; set; } = "bm_fire";
        public string Health { get; set; } = "bm_health";
        public string Invincibility { get; set; } = "bm_invincibility";
        public string Nuke { get; set; } = "bm_nuke";
        public string Stealth { get; set; } = "bm_stealth";
        public string Teleport { get; set; } = "bm_teleport";
    }
    public Sounds_Blocks Blocks { get; set; } = new();

    public class Sounds_Building
    {
        public bool Enabled { get; set; } = true;
        public string Create { get; set; } = "bm_create";
        public string Delete { get; set; } = "bm_delete";
        public string Place { get; set; } =  "bm_place";
        public string Rotate { get; set; } = "bm_rotate";
        public string Save { get; set; } = "bm_save";
    }
    public Sounds_Building Building { get; set; } = new();
}