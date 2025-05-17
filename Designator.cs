using RimWorld;
using Verse;
using UnityEngine;

namespace SmartHarvestOrganTax
{
    public class Designator_AutoHarvestOrgans : Designator
    {
        public Designator_AutoHarvestOrgans()
        {
            defaultLabel = "Auto-Harvest Organs";
            defaultDesc = "Drag over imprisoned pawns to schedule optimal organ harvesting.";
            icon = ContentFinder<Texture2D>.Get("UI/Designators/HarvestOrgans");
            useMouseIcon = true;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
        }

        public override int DraggableDimensions => 2;               // 区域拖拽
        public override bool DragDrawMeasurements => false;
        protected override DesignationDef Designation => null;         // 不用游戏内的 Designation 系统

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            foreach (Thing t in Map.thingGrid.ThingsListAtFast(c))
                if (CanDesignateThing(t).Accepted) return true;
            return false;
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t is Pawn p && p.IsPrisonerOfColony) return true;
            return false;
        }

        public override void DesignateThing(Thing t)
        {
            Pawn p = (Pawn)t;
            var tracker = p.TryGetComp<CompAutoHarvestTracker>();
            if (tracker != null) tracker.EvaluateNow();
        }
    }

}
