//////////////////////////////////////////////////
//                 Mode.cs                      //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////


namespace MutaRaidBT.Settings
{
    static class Mode
    {
        static public Helpers.Enumeration.CooldownUse mCooldownUse { get; set; }
        static public Helpers.Enumeration.LocationContext mLocationSettings { get; set; }

        static public Helpers.Enumeration.PoisonSpellId[] mPoisonsMain { get; set; }
        static public Helpers.Enumeration.PoisonSpellId[] mPoisonsOff { get; set; }

        static public bool[] mUsePoisons { get; set; }

        static public bool mOverrideContext { get; set; }

        static public bool mUseMovement { get; set; }
        static public bool mUseCooldowns { get; set; }
        static public bool mUseAoe { get; set; }
        static public bool mUseCombat { get; set; }
        static public bool mForceBehind { get; set; }

        static Mode()
        {
            mUsePoisons  = new bool[6];
            mPoisonsMain = new Helpers.Enumeration.PoisonSpellId[6];
            mPoisonsOff  = new Helpers.Enumeration.PoisonSpellId[6];

            mUsePoisons[(int) Helpers.Enumeration.LocationContext.Raid]          = false;
            mUsePoisons[(int) Helpers.Enumeration.LocationContext.HeroicDungeon] = false;
            mUsePoisons[(int) Helpers.Enumeration.LocationContext.Dungeon]       = true;
            mUsePoisons[(int) Helpers.Enumeration.LocationContext.Battleground]  = true;
            mUsePoisons[(int) Helpers.Enumeration.LocationContext.World]         = true;

            for (int i = 1; i < 6; i++)
            {
                mPoisonsMain[i] = Helpers.Enumeration.PoisonSpellId.Instant;
                mPoisonsOff[i] = Helpers.Enumeration.PoisonSpellId.Deadly;
            }

            mCooldownUse = Helpers.Enumeration.CooldownUse.OnlyOnBosses;

            mOverrideContext = false;

            mUseMovement = true;
            mUseCooldowns = true;
            mUseAoe = true;
            mUseCombat = true;
            mForceBehind = false;
        }
    }
}
