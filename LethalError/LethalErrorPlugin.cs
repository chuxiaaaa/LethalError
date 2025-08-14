using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using GameNetcodeStuff;

using HarmonyLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Unity.Collections;
using Unity.Netcode;

using UnityEngine;

using YamlDotNet.Serialization.ObjectFactories;

using static UnityEngine.Scripting.GarbageCollector;

namespace LethalError
{
    [BepInPlugin(PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION)]
    public class LethalErrorPlugin : BaseUnityPlugin
    {
        public static LethalErrorConfig config { get; set; }

        public static ManualLogSource ManualLog = null;

        public static readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        public static readonly List<uint> VanillaPrefabs = new List<uint>
        {
            90896466,129367788,132513440,153348683,165354329,181065763,191913935,202426003,229852873,276978119,288960001,299647201,356651644,400294665,429829681,436137808,471591979,472780805,475937833,484661369,516507517,518371223,568460665,570495721,579959996,603300103,617338154,716660581,738627960,820092154,836821693,842876279,860552555,918740636,927582704,948167977,957872028,961522418,968052384,1062544579,1093242804,1144478515,1177294914,1190488538,1192602505,1208569611,1261883438,1290082603,1291577939,1324983542,1395910455,1398893398,1434649207,1455339477,1473878890,1519214796,1520713927,1525346363,1538715501,1545508553,1557375639,1562055063,1565614057,1585670844,1636750849,1645880361,1645994684,1669872752,1695918524,1708429739,1719037155,1721201679,1724127744,1727953579,1740344223,1808998928,1900020765,1910674915,1948025525,1974540975,1998237813,2023383400,2030693286,2133670236,2183144740,2234829884,2256688011,2270722052,2275085878,2292481783,2294211125,2353524689,2359790209,2437831202,2452351744,2453550126,2456900357,2470949680,2498931702,2513027068,2555293264,2561288184,2639144151,2661411798,2698950182,2733512605,2744692569,2754618432,2759639499,2784234648,2787274700,2825665221,2831235556,2915516243,2925526683,2949085139,2982659526,2995307756,3016010214,3018959210,3025586981,3036571934,3038553182,3051401024,3053807618,3060733885,3063950988,3074914178,3103629558,3115962917,3164721612,3238162239,3243961054,3299481859,3327936545,3330146594,3342949139,3369713594,3372165991,3419689129,3421797719,3429983800,3433351349,3433739795,3484633077,3499201763,3506119446,3508245293,3565211967,3567956311,3569922288,3583454401,3610174872,3612841114,3727661730,3775810923,3782272570,3804394268,3817036674,3854849053,3871319671,3915035863,3920935616,3943463324,3948016949,3959212616,3962754254,4086908256,4100675023,4119299708,4223888841,4288067500
        };
        public static ConfigEntry<bool> Debug { get; set; }

        public static string configPath { get; set; }

        public void Awake()
        {
            Debug = Config.Bind("LethalError", "Debug", false, "Write Mod Prefabs To File");
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            configPath = Utility.CombinePaths(fi.Directory.FullName, $"LethalError.Mods.yml");
            ManualLog = Logger;
            LoadConfig();
            _harmony.PatchAll(typeof(Patches));

        }

        public static void SaveConfig()
        {
            if (config.Mods == null)
            {
                config.Mods = new List<ModConfig>();
            }
            if (config.ModHashs == null)
            {
                config.ModHashs = new Dictionary<ulong, string>();
            }
            File.WriteAllText(configPath, SerializeHelper.YamlSerialize(config));
            ManualLog.LogInfo($"Save Mods Success({config.Mods.Count}|{config.VanillaHash})");
        }

        public static void LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                config = new LethalErrorConfig() { Mods = new List<ModConfig>() };
            }
            else
            {
                try
                {
                    config = SerializeHelper.YamlDeserialize<LethalErrorConfig>(File.ReadAllText(configPath));
                }
                catch (Exception ex)
                {
                    ManualLog.LogError($"Load Yaml Fail!\r\n{ex.ToString()}");
                    config = new LethalErrorConfig();
                }
            }
            if (config.Mods == null)
            {
                config.Mods = new List<ModConfig>();
            }
            if (config.ModHashs == null)
            {
                config.ModHashs = new Dictionary<ulong, string>();
            }
            ManualLog.LogInfo($"Load Mods Success({config.Mods.Count}|{config.VanillaHash})");
        }
    }




    public class Patches
    {
        private static Dictionary<ulong, ulong> ClientHash { get; set; } = new Dictionary<ulong, ulong>();
        private static Dictionary<ulong, bool> ClientDeserialize { get; set; } = new Dictionary<ulong, bool>();

        [HarmonyPatch(typeof(ConnectionRequestMessage), "Deserialize")]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void Postfix(ConnectionRequestMessage __instance, ref NetworkContext context)
        {
            if (ClientHash.ContainsKey(context.SenderId))
            {
                ClientHash[context.SenderId] = __instance.ConfigHash;
            }
            else
            {
                ClientHash.Add(context.SenderId, __instance.ConfigHash);
            }
            LethalErrorPlugin.ManualLog.LogInfo($"ConfigHash:{__instance.ConfigHash}|SenderId:{context.SenderId}");
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject()
        {
            if (LethalErrorPlugin.Debug.Value)
            {
                var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var debugPath = Utility.CombinePaths(fi.Directory.FullName, $"{NetworkManager.Singleton.NetworkConfig.GetConfig()}.txt");
                StringBuilder sb = new StringBuilder();
                var mod = new ModConfig();
                mod.ModName = "ChangeNameToTheMod";
                mod.Prefabs = new List<ModConfig.Prefab>();
                foreach (KeyValuePair<uint, NetworkPrefab> keyValuePair in NetworkManager.Singleton.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.OrderBy((KeyValuePair<uint, NetworkPrefab> x) => x.Key))
                {
                    uint key = keyValuePair.Key;
                    if (LethalErrorPlugin.VanillaPrefabs.Contains(key))
                    {
                        continue;
                    }
                    mod.Prefabs.Add(new ModConfig.Prefab()
                    {
                        Hash = key,
                        PrefabName = keyValuePair.Value.Prefab.name,
                        Nullable = false
                    });
                }
                File.WriteAllText(debugPath, SerializeHelper.YamlSerialize(new LethalErrorConfig() { Mods = new List<ModConfig>() { mod } }));
            }
        }

        private static readonly HashSet<ulong> delayedClients = new HashSet<ulong>();


        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkConnectionManager), "DisconnectClient")]
        [HarmonyWrapSafe]
        public static bool DisconnectClient(NetworkConnectionManager __instance, ulong clientId, string reason)
        {
            LethalErrorPlugin.ManualLog.LogInfo($"DisconnectClient:{clientId} reason:{reason}");
            if (!delayedClients.Contains(clientId) && string.IsNullOrWhiteSpace(reason))
            {
                LethalErrorPlugin.ManualLog.LogInfo($"Delay");
                StartOfRound.Instance.StartCoroutine(DelayedDisconnect(clientId));
                delayedClients.Add(clientId);
                return false;
            }
            return true;
        }

        private static IEnumerator DelayedDisconnect(ulong clientId)
        {
            yield return new WaitForSeconds(0.1f);
            if (!ClientHash.TryGetValue(clientId, out var hash))
            {
                NetworkManager.Singleton.DisconnectClient(clientId, null);
                yield break;
            }
            var networkHash = NetworkManager.Singleton.NetworkConfig.GetConfig();
            if (hash == LethalErrorPlugin.config.VanillaHash)
            {
                List<string> mods = new List<string>();
                var registeredPrefabKeys = NetworkManager.Singleton.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.Select(p => p.Key).ToHashSet();
                foreach (var mod in LethalErrorPlugin.config.Mods)
                {
                    bool modInstalled = true;

                    foreach (var prefab in mod.Prefabs)
                    {
                        // 如果 prefab 必须存在，但不在注册列表中 → 模组没装
                        if (!prefab.Nullable && !registeredPrefabKeys.Contains(prefab.Hash))
                        {
                            modInstalled = false;
                            break;
                        }
                        // 如果 prefab 是可缺的，就不用判断
                    }

                    if (modInstalled)
                    {
                        mods.Add(mod.ModName);
                    }
                }
                if (mods.Count == 0)
                {
                    NetworkManager.Singleton.DisconnectClient(clientId, "<size=14><color=red>Join Lobby Failed</color></size>\r\n\r\n<size=11>This lobby is modded, but you're vanilla.</size>");
                }
                else
                {
                    NetworkManager.Singleton.DisconnectClient(clientId, $"<size=14><color=red>Join Lobby Failed</color></size>\r\n\r\n<size=11>The following mods you're not installed:\r\n{string.Join(Environment.NewLine, mods)}</size>");
                    LethalErrorPlugin.ManualLog.LogInfo($"host install:{string.Join(",", mods)}");
                }
                yield break;
            }
            else if (hash == networkHash)
            {
                NetworkManager.Singleton.DisconnectClient(clientId, null);
                yield break;
            }
            else if (LethalErrorPlugin.config.ModHashs.TryGetValue(hash, out var existingReason))
            {
                LethalErrorPlugin.ManualLog.LogInfo($"Kick for {existingReason}");
                NetworkManager.Singleton.DisconnectClient(clientId, $"<size=14><color=red>Join Lobby Failed</color></size>\r\n\r\n<size=11>The following mods are not installed on the <color=red>host</color>:\r\n{existingReason}</size>");
                yield break;
            }
            foreach (var item in LethalErrorPlugin.config.Mods)
            {
                var nullablePrefabs = item.Prefabs.Where(p => p.Nullable).ToList();
                var nonNullablePrefabs = item.Prefabs.Where(p => !p.Nullable).Select(p => p.Hash).ToList();

                int n = nullablePrefabs.Count;
                int combinations = 1 << n;

                for (int i = 0; i < combinations; i++)
                {
                    var prefabNames = new List<string>();
                    for (int bit = 0; bit < n; bit++)
                        if ((i & (1 << bit)) != 0)
                            prefabNames.Add(nullablePrefabs[bit].PrefabName);

                    string combinationKey = prefabNames.Count > 0
                        ? $"{item.ModName} [{string.Join(", ", prefabNames)}]"
                        : item.ModName;

                    if (LethalErrorPlugin.config.ModHashs.Values.Contains(combinationKey))
                        continue;

                    var prefabsToInclude = new List<uint>(nonNullablePrefabs);
                    for (int bit = 0; bit < n; bit++)
                        if ((i & (1 << bit)) != 0)
                            prefabsToInclude.Add(nullablePrefabs[bit].Hash);

                    ulong calHash = GetConfig(prefabsToInclude);
                    LethalErrorPlugin.config.ModHashs.Add(calHash, combinationKey);
                    LethalErrorPlugin.ManualLog.LogInfo($"calHash:{calHash}|combinationKey:{combinationKey}|{string.Join(",", prefabsToInclude)}");
                    if (calHash == hash)
                    {
                        LethalErrorPlugin.SaveConfig();
                        LethalErrorPlugin.ManualLog.LogInfo($"Kick for {combinationKey}");
                        NetworkManager.Singleton.DisconnectClient(clientId, $"<size=14><color=red>Join Lobby Failed</color></size>\r\n\r\n<size=11>The following mods are not installed on the host:\r\n{combinationKey}</size>");
                        yield break;
                    }
                }
            }
            LethalErrorPlugin.SaveConfig();
            NetworkManager.Singleton.DisconnectClient(clientId, "<size=14><color=red>Join Lobby Failed</color></size>\r\n<size=11>Your installed mods don't match the server</size>");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkConnectionManager), "DisconnectRemoteClient")]
        [HarmonyWrapSafe]
        public static bool DisconnectRemoteClient(NetworkConnectionManager __instance, ulong clientId)
        {
            StartOfRound.Instance.localPlayerController.StartCoroutine(doDisconnectRemoteClient(__instance, clientId));
            LethalErrorPlugin.ManualLog.LogInfo($"DisconnectRemoteClient:{clientId}");
            return false;
        }

        public static IEnumerator doDisconnectRemoteClient(NetworkConnectionManager __instance, ulong clientId)
        {
            var MessageManager = AccessTools.DeclaredField(typeof(NetworkConnectionManager), "MessageManager").GetValue(__instance);
            AccessTools.DeclaredMethod(MessageManager.GetType(), "ProcessSendQueues").Invoke(MessageManager, null);
            yield return new WaitForSeconds(0.5f);
            AccessTools.DeclaredMethod(typeof(NetworkConnectionManager), "OnClientDisconnectFromServer").Invoke(__instance, new object[] { clientId });
        }

        public static ulong GetConfig(List<uint> prefabs)
        {
            var config = NetworkManager.Singleton.NetworkConfig;
            using (var fastBufferWriter = new FastBufferWriter(1024, Allocator.Temp, int.MaxValue))
            {
                fastBufferWriter.WriteValueSafe<ushort>(in config.ProtocolVersion, default(FastBufferWriter.ForPrimitives));
                fastBufferWriter.WriteValueSafe("15.0.0", false);
                var Prefabs = new List<uint>(config.Prefabs.NetworkPrefabOverrideLinks.Select(x => x.Value.SourcePrefabGlobalObjectIdHash));
                Prefabs.AddRange(prefabs);
                foreach (var item in Prefabs.OrderBy(x => x))
                {
                    fastBufferWriter.WriteValueSafe(in item, default);
                }
                fastBufferWriter.WriteValueSafe<uint>(in config.TickRate, default);
                fastBufferWriter.WriteValueSafe(in config.ConnectionApproval, default);
                fastBufferWriter.WriteValueSafe(in config.ForceSamePrefabs, default);
                fastBufferWriter.WriteValueSafe(in config.EnableSceneManagement, default);
                fastBufferWriter.WriteValueSafe(in config.EnsureNetworkVariableLengthSafety, default);
                fastBufferWriter.WriteValueSafe(in config.RpcHashSize, default);
                return fastBufferWriter.ToArray().Hash64();
            }
        }
    }
}
