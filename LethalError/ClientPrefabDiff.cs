using LethalError.Lang;

using Steamworks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Unity.Netcode;

using static Netcode.Transports.Facepunch.FacepunchTransport;

namespace LethalError
{
    public static class ClientPrefabDiff
    {
        private static Dictionary<ulong, (string modName, string prefabName)> map;
        public static void Init()
        {
            BuildMap();
        }

        public static void BuildMap()
        {
            map = new Dictionary<ulong, (string, string)>();
            if (LethalErrorPlugin.config?.Mods == null) return;

            foreach (var mod in LethalErrorPlugin.config.Mods)
            {
                if (mod.Prefabs == null) continue;
                foreach (var prefab in mod.Prefabs)
                {
                    map[prefab.Hash] = (mod.ModName, prefab.PrefabName);
                }
            }
        }

        public static void Process(List<(uint hash, string name)> serverList)
        {
            Init();
            var clientHashes = NetworkManager.Singleton.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks
                   .Select(p => p.Key)
                   .ToHashSet();

            var serverSet = serverList.ToDictionary(x => x.hash, x => x.name);

            var missingOnClientHashes = serverSet.Keys.Except(clientHashes).ToList();

            var missingOnServerHashes = clientHashes.Except(serverSet.Keys).ToList();

            if (!missingOnClientHashes.Any() && !missingOnServerHashes.Any())
            {
                // 完全匹配，无需操作
                return;
            }
            var missingOnServerList = ResolveNames(missingOnServerHashes, serverSet);
            var missingOnClientList = ResolveNames(missingOnClientHashes, serverSet);
            string missingOnServerStr = string.Join("\n", missingOnServerList);
            string missingOnClientStr = string.Join("\n", missingOnClientList);
            GameNetworkManager.Instance.Disconnect();
            LethalErrorPlugin.ManualLog.LogWarning(LocalText.GetText("ModMismatch2", missingOnServerStr, missingOnClientStr));
        }

        private static List<string> ResolveNames(List<uint> hashes, Dictionary<uint, string> serverMap)
        {
            var result = new List<string>();

            foreach (var h in hashes)
            {
                ulong hashLong = h;
                string displayName;

                if (map.TryGetValue(hashLong, out var info))
                {
                    // 能匹配到模组：显示 "模组名 (预制体名)"
                    displayName = $"{info.modName} ({info.prefabName})";
                }
                else
                {
                    // 无法匹配：尝试显示服务器传来的预制体名
                    if (serverMap.TryGetValue(h, out var serverPrefabName))
                    {
                        displayName = $"Unknown Mod: {serverPrefabName}";
                    }
                    else
                    {
                        displayName = $"Unknown Prefab (Hash: {h})";
                    }
                }
                result.Add(displayName);
            }

            return result.Distinct().ToList();
        }


        private static List<string> ResolveExtra(List<uint> hashes)
        {
            var result = new List<string>();

            foreach (var h in hashes)
            {
                if (TryGetModName(h, out var mod))
                    result.Add(mod);
                else
                    result.Add($"UnknownPrefab({h})");
            }

            return result.Distinct().ToList();
        }

        private static bool TryGetModName(ulong hash, out string modName)
        {
            foreach (var mod in LethalErrorPlugin.config.Mods)
            {
                foreach (var prefab in mod.Prefabs)
                {
                    if (prefab.Hash == hash)
                    {
                        modName = mod.ModName;
                        return true;
                    }
                }
            }

            modName = null;
            return false;
        }
    }
}
