using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;

namespace SmartHarvestOrganTax
{
    /// <summary>打所有补丁</summary>
    [StaticConstructorOnStartup]
    public static class AutoHarvestHarmonyInit
    {
        static AutoHarvestHarmonyInit()
        {
            new Harmony("SmartHarvestOrganTax").PatchAll();
        }
    }

    // 让“取消”光标可以点到带组件的殖民者
    [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.CanDesignateThing))]
    public static class Patch_Cancel_CanDesignateThing
    {
        static void Postfix(ref AcceptanceReport __result, Thing t)
        {
            if (!__result.Accepted && t is Pawn pawn && pawn.TryGetComp<CompAutoHarvestTracker>() != null)
                __result = true;
        }
    }

    // 卸掉组件
    [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.DesignateThing))]
    public static class Patch_Cancel_DesignateThing
    {
        static void Postfix(Thing t)
        {
            if (t is Pawn pawn)
            {
                Designator_SmartHarvestOrganTax.RemoveTracking(pawn);
            }
        }
    }

    // Patch手术完成事件，无论成功失败都会触发
    [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
    public static class Patch_Surgery_CheckSurgeryFail
    {
        public static void Postfix(Recipe_Surgery __instance, Pawn surgeon, Pawn patient, bool __result)
        {
            // 无论手术成功还是失败，都重新评估
            patient?.TryGetComp<CompAutoHarvestTracker>()?.EvaluateNow();
        }
    }

    // 额外的保险：Patch账单完成
    [HarmonyPatch(typeof(Bill_Medical), "Notify_IterationCompleted")]
    public static class Patch_Bill_Medical_Notify_IterationCompleted
    {
        public static void Postfix(Bill_Medical __instance, Pawn billDoer, List<Thing> ingredients)
        {
            // 如果是摘除器官的手术
            if (__instance.recipe.Worker is Recipe_RemoveBodyPart)
            {
                // 从账单堆栈的拥有者获取pawn
                if (__instance.billStack?.billGiver is Pawn pawn)
                {
                    pawn.TryGetComp<CompAutoHarvestTracker>()?.EvaluateNow();
                }
            }
        }
    }


    // 最后的保险：直接Patch手术结束
    [HarmonyPatch(typeof(Recipe_RemoveBodyPart), nameof(Recipe_RemoveBodyPart.ApplyOnPawn))]
    public static class Patch_Surgery_ApplyOnPawn
    {
        public static void Postfix(Recipe_Surgery __instance, Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            // 只针对摘除器官手术
            if (__instance is Recipe_RemoveBodyPart)
            {
                pawn?.TryGetComp<CompAutoHarvestTracker>()?.EvaluateNow();
            }
        }
    }

}