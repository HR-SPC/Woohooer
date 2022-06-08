using DarkIntentionsWoohoo.mod.settings;
using RimWorld;
using Verse;
using Verse.AI;

namespace DarkIntentionsWoohoo;

internal class WorkGiver_Woohoo : WorkGiver_Scanner
{
    private Building_Bed bed;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t == null || pawn == null)
        {
            return false;
        }

        if (t is not Pawn mate || pawn == mate || !forced && !canAutoLove(pawn, mate) || mate.Downed ||
            mate.Faction != pawn.Faction && (mate.guest == null || mate.Drafted))
        {
            return false;
        }

        if (!pawn.IsHumanoid() || !mate.IsHumanoid())
        {
            JobFailReason.Is("Whohooer.Humanoid".Translate());
            return false;
        }

        if (!mate.IsNotWoohooing() || !pawn.IsNotWoohooing())
        {
            JobFailReason.Is("Whohooer.AlreadyWohooing".Translate());
            return false;
        }

        if (ModsConfig.IdeologyActive && !BedUtility.WillingToShareBed(pawn, mate))
        {
            JobFailReason.Is("IdeoligionForbids".Translate());
            return false;
        }

        LocalTargetInfo target = mate;
        if (!pawn.CanReserve(target, 1, -1, null, forced))
        {
            return false;
        }

        bed = BetterBedFinder.DoBetterBedFinder(pawn, mate);

        if (bed != null)
        {
            return true;
        }

        JobFailReason.Is("Whohooer.NoBed".Translate());
        return false;
    }

    private bool canAutoLove(Pawn pawn, Pawn pawn2)
    {
        var ticksGame = Find.TickManager.TicksGame;
        int result;
        if (WoohooSettingHelper.latest.allowAIWoohoo && pawn.mindState.canLovinTick < ticksGame &&
            pawn2.mindState.canLovinTick < ticksGame && JobUtilityIdle.isIdle(pawn2) &&
            pawn2.needs?.joy?.tolerances != null && pawn.needs?.joy?.tolerances != null)
        {
            var needs = pawn2.needs;
            if (needs != null)
            {
                var mood = needs.mood;
                if (mood != null)
                {
                    _ = mood.CurLevel;
                    var needs2 = pawn.needs;
                    if (needs2 != null)
                    {
                        var mood2 = needs2.mood;
                        if (mood2 != null)
                        {
                            _ = mood2.CurLevel;
                            if (!pawn2.needs.joy.tolerances.BoredOf(Constants.Joy_Woohoo) &&
                                !pawn.needs.joy.tolerances.BoredOf(Constants.Joy_Woohoo) &&
                                (pawn2.needs.joy.CurLevel < 0.6f || pawn2.needs.mood.CurLevel < 0.6f) &&
                                (pawn.needs.joy.CurLevel < 0.6f || pawn.needs.mood.CurLevel < 0.6f) &&
                                Rand.Value < 0.1f && RelationsUtility.PawnsKnowEachOther(pawn, pawn2))
                            {
                                result = WoohooSettingHelper.latest.familyWeight *
                                         LovePartnerRelationUtility.IncestOpinionOffsetFor(pawn2, pawn) *
                                         Rand.Value <
                                         0.5f
                                    ? 1
                                    : 0;
                                goto IL_01c1;
                            }
                        }
                    }
                }
            }
        }

        result = 0;
        IL_01c1:
        return (byte)result != 0;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t == null || pawn == null)
        {
            return null;
        }

        var mate = t as Pawn;
        if (!pawn.IsHumanoid() || !mate.IsHumanoid())
        {
            return null;
        }

        if (IsMate(pawn, mate) && PawnHelper.IsSameRaceHumanoid(pawn, mate))
        {
            return new Job(Constants.JobWooHoo_Baby, mate, bed)
            {
                count = 1
            };
        }

        return new Job(Constants.JobWooHoo, mate, bed)
        {
            count = 1
        };
    }

    public virtual float MateChance()
    {
        return WoohooSettingHelper.latest.woohooChildChance;
    }

    public virtual bool IsMate(Pawn pawn, Pawn pawn2)
    {
        var num = FertilityChecker.getFetility(pawn) + (FertilityChecker.getFetility(pawn2) / 2f);
        num *= MateChance();
        if (pawn.gender == pawn2.gender && !WoohooSettingHelper.latest.sameGender)
        {
            return false;
        }

        return Rand.Value < num;
    }
}