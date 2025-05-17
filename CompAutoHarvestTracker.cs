using RimWorld;
using Verse;
using System.Linq;

namespace SmartHarvestOrganTax
{
    public class CompProperties_AutoHarvestTracker : CompProperties
    {
        public CompProperties_AutoHarvestTracker() => compClass = typeof(CompAutoHarvestTracker);
    }

    public class CompAutoHarvestTracker : ThingComp
    {
        private Pawn Pawn => (Pawn)parent;
        public CompAutoHarvestTracker() { }

        #region 主入口
        public void EvaluateNow()
        {
            if (!Pawn.Spawned || Pawn.Dead || !Pawn.IsPrisonerOfColony) return;
            ClearInvalidBills();
            if (Pawn.health.Dead) return; // 已经被摘死

            if (!NeedMoreBills()) return; // 已排够

            // 依次尝试：肾→肺→肝
            if (TryQueueKidney()) return;
            if (TryQueueLung()) return;
            TryQueueLiver();      // 最后一项可能致命
        }
        #endregion

        #region 器官决策
        private bool TryQueueKidney()
        {
            var kidneys = Pawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def.defName == "Kidney" && MedicalRecipesUtility.IsCleanAndDroppable(Pawn, p))
                .ToList();
            if (kidneys.Count != 2) return false;        // 少于 2 个就别下单
            AddBill("HarvestOrganKidney", kidneys.First());
            return true;
        }

        private bool TryQueueLung()
        {
            var lungs = Pawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def.defName == "Lung" && MedicalRecipesUtility.IsCleanAndDroppable(Pawn, p))
                .ToList();
            if (lungs.Count != 2) return false;
            AddBill("HarvestOrganLung", lungs.First());
            return true;
        }

        private bool TryQueueLiver()
        {
            var liver = Pawn.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(p => p.def.defName == "Liver" && MedicalRecipesUtility.IsCleanAndDroppable(Pawn, p));
            if (liver == null) return false;
            AddBill("HarvestOrganLiver", liver);
            return true;
        }
        #endregion

        #region 工具
        private void AddBill(string recipeDefName, BodyPartRecord part)
        {
            var recipe = DefDatabase<RecipeDef>.GetNamed(recipeDefName);
            var bill = new Bill_Medical(recipe, null) { Part = part };
            // 插到队列顶端，确保顺序
            Pawn.BillStack.AddBill(bill);
            Pawn.BillStack.Reorder(bill, 0);
        }

        private void ClearInvalidBills()
        {
            Pawn.BillStack.Bills.RemoveAll(b =>
            {
                if (!(b is Bill_Medical bm)) return false;
                if (bm.recipe.Worker is Recipe_RemoveBodyPart && bm.Part != null)
                    // 若该部位已不干净/已缺失则撤销
                    return !Pawn.health.hediffSet.GetNotMissingParts().Contains(bm.Part);
                return false;
            });
        }

        private bool NeedMoreBills()
            => !Pawn.BillStack.Bills.Any(b => b.recipe.Worker is Recipe_RemoveBodyPart);
        #endregion
    }

}
