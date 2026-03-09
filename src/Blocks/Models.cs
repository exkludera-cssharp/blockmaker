using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json.Serialization;
using System.Text.Json;

public partial class Blocks
{
    public class BlockModel
    {
        public string Title { get; set; } = "";
        public string Block { get; set; } = "";
        public string Pole { get; set; } = "";
    }

    public class BlockSize
    {
        public string Title { get; set; }
        public float Size { get; set; }

        public BlockSize(string title, float size)
        {
            Title = title;
            Size = size;
        }
    }

    public class Models
    {
        public BlockPlatform Platform { get; set; } = new();
        public BlockBhop Bhop { get; set; } = new();
        public BlockHealth Health { get; set; } = new();
        public BlockGrenade Grenade { get; set; } = new();
        public BlockGravity Gravity { get; set; } = new();
        public BlockGlass Glass { get; set; } = new();
        public BlockFrost Frost { get; set; } = new();
        public BlockFlash Flash { get; set; } = new();
        public BlockFire Fire { get; set; } = new();
        public BlockDelay Delay { get; set; } = new();
        public BlockDeath Death { get; set; } = new();
        public BlockDamage Damage { get; set; } = new();
        public BlockPistol Pistol { get; set; } = new();
        public BlockRifle Rifle { get; set; } = new();
        public BlockSniper Sniper { get; set; } = new();
        public BlockSMG SMG { get; set; } = new();
        public BlockShotgunHeavy ShotgunHeavy { get; set; } = new();
        public BlockStealth Stealth { get; set; } = new();
        public BlockSpeed Speed { get; set; } = new();
        public BlockSpeedBoost SpeedBoost { get; set; } = new();
        public BlockSlap Slap { get; set; } = new();
        public BlockRandom Random { get; set; } = new();
        public BlockNuke Nuke { get; set; } = new();
        public BlockInvincibility Invincibility { get; set; } = new();
        public BlockIce Ice { get; set; } = new();
        public BlockCamouflage Camouflage { get; set; } = new();
        public BlockTrampoline Trampoline { get; set; } = new();
        public BlockNoFallDmg NoFallDmg { get; set; } = new();
        public BlockHoney Honey { get; set; } = new();
        public BlockBarrier Barrier { get; set; } = new();

        public List<CustomBlockModel> CustomBlocks { get; set; } = new List<CustomBlockModel>();
        public List<BlockModel> GetAllBlocks()
        {
            var allBlocks = new List<BlockModel>
        {
            Platform, Bhop, Health, Grenade, Gravity, Glass, Frost, Flash, Fire, Delay,
            Death, Damage, Pistol, Rifle, Sniper, SMG, ShotgunHeavy, Stealth, Speed,
            SpeedBoost, Slap, Random, Nuke, Invincibility, Ice, Camouflage, Trampoline,
            NoFallDmg, Honey, Barrier
        };

            if (CustomBlocks != null && CustomBlocks.Count > 0)
                allBlocks.AddRange(CustomBlocks);

            return allBlocks;
        }

        public static Models Data { get; set; } = new Models();
        public static void Load()
        {
            string directoryPath = Path.GetDirectoryName(Plugin.Instance.Config.GetConfigPath())!;
            string correctedPath = directoryPath.Replace("/BlockMaker.json", "");

            var modelsPath = Path.Combine(correctedPath, "models.json");

            if (!string.IsNullOrEmpty(modelsPath))
            {
                if (!File.Exists(modelsPath))
                {
                    using (FileStream fs = File.Create(modelsPath))
                        fs.Close();

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    string jsonContent = JsonSerializer.Serialize(Entities, options);

                    File.WriteAllText(modelsPath, jsonContent);
                }
            }

            if (!string.IsNullOrEmpty(modelsPath) && File.Exists(modelsPath))
            {
                string jsonContent = File.ReadAllText(modelsPath);
                Data = JsonSerializer.Deserialize<Models>(jsonContent) ?? new Models();

                LoadTitles();
            }
        }
    }

    public class CustomBlockModel : BlockModel
    {
        public string[] Command { get; set; } = ["css_example {NAME} {STEAMID} {STEAMID64} {USERID} {SLOT}", "css_yourcommand {STEAMID64} 1"];
    }

    public class BlockPlatform : BlockModel
    {
        public BlockPlatform()
        {
            Title = "Platform";
            Block = "models/blockmaker/platform/block.vmdl";
            Pole = "models/blockmaker/platform/pole.vmdl";
        }
    }

    public class BlockBhop : BlockModel
    {
        public BlockBhop()
        {
            Title = "Bhop";
            Block = "models/blockmaker/bhop/block.vmdl";
            Pole = "models/blockmaker/bhop/pole.vmdl";
        }
    }

    public class BlockNoFallDmg : BlockModel
    {
        public BlockNoFallDmg()
        {
            Title = "NoFallDmg";
            Block = "models/blockmaker/nofall/block.vmdl";
            Pole = "models/blockmaker/nofall/pole.vmdl";
        }
    }

    public class BlockHealth : BlockModel
    {
        public BlockHealth()
        {
            Title = "Health";
            Block = "models/blockmaker/health/block.vmdl";
            Pole = "models/blockmaker/health/pole.vmdl";
        }
    }

    public class BlockGrenade : BlockModel
    {
        public BlockGrenade()
        {
            Title = "Grenade";
            Block = "models/blockmaker/grenade/block.vmdl";
            Pole = "models/blockmaker/grenade/pole.vmdl";
        }
    }

    public class BlockGravity : BlockModel
    {
        public BlockGravity()
        {
            Title = "Gravity";
            Block = "models/blockmaker/gravity/block.vmdl";
            Pole = "models/blockmaker/gravity/pole.vmdl";
        }
    }

    public class BlockGlass : BlockModel
    {
        public BlockGlass()
        {
            Title = "Glass";
            Block = "models/blockmaker/glass/block.vmdl";
            Pole = "models/blockmaker/glass/pole.vmdl";
        }
    }

    public class BlockFrost : BlockModel
    {
        public BlockFrost()
        {
            Title = "Frost";
            Block = "models/blockmaker/frost/block.vmdl";
            Pole = "models/blockmaker/frost/pole.vmdl";
        }
    }

    public class BlockFlash : BlockModel
    {
        public BlockFlash()
        {
            Title = "Flash";
            Block = "models/blockmaker/flash/block.vmdl";
            Pole = "models/blockmaker/flash/pole.vmdl";
        }
    }

    public class BlockFire : BlockModel
    {
        public BlockFire()
        {
            Title = "Fire";
            Block = "models/blockmaker/fire/block.vmdl";
            Pole = "models/blockmaker/fire/pole.vmdl";
        }
    }

    public class BlockDelay : BlockModel
    {
        public BlockDelay()
        {
            Title = "Delay";
            Block = "models/blockmaker/delay/block.vmdl";
            Pole = "models/blockmaker/delay/pole.vmdl";
        }
    }

    public class BlockDeath : BlockModel
    {
        public BlockDeath()
        {
            Title = "Death";
            Block = "models/blockmaker/death/block.vmdl";
            Pole = "models/blockmaker/death/pole.vmdl";
        }
    }

    public class BlockDamage : BlockModel
    {
        public BlockDamage()
        {
            Title = "Damage";
            Block = "models/blockmaker/damage/block.vmdl";
            Pole = "models/blockmaker/damage/pole.vmdl";
        }
    }

    public class BlockPistol : BlockModel
    {
        public BlockPistol()
        {
            Title = "Pistol";
            Block = "models/blockmaker/pistol/block.vmdl";
            Pole = "models/blockmaker/pistol/pole.vmdl";
        }
    }

    public class BlockRifle : BlockModel
    {
        public BlockRifle()
        {
            Title = "Rifle";
            Block = "models/blockmaker/rifle/block.vmdl";
            Pole = "models/blockmaker/rifle/pole.vmdl";
        }
    }

    public class BlockSniper : BlockModel
    {
        public BlockSniper()
        {
            Title = "Sniper";
            Block = "models/blockmaker/sniper/block.vmdl";
            Pole = "models/blockmaker/sniper/pole.vmdl";
        }
    }

    public class BlockSMG : BlockModel
    {
        public BlockSMG()
        {
            Title = "SMG";
            Block = "models/blockmaker/smg/block.vmdl";
            Pole = "models/blockmaker/smg/pole.vmdl";
        }
    }

    public class BlockShotgunHeavy : BlockModel
    {
        public BlockShotgunHeavy()
        {
            Title = "Shotgun/Heavy";
            Block = "models/blockmaker/heavy/block.vmdl";
            Pole = "models/blockmaker/heavy/pole.vmdl";
        }
    }

    public class BlockTrampoline : BlockModel
    {
        public BlockTrampoline()
        {
            Title = "Trampoline";
            Block = "models/blockmaker/trampoline/block.vmdl";
            Pole = "models/blockmaker/trampoline/pole.vmdl";
        }
    }

    public class BlockStealth : BlockModel
    {
        public BlockStealth()
        {
            Title = "Stealth";
            Block = "models/blockmaker/stealth/block.vmdl";
            Pole = "models/blockmaker/stealth/pole.vmdl";
        }
    }

    public class BlockSpeedBoost : BlockModel
    {
        public BlockSpeedBoost()
        {
            Title = "SpeedBoost";
            Block = "models/blockmaker/speedboost/block.vmdl";
            Pole = "models/blockmaker/speedboost/pole.vmdl";
        }
    }

    public class BlockSpeed : BlockModel
    {
        public BlockSpeed()
        {
            Title = "Speed";
            Block = "models/blockmaker/speed/block.vmdl";
            Pole = "models/blockmaker/speed/pole.vmdl";
        }
    }

    public class BlockSlap : BlockModel
    {
        public BlockSlap()
        {
            Title = "Slap";
            Block = "models/blockmaker/slap/block.vmdl";
            Pole = "models/blockmaker/slap/pole.vmdl";
        }
    }

    public class BlockRandom : BlockModel
    {
        public BlockRandom()
        {
            Title = "Random";
            Block = "models/blockmaker/random/block.vmdl";
            Pole = "models/blockmaker/random/pole.vmdl";
        }
    }

    public class BlockNuke : BlockModel
    {
        public BlockNuke()
        {
            Title = "Nuke";
            Block = "models/blockmaker/nuke/block.vmdl";
            Pole = "models/blockmaker/nuke/pole.vmdl";
        }
    }

    public class BlockInvincibility : BlockModel
    {
        public BlockInvincibility()
        {
            Title = "Invincibility";
            Block = "models/blockmaker/invincibility/block.vmdl";
            Pole = "models/blockmaker/invincibility/pole.vmdl";
        }
    }

    public class BlockIce : BlockModel
    {
        public BlockIce()
        {
            Title = "Ice";
            Block = "models/blockmaker/ice/block.vmdl";
            Pole = "models/blockmaker/ice/pole.vmdl";
        }
    }

    public class BlockCamouflage : BlockModel
    {
        public BlockCamouflage()
        {
            Title = "Camouflage";
            Block = "models/blockmaker/camouflage/block.vmdl";
            Pole = "models/blockmaker/camouflage/pole.vmdl";
        }
    }

    public class BlockHoney : BlockModel
    {
        public BlockHoney()
        {
            Title = "Honey";
            Block = "models/blockmaker/honey/block.vmdl";
            Pole = "models/blockmaker/honey/pole.vmdl";
        }
    }

    public class BlockBarrier : BlockModel
    {
        public BlockBarrier()
        {
            Title = "Barrier";
            Block = "models/blockmaker/barrier/block.vmdl";
            Pole = "models/blockmaker/barrier/pole.vmdl";
        }
    }
}