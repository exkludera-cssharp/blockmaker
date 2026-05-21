public static class WeaponList
{
    public class Weapon
    {
        public string Name { get; set; }
        public string Designer { get; set; }

        public Weapon(string weaponName, string weaponID)
        {
            Name = weaponName;
            Designer = weaponID;
        }
    }

    public static List<Weapon> Weapons = new List<Weapon>
    {
        new Weapon("FAMAS", "weapon_famas"),
        new Weapon("AK-47", "weapon_ak47"),
        new Weapon("AUG", "weapon_aug"),
        new Weapon("M4A1", "weapon_m4a1"),
        new Weapon("M4A1-S", "weapon_m4a1_silencer"),
        new Weapon("Galil AR", "weapon_galilar"),
        new Weapon("SG 553", "weapon_sg556"),

        new Weapon("SCAR-20", "weapon_scar20"),
        new Weapon("SSG 08", "weapon_ssg08"),
        new Weapon("AWP", "weapon_awp"),
        new Weapon("G3SG1", "weapon_g3sg1"),

        new Weapon("Glock-18", "weapon_glock"),
        new Weapon("P2000", "weapon_hkp2000"),
        new Weapon("CZ75-Auto", "weapon_cz75a"),
        new Weapon("Desert Eagle", "weapon_deagle"),
        new Weapon("Dual Berettas", "weapon_elite"),
        new Weapon("Five-Seven", "weapon_fiveseven"),
        new Weapon("P250", "weapon_p250"),
        new Weapon("Tec-9", "weapon_tec9"),
        new Weapon("USP-S", "weapon_usp_silencer"),
        new Weapon("R8 Revolver", "weapon_revolver"),

        new Weapon("UMP-45", "weapon_ump45"),
        new Weapon("P90", "weapon_p90"),
        new Weapon("MP5-SD", "weapon_mp5sd"),
        new Weapon("MP7", "weapon_mp7"),
        new Weapon("MP9", "weapon_mp9"),
        new Weapon("PP-Bizon", "weapon_bizon"),
        new Weapon("MAC-10", "weapon_mac10"),

        new Weapon("MAG-7", "weapon_mag7"),
        new Weapon("Nova", "weapon_nova"),
        new Weapon("XM1014", "weapon_xm1014"),
        new Weapon("Sawed-Off", "weapon_sawedoff"),
        new Weapon("M249", "weapon_m249"),
        new Weapon("Negev", "weapon_negev")
    };

    public static Dictionary<string, List<string>> Categories = new Dictionary<string, List<string>>
    {
        { Blocks.Models.Data.Pistol.Title, new List<string> { "weapon_glock", "weapon_hkp2000", "weapon_cz75a", "weapon_deagle", "weapon_elite", "weapon_fiveseven", "weapon_p250", "weapon_tec9", "weapon_usp_silencer", "weapon_revolver" } },
        { Blocks.Models.Data.Sniper.Title, new List<string> { "weapon_scar20", "weapon_ssg08", "weapon_awp", "weapon_g3sg1" } },
        { Blocks.Models.Data.Rifle.Title, new List<string> { "weapon_famas", "weapon_ak47", "weapon_aug", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_galilar", "weapon_sg556" } },
        { Blocks.Models.Data.SMG.Title, new List<string> { "weapon_ump45", "weapon_p90", "weapon_mp5sd", "weapon_mp7", "weapon_mp9", "weapon_bizon", "weapon_mac10" } },
        { Blocks.Models.Data.ShotgunHeavy.Title, new List<string> { "weapon_mag7", "weapon_nova", "weapon_xm1014", "weapon_sawedoff", "weapon_m249", "weapon_negev" } }
    };

    public static Dictionary<string, string> SpecialWeapons = new Dictionary<string, string>
    {
        { "weapon_m4a1_silencer", "weapon_m4a1" },
        { "weapon_usp_silencer", "weapon_hkp2000" },
        { "weapon_revolver", "weapon_deagle" },
        { "weapon_mp5sd", "weapon_mp7" }
    };
}