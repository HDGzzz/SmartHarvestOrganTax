using RimWorld;
using System;
using System.Linq;
using System.Runtime;
using Verse;

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

        private bool _disabled = false;

        #region 主入口
        public void EvaluateNow()
        {
            if ( Pawn.Dead || !Pawn.IsPrisonerOfColony)
            {
                _disabled = true; // 跳过后续 tick
                Designator_SmartHarvestOrganTax.RemoveTracking(Pawn);
                return;
            }
            //ClearInvalidBills();
            //if (Pawn.health.Dead) return; 
            if (AutoHarvestMod.settings.IsChangeMedCare)
            {
                Pawn.playerSettings.medCare = AutoHarvestMod.settings.surgeryMedCare;

            }

            if (!NeedMoreBills()) return; // 有正在摘的了
            _disabled = false;
            // 依次尝试：肾→肺→根据设置决定肝脏和心脏的优先级
            if (TryQueueKidney(AutoHarvestMod.settings.IsLeft)) return; // 左肾(index=0)右肾(index=1)
            if (TryQueueLung(AutoHarvestMod.settings.IsLeft)) return;   // 左肺(index=0)右肺(index=1)

            // 根据设置决定肝脏和心脏的摘取顺序


            //var (firstOrgan, secondOrgan) = AutoHarvestMod.settings.IsLiverFirst
            //    ? ((Func<bool>)TryQueueLiver, (Func<bool>)TryQueueHeart)
            //    : ((Func<bool>)TryQueueHeart, (Func<bool>)TryQueueLiver);

            //if (firstOrgan()) return;
            //if (secondOrgan()) return;

            if (AutoHarvestMod.settings.IsLiverFirst ? TryQueueLiver() : TryQueueHeart()) return;
            if (AutoHarvestMod.settings.IsLiverFirst ? TryQueueHeart() : TryQueueLiver()) return;

            QueueRest();//还没死就再走圈流程直接摘肺
        }
        #endregion

        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(300)) // 300 tick 检查一次
            {
                if (_disabled) return;
                EvaluateNow();
            }
        }

        #region 器官决策
        private bool TryQueueKidney(bool isLeft, bool? ignoreRemainingCheck = null)
        {
            var kidneysAll = Pawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def.defName == "Kidney")
                .OrderBy(p => GetPartIndex(p))
                .ToList();

            if (!(ignoreRemainingCheck ?? false) && kidneysAll.Count < 2)
                return false; // 默认检查：至少要有两个肾脏

            // 如果忽略检查或只剩一个肾，也允许继续尝试
            var kidneyLeft = kidneysAll.FirstOrDefault();
            var kidneyRight = kidneysAll.Skip(1).FirstOrDefault();

            BodyPartRecord first = isLeft ? kidneyLeft : kidneyRight;
            BodyPartRecord second = isLeft ? kidneyRight : kidneyLeft;

            BodyPartRecord kidney = null;

            if (first != null && MedicalRecipesUtility.IsCleanAndDroppable(Pawn, first))
            {
                kidney = first;
            }
            else if (second != null && MedicalRecipesUtility.IsCleanAndDroppable(Pawn, second))
            {
                kidney = second;
            }
            else
            {
                return false;
            }

            AddBill(kidney);
            return true;
        }

        private bool TryQueueLung(bool isLeft, bool? ignoreRemainingCheck = null)
        {
            var lungsAll = Pawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def.defName == "Lung")
                .OrderBy(p => GetPartIndex(p))
                .ToList();

            if (!(ignoreRemainingCheck ?? false) && lungsAll.Count < 2)
                return false; // 默认要求两个肺

            var lungLeft = lungsAll.FirstOrDefault();
            var lungRight = lungsAll.Skip(1).FirstOrDefault();

            BodyPartRecord first = isLeft ? lungLeft : lungRight;
            BodyPartRecord second = isLeft ? lungRight : lungLeft;

            BodyPartRecord lung = null;

            if (first != null && MedicalRecipesUtility.IsCleanAndDroppable(Pawn, first))
            {
                lung = first;
            }
            else if (second != null && MedicalRecipesUtility.IsCleanAndDroppable(Pawn, second))
            {
                lung = second;
            }
            else
            {
                return false;
            }

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

        /// <summary>
        /// 依次为 Pawn 排队摘除剩余高价值器官。  
        /// 先尝试在“安全阈值”内摘肾、肺、肝；  
        /// 若只剩 1 肾 + 1 肺且肝/心不可摘，则先排最后一只肺，随后继续排剩余肾，  
        /// 以便在拥有「不死基因」的情况下继续榨取价值。
        /// </summary>
        private void QueueRest()
        {
            // 1 先摘「多余」的肾（保证至少留 1 个）
            if (TryQueueKidney(AutoHarvestMod.settings.IsLeft))
                return;

            // 2 再摘「多余」的肺（保证至少留 1 个）
            if (TryQueueLung(AutoHarvestMod.settings.IsLeft))
                return;

            // 3 尝试摘肝
            if (TryQueueLiver())
                return;

            // 4️⃣ 摘最后一只肺（忽略数量限制）
            if(TryQueueLung(AutoHarvestMod.settings.IsLeft, true))
                return;

            // 5️⃣ 如果摘肺后 Pawn 还活着，继续尝试摘剩余肾
            if(TryQueueKidney(AutoHarvestMod.settings.IsLeft, true))
                return;

            //6 到这能摘的都摘了,移除追踪组件
            _disabled = true; // 跳过后续 tick
            Designator_SmartHarvestOrganTax.RemoveTracking(Pawn);
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

        //private void ClearInvalidBills()
        //{
        //    Pawn.BillStack.Bills.RemoveAll(b =>
        //    {
        //        if (!(b is Bill_Medical bm)) return false;
        //        if (bm.recipe.Worker is Recipe_RemoveBodyPart && bm.Part != null)
                    
        //            return !Pawn.health.hediffSet.GetNotMissingParts().Contains(bm.Part);
        //        return false;
        //    });
        //}

        private bool NeedMoreBills()
            => !Pawn.BillStack.Bills.Any(b => b.recipe.Worker is Recipe_RemoveBodyPart);
        #endregion
    }
}