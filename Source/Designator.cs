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
            icon = ContentFinder<Texture2D>.Get("HarvestOrgans");
            useMouseIcon = true;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
        }

        public override int DraggableDimensions => 2;               
        public override bool DragDrawMeasurements => false;
        protected override DesignationDef Designation => null;         

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

        public override void DesignateSingleCell(IntVec3 c)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            foreach (Thing thing in c.GetThingList(map))
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && CanDesignateThing(pawn) == AcceptanceReport.WasAccepted)
                {
                    // 添加你自己的逻辑，比如打上一个自定义的设计ation标签等
                    pawn.TryGetComp<CompAutoHarvestTracker>()?.EvaluateNow();
                }
            }
        }


    }

}
