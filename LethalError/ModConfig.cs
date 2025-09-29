using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalError
{
    public class LethalErrorConfig
    {
        public List<ModConfig> Mods { get; set; } = new List<ModConfig>();

        public ulong? VanillaHash { get; set; }

        public Dictionary<ulong, string> ModHashs { get; set; } = new Dictionary<ulong, string>();
    }

    public class ModConfig
    {
        public string ModName { get; set; }
        public List<Prefab> Prefabs { get; set; }
        public class Prefab
        {
            public string PrefabName { get; set; }
            public uint Hash { get; set; }
            public bool Nullable { get; set; }
        }
    }
}
