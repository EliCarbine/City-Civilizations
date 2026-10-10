using System.Collections.Generic;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M4 (F58): the read side of the journal - diary lines and eulogies composed from
    /// the real rings (F43). Pure string assembly; the panel asks, we render.
    /// </summary>
    public static class SoulDiary
    {
        /// <summary>One journal entry as a diary line.</summary>
        public static string Line(uint day, SoulEventKind kind, short value)
        {
            return "Day " + day + " - " + Phrase(kind, value) + ".";
        }

        public static string Phrase(SoulEventKind kind, short value)
        {
            switch (kind)
            {
                case SoulEventKind.Born: return "arrived in the city";
                case SoulEventKind.Met: return "met someone new";
                case SoulEventKind.Befriended: return "made a friend";
                case SoulEventKind.FellOut: return "drifted apart from a friend";
                case SoulEventKind.Fought: return "got into a fight";
                case SoulEventKind.RivalMade: return "made an enemy";
                case SoulEventKind.Injured: return "got hurt";
                case SoulEventKind.Healed: return "recovered from an injury";
                case SoulEventKind.LevelUp: return "grew a little sharper";
                case SoulEventKind.GoodDay: return "had a good day";
                case SoulEventKind.HardDay: return "had a hard day";
                case SoulEventKind.SleptWell: return "slept like a rock";
                case SoulEventKind.SleptBad: return "barely slept";
                case SoulEventKind.Meditated: return "found a quiet moment";
                case SoulEventKind.CrushFormed: return "felt a spark";
                case SoulEventKind.CourtStarted: return "started seeing someone";
                case SoulEventKind.Engaged: return "got engaged";
                case SoulEventKind.Married: return "got married";
                case SoulEventKind.Widowed: return "lost their partner";
                case SoulEventKind.Heartbroken: return "had their heart broken";
                case SoulEventKind.Anniversary: return "marked a wedding anniversary";
                case SoulEventKind.WorkSpouse: return "found a partner at work";
                case SoulEventKind.Birthday: return "another year around the sun";
                case SoulEventKind.BirthdayParty: return "celebrated a birthday with friends";
                case SoulEventKind.FirstCloseFriend: return "made their first true friend";
                case SoulEventKind.FirstCrush: return "felt their first crush";
                case SoulEventKind.Confided: return "shared a secret";
                case SoulEventKind.FeudStarted: return "let a grudge harden";
                case SoulEventKind.FeudClash: return "clashed with a rival";
                case SoulEventKind.Reconciled: return "made peace with an old foe";
                case SoulEventKind.WingmanAssist: return "nudged two friends together";
                case SoulEventKind.Traumatized: return "saw something that cut deep";
                case SoulEventKind.AnniversaryFlare: return "an old wound ached";
                case SoulEventKind.TraumaGrowth: return "grew around an old wound";
                case SoulEventKind.TherapyStarted: return "started therapy";
                case SoulEventKind.TherapyHelped: return "therapy is helping";
                case SoulEventKind.DepressionOnset: return "the grey settled in";
                case SoulEventKind.DepressionLifted: return "the grey finally broke";
                case SoulEventKind.AnxietySpike: return "crowds became too much";
                case SoulEventKind.CopingHabit: return "found something to take the edge off";
                case SoulEventKind.HabitEscalated: return "the habit took hold";
                case SoulEventKind.SoberStreak: return "hit " + value + " clean days";
                case SoulEventKind.Relapsed: return "slipped back into old habits";
                case SoulEventKind.ChronicDx: return "received hard news from a doctor";
                case SoulEventKind.ChronicManaged: return "kept the routine steady";
                case SoulEventKind.Mourned: return value >= SoulTune.MournCloseAffinity ? "mourned someone they loved" : "mourned someone they knew";
                case SoulEventKind.Theft: return "took what was not theirs";
                case SoulEventKind.Vandalism: return "left a mark on the city";
                case SoulEventKind.GangJoined: return "found belonging in a gang";
                case SoulEventKind.Arrested: return "was picked up by the police";
                case SoulEventKind.TrialConvicted: return "was convicted and sentenced";
                case SoulEventKind.TrialAcquitted: return "was cleared at trial";
                case SoulEventKind.Released: return "came home after time away";
                case SoulEventKind.PetAdopted: return "brought home a companion";
                case SoulEventKind.PetDied: return "said goodbye to a pet";
                case SoulEventKind.Reunited: return "ran into an old friend after " + value + " days";
case SoulEventKind.AffairStarted: return "started something they shouldn't have";
case SoulEventKind.AffairDiscovered: return "got caught living a lie";
case SoulEventKind.Divorced: return "signed the papers and walked away";
case SoulEventKind.BecameParent: return "welcomed a child into the family";
case SoulEventKind.Grandfamily: return "spent the day with the grandchildren";
case SoulEventKind.Promoted: return "earned a step up at work";
case SoulEventKind.FiredFromJob: return "lost their job";
case SoulEventKind.NewJob: return "started a new job";
case SoulEventKind.HustleStarted: return "started a side hustle";
case SoulEventKind.ShopOpened: return "opened a shop of their own";
case SoulEventKind.Burnout: return "worked themselves into the ground";
case SoulEventKind.DebtCrunch: return "fell behind on money";
case SoulEventKind.CharityGiven: return "helped someone get back on their feet";
case SoulEventKind.ProbationStarted: return "started their probation";
case SoulEventKind.CheckedIn: return "showed up clean";
case SoulEventKind.Violation: return "tripped a condition of probation";
case SoulEventKind.Backslid: return "fell back into old ways";
case SoulEventKind.ProbationFinished: return "finished probation";
case SoulEventKind.ReformProgram: return "joined a reform program";
case SoulEventKind.ProgramCompleted: return "graduated the reform program";
case SoulEventKind.CommunityService: return "put in work for the city";
case SoulEventKind.MentorFound: return "found someone who believed";
case SoulEventKind.SteadyWork: return "held down steady work";
case SoulEventKind.RecordCleared: return "cleared their name";
case SoulEventKind.MidlifeStir: return "felt the years ask for something";
case SoulEventKind.MidlifePivot: return "turned a corner mid-life";
case SoulEventKind.SecondWind: return "found a second wind";
case SoulEventKind.WishFormed: return "set their heart on something";
case SoulEventKind.WishFulfilled: return "chased a wish down";
case SoulEventKind.WishFaded: return "let an old wish go";
case SoulEventKind.TalentUnlocked: return "found a gift nobody knew was there";
case SoulEventKind.GiftBentLife: return "the craft quietly became the life";
case SoulEventKind.BuskStarted: return "played the street corner";
case SoulEventKind.StreetLegend: return "became a fixture on their corner";
case SoulEventKind.BandFormed: return "started a band";
case SoulEventKind.BandGig: return "played a small room";
case SoulEventKind.BandBreakup: return "the band fell apart";
case SoulEventKind.BreakoutAct: return "made it out of the small rooms";
case SoulEventKind.BookFinished: return "finished a manuscript";
case SoulEventKind.BookPublished: return "saw their name in print";
case SoulEventKind.Bestseller: return "wrote a bestseller";
case SoulEventKind.WritersGroup: return "found a circle of writers";
case SoulEventKind.MuralDone: return "painted the city's walls";
case SoulEventKind.ArtCommission: return "painted for paying strangers";
case SoulEventKind.PhotoExhibit: return "hung prints in public";
case SoulEventKind.OpeningNight: return "stepped under the lights";
case SoulEventKind.StageFright: return "the lights were too bright";
case SoulEventKind.Ovation: return "earned a standing ovation";
case SoulEventKind.ShortFilm: return "finished a first short film";
case SoulEventKind.FilmViral: return "had a clip go around the city";
case SoulEventKind.CraftFair: return "sold wares at a street fair";
case SoulEventKind.HandmadeGift: return "made something by hand, just for them";
case SoulEventKind.Heirloom: return "made something meant to outlive them";
case SoulEventKind.DancePartner: return "found someone to dance with";
case SoulEventKind.DanceComp: return "danced before judges";
case SoulEventKind.FirstDance: return "danced at a wedding";
case SoulEventKind.ArtStageUp: return "took a step up in the craft";
                default: return "something stirred";
            }
        }

        /// <summary>Newest-first diary lines, up to maxLines. Returns the count written.</summary>
        public static int Render(EntityManager em, Entity e, List<string> lines, int maxLines)
        {
            if (lines == null) return 0;
            lines.Clear();
            if (!em.HasBuffer<SoulJournalEntry>(e)) return 0;
            DynamicBuffer<SoulJournalEntry> ring = em.GetBuffer<SoulJournalEntry>(e);
            int n = 0;
            for (int i = ring.Length - 1; i >= 0 && n < maxLines; i--)
            {
                SoulJournalEntry j = ring[i];
                lines.Add(Line(j.m_Day, j.m_Kind, j.m_Value));
                n++;
            }
            return n;
        }

        /// <summary>The eulogy line for a remembered passing (F43).</summary>
        public static string Eulogy(uint day, int mourners, int memories)
        {
            if (mourners <= 0)
            {
                return "Day " + day + " - a quiet goodbye; no one who knew them remained.";
            }
            if (memories <= 0)
            {
                return "Day " + day + " - a goodbye by " + mourners + " souls who shared their days.";
            }
            return "Day " + day + " - a goodbye; " + mourners + " souls carried " + memories + " memories of them.";
        }
    }

    /// <summary>
    /// M4: the soul inspector view-model. Everything the panel shows for one soul,
    /// already formatted - the UI layer only has to display the strings.
    /// </summary>
    public static class SoulInspector
    {
        public struct View
        {
            public string Summary;
            public string Details;
            public string Skills;
            public string Recent;
        }

        public static View Build(EntityManager em, Entity e)
        {
            View v = default(View);
            if (!em.HasComponent<SoulMeta>(e))
            {
                v.Summary = "(no soul)";
                return v;
            }

            int ties = 0;
            int best = 0;
            int worst = 0;
            if (em.HasBuffer<SoulEdge>(e))
            {
                DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
                ties = edges.Length;
                for (int i = 0; i < edges.Length; i++)
                {
                    int aff = edges[i].m_Affinity;
                    if (aff > best) best = aff;
                    if (aff < worst) worst = aff;
                }
            }

            string traits = "";
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality per = em.GetComponentData<SoulPersonality>(e);
                traits = " extrav " + per.m_Extraversion + ", consist " + per.m_Conscientiousness;
            }
            int valence = 0;
            if (em.HasComponent<Mood>(e)) valence = em.GetComponentData<Mood>(e).m_Valence;
            v.Summary = "mood " + valence + "; ties " + ties + " (best " + best + ", worst " + worst + ");" + traits;

            string details = "";
            if (em.HasComponent<SoulMind>(e))
            {
                SoulMind m = em.GetComponentData<SoulMind>(e);
                details = "trauma " + m.m_Trauma + ", depression " + m.m_Depression + ", worry " + m.m_Worry + ", addiction " + m.m_Addiction;
                if ((m.m_Flags & SoulMind.FlagInTherapy) != 0) details += ", in therapy";
            }
            if (em.HasComponent<SoulPet>(e))
            {
                SoulPet pt = em.GetComponentData<SoulPet>(e);
                if (pt.m_Kind != 0) details += "; companion kind " + pt.m_Kind + " (bond " + pt.m_Bond + ")";
            }
            if (em.HasComponent<SoulChronic>(e))
            {
                SoulChronic c = em.GetComponentData<SoulChronic>(e);
                if (c.m_Kind != 0) details += "; managing condition " + c.m_Kind;
            }
            if (em.HasComponent<SoulCrime>(e))
            {
                SoulCrime c = em.GetComponentData<SoulCrime>(e);
                if (c.m_Notoriety > 0 || c.m_Records > 0) details += "; notoriety " + c.m_Notoriety + ", convictions " + c.m_Records;
            }
            v.Details = details;

            if (em.HasComponent<Skills>(e))
            {
                Skills sk = em.GetComponentData<Skills>(e);
                v.Skills = "social " + (sk.m_Social / 100) + ", fitness " + (sk.m_Fitness / 100)
                    + ", logic " + (sk.m_Logic / 100) + ", craft " + (sk.m_Craft / 100);
            }

            List<string> lines = new List<string>();
            SoulDiary.Render(em, e, lines, 8);
            int start = lines.Count - 1;
            string recent = "";
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) recent += " / ";
                recent += lines[i];
            }
            v.Recent = recent;
            return v;
        }
    }
}
