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

        public bool isLeft = true;

        #region 主入口
        public void EvaluateNow()
        {
            if (!Pawn.Spawned || Pawn.Dead || !Pawn.IsPrisonerOfColony) return;
            ClearInvalidBills();
            if (Pawn.health.Dead) return; // 已经被摘死
            if (!NeedMoreBills()) return; // 已排够

            // 依次尝试：右肾→左肾→右肺→左肺→肝→心脏
            if (TryQueueKidney(isLeft)) return; // 右肾(index=1)左肾(index=0)
            if (TryQueueLung(isLeft)) return;   // 右肺(index=1)左肺(index=0)
            if (TryQueueLiver()) return;
            if (TryQueueHeart()) return; // 最后摘心脏
            QueueRest();//还没死就再走圈流程直接摘肺
        }
        #endregion

        #region 器官决策
        private bool TryQueueKidney(bool isLeft)
        {
            var kidneysAll = Pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def.defName == "Kidney").OrderBy(p => GetPartIndex(p)).ToList();
            if (kidneysAll.Count < 2) return false; // 至少要有两个肾脏

            var kidneyLeft = kidneysAll[0]; // 左肾index=0, 右肾index=1
            var kidneyRight = kidneysAll[1];
            BodyPartRecord kidney;

            BodyPartRecord first = isLeft ? kidneyLeft : kidneyRight;
            BodyPartRecord second = isLeft ? kidneyRight : kidneyLeft;

            if (MedicalRecipesUtility.IsCleanAndDroppable(Pawn, first))
            {
                kidney = first;
            }
            else if (MedicalRecipesUtility.IsCleanAndDroppable(Pawn, second))
            {
                kidney = second;
            }
            else
            {
                return false;
            }


            if (kidney == null) return false;

            AddBill(kidney);
            return true;
        }

        private bool TryQueueLung(bool isLeft)
        {
            var lungsAll = Pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def.defName == "Lung").OrderBy(p => GetPartIndex(p)).ToList();
            if (lungsAll.Count < 2) return false; // 至少要有两个肺
            var lungLeft = lungsAll[0]; // 左肺index=0, 右肺index=1
            var lungRight = lungsAll[1];
            BodyPartRecord lung;
            BodyPartRecord first = isLeft ? lungLeft : lungRight;
            BodyPartRecord second = isLeft ? lungRight : lungLeft;
            if (MedicalRecipesUtility.IsCleanAndDroppable(Pawn, first))
            {
                lung = first;
            }
            else if (MedicalRecipesUtility.IsCleanAndDroppable(Pawn, second))
            {
                lung = second;
            }
            else
            {
                return false;
            }
            if (lung == null) return false;
            AddBill(lung);
            return true;
        }

        private bool TryQueueLiver()
        {
            var liversAll = Pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def.defName == "Liver").OrderBy(p => GetPartIndex(p)).ToList();
            if (liversAll.Count < 1) return false; // 至少要有一个肝脏
            BodyPartRecord liver = null;
            // 遍历所有肝脏，找到第一个可用的
            foreach (var liverPart in liversAll)
            {
                if (MedicalRecipesUtility.IsCleanAndDroppable(Pawn, liverPart))
                {
                    liver = liverPart;
                    break;
                }
            }
            if (liver == null) return false;
            AddBill(liver);
            return true;
        }

        private bool TryQueueHeart()
        {
            var heartsAll = Pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def.defName == "Heart").OrderBy(p => GetPartIndex(p)).ToList();
            if (heartsAll.Count < 1) return false; // 至少要有一个心脏
            BodyPartRecord heart = null;
            // 遍历所有心脏，找到第一个可用的
            foreach (var heartPart in heartsAll)
            {
                if (MedicalRecipesUtility.IsCleanAndDroppable(Pawn, heartPart))
                {
                    heart = heartPart;
                    break;
                }
            }
            if (heart == null) return false;
            AddBill(heart);
            return true;
        }

        private void QueueRest()
        {
            if (TryQueueKidney(isLeft)) return; // 右肾(index=1)左肾(index=0)
            if (TryQueueLung(isLeft)) return;   // 右肺(index=1)左肺(index=0)
            if (TryQueueLiver()) return;

            //摘最后一个肺
            var lungsAll = Pawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def.defName == "Liver"&&MedicalRecipesUtility.IsCleanAndDroppable(Pawn,p))
                .OrderBy(p => GetPartIndex(p)).ToList();
            if (lungsAll.Count < 0) return; // 至少要有一个肺
            if (lungsAll[0] == null) return;
            AddBill(lungsAll[0]);
            return;

        }
        #endregion

        #region 工具方法
        private int GetPartIndex(BodyPartRecord part)
        {
            // 获取该部位在身体定义中的索引位置
            // 这样可以确保左右器官的判断一致性
            return Pawn.RaceProps.body.AllParts.IndexOf(part);
        }

        private void AddBill(BodyPartRecord part)
        {
            var bill = new Bill_Medical(RecipeDefOf.RemoveBodyPart, null) { Part = part };
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