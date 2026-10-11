// Citizen Soul -- ArtsSystem (M5d): talent, craft & creation (F62-F71).
// A hidden gift surfaces in the teenage years; practice turns the gift
// into a craft; the craft slowly becomes a life -- bands, books, murals,
// stages, films, handmade things and one dance that means everything.
// Reads quirks, friendships and romance; writes only its own surfaces.
using System;
using Unity.Collections;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// F62-F71: hidden talents, bands, busking, novels, murals, galleries,
    /// stages, film, handicraft and dancing -- the creative half of a life.
    /// </summary>
    public partial class ArtsSystem : SoulSystemBase
    {
        private const int ArtsLanes = 128;
        private const int MigrateBudget = 64;
        private const int FaultLogCap = 4;

        /// <summary>128 lanes -> 1 pass/citizen/day.</summary>
        protected override int Lanes { get { return ArtsLanes; } }

        private EntityQuery m_Arts;
        private EntityQuery m_All;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Arts = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>(),
                ComponentType.ReadWrite<SoulArts>());
            m_All = GetEntityQuery(
                ComponentType.ReadOnly<SoulTag>(),
                ComponentType.ReadOnly<SoulMeta>());
        }

        protected override void OnSoulUpdate()
        {
            if (!AdvanceDay(out uint day, out int lane)) return;
            EntityManager em = EntityManager;

            try
            {
                // Migration: every soul carries a gift and an arts book.
                if (m_All.CalculateEntityCount() > 0)
                {
                    NativeArray<Entity> all = m_All.ToEntityArray(Allocator.Temp);
                    int filled = 0;
                    for (int i = 0; i < all.Length && filled < MigrateBudget; i++)
                    {
                        Entity e = all[i];
                        if (em.HasComponent<SoulArts>(e)) continue;
                        try
                        {
                            em.AddComponentData(e, SoulTalent.Fresh(day));
                            em.AddComponentData(e, SoulArts.Fresh(day));
                            filled++;
                        }
                        catch (Exception ex)
                        {
                            SoulLog.Exception("arts.migrate." + e.Index, ex);
                        }
                    }
                    all.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Throttled("arts.migrate", "ArtsSystem: migration pass failed: " + ex.Message, 60000);
            }

            try
            {
                NativeArray<Entity> souls = m_Arts.ToEntityArray(Allocator.Temp);
                try
                {
                    int faults = 0;
                    for (int i = 0; i < souls.Length; i++)
                    {
                        Entity e = souls[i];
                        if (!Matches(e, lane, ArtsLanes)) continue;
                        try
                        {
                            Advance(em, e, day);
                        }
                        catch (Exception ex)
                        {
                            faults++;
                            if (faults <= FaultLogCap) SoulLog.Exception("arts.fault." + e.Index, ex);
                        }
                    }
                }
                finally
                {
                    souls.Dispose();
                }
            }
            catch (Exception ex)
            {
                SoulLog.Exception("arts.loop", ex);
            }
        }

        private static void Advance(EntityManager em, Entity e, uint day)
        {
            if (!em.HasComponent<SoulArts>(e) || !em.HasComponent<SoulTalent>(e)) return;
            SoulArts a = em.GetComponentData<SoulArts>(e);
            if (a.m_LastDay == day) return;
            a.m_LastDay = day;

            SoulTalent t = em.GetComponentData<SoulTalent>(e);
            SoulMeta meta = em.GetComponentData<SoulMeta>(e);
            uint age = day >= meta.m_BirthDay ? day - meta.m_BirthDay : 0u;
            uint salt = (uint)e.Index * 2654435761u;

            byte openness = 120;
            byte conscientiousness = 100;
            bool artistic = false;
            if (em.HasComponent<SoulPersonality>(e))
            {
                SoulPersonality p = em.GetComponentData<SoulPersonality>(e);
                openness = p.m_Openness;
                conscientiousness = p.m_Conscientiousness;
                artistic = SoulQuirks.Has(p.m_Quirk, SoulQuirks.Artistic);
            }

            // A. hidden gifts (F62): everyone carries one or two, quietly.
            if (t.m_Latent == 0u)
            {
                int first = (int)Roll(day, salt + 1u, (uint)SoulArtForm.Count) + 1;
                t.m_Latent = SoulArtForm.Bit(first);
                if (Roll(day, salt + 2u, 100u) < 35u)
                {
                    int second = (int)Roll(day, salt + 3u, (uint)(SoulArtForm.Count - 1)) + 1;
                    if (second >= first) second++;
                    t.m_Latent |= SoulArtForm.Bit(second);
                }
            }

            // B. the gift surfaces (F62): late teens, sooner for artistic souls.
            if (t.m_Form == 0 && meta.m_BirthDay != 0u && age >= (uint)SoulTune.TalentMinAgeDays)
            {
                uint chance = (uint)(SoulTune.TalentPerMille + (artistic ? SoulTune.TalentQuirkBonus : 0) + openness / 8);
                if (Roll(day, salt + 7u, 1000u) < chance)
                {
                    byte form = PickLatent(t, day, salt);
                    t.m_Form = form;
                    t.m_FoundDay = day;
                    a.m_Form = form;
                    a.m_Flags |= SoulArtsFlags.TalentFound;
                    SoulFeel.Note(em, e, day, SoulEventKind.TalentUnlocked, form, e);
                    SoulFeel.Pulse(em, e, SoulTune.TalentMood);
                }
            }

            // C. practice (F62): quiet hours turn a gift into a craft.
            if (a.m_Form != 0 && day >= a.m_NextBeatDay)
            {
                uint practice = (uint)(SoulTune.ArtPracticePerMille + (artistic ? SoulTune.ArtQuirkBonus : 0) + conscientiousness / 10);
                if (Roll(day, salt + 11u, 1000u) < practice)
                {
                    a.m_NextBeatDay = day + (uint)SoulTune.ArtPracticeDays;
                    if (a.m_Sessions < 200) a.m_Sessions++;
                    if (a.m_Skill < 100)
                    {
                        a.m_Xp += (ushort)SoulTune.ArtXpPerSession;
                        while (a.m_Xp >= 100)
                        {
                            a.m_Xp -= 100;
                            a.m_Skill++;
                            StageUp(em, e, day, ref a);
                            if (a.m_Skill >= 100) break;
                        }
                        if (a.m_Skill >= 100) a.m_Xp = 0;
                    }
                    if ((a.m_Flags & SoulArtsFlags.GiftReshaped) == 0 && a.m_Skill >= (byte)SoulTune.GiftBentSkill)
                    {
                        a.m_Flags |= SoulArtsFlags.GiftReshaped;
                        SoulFeel.Note(em, e, day, SoulEventKind.GiftBentLife, a.m_Skill, e);
                        SoulFeel.Pulse(em, e, SoulTune.GiftBentMood);
                    }
                }
            }

            // D. busking (F64): music, dance or craft taken to the street.
            if (a.m_Form == SoulArtForm.Music || a.m_Form == SoulArtForm.Dance || a.m_Form == SoulArtForm.Craft)
            {
                if ((a.m_Flags & SoulArtsFlags.Busking) == 0)
                {
                    if (a.m_Skill >= (byte)SoulTune.BuskSkill && Roll(day, salt + 13u, 1000u) < (uint)SoulTune.BuskStartPerMille)
                    {
                        a.m_Flags |= SoulArtsFlags.Busking;
                        SoulFeel.Note(em, e, day, SoulEventKind.BuskStarted, a.m_Skill, e);
                        SoulFeel.Pulse(em, e, SoulTune.BuskMood);
                    }
                }
                else if (Roll(day, salt + 15u, 1000u) < (uint)SoulTune.BuskPerMille)
                {
                    if (a.m_BuskedDays < 200) a.m_BuskedDays++;
                    if (a.m_BuskedDays % (byte)SoulTune.BuskRegularDays == 0 && a.m_Regulars < 100) a.m_Regulars++;
                    if ((a.m_Flags & SoulArtsFlags.StreetLegend) == 0 && a.m_BuskedDays >= (byte)SoulTune.StreetLegendDays)
                    {
                        a.m_Flags |= SoulArtsFlags.StreetLegend;
                        Renown(ref a, SoulTune.StreetLegendRenown);
                        SoulFeel.Note(em, e, day, SoulEventKind.StreetLegend, a.m_BuskedDays, e);
                        SoulFeel.Pulse(em, e, SoulTune.StreetLegendMood);
                    }
                }
            }

            // E. bands (F63): a musical soul and a friend start one together.
            if (a.m_Form == SoulArtForm.Music)
            {
                if ((a.m_Flags & SoulArtsFlags.Banded) == 0 && !em.HasComponent<SoulBand>(e) &&
                    Roll(day, salt + 17u, 1000u) < (uint)SoulTune.BandFormPerMille)
                {
                    Entity mate = FindFriendFor(em, e, SoulArtForm.Music, SoulTune.BandMinAffinity);
                    if (mate != Entity.Null && !em.HasComponent<SoulBand>(mate))
                    {
                        em.AddComponentData(e, SoulBand.Fresh(day, mate));
                        em.AddComponentData(mate, SoulBand.Fresh(day, e));
                        a.m_Flags |= SoulArtsFlags.Banded;
                        a.m_Partner = mate;
                        SoulFeel.Note(em, e, day, SoulEventKind.BandFormed, 1, mate);
                        SoulFeel.Note(em, mate, day, SoulEventKind.BandFormed, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.BandMood);
                        SoulFeel.Pulse(em, mate, SoulTune.BandMood);
                    }
                }

                if (em.HasComponent<SoulBand>(e))
                {
                    SoulBand b = em.GetComponentData<SoulBand>(e);
                    if (day >= b.m_NextGigDay)
                    {
                        b.m_NextGigDay = day + (uint)SoulTune.BandGigDays;
                        if (Roll(day, salt + 19u, 1000u) < (uint)SoulTune.BandGigPerMille)
                        {
                            b.m_Gigs++;
                            Renown(ref a, SoulTune.BandGigRenown);
                            a.m_Sessions = 0;
                            SoulFeel.Note(em, e, day, SoulEventKind.BandGig, b.m_Gigs, b.m_Mate);
                            SoulFeel.Note(em, b.m_Mate, day, SoulEventKind.BandGig, b.m_Gigs, e);
                            if ((a.m_Flags & SoulArtsFlags.BrokeOut) == 0 && b.m_Gigs >= (byte)SoulTune.BreakoutMinGigs && a.m_Renown >= (byte)SoulTune.BreakoutMinRenown)
                            {
                                a.m_Flags |= SoulArtsFlags.BrokeOut;
                                Renown(ref a, SoulTune.BreakoutRenownBonus);
                                SoulFeel.Note(em, e, day, SoulEventKind.BreakoutAct, b.m_Gigs, b.m_Mate);
                                SoulFeel.Note(em, b.m_Mate, day, SoulEventKind.BreakoutAct, b.m_Gigs, e);
                                SoulFeel.Pulse(em, e, SoulTune.BreakoutMood);
                                SoulFeel.Pulse(em, b.m_Mate, SoulTune.BreakoutMood);
                            }
                        }
                        em.SetComponentData(e, b);
                    }

                    if ((a.m_Flags & SoulArtsFlags.Banded) != 0 && (a.m_Flags & SoulArtsFlags.BrokeOut) == 0)
                    {
                        byte neuroticism = 100;
                        if (em.HasComponent<SoulPersonality>(e)) neuroticism = em.GetComponentData<SoulPersonality>(e).m_Neuroticism;
                        if (neuroticism >= (byte)SoulTune.BandBreakupNeuroticism && Roll(day, salt + 23u, 1000u) < (uint)SoulTune.BandBreakupPerMille)
                        {
                            SoulBand band = em.GetComponentData<SoulBand>(e);
                            Entity mate = band.m_Mate;
                            short together = (short)Math.Min(day - band.m_FormedDay, 32000u);
                            em.RemoveComponent<SoulBand>(e);
                            if (em.HasComponent<SoulBand>(mate)) em.RemoveComponent<SoulBand>(mate);
                            a.m_Flags &= ~SoulArtsFlags.Banded;
                            a.m_Partner = Entity.Null;
                            SoulFeel.Note(em, e, day, SoulEventKind.BandBreakup, together, mate);
                            SoulFeel.Note(em, mate, day, SoulEventKind.BandBreakup, together, e);
                            SoulFeel.Pulse(em, e, SoulTune.BandBreakupMood);
                        }
                    }
                }
            }

            // F. the written word (F65): drafts, print, and the whole city reading.
            if (a.m_Form == SoulArtForm.Writing)
            {
                if (a.m_Sessions >= (byte)SoulTune.WorkSessions)
                {
                    a.m_Sessions = 0;
                    a.m_Flags |= SoulArtsFlags.FirstWork;
                    Renown(ref a, SoulTune.WorkRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.BookFinished, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.WorkMood);
                }
                if ((a.m_Flags & SoulArtsFlags.FirstWork) != 0 && (a.m_Flags & SoulArtsFlags.Published) == 0 &&
                    a.m_Skill >= (byte)SoulTune.PublishSkill && Roll(day, salt + 29u, 1000u) < (uint)SoulTune.PublishPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.Published;
                    Renown(ref a, SoulTune.PublishRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.BookPublished, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.PublishMood);
                }
                if ((a.m_Flags & SoulArtsFlags.Published) != 0 && (a.m_Flags & SoulArtsFlags.Bestseller) == 0 &&
                    a.m_Skill >= (byte)SoulTune.BestsellerSkill && Roll(day, salt + 31u, 1000u) < (uint)SoulTune.BestsellerPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.Bestseller;
                    Renown(ref a, SoulTune.BestsellerRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.Bestseller, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.BestsellerMood);
                }
                if ((a.m_Flags & SoulArtsFlags.WritersGroup) == 0 && Roll(day, salt + 33u, 1000u) < (uint)SoulTune.WritersGroupPerMille)
                {
                    Entity mate = FindFriendFor(em, e, SoulArtForm.Writing, SoulTune.WritersMinAffinity);
                    if (mate != Entity.Null)
                    {
                        a.m_Flags |= SoulArtsFlags.WritersGroup;
                        BumpBoth(em, e, mate, SoulTune.WritersBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.WritersGroup, 1, mate);
                    }
                }
            }

            // G. painting (F66): a mural for the city, commissions to live on.
            if (a.m_Form == SoulArtForm.Painting)
            {
                if ((a.m_Flags & SoulArtsFlags.Mural) == 0 && a.m_Skill >= (byte)SoulTune.MuralSkill &&
                    Roll(day, salt + 35u, 1000u) < (uint)SoulTune.MuralPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.Mural;
                    Renown(ref a, SoulTune.MuralRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.MuralDone, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.MuralMood);
                }
                if (a.m_Skill >= (byte)SoulTune.CommissionSkill && day >= a.m_NextEventDay &&
                    Roll(day, salt + 37u, 1000u) < (uint)SoulTune.CommissionPerMille)
                {
                    a.m_NextEventDay = day + (uint)SoulTune.CommissionGapDays;
                    a.m_Flags |= SoulArtsFlags.Commissioned;
                    Renown(ref a, SoulTune.CommissionRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.ArtCommission, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.CommissionMood);
                }
            }

            // H. photography (F67): strangers pause in front of the prints.
            if (a.m_Form == SoulArtForm.Photo)
            {
                if ((a.m_Flags & SoulArtsFlags.PhotoExhibited) == 0 && a.m_Skill >= (byte)SoulTune.PhotoExhibitSkill &&
                    Roll(day, salt + 41u, 1000u) < (uint)SoulTune.PhotoExhibitPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.PhotoExhibited;
                    Renown(ref a, SoulTune.PhotoExhibitRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.PhotoExhibit, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.PhotoExhibitMood);
                }
            }

            // I. the stage (F68): opening night, the fear, the ovation.
            if (a.m_Form == SoulArtForm.Theater)
            {
                if ((a.m_Flags & SoulArtsFlags.Opened) == 0 && a.m_Skill >= (byte)SoulTune.OpeningSkill &&
                    Roll(day, salt + 43u, 1000u) < (uint)SoulTune.OpeningPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.Opened;
                    Renown(ref a, SoulTune.OpeningRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.OpeningNight, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.OpeningMood);
                }
                byte stageFrightNeuro = 100;
                if (em.HasComponent<SoulPersonality>(e)) stageFrightNeuro = em.GetComponentData<SoulPersonality>(e).m_Neuroticism;
                if ((a.m_Flags & SoulArtsFlags.StageFright) == 0 && stageFrightNeuro >= (byte)SoulTune.FrightNeuroticism &&
                    Roll(day, salt + 45u, 1000u) < (uint)SoulTune.FrightPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.StageFright;
                    SoulFeel.Note(em, e, day, SoulEventKind.StageFright, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.FrightMood);
                }
                if ((a.m_Flags & SoulArtsFlags.Ovation) == 0 && (a.m_Flags & SoulArtsFlags.StageFright) != 0 &&
                    a.m_Skill >= (byte)SoulTune.OvationSkill && Roll(day, salt + 47u, 1000u) < (uint)SoulTune.OvationPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.Ovation;
                    Renown(ref a, SoulTune.OvationRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.Ovation, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.OvationMood);
                }
            }

            // J. film (F69): a first short, and maybe one it goes around.
            if (a.m_Form == SoulArtForm.Film)
            {
                if ((a.m_Flags & SoulArtsFlags.ShortFilm) == 0 && a.m_Skill >= (byte)SoulTune.ShortFilmSkill &&
                    Roll(day, salt + 53u, 1000u) < (uint)SoulTune.ShortFilmPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.ShortFilm;
                    Renown(ref a, SoulTune.ShortFilmRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.ShortFilm, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.ShortFilmMood);
                }
                if ((a.m_Flags & SoulArtsFlags.Viral) == 0 && (a.m_Flags & SoulArtsFlags.ShortFilm) != 0 &&
                    a.m_Skill >= (byte)SoulTune.ViralMinSkill && Roll(day, salt + 55u, 1000u) < (uint)SoulTune.ViralPerMille)
                {
                    a.m_Flags |= SoulArtsFlags.Viral;
                    Renown(ref a, SoulTune.PublishRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.FilmViral, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.ViralMood);
                }
            }

            // K. handmade things (F70): fairs, gifts and one for the ages.
            if (a.m_Form == SoulArtForm.Craft)
            {
                if (a.m_Skill >= (byte)SoulTune.FairSkill && day >= a.m_NextEventDay &&
                    Roll(day, salt + 57u, 1000u) < (uint)SoulTune.FairPerMille)
                {
                    a.m_NextEventDay = day + (uint)SoulTune.FairGapDays;
                    a.m_Flags |= SoulArtsFlags.CraftFair;
                    Renown(ref a, SoulTune.FairRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.CraftFair, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.FairMood);
                }
                if ((a.m_Flags & SoulArtsFlags.GiftMade) == 0 && a.m_Skill >= (byte)SoulTune.FairSkill)
                {
                    Entity friend = FindAnyFriend(em, e, SoulTune.GiftMinAffinity);
                    if (friend != Entity.Null)
                    {
                        a.m_Flags |= SoulArtsFlags.GiftMade;
                        BumpBoth(em, e, friend, SoulTune.GiftBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.HandmadeGift, a.m_Skill, friend);
                        SoulFeel.Pulse(em, e, SoulTune.GiftMood);
                    }
                }
                if ((a.m_Flags & SoulArtsFlags.Heirloom) == 0 && a.m_Skill >= (byte)SoulTune.HeirloomSkill &&
                    age >= (uint)SoulTune.HeirloomAgeDays)
                {
                    a.m_Flags |= SoulArtsFlags.Heirloom;
                    Renown(ref a, SoulTune.HeirloomRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.Heirloom, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.HeirloomMood);
                }
            }

            // L. dancing (F71): a partner, the judges, the wedding dance.
            if (a.m_Form == SoulArtForm.Dance)
            {
                if ((a.m_Flags & SoulArtsFlags.Partnered) == 0 && Roll(day, salt + 59u, 1000u) < (uint)SoulTune.DancePartnerPerMille)
                {
                    Entity mate = FindAnyFriend(em, e, SoulTune.DancePartnerMinAffinity);
                    if (mate != Entity.Null)
                    {
                        a.m_Flags |= SoulArtsFlags.Partnered;
                        a.m_Partner = mate;
                        BumpBoth(em, e, mate, SoulTune.DancePartnerBump);
                        SoulFeel.Note(em, e, day, SoulEventKind.DancePartner, 1, mate);
                        SoulFeel.Note(em, mate, day, SoulEventKind.DancePartner, 1, e);
                        SoulFeel.Pulse(em, e, SoulTune.DancePartnerMood);
                        SoulFeel.Pulse(em, mate, SoulTune.DancePartnerMood);
                    }
                }
                if (a.m_Skill >= (byte)SoulTune.DanceCompSkill && day >= a.m_NextEventDay &&
                    Roll(day, salt + 61u, 1000u) < (uint)SoulTune.DanceCompPerMille)
                {
                    a.m_NextEventDay = day + (uint)SoulTune.DanceCompGapDays;
                    a.m_Flags |= SoulArtsFlags.Competed;
                    Renown(ref a, SoulTune.DanceCompRenown);
                    SoulFeel.Note(em, e, day, SoulEventKind.DanceComp, a.m_Skill, e);
                    SoulFeel.Pulse(em, e, SoulTune.DanceCompMood);
                }
                if ((a.m_Flags & SoulArtsFlags.FirstDance) == 0 && em.HasComponent<SoulRomance>(e))
                {
                    SoulRomance rom = em.GetComponentData<SoulRomance>(e);
                    if (rom.m_Stage >= 4)
                    {
                        a.m_Flags |= SoulArtsFlags.FirstDance;
                        SoulFeel.Note(em, e, day, SoulEventKind.FirstDance, 1, rom.m_Partner);
                        SoulFeel.Pulse(em, e, SoulTune.FirstDanceMood);
                    }
                }
            }

            em.SetComponentData(e, t);
            em.SetComponentData(e, a);
        }

        // ---- helpers --------------------------------------------------------

        private static void StageUp(EntityManager em, Entity e, uint day, ref SoulArts a)
        {
            byte want = 0;
            if (a.m_Skill >= (byte)SoulTune.StageRenownedSkill) want = 3;
            else if (a.m_Skill >= (byte)SoulTune.StageAccomplishedSkill) want = 2;
            else if (a.m_Skill >= (byte)SoulTune.StageDevotedSkill) want = 1;
            if (want <= a.m_Stage) return;
            a.m_Stage = want;
            Renown(ref a, SoulTune.StageUpRenown);
            SoulFeel.Note(em, e, day, SoulEventKind.ArtStageUp, want, e);
            SoulFeel.Pulse(em, e, SoulTune.StageUpMood);
        }

        private static void Renown(ref SoulArts a, int delta)
        {
            int v = a.m_Renown + delta;
            if (v < 0) v = 0;
            if (v > 100) v = 100;
            a.m_Renown = (byte)v;
        }

        private static void BumpBoth(EntityManager em, Entity e, Entity other, int delta)
        {
            BumpEdge(em, e, other, delta);
            BumpEdge(em, other, e, delta);
        }

        private static void BumpEdge(EntityManager em, Entity e, Entity other, int delta)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Other != other) continue;
                int v = ed.m_Affinity + delta;
                if (v < -100) v = -100;
                if (v > 100) v = 100;
                ed.m_Affinity = (sbyte)v;
                edges[i] = ed;
                return;
            }
        }

        private static Entity FindFriendFor(EntityManager em, Entity e, int form, int minAffinity)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Affinity < minAffinity) continue;
                Entity o = ed.m_Other;
                if (o == e) continue;
                if (!em.HasComponent<SoulArts>(o)) continue;
                if (em.GetComponentData<SoulArts>(o).m_Form != form) continue;
                return o;
            }
            return Entity.Null;
        }

        private static Entity FindAnyFriend(EntityManager em, Entity e, int minAffinity)
        {
            if (!em.HasBuffer<SoulEdge>(e)) return Entity.Null;
            DynamicBuffer<SoulEdge> edges = em.GetBuffer<SoulEdge>(e);
            for (int i = 0; i < edges.Length; i++)
            {
                SoulEdge ed = edges[i];
                if (ed.m_Affinity < minAffinity) continue;
                Entity o = ed.m_Other;
                if (o == e) continue;
                if (!em.HasComponent<SoulMeta>(o)) continue;
                return o;
            }
            return Entity.Null;
        }

        private static byte PickLatent(SoulTalent t, uint day, uint salt)
        {
            int n = 0;
            for (int f = 1; f <= SoulArtForm.Count; f++)
                if ((t.m_Latent & SoulArtForm.Bit(f)) != 0u) n++;
            if (n <= 1)
            {
                for (int f = 1; f <= SoulArtForm.Count; f++)
                    if ((t.m_Latent & SoulArtForm.Bit(f)) != 0u) return (byte)f;
                return (byte)SoulArtForm.Music;
            }
            uint pick = Roll(day, salt + 67u, (uint)n);
            for (int f = 1; f <= SoulArtForm.Count; f++)
            {
                if ((t.m_Latent & SoulArtForm.Bit(f)) == 0u) continue;
                if (pick == 0u) return (byte)f;
                pick--;
            }
            return (byte)SoulArtForm.Music;
        }

        private static uint Roll(uint day, uint salt, uint span)
        {
            uint h = day * 2654435761u ^ salt * 2246822519u;
            h ^= h >> 15;
            h *= 3266489917u;
            h ^= h >> 13;
            return h % span;
        }
    }
}
