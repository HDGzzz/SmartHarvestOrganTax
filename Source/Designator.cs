using RimWorld;
using Verse;
using UnityEngine;

namespace SmartHarvestOrganTax
{
    public class Designator_SmartHarvestOrganTax : Designator
    {
        public Designator_SmartHarvestOrganTax()
        {
            defaultLabel = "SmartHarvestOrganTax_Designator_Label".Translate();
            defaultDesc = "SmartHarvestOrganTax_Designator_Desc".Translate();
            icon = ContentFinder<Texture2D>.Get("HarvestOrgans");
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Haul;
        }
        [DefOf]
        public static class SmartHarvestOrganTaxDefOf
        {
            public static DesignationDef SmartHarvestOrganTax;
        }

        public override int DraggableDimensions => 2;               
        public override bool DragDrawMeasurements => false;

        protected override DesignationDef Designation
        {
            get { return SmartHarvestOrganTaxDefOf.SmartHarvestOrganTax; }
        }

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
            Pawn pawn = (Pawn)t;
            Map.designationManager.AddDesignation(new Designation(pawn, Designation));

            // 添加追踪组件并立即评估
            AddTracking(pawn);
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
                    DesignateThing(thing);
                }
            }
        }

        public static void AddTracking(Pawn pawn)
        {
            if (pawn.TryGetComp<CompAutoHarvestTracker>() != null) return;

            // 动态添加CompAutoHarvestTracker
            var comp = new CompAutoHarvestTracker();
            comp.parent = pawn;
            comp.Initialize(new CompProperties_AutoHarvestTracker());
            pawn.AllComps.Add(comp);

            // 立即评估
            comp.EvaluateNow();

            //Messages.Message($"Started auto-harvesting for {pawn.NameShortColored}",
            //    pawn, MessageTypeDefOf.TaskCompletion);
        }

        public static void RemoveTracking(Pawn pawn)
        {
            if (pawn == null) return;
            var map = pawn.Map;
            if (map != null)
            {
                var designation = map.designationManager.DesignationOn(pawn, SmartHarvestOrganTaxDefOf.SmartHarvestOrganTax);
                if (designation != null)
                {
                    map.designationManager.RemoveDesignation(designation);
                }
            }
            var comp = pawn.TryGetComp<CompAutoHarvestTracker>();
            if (comp != null)
            {
                pawn.AllComps.Remove(comp);
                //Messages.Message(
                //    $"Stopped auto-harvesting for {pawn.NameShortColored}",
                //    pawn,
                //    MessageTypeDefOf.RejectInput);
            }
        }



    }

}
