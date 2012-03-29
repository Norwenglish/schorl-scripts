namespace MutaRaidBT.Helpers
{
    public delegate uint PoisonDelegate (object context); 
    class Enumeration
    {
        public enum PoisonSpellId
        {
            Deadly = 2892,
            Instant = 6947,
            Crippling = 3775,
            Wound = 10918,
            MindNumbing = 5237
        }

        public enum TalentTrees
        {
            None = 0,
            Assassination,
            Combat,
            Subtlety
        }

        public enum LocationContext
        {
            Undefined = 0,
            Raid,
            HeroicDungeon,
            Dungeon,
            Battleground,
            World
        }

        public enum CooldownUse
        {
            Always = 0,
            ByFocus,
            OnlyOnBosses,
            Never
        }
    }
}
