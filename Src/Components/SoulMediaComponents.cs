// Citizen Soul -- M5l: SoulMedia component (F135-F141).
// What a person watches and listens to: the must-watch night, the comfort
// rewatch and the cliffhanger that follows, the music era that owns them,
// festival tickets and the merch after, the morning show and the headlines,
// the podcast that grows (or blows up), cinema nights and the shared film,
// and the tabletop campaign that outlives the group chat.
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace CitizenSoul
{
    public static class SoulMediaFlags
    {
        public const byte MustWatch = 1 << 0;   // F135: keeps a weekly appointment with a show
        public const byte Rewatcher = 1 << 1;   // F135: the comfort one, again and again
        public const byte Cliffhanger = 1 << 2; // F135: left hanging and it itches
        public const byte EraSet = 1 << 3;      // F136: a decade claimed them
        public const byte ConcertGoer = 1 << 4; // F137: was in the crowd, voice gone
        public const byte FestGoer = 1 << 5;    // F137: goes back every year
        public const byte MorningShow = 1 << 6; // F138: the morning show knows them
        public const byte NewsJunkie = 1 << 7;  // F138: reads the headline and feels it
    }

    public static class SoulMediaFlags2
    {
        public const byte ShopRadio = 1 << 0;   // F138: hums along in the aisles
        public const byte Announcer = 1 << 1;   // F138: the traffic voice is theirs
        public const byte Podcaster = 1 << 2;   // F139: pressed record one night
        public const byte PodBlown = 1 << 3;    // F139: it got out of hand, beautifully
        public const byte Commuter = 1 << 4;    // F139: commutes to a stranger's voice
        public const byte Cinephile = 1 << 5;   // F140: the big screen, always
        public const byte Critic = 1 << 6;      // F140: writes it down, people quote it
        public const byte DM = 1 << 7;          // F141: took the chair behind the screen
    }

    public struct SoulMedia : IComponentData, ISerializable
    {
        public byte m_Flags;          // SoulMediaFlags
        public byte m_Flags2;         // SoulMediaFlags2
        public byte m_Era;            // F136: which decade owns them (1..5), 0 = not yet
        public byte m_Show;           // F135: the weekly show (1..4), 0 = none yet
        public byte m_PodStage;       // F139: 0 none, 1 started, 2 growing, 3 blown up
        public byte m_Sessions;       // F141: tabletop sessions played so far
        public Entity m_CampaignMate; // F141: the friend in the campaign
        public Entity m_Fandom;       // F137: someone met in the crowd
        public uint m_LastSongSummer; // F136: last cycle the song hit
        public uint m_LastFestival;   // F137: last time the festival took them
        public uint m_LastSessionDay; // F141: when the dice last rolled
        public uint m_LastDay;

        public static SoulMedia Fresh(uint day)
        {
            SoulMedia m = default;
            m.m_LastDay = day;
            return m;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Flags);
            writer.Write(m_Flags2);
            writer.Write(m_Era);
            writer.Write(m_Show);
            writer.Write(m_PodStage);
            writer.Write(m_Sessions);
            writer.Write(m_CampaignMate);
            writer.Write(m_Fandom);
            writer.Write(m_LastSongSummer);
            writer.Write(m_LastFestival);
            writer.Write(m_LastSessionDay);
            writer.Write(m_LastDay);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Flags);
            reader.Read(out m_Flags2);
            reader.Read(out m_Era);
            reader.Read(out m_Show);
            reader.Read(out m_PodStage);
            reader.Read(out m_Sessions);
            reader.Read(out m_CampaignMate);
            reader.Read(out m_Fandom);
            reader.Read(out m_LastSongSummer);
            reader.Read(out m_LastFestival);
            reader.Read(out m_LastSessionDay);
            reader.Read(out m_LastDay);
        }
    }
}
