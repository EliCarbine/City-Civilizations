// Citizen Soul -- EP2 utility: tuning, odds and the city's voice tables.
// All randomness here is deterministic per soul per day -- the same dice
// the whole game can re-derive, no hidden state, no allocations in loops.
using System;
using Unity.Entities;

namespace CitizenSoul
{
    public static class SoulEp2Util
    {
        // ---- money & odds (tuning, mirrors SoulTune house style) ----
        public const int WageBase = 22;          // dollars per payday, before luck
        public const int WageLuckStep = 2;       // wage nudge per luck point
        public const int UpkeepDay = 9;          // daily cost of living
        public const int DebtFloor = -120;       // the absolute lean bottom
        public const uint PayCycle = 7;          // paydays every N days
        public const int WindfallPerMille = 4;   // sudden money
        public const int MishapPerMille = 4;     // sudden loss
        public const int MishapCost = 60;        // typical mishap bite
        public const int WindfallGain = 85;      // typical windfall
        public const int Milestone2 = 1000;
        public const int Milestone3 = 10000;
        public const int Milestone4 = 100000;

        // ---- ventures (F157-F166) ----
        public const int VentureStartPerMille = 7;  // daily appetite for a sign
        public const int VentureStake = 160;        // starter cash a founder puts in
        public const int VentureHealthStart = 8;    // opening sentiment
        public const int VentureHealthDrift = 13;   // daily drift magnitude
        public const int VentureGood = 34;          // healthy enough to be Thriving
        public const int VentureStrain = -34;       // weak enough to be Strained
        public const int VentureHireHealth = 45;    // can afford staff
        public const int VentureCloseHealth = -72;  // he's done
        public const int IpoHealth = 55;            // IPO bar: health
        public const int IpoGoodDays = 40;          // ...held this many straight good days
        public const int RailPerMille = 6;          // a closed soul tries again someday

        // ---- the exchange (F167-F178) ----
        public const int ExchangeSeed = 1000;
        public const int IndexDrift = 9;            // daily base drift magnitude
        public const int CrashPerMille = 7;         // a red day happens
        public const int BoomPerMille = 9;          // a golden day happens
        public const int CrashDepth = 90;           // index points lost on a crash
        public const int BoomHeight = 70;           // index points gained on a rally
        public const int TradeMin = 55;             // dollars a trading soul will deploy
        public const int TradeMax = 2600;
        public const int LotsPerTrade = 9;          // shares per trade, before luck
        public const uint DivCycle = 30;            // dividends every N days
        public const int DivPerShare = 2;           // cents-on-the-dollar-ish
        public const int ExitLossPerMille = 120;    // fast exits on big red days (F174)
        public const int StockCatchUp = 4;          // tape breath per day since last read

        // ---- broadcast (F179-F182) ----
        public const uint SeasonDays = 26;          // how long a lineup runs
        public const int WatchPerMille = 260;       // souls who watch the box
        public const int DevotePerMille = 40;       // ...who make one show theirs
        public const int CartoonPerMille = 55;      // kids who find the cartoons
        public const int CoolPerMille = 30;         // everyday sources of word of mouth

        // ---- newsroom (F183) ----
        public const int HeadlineCap = 16;          // ring of led days
        public const int ScoopCap = 64;             // wire backlog cap
        public const int AirtimePerMille = 30;      // city souls who hear the top story
        public const int AirReactionCap = 6;        // reactions rolled per day
        public const int LeadMinPrio = 1;           // anything lands somewhere

        // ---- life paths (F184) ----
        public const int ProgressStudy = 2;         // daily growth from study years
        public const int ProgressWork = 1;          // ...and from laboring
        public const int ProgressTalent = 2;        // ...bonus where talent lives
        public const int RiseAt = 120;              // Rising
        public const int TopAt = 480;               // the top of the field
        public const int TopLuckFloor = -6;         // luck below this can't top out
        public const int LateBloomAt = 300;         // late bloomers cross here...
        public const int LateBloomPerMille = 90;    // ...if the coin says so
        public const int ReroutePerMille = 5;       // the road bends
        public const int DeferPerMille = 12;        // the dream goes on the shelf
        public const int RekindlePerMille = 260;    // a shelved dream comes back
        public const int TeenDay = 4100;            // local school years (tuning)
        public const int SelfMadeLuck = 12;         // luck at which dreams self-start
        public const int ProgressCap = 600;         // ceiling on SoulPath.m_Progress (0..600)

        // ---- rate limiting & misc ----
        public const uint NoteGapDays = 60;         // routine beats never spam

        /// <summary>The house salt: cheap, stable per-soul entropy.</summary>
        public static uint Salt(Entity e)
        {
            return (uint)e.Index * 2654435761u;
        }

        /// <summary>The house mix: two uints and a counter into one.</summary>
        public static uint Mix(uint a, uint b, uint c)
        {
            uint h = a ^ (b + 0x9E3779B9u) ^ (c * 0x85EBCA6Bu);
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            return h;
        }

        /// <summary>Deterministic per-mille chance. Same dice every replay.</summary>
        public static bool Roll(uint salt, uint day, int perMille)
        {
            return (int)(Mix(salt, day, 0x5F3759DFu) % 1000u) < perMille;
        }

        /// <summary>Deterministic 0..span outcome (span exclusive).</summary>
        public static int Range(uint salt, uint day, int span, uint channel)
        {
            if (span <= 0)
            {
                return 0;
            }
            return (int)(Mix(salt, day, channel) % (uint)span);
        }

        public static int Clamp(int v, int lo, int hi)
        {
            return (v < lo) ? lo : ((v > hi) ? hi : v);
        }

        public static int Abs(int v)
        {
            return (v < 0) ? -v : v;
        }

        // ---- the city's voice (F157+ display strings) ----

        public static readonly string[] VentureNames =
        {
            "", "Bakery", "Cafe", "Barbershop", "Print Shop",
            "Record Shop", "Garage", "Food Truck", "Flower Stand"
        };

        public static readonly string[] Tickers =
        {
            "BKY", "CFE", "BAR", "PRN", "REC", "GRG", "FTR", "FLW",
            "HMR", "SUN", "OAK", "IVY", "RVR", "WND", "COA", "LMP",
            "BRS", "STN", "MIL", "EGG", "TEA", "PIE", "JAM", "RUG"
        };

        public static readonly string[] TitleA =
        {
            "Quiet", "Golden", "Late", "Second", "Small", "Bright",
            "Long", "Old", "Neon", "Winter", "Paper", "Copper",
            "Glass", "Hidden", "Far", "Cozy", "Crooked", "Gentle",
            "Restless", "Master", "Blue", "Little", "Grand", "Slow"
        };

        public static readonly string[] TitleB =
        {
            "Harbor", "Kitchen", "Hour", "Street", "Season", "Room",
            "Shift", "Table", "Line", "Post", "Bell", "Garden",
            "Station", "Crossing", "Signal", "Orchard", "Laundry",
            "Bakery", "Studio", "Chapel", "Depot", "Diner", "Fence", "Corner"
        };

        public static readonly string[] CartoonTitles =
        {
            "Robo Raccoon", "Captain Dumpling", "The Sock Squad", "Mecha Muffin",
            "Tiny Tornado", "Sir Yapsalot", "Cosmic Cat", "Grandma Gadget",
            "Duck Detective", "Pixel Pigeon", "The Jelly Bean Gang", "Baron von Bubbles",
            "Moon Mop", "Turbo Toast", "Wobble and Waddle", "The Paper Knights"
        };

        public static readonly string[] DreamNames =
        {
            "a mystery", "the arts", "a craft", "a trade",
            "business", "sport", "knowledge"
        };

        /// <summary>Compose a show title from two scripted words.</summary>
        public static string ShowTitle(byte n1, byte n2)
        {
            int a = n1 % TitleA.Length;
            int b = n2 % TitleB.Length;
            return "The " + TitleA[a] + " " + TitleB[b];
        }

        public static string CartoonTitle(byte id)
        {
            return CartoonTitles[id % CartoonTitles.Length];
        }

        public static string TickerOf(byte t)
        {
            return Tickers[t % (byte)Tickers.Length];
        }

        /// <summary>Stable ticker for a listing: souls cycle the tape in order.</summary>
        public static byte TickerIndex(Entity e)
        {
            return (byte)(Salt(e) % (uint)Tickers.Length);
        }

        public static string VentureName(byte kind)
        {
            if (kind == 0 || kind >= VentureNames.Length)
            {
                return "";
            }
            return VentureNames[kind];
        }

        public static string DreamName(byte d)
        {
            if (d == 0 || d >= DreamNames.Length)
            {
                return "a mystery";
            }
            return DreamNames[d];
        }

        /// <summary>F183: file a scoop. Anyone may write the wire buffer.</summary>
        public static void Scoop(EntityManager em, Entity desk, byte kind, byte prio, int value, Entity about, uint day)
        {
            if (desk == Entity.Null || !em.Exists(desk) || !em.HasBuffer<SoulScoop>(desk))
            {
                return;
            }
            DynamicBuffer<SoulScoop> wire = em.GetBuffer<SoulScoop>(desk);
            if (wire.Length >= ScoopCap)
            {
                wire.RemoveAt(0); // the oldest story nobody ran dies quietly
            }
            short v = (short)Clamp(value, -30000, 30000);
            wire.Add(SoulScoop.Make(kind, prio, v, about, day));
        }
    }
}
