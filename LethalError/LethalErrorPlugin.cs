using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using GameNetcodeStuff;

using HarmonyLib;

using LethalError.Lang;

using Steamworks.Ugc;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            90896466,129367788,132513440,153348683,165354329,181065763,191913935,202426003,229852873,276978119,288960001,299647201,356651644,400294665,429829681,436137808,471591979,472780805,475937833,484661369,516507517,518371223,568460665,570495721,579959996,603300103,617338154,716660581,738627960,820092154,836821693,842876279,860552555,918740636,927582704,948167977,957872028,961522418,968052384,1062544579,1093242804,1144478515,1177294914,1190488538,1192602505,1208569611,1261883438,1290082603,1291577939,1324983542,1395910455,1398893398,1434649207,1455339477,1473878890,1519214796,1520713927,1525346363,1538715501,1545508553,1557375639,1562055063,1565614057,1585670844,1636750849,1645880361,1645994684,1669872752,1695918524,1708429739,1719037155,1721201679,1724127744,1727953579,1740344223,1808998928,1900020765,1910674915,1948025525,1974540975,1998237813,2023383400,2030693286,2133670236,2183144740,2234829884,2256688011,2270722052,2275085878,2292481783,2294211125,2353524689,2359790209,2437831202,2452351744,2453550126,2456900357,2470949680,2498931702,2513027068,2555293264,2561288184,2639144151,2661411798,2698950182,2733512605,2744692569,2754618432,2759639499,2784234648,2787274700,2825665221,2831235556,2915516243,2925526683,2949085139,2982659526,2995307756,3016010214,3018959210,3025586981,3036571934,3038553182,3051401024,3053807618,3060733885,3063950988,3074914178,3103629558,3115962917,3164721612,3238162239,3243961054,3299481859,3327936545,3330146594,3342949139,3369713594,3372165991,3419689129,3421797719,3429983800,3433351349,3433739795,3484633077,3499201763,3506119446,3508245293,3565211967,3567956311,3569922288,3583454401,3610174872,3612841114,3727661730,3775810923,3782272570,3804394268,3817036674,3854849053,3871319671,3915035863,3920935616,3943463324,3948016949,3959212616,3962754254,4086908256,4100675023,4119299708,4223888841,4288067500,178332144,218316246,1011693577,1225366653,3123575941,3706709186,3884775518,4063793843
        };



        public static ConfigEntry<bool> Debug { get; set; }

        public static ConfigEntry<Lang.LocalText.Language> Lang { get; set; }

        public static string configPath { get; set; }
        private FileSystemWatcher _watcher;



        public void Awake()
        {
            Debug = Config.Bind("LethalError", "Debug", false, "Write Mod Prefabs To File");
            Lang = Config.Bind("LethalError", "Language", LethalError.Lang.LocalText.Language.Auto);
            string cfgPath = Config.ConfigFilePath;
            string dir = Path.GetDirectoryName(cfgPath);
            string file = Path.GetFileName(cfgPath);
            _watcher = new FileSystemWatcher(dir, file);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime;
            _watcher.Changed += (s, e) =>
            {
                System.Threading.Thread.Sleep(50);
                try
                {
                    Config.Reload();
                    SetLanguage();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Reload config failed: {ex}");
                }
            };
            _watcher.EnableRaisingEvents = true;
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            configPath = Utility.CombinePaths(fi.Directory.FullName, $"LethalError.Mods.yml");
            ManualLog = Logger;
            LoadConfig();
            _harmony.PatchAll(typeof(Patches));
            _harmony.PatchAll(typeof(TimeoutPatches));

        }

        private static void SetLanguage()
        {
            if (Lang.Value == LethalError.Lang.LocalText.Language.Auto)
            {
                var lang = System.Globalization.CultureInfo.CurrentCulture.Name.ToLower();
                if (lang.StartsWith("zh-"))
                {
                    LocalText.CurrentLanguage = LocalText.Language.Chinese;
                }
                else
                {
                    LocalText.CurrentLanguage = LocalText.Language.English;
                }
            }
            else
            {
                LocalText.CurrentLanguage = Lang.Value;
            }
        }

        private void Lang_SettingChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public static void SaveConfig()
        {
            if (config.Mods == null)
            {
                config.Mods = new List<ModConfig>();
            }
            File.WriteAllText(configPath, SerializeHelper.YamlSerialize(config));
            ManualLog.LogInfo($"Save Mods Success({config.Mods.Count}");
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
            SetLanguage();
            ManualLog.LogInfo($"Load Mods Success({config.Mods.Count})");
        }
    }


    public static class TimeoutPatches
    {
        static MethodBase TargetMethod()
        {
            var stateMachineType = AccessTools.Inner(typeof(NetworkConnectionManager), "<ApprovalTimeout>d__56");
            return AccessTools.Method(stateMachineType, "MoveNext");
        }

        [HarmonyPatch]
        class ApprovalTimeoutPatch
        {
            static MethodBase TargetMethod()
            {
                var type = AccessTools.Inner(typeof(NetworkConnectionManager), "<ApprovalTimeout>d__56");
                return AccessTools.Method(type, "MoveNext");
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = new List<CodeInstruction>(instructions);

                var disconnectMethod = AccessTools.Method(
                    typeof(NetworkConnectionManager),
                    "DisconnectClient",
                    new Type[] { typeof(ulong), typeof(string) });

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Calls(disconnectMethod))
                    {
                        // 在 DisconnectClient 前插入
                        list.InsertRange(i, new[]
                        {
                            new CodeInstruction(OpCodes.Ldarg_0), // stateMachine
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ApprovalTimeoutPatch), nameof(OnTimeout)))
                        });

                        break;
                    }
                }

                return list;
            }

            static void OnTimeout(object stateMachine)
            {
                ulong clientId = (ulong)AccessTools.Field(stateMachine.GetType(), "clientId").GetValue(stateMachine);

                if (!Patches.TimeoutClients.Contains(clientId))
                {
                    LethalErrorPlugin.ManualLog.LogInfo($"客户端连接审批超时: {clientId}");
                    Patches.TimeoutClients.Add(clientId);
                }
            }
        }
    }


    public class Patches
    {

        [HarmonyPatch(typeof(NetworkManager), "SetSingleton")]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.VeryLow)]
        public static void SetSingleton(NetworkManager __instance)
        {
            ClientHash = new Dictionary<ulong, ulong>();
            TimeoutClients = new HashSet<ulong>();
            DelayedClients = new HashSet<ulong>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.Initialize))]
        private static void AfterInitialize()
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                  MsgPrefabSync,
                  OnReceivePrefabList
              );
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "SetInstanceValuesBackToDefault")]
        public static void SetInstanceValuesBackToDefault()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null)
                return;
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(MsgPrefabSync);
        }

        private static Dictionary<ulong, ulong> ClientHash { get; set; } = new Dictionary<ulong, ulong>();

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
            LethalErrorPlugin.ManualLog.LogInfo($"ConfigHash:{__instance.ConfigHash}|SenderId:{context.SenderId}|Server:{NetworkManager.Singleton.NetworkConfig.GetConfig()}");
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void ConnectClientToPlayerObject()
        {
            if (LethalErrorPlugin.Debug.Value)
            {
                StringBuilder log = new StringBuilder();
                log.AppendLine();
                log.AppendLine();
                log.AppendLine($"ProtocolVersion:{NetworkManager.Singleton.NetworkConfig.ProtocolVersion}");
                log.AppendLine($"15.0.0");
                log.AppendLine($"ForceSamePrefabs:{NetworkManager.Singleton.NetworkConfig.ForceSamePrefabs}");
                var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
                ulong hash = NetworkManager.Singleton.NetworkConfig.GetConfig();
                var debugPath = Utility.CombinePaths(fi.Directory.FullName, $"{hash}.txt");
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
                    log.AppendLine($"{keyValuePair.Value.Prefab.name}:{key}");
                    mod.Prefabs.Add(new ModConfig.Prefab()
                    {
                        Hash = key,
                        PrefabName = keyValuePair.Value.Prefab.name,
                        Nullable = false
                    });
                }
                log.AppendLine($"TickRate:{NetworkManager.Singleton.NetworkConfig.TickRate}");
                log.AppendLine($"ConnectionApproval:{NetworkManager.Singleton.NetworkConfig.ConnectionApproval}");
                log.AppendLine($"ForceSamePrefabs:{NetworkManager.Singleton.NetworkConfig.ForceSamePrefabs}");
                log.AppendLine($"EnableSceneManagement:{NetworkManager.Singleton.NetworkConfig.EnableSceneManagement}");
                log.AppendLine($"EnsureNetworkVariableLengthSafety:{NetworkManager.Singleton.NetworkConfig.EnsureNetworkVariableLengthSafety}");
                log.AppendLine($"RpcHashSize:{NetworkManager.Singleton.NetworkConfig.RpcHashSize}");
                LethalErrorPlugin.ManualLog.LogInfo($"NetworkConfigValue:{hash}{log.ToString()}");
                File.WriteAllText(debugPath, SerializeHelper.YamlSerialize(new LethalErrorConfig() { Mods = new List<ModConfig>() { mod } }));
            }
        }

        private static HashSet<ulong> DelayedClients = new HashSet<ulong>();
        public static HashSet<ulong> TimeoutClients = new HashSet<ulong>();


        

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkConnectionManager), "DisconnectClient")]
        [HarmonyWrapSafe]
        public static bool DisconnectClient(NetworkConnectionManager __instance, ulong clientId, string reason)
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.IsHost)
            {
                LethalErrorPlugin.ManualLog.LogInfo($"DisconnectClient:{clientId} reason:{reason}");
                if (!DelayedClients.Contains(clientId) && string.IsNullOrWhiteSpace(reason))
                {
                    LethalErrorPlugin.ManualLog.LogInfo($"Delay");
                    StartOfRound.Instance.StartCoroutine(DelayedDisconnect(clientId));
                    DelayedClients.Add(clientId);
                    return false;
                }
            }
            return true;
        }

        private const string MsgPrefabSync = "LethalError_PrefabSync";


        public struct PrefabData
        {
            public uint hash;
            public string prefabName;
        }

        private static IEnumerator DelayedDisconnect(ulong clientId)
        {
            yield return new WaitForSeconds(0.1f);
            if (TimeoutClients.Contains(clientId))
            {
                NetworkManager.Singleton.DisconnectClient(clientId, $"{$"<size=0>LethalError|Timeout</size>"}{LocalText.GetText("Timeout")}");
                yield break;
            }
            if (!ClientHash.TryGetValue(clientId, out var hash))
            {
                NetworkManager.Singleton.DisconnectClient(clientId, null);
                yield break;
            }
            var networkHash = NetworkManager.Singleton.NetworkConfig.GetConfig();
            if (hash == networkHash)
            {
                NetworkManager.Singleton.DisconnectClient(clientId, "");
                yield break;
            }
            else
            {
                var prefabData = NetworkManager.Singleton.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks
                  .Select(p => new PrefabData()
                  {
                      hash = p.Key,
                      prefabName = p.Value.Prefab.name
                  }).ToList();

                SendPrefabList(clientId, prefabData);
                yield return new WaitForSeconds(1f);
                LethalErrorPlugin.ManualLog.LogInfo(
                    $"Kick for ModMismatch|Server:{networkHash}|Client:{hash}");
                NetworkManager.Singleton.DisconnectClient(
                    clientId,
                    $"<size=0>LethalError|ModMismatch</size>{LocalText.GetText("ModMismatch")}"
                );
            }
        }

        private const int SAFE_PAYLOAD_LIMIT = 900; // 单包安全负载上限

        // --- 接收端缓存结构 ---
        // 用于暂存未完成的批次数据
        // Key: senderClientId (防止不同客户端的数据混在一起)
        // Value: 重组上下文对象
        private static readonly Dictionary<ulong, ReassemblyContext> s_reassemblyCache = new Dictionary<ulong, ReassemblyContext>();

        /// <summary>
        /// 发送预制体列表（带批次信息的自动分包）
        /// </summary>
        public static void SendPrefabList(ulong clientId, List<PrefabData> list)
        {
            if (list == null || list.Count == 0) return;

          
            int totalBatches = 0;
            int index = 0;

            // 临时计算总批次数
            while (index < list.Count)
            {
                int currentBatchCount = 0;
                int currentSizeEstimate = 0;
                int sizeForHeaders = 8; // 预留：Count(2) + BatchIndex(2) + TotalBatches(2) + 余量

                while (index + currentBatchCount < list.Count)
                {
                    var item = list[index + currentBatchCount];
                    int itemSize;
                    using (var tempWriter = new FastBufferWriter(1024, Allocator.Temp, 4096))
                    {
                        BytePacker.WriteValueBitPacked(tempWriter, item.hash);
                        BytePacker.WriteValuePacked(tempWriter, item.prefabName);
                        itemSize = tempWriter.Position;
                    }

                    if (currentBatchCount > 0 && (currentSizeEstimate + sizeForHeaders + itemSize > SAFE_PAYLOAD_LIMIT))
                        break;

                    currentSizeEstimate += itemSize;
                    currentBatchCount++;
                }

                if (currentBatchCount == 0) currentBatchCount = 1;

                index += currentBatchCount;
                totalBatches++;
            }

            // 第二步：正式发送数据
            index = 0;
            int currentBatchIndex = 0;

            while (index < list.Count)
            {
                int currentBatchCount = 0;
                int currentSizeEstimate = 0;
                int sizeForHeaders = 8;

                // 再次计算当前包能装多少（逻辑同上）
                while (index + currentBatchCount < list.Count)
                {
                    var item = list[index + currentBatchCount];
                    int itemSize;
                    using (var tempWriter = new FastBufferWriter(1024, Allocator.Temp, 4096))
                    {
                        BytePacker.WriteValueBitPacked(tempWriter, item.hash);
                        BytePacker.WriteValuePacked(tempWriter, item.prefabName);
                        itemSize = tempWriter.Position;
                    }

                    if (currentBatchCount > 0 && (currentSizeEstimate + sizeForHeaders + itemSize > SAFE_PAYLOAD_LIMIT))
                        break;

                    currentSizeEstimate += itemSize;
                    currentBatchCount++;
                }

                if (currentBatchCount == 0) currentBatchCount = 1;

                // 创建 Writer
                using (var writer = new FastBufferWriter(currentSizeEstimate + 20, Allocator.Temp, 1024 * 100))
                {
                    BytePacker.WriteValueBitPacked(writer, currentBatchCount);
                    BytePacker.WriteValueBitPacked(writer, currentBatchIndex);
                    BytePacker.WriteValueBitPacked(writer, totalBatches);

                    for (int j = 0; j < currentBatchCount; j++)
                    {
                        var item = list[index + j];
                        BytePacker.WriteValueBitPacked(writer, item.hash);
                        BytePacker.WriteValuePacked(writer, item.prefabName);
                    }

                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                        MsgPrefabSync,
                        clientId,
                        writer,
                        NetworkDelivery.Reliable
                    );
                }

                index += currentBatchCount;
                currentBatchIndex++;
            }
        }

        /// <summary>
        /// 接收预制体列表（带重组逻辑）
        /// </summary>
        public static void OnReceivePrefabList(ulong senderClientId, FastBufferReader reader)
        {
            // 1. 读取头部信息
            ByteUnpacker.ReadValueBitPacked(reader, out int count);
            ByteUnpacker.ReadValueBitPacked(reader, out int batchIndex);
            ByteUnpacker.ReadValueBitPacked(reader, out int totalBatches);
            // 2. 获取或创建该客户端的重组上下文
            if (!s_reassemblyCache.TryGetValue(senderClientId, out var context))
            {
                context = new ReassemblyContext(totalBatches);
                s_reassemblyCache[senderClientId] = context;
            }

            // 如果总批次数量发生变化（例如发送端数据变了，但旧缓存还在），重置上下文
            if (context.TotalBatches != totalBatches)
            {
                context.Reset(totalBatches);
            }

            // 3. 读取当前批次的数据
            var batchList = new List<(uint hash, string name)>(count);
            for (int i = 0; i < count; i++)
            {
                ByteUnpacker.ReadValueBitPacked(reader, out uint hash);
                ByteUnpacker.ReadValuePacked(reader, out string name);
                batchList.Add((hash, name));
            }

            // 4. 存入缓存
            context.StoreBatch(batchIndex, batchList);

            // 5. 检查是否收齐
            if (context.IsComplete())
            {
                // 合并所有批次数据
                var finalList = new List<(uint hash, string name)>();
                foreach (var batch in context.Batches)
                {
                    if (batch != null) finalList.AddRange(batch);
                }

                // 统一处理
                ClientPrefabDiff.Process(finalList);

                // 清理缓存
                s_reassemblyCache.Remove(senderClientId);
            }
        }

        // --- 辅助类：用于管理重组状态 ---
        private class ReassemblyContext
        {
            public int TotalBatches { get; private set; }
            public List<(uint hash, string name)>[] Batches { get; private set; }
            private int _receivedCount;

            public ReassemblyContext(int totalBatches)
            {
                Reset(totalBatches);
            }

            public void Reset(int totalBatches)
            {
                TotalBatches = totalBatches;
                Batches = new List<(uint hash, string name)>[totalBatches];
                _receivedCount = 0;
            }

            public void StoreBatch(int index, List<(uint hash, string name)> data)
            {
                if (index >= 0 && index < TotalBatches)
                {
                    // 防止重复接收同一批次覆盖数据（虽然 Reliable 模式下很少见）
                    if (Batches[index] == null)
                    {
                        Batches[index] = data;
                        _receivedCount++;
                    }
                }
            }

            public bool IsComplete()
            {
                return _receivedCount == TotalBatches;
            }
        }

 
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuManager), "DisplayMenuNotification")]
        [HarmonyWrapSafe]
        public static bool DisplayMenuNotification(ref string notificationText)
        {
            var match = Regex.Match(notificationText, @"<size=0>(.*?)</size>");
            if (match.Success)
            {
                string hiddenContent = match.Groups[1].Value;
                string[] parts = hiddenContent.Split('|');
                if (parts.Length >= 2)
                {
                    string messageType = parts[0];
                    string localizationKey = parts[1];
                    string parameters = parts.Length > 2 ? parts[2] : "";

                    switch (messageType)
                    {
                        case "LethalError":
                            notificationText = LocalText.GetText(localizationKey, parameters);
                            return true;
                        default:
                            return true;
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkConnectionManager), "DisconnectRemoteClient")]
        [HarmonyWrapSafe]
        public static bool DisconnectRemoteClient(NetworkConnectionManager __instance, ulong clientId)
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.IsHost)
            {
                StartOfRound.Instance.localPlayerController.StartCoroutine(doDisconnectRemoteClient(__instance, clientId));
                LethalErrorPlugin.ManualLog.LogInfo($"DisconnectRemoteClient:{clientId}");
                return false;
            }
            return true;
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
