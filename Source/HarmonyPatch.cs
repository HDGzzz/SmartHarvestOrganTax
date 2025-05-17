using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;

namespace SmartHarvestOrganTax
{
    /// <summary>自动打所有补丁</summary>
    [StaticConstructorOnStartup]
    public static class AutoHarvestHarmonyInit
    {
        static AutoHarvestHarmonyInit()
        {
            new Harmony("SmartHarvestOrganTax").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Recipe_RemoveBodyPart), "ApplyOnPawn")]
    public static class Patch_RemoveBodyPart_ApplyOnPawn
    {
        public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer)
        {
            pawn?.TryGetComp<CompAutoHarvestTracker>()?.EvaluateNow();
        }
    }

}
