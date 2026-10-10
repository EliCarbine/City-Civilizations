using System;

namespace CitizenSoul
{
    /// <summary>
    /// The rule engine: all simulation judgment lives here as pure integer functions
    /// (no ECS, no Unity types beyond nothing). Every rule is unit-testable and
    /// deterministic given its inputs — systems only wire data in and out.
    /// </summary>
    public static class SoulRules
    {
        /// <summary>Personality compatibility, roughly -60..+60. Symmetric.</summary>
        public static int PersonalityCompat(SoulPersonality a, SoulPersonality b)
        {
            int agree = ((a.m_Agreeableness + b.m_Agreeableness) / 2 - 128) / 4;      // -32..+32
            int extSim = (32 - SoulMath.Abs(a.m_Extraversion - b.m_Extraversion) / 8); // 0..32
            int opnSim = (32 - SoulMath.Abs(a.m_Openness - b.m_Openness) / 8);         // 0..32
            int neuGap = -SoulMath.Abs(a.m_Neuroticism - b.m_Neuroticism) / 10;        // -25..0
            return SoulMath.Clamp(agree + extSim / 2 + opnSim / 2 + neuGap, -60, 60);
        }

        /// <summary>
        /// Per-interaction affinity delta for one side. rng01 is 0..99 from the
        /// caller's deterministic stream. venueFlags uses SoulEdgeFlags bits.
        /// </summary>
        public static int InteractionDelta(int compat, uint quirkA, uint quirkB, int moodA, int moodB, byte venueFlags, int rng01)
        {
            int delta = compat / 8;                                   // -7..+7
            delta += (moodA + moodB) / 80;                            // -2..+2
            if (SoulEdgeFlags.IsFamily(venueFlags)) delta += 1;
            if (SoulEdgeFlags.Has(venueFlags, SoulEdgeFlags.Colleague)) delta += 1;

            bool hotA = SoulQuirks.Has(quirkA, SoulQuirks.Hothead);
            bool hotB = SoulQuirks.Has(quirkB, SoulQuirks.Hothead);
            bool lonA = SoulQuirks.Has(quirkA, SoulQuirks.Loner);
            bool lonB = SoulQuirks.Has(quirkB, SoulQuirks.Loner);
            if (hotA && hotB) delta -= 3;
            if (lonA || lonB) delta -= 1;

            if (SoulQuirks.Has(quirkA, SoulQuirks.Charming) && (rng01 & 1) == 0) delta += 2;
            if (SoulQuirks.Has(quirkB, SoulQuirks.Charming) && (rng01 & 2) == 0) delta += 2;

            delta += (rng01 % 7) - 3;                                 // -3..+3
            return SoulMath.Clamp(delta, -12, 15);
        }

        /// <summary>Meeting gate: extraverts seek ties, loners defer. rng01 0..99.</summary>
        public static bool FormsAcquaintance(SoulPersonality initiator, int compat, int rng01)
        {
            int chance = 20 + initiator.m_Extraversion / 4 + compat / 5;  // ~20..100
            if (SoulQuirks.Has(initiator.m_Quirk, SoulQuirks.Loner)) chance -= 20;
            return rng01 < SoulMath.Clamp(chance, 5, 95);
        }

        /// <summary>Fight scoring: fitness skill dominance + hothead aggression + bad mood.</summary>
        public static int FightScore(ushort fitnessSkill, uint quirk, int moodValence, int rng01)
        {
            int score = (fitnessSkill / 100) * 6;                     // levels matter most
            if (SoulQuirks.Has(quirk, SoulQuirks.Hothead)) score += 12;
            if (moodValence < -30) score += 8;
            score += rng01 / 5;                                       // 0..19 luck
            return score;
        }

        /// <summary>0 = draw, 1 = A wins, 2 = B wins.</summary>
        public static int FightOutcome(int scoreA, int scoreB, int rng01)
        {
            int diff = scoreA - scoreB + (rng01 - 50) / 5;
            if (diff > 6) return 1;
            if (diff < -6) return 2;
            return 0;
        }

        /// <summary>Personality-driven resting mood, -50..+50.</summary>
        public static int MoodBaseline(SoulPersonality p)
        {
            int social = ((p.m_Extraversion + p.m_Agreeableness) * 60) / 510; // 0..60
            int worry = (p.m_Neuroticism * 60) / 255;                         // 0..60
            return SoulMath.Clamp(social - worry, -50, 50);
        }

        /// <summary>End-of-day mood classification for the journal, or None.</summary>
        public static SoulEventKind DayEndMoodKind(int valence)
        {
            if (valence >= 40) return SoulEventKind.GoodDay;
            if (valence <= -40) return SoulEventKind.HardDay;
            return SoulEventKind.None;
        }

        /// <summary>Idle days before an edge starts decaying, by bond type.</summary>
        public static uint DecayDelay(byte edgeFlags)
        {
            uint mult = 1;
            if (SoulEdgeFlags.IsFamily(edgeFlags)) mult = SoulConsts.FamilyIdleMultiplier;
            else if (SoulEdgeFlags.Has(edgeFlags, SoulEdgeFlags.Rival)) mult = SoulConsts.RivalIdleMultiplier;
            return (uint)SoulConsts.IdleDaysBeforeDecay * mult;
        }

        /// <summary>Adds xp, returns true when a level boundary was crossed. Cap: level 99.</summary>
        public static bool SkillAddXp(ref ushort skill, int xp, out int newLevel)
        {
            if (xp < 0) xp = 0;
            int v = skill + xp;
            if (v > 9999) v = 9999;
            newLevel = v / 100;
            bool leveled = newLevel > skill / 100;
            skill = (ushort)v;
            return leveled;
        }

        /// <summary>Level (0-99) from a packed skill value.</summary>
        public static int SkillLevel(ushort skill) { return skill / 100; }

        /// <summary>
        /// Initial personality from the vanilla pseudo-random byte and the soul seed.
        /// Distributions: bell-ish around 128 so extremes are rare.
        /// </summary>
        public static SoulPersonality Roll(ushort soulSeed, byte vanillaPseudo)
        {
            SoulPersonality p = default(SoulPersonality);
            uint s = (uint)soulSeed * 2654435761u ^ ((uint)vanillaPseudo << 13);
            p.m_Openness = Bell(ref s, 84);
            p.m_Conscientiousness = Bell(ref s, 84);
            p.m_Extraversion = Bell(ref s, 84);
            p.m_Agreeableness = Bell(ref s, 84);
            p.m_Neuroticism = Bell(ref s, 84);
            p.m_Quirk = SoulQuirks.Roll(soulSeed);
            p.m_SoulSeed = soulSeed;
            return p;
        }

        /// <summary>Mean-128 bell-ish byte: average of two uniforms scaled by spread.</summary>
        private static byte Bell(ref uint state, int spread)
        {
            state = state * 1664525u + 1013904223u;
            int a = (int)(state >> 24);
            state = state * 1664525u + 1013904223u;
            int b = (int)(state >> 24);
            int avg = (a + b) / 2; // 0..255 triangular around 128
            int v = 128 + ((avg - 128) * spread) / 128;
            return SoulMath.ToByte(v);
        }
    }
}
