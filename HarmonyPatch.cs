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

    /// <summary>给 Recipe_Surgery.ApplyOnPawn 打后缀</summary>
    [HarmonyPatch(typeof(Recipe_Surgery))]
    [HarmonyPatch(nameof(Recipe_Surgery.ApplyOnPawn),
        new Type[] {
            typeof(Pawn),               // pawn
            typeof(BodyPartRecord),     // part
            typeof(Pawn),               // billDoer
            typeof(List<Thing>),        // ingredients
            typeof(Bill)                // bill
        })]
    internal static class Patch_RecipeSurgery_ApplyOnPawn
    {
        private static void Postfix(Pawn pawn)
        {
            pawn.TryGetComp<CompAutoHarvestTracker>()?.EvaluateNow();
        }
    }
}
