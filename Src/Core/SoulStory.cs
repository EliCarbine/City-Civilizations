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
case SoulEventKind.CrewFormed: return "found a regular crew";
case SoulEventKind.PickupLegend: return "ran the court all summer";
case SoulEventKind.LeagueJoined: return "signed with a district team";
case SoulEventKind.LeagueChamp: return "won the district title";
case SoulEventKind.RunStarted: return "started running in the mornings";
case SoulEventKind.Marathon: return "went the whole marathon";
case SoulEventKind.DojoJoined: return "bowed into the dojo";
case SoulEventKind.Knockout: return "took a hard knockout";
case SoulEventKind.LANChamp: return "took the LAN crown";
case SoulEventKind.ParkMatch: return "played chess in the park";
case SoulEventKind.NemesisMet: return "found a rival across the board";
case SoulEventKind.KidSportStart: return "first season on the field";
case SoulEventKind.KidScouted: return "got noticed by the scouts";
case SoulEventKind.KidQuit: return "put the cleats away";
case SoulEventKind.TitleRun: return "rode a deep playoff run";
case SoulEventKind.TitleWon: return "brought the title home";
case SoulEventKind.ParadeDay: return "rode the parade route";
case SoulEventKind.DerbyDay: return "stood in the derby crowd";
case SoulEventKind.SplitFamily: return "took sides on derby day";
case SoulEventKind.SportInjury: return "went down with a knock";
case SoulEventKind.Comeback: return "made it back on the field";
case SoulEventKind.LastSeason: return "announced a last season";
case SoulEventKind.SportRetired: return "hung the boots up";
case SoulEventKind.SportStageUp: return "moved up a rung in their sport";
case SoulEventKind.BookClubJoined: return "joined a book club";
case SoulEventKind.BookClubSchism: return "watched the club split over an ending";
case SoulEventKind.NightClass: return "started night classes";
case SoulEventKind.CohortBond: return "the class cohort became friends";
case SoulEventKind.LanguageFluency: return "got fluent in a second language";
case SoulEventKind.TranslatedForKin: return "translated for the family";
case SoulEventKind.MuseumVisit: return "spent an afternoon at the museum";
case SoulEventKind.GalleryOpening: return "went to a gallery opening";
case SoulEventKind.TriviaNight: return "made it a trivia night";
case SoulEventKind.BoardGameFeud: return "let a board game end a friendship";
case SoulEventKind.Stargazing: return "spent an evening under the stars";
case SoulEventKind.MeteorShower: return "watched the meteor shower";
case SoulEventKind.TelescopeGift: return "got a telescope for a birthday";
case SoulEventKind.LateNightTalk: return "talked past midnight";
case SoulEventKind.GreatArgument: return "argued well, and kept a friend";
case SoulEventKind.ApprenticeJoined: return "signed on with a master";
case SoulEventKind.MasteryDay: return "passed mastery day";
case SoulEventKind.Noticed: return "noticed something small and lovely";
case SoulEventKind.PhoneFight: return "fought about the phone again";
case SoulEventKind.Scrolling: return "lost an evening to the feed";
case SoulEventKind.Posted: return "posted a thought";
case SoulEventKind.ViralPost: return "went viral";
case SoulEventKind.ChatFormed: return "the group chat came alive";
case SoulEventKind.ChatJoke: return "an inside joke got out";
case SoulEventKind.ChatQuiet: return "the chat went quiet";
case SoulEventKind.LeftOnRead: return "was left on read";
case SoulEventKind.BingeNight: return "watched one more episode, then six";
case SoulEventKind.WatchParty: return "hosted a watch party";
case SoulEventKind.SpoilerCrime: return "spoiled the finale";
case SoulEventKind.EveryoneWatching: return "joined the show everyone follows";
case SoulEventKind.NewGadget: return "queued for the new gadget";
case SoulEventKind.GadgetGrumble: return "grumbled about newfangled things";
case SoulEventKind.TechAbstain: return "still isn't on the phone";
case SoulEventKind.TechEra: return "watched the streets change again";
case SoulEventKind.RemoteWork: return "started working from the café";
case SoulEventKind.LonelyRemote: return "missed the old office";
case SoulEventKind.ArcadeNight: return "took a ranked night at the arcade";
case SoulEventKind.ArcadeKid: return "beat everyone at the arcade";
case SoulEventKind.ArcadeClosed: return "saw the arcade close";
case SoulEventKind.ForumJoined: return "found a forum for their thing";
case SoulEventKind.ForumFriend: return "met a forum friend at last";
case SoulEventKind.DigitalLegacy: return "left the city a digital memory";
case SoulEventKind.Volunteered: return "signed up to volunteer";
case SoulEventKind.VolClique: return "found the volunteer regulars";
case SoulEventKind.VolFatigue: return "burned out on giving";
case SoulEventKind.WatchFormed: return "started a neighborhood watch";
case SoulEventKind.PatrolWalk: return "walked the evening patrol";
case SoulEventKind.WatchOverreach: return "went too far on watch duty";
case SoulEventKind.GardenPlot: return "took a plot in the community garden";
case SoulEventKind.TomatoRival: return "feuded over tomatoes";
case SoulEventKind.HarvestShare: return "shared the harvest";
case SoulEventKind.GardenVandal: return "found the garden wrecked";
case SoulEventKind.Petition: return "started a petition";
case SoulEventKind.March: return "marched with the neighborhood";
case SoulEventKind.PetitionWin: return "won the petition";
case SoulEventKind.PetitionIgnored: return "was ignored by city hall";
case SoulEventKind.TownHall: return "spoke at a town hall";
case SoulEventKind.Speech: return "roused the whole room";
case SoulEventKind.FirstTimer: return "went to their first town hall";
case SoulEventKind.GiveDrive: return "gave to the drive";
case SoulEventKind.GoalBeaten: return "helped the district beat its goal";
case SoulEventKind.JurySummons: return "was summoned for jury duty";
case SoulEventKind.Foreman: return "served as foreman";
case SoulEventKind.JuryStory: return "still tells the trial story";
case SoulEventKind.BlockParty: return "threw the block party";
case SoulEventKind.Cleanup: return "cleaned up the street";
case SoulEventKind.CrisisHelp: return "helped through the crisis";
case SoulEventKind.CrisisHero: return "pulled a neighbor out";
case SoulEventKind.CrisisCohesion: return "grieved with the block";
case SoulEventKind.CrisisTrauma: return "still hears that night";
case SoulEventKind.Planted: return "put in a windowsill garden";
case SoulEventKind.PlantMurder: return "murdered another houseplant";
case SoulEventKind.GreenThumb: return "got something to thrive";
case SoulEventKind.CuttingSwap: return "swapped cuttings with a friend";
case SoulEventKind.RosePrize: return "grew the prized rose";
case SoulEventKind.Birding: return "started a bird life list";
case SoulEventKind.BirdSpot: return "spotted a rare one";
case SoulEventKind.EagleNest: return "saw the eagles nesting";
case SoulEventKind.FirstHike: return "took to the trails";
case SoulEventKind.TrailFriend: return "made a trail friend";
case SoulEventKind.CampTrip: return "camped under it all";
case SoulEventKind.TripStory: return "still tells the trip that almost went wrong";
case SoulEventKind.FishingOn: return "started fishing the pier";
case SoulEventKind.BigCatch: return "lost the one that got away";
case SoulEventKind.FishStory: return "retold it, bigger every time";
case SoulEventKind.ForageOn: return "learned to forage";
case SoulEventKind.Mushroom: return "had a good mushroom season";
case SoulEventKind.BadMushroom: return "ate the wrong mushroom";
case SoulEventKind.CatFeeder: return "started feeding the street cats";
case SoulEventKind.Adopted: return "adopted a rescue";
case SoulEventKind.GlowUp: return "watched the rescue come back to life";
case SoulEventKind.FirstSnow: return "delighted in the first snow";
case SoulEventKind.HeatWave: return "got cranky in the heat";
case SoulEventKind.GrayStreak: return "sagged with the gray";
case SoulEventKind.StormWorry: return "worried through the storm";
case SoulEventKind.PlantDrive: return "joined the planting drive";
case SoulEventKind.TreeFight: return "fought for the old tree";
case SoulEventKind.TreeSaved: return "kept the old tree standing";
case SoulEventKind.RitualOn: return "found a lucky ritual";
case SoulEventKind.LuckyWin: return "the lucky ritual came through";
case SoulEventKind.RitualBlame: return "blamed the ritual for that one";
case SoulEventKind.RitualShared: return "a friend picked up the ritual";
case SoulEventKind.CollectOn: return "started a little collection";
case SoulEventKind.CollectFind: return "found another piece for the shelf";
case SoulEventKind.CollectMiss: return "a piece got away";
case SoulEventKind.SetDone: return "completed the set";
case SoulEventKind.FearOn: return "named the big fear";
case SoulEventKind.FearFreeze: return "froze when it got close";
case SoulEventKind.FearRoute: return "took the long way around";
case SoulEventKind.ExposureArc: return "faced it with a friend";
case SoulEventKind.FearShrunk: return "the fear finally shrank";
case SoulEventKind.PhraseCoin: return "coined a catchphrase";
case SoulEventKind.Nickname: return "gave a friend a nickname";
case SoulEventKind.PhraseSpread: return "said the phrase again";
case SoulEventKind.SlangCity: return "the phrase went city-wide";
case SoulEventKind.StatOn: return "started keeping little stats";
case SoulEventKind.StatQuote: return "quoted the notebook";
case SoulEventKind.StatArchive: return "the notebooks reached the archive";
case SoulEventKind.ListOn: return "built a little system";
case SoulEventKind.ListWin: return "the little system held";
case SoulEventKind.ListChaos: return "the little system broke";
case SoulEventKind.CharmOn: return "found a comfort object";
case SoulEventKind.CharmCalm: return "the charm did its quiet job";
case SoulEventKind.CharmLost: return "the charm went missing";
case SoulEventKind.CharmBack: return "the charm came back";
case SoulEventKind.TradOn: return "started a private tradition";
case SoulEventKind.TradKeep: return "kept the tradition";
case SoulEventKind.TradShared: return "loved ones joined in";
case SoulEventKind.TradBreak: return "the tradition broke";
case SoulEventKind.TalkOn: return "found their voice in a room";
case SoulEventKind.ListenOn: return "became someone people confide in";
case SoulEventKind.DuoOn: return "found the other half of a conversation";
case SoulEventKind.ConfideOn: return "was trusted with something quiet";
case SoulEventKind.SleepOn: return "settled into their own hours";
case SoulEventKind.LateOn: return "kept someone waiting";
case SoulEventKind.LateAccounted: return "friends stopped counting the minutes";
case SoulEventKind.WaitOn: return "waited on the one who is always late";
case SoulEventKind.PunctualOn: return "was early like always";
case SoulEventKind.TripPlanOn: return "circled a date on the calendar";
case SoulEventKind.TripGoOn: return "left town with a bag packed";
case SoulEventKind.TripBackOn: return "came home with sand in everything";
case SoulEventKind.DayTripOn: return "played tourist in their own city";
case SoulEventKind.FieldTripOn: return "still talks about the school bus";
case SoulEventKind.FirstCarOn: return "got the keys to their first car";
case SoulEventKind.CarWashOn: return "washed the car till it shined";
case SoulEventKind.SundayDriveOn: return "took the long way home";
case SoulEventKind.RoadRageOn: return "lost their cool in traffic";
case SoulEventKind.MissHomeOn: return "missed somewhere they left behind";
case SoulEventKind.CalledHomeOn: return "called home, just to hear the room";
case SoulEventKind.OldLetterOn: return "opened a letter years in coming";
case SoulEventKind.WelcomedOn: return "got the welcome tour";
case SoulEventKind.ColdShoulderOn: return "never got a welcome";
case SoulEventKind.HostStudentOn: return "put a stranger up for a year";
case SoulEventKind.OverseasLetterOn: return "read word from across the water";
case SoulEventKind.PilgrimOn: return "went back to where the family is from";
case SoulEventKind.RememberWhenOn: return "remembered when this was all fields";
case SoulEventKind.MustWatchOn: return "never missed the weekly show";
case SoulEventKind.RewatchOn: return "went back to the comfort one";
case SoulEventKind.CliffhangerOn: return "left hanging and it stung";
case SoulEventKind.EraShiftOn: return "fell for a whole decade";
case SoulEventKind.SongSummerOn: return "had a song own the summer";
case SoulEventKind.TixChaosOn: return "fought the queue for tickets";
case SoulEventKind.ConcertGoOn: return "lost their voice at the show";
case SoulEventKind.MerchOn: return "bought the shirt at the merch stand";
case SoulEventKind.EncoreOn: return "got the encore they shouted for";
case SoulEventKind.MorningShowOn: return "the morning show knows them";
case SoulEventKind.HeadlineOn: return "read the headline and felt it";
case SoulEventKind.ShopRadioOn: return "hummed along to the shop radio";
case SoulEventKind.AnnouncerOn: return "heard their own voice on the traffic report";
case SoulEventKind.PodcastGoOn: return "started a podcast about it";
case SoulEventKind.PodcastGrowOn: return "listeners kept showing up";
case SoulEventKind.PodcastBlowOn: return "the podcast blew up";
case SoulEventKind.CommuteOn: return "commuted to a stranger's voice";
case SoulEventKind.CinemaNightOn: return "saw it on the big screen";
case SoulEventKind.CriticOn: return "wrote the review everyone quoted";
case SoulEventKind.DateFilmOn: return "shared a film in the dark";
case SoulEventKind.RainyFilmOn: return "let the rain pick the film";
case SoulEventKind.HomePremiereOn: return "watched the city premiere itself";
case SoulEventKind.CampaignGoOn: return "started a tabletop campaign";
case SoulEventKind.SessionOn: return "rolled dice with the regulars";
case SoulEventKind.DMRoleOn: return "took the chair behind the screen";
case SoulEventKind.FinaleOn: return "ended the campaign the right way";
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
