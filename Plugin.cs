using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaSteps
{
    [BepInPlugin("zaynethedev.gorillasteps", "GorillaSteps", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private bool inRoom, isInit;
        public static GorillaHuntComputer huntMan;
        private List<GameObject> huntObjects = new List<GameObject>();
        public static ConfigEntry<int> savedSteps;

        void Awake()
        {
            savedSteps = Config.Bind("General", "SavedSteps", 0, "The number of steps taken, saved between sessions.");
            RigSerializerPatches.steps = savedSteps.Value;

        }

        void Start()
        {
            var harmony = Harmony.CreateAndPatchAll(GetType().Assembly, "zaynethedev.gorillasteps");
            var vrRigSerializerType = typeof(GorillaTagger).Assembly.GetType("VRRigSerializer");
            harmony.Patch(AccessTools.Method(vrRigSerializerType, "OnHandTapRPCShared"), postfix: new HarmonyMethod(typeof(RigSerializerPatches), nameof(RigSerializerPatches.GetOnHandTap)));
            GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        }

        void OnGameInitialized()
        {
            huntMan = GorillaTagger.Instance.offlineVRRig.huntComputer.GetComponent<GorillaHuntComputer>();
            huntObjects.Add(huntMan.badge.gameObject);
            huntObjects.Add(huntMan.leftHand.gameObject);
            huntObjects.Add(huntMan.rightHand.gameObject);
            huntObjects.Add(huntMan.hat.gameObject);
            huntObjects.Add(huntMan.face.gameObject);
            huntMan.text.text = $"STEPS: {RigSerializerPatches.steps}";
            isInit = true;
        }

        void Update()
        {
            if (isInit)
            {
                if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.GameModeString.Contains("MODDED") && !NetworkSystem.Instance.GameModeString.Contains("HUNT"))
                {
                    if (!inRoom)
                    {
                        inRoom = true;
                    }
                    huntMan.enabled = false;
                    GorillaTagger.Instance.offlineVRRig.EnableHuntWatch(true);
                    foreach (GameObject obj in huntObjects)
                    {
                        obj.SetActive(false);
                    }
                }
                else
                {
                    if (inRoom)
                    {
                        inRoom = false;
                    }
                    huntMan.enabled = false;
                    GorillaTagger.Instance.offlineVRRig.EnableHuntWatch(false);
                    foreach (GameObject obj in huntObjects)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
    }

    [HarmonyPatch]
    public class RigSerializerPatches
    {
        public static int steps;
        public static void GetOnHandTap(int surfaceIndex, bool leftHanded, float handSpeed, long packedDir, PhotonMessageInfoWrapped info)
        {
            steps++;
            Plugin.huntMan.text.text = $"STEPS: {steps}";
            Plugin.savedSteps.Value = steps;
        }
    }
}