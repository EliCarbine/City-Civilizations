# Citizen Soul — Feature List

**Cities: Skylines II mod · spec v0.3 — 2026-10-10 · 151 features across 17 themes**

Every citizen gets a persistent soul — personality, emotions, skills, friendships, rivalries, injuries, fights — with a life story that evolves every day and saves with the city. None of these are bespoke systems: every feature emerges from six shared engines (personality · mood · relationship graph · social transmission · daily scheduler · story surfaces), so the 151 named features are the floor, not the ceiling.

## At a glance

### A. Romance, Family & Kin
F01 Crushes & unrequited love · F02 Courtship & dating · F03 Engagements & weddings · F04 Anniversaries & gifts · F05 Affairs & infidelity · F06 Divorce & split households · F07 Parenthood styles · F08 Childhood firsts · F09 Grandfamily · F10 Grief & widowhood

### B. Mind, Health & Coping
F11 Trauma & resilience · F12 Therapy & counseling · F13 Depression & anxiety arcs · F14 Coping habits · F15 Addiction & recovery · F16 Fitness & body arcs · F17 Chronic conditions & disability · F18 Sleep & energy · F19 Meditation & inner life · F20 Mortality awareness

### C. Work, Money & Ambition
F21 Career ambitions · F22 Performance, promotions & envy · F23 Firings, layoffs & job hunts · F24 Side hustles & street business · F25 Entrepreneurship & shopkeeping · F26 Workplace social web · F27 Burnout & sabbaticals · F28 Strikes & labor moments · F29 Lotteries & gambling · F30 Debt, hardship & charity

### D. Crime, Justice & Redemption
F31 Shoplifting & petty theft · F32 Vandalism & turf · F33 Gangs & recruitment · F34 Organized crime · F35 Arrests, trial & prison · F36 Records & second chances · F37 Corruption & scandal · F38 Probation, service & reform

### E. Culture, Leisure & Daily Life
F39 Routines & favorite spots · F40 Food culture · F41 Faith & spirituality · F42 Birthdays & parties · F43 Funerals & remembrance · F44 Nightlife & afterparties · F45 Fandoms & obsessions · F46 Fashion & status · F47 Pets & animals · F48 Holidays & seasons · F49 Neighborhood character

### F. Social Fabric
F50 Gossip engine · F51 Reputation · F52 Secrets & confidences · F53 Feuds & reconciliation · F54 Mentorship & teaching · F55 Wingmen & matchmaking

### G. Life Arc, Legacy & Memory
F56 Midlife shakeups · F57 Bucket lists & last wishes · F58 Diaries & memoirs · F59 The City Chronicle · F60 Family trees & dynasties · F61 Reunions & returns

### H. Arts, Craft & Creation
F62 Latent talents · F63 Music & bands · F64 Busking & street performance · F65 Writing & books · F66 Painting, murals & sculpture · F67 Photography · F68 Theater & performance · F69 Film & video · F70 Crafts, sewing & making · F71 Dancing

### I. Sports & Competition
F72 Pickup games · F73 Amateur leagues · F74 Running & fitness culture · F75 Martial arts & boxing · F76 Esports & gaming · F77 Chess & mind games · F78 School sports & kids' leagues · F79 Championship runs & parades · F80 Sports rivalries · F81 Injuries & comebacks

### J. Learning & Curiosity
F82 Book clubs & libraries · F83 Night classes & adult learning · F84 Languages · F85 Museums & galleries · F86 Trivia & game nights · F87 Stargazing & science nerds · F88 Philosophy & late-night talks · F89 Apprenticeships (formal) · F90 Noticing

### K. Technology & Modern Life
F91 Phones & screen habits · F92 Social posting · F93 Group chats · F94 Streaming & binge culture · F95 Tech adoption & phobias · F96 Remote work & the office era · F97 Arcades & gaming cafés · F98 Forums & pen pals · F99 Digital legacy

### L. Community & Civic Life
F100 Volunteering · F101 Neighborhood watch · F102 Community gardens · F103 Protests & petitions · F104 Town halls & local politics · F105 Charity drives & giving · F106 Jury duty & civic duty · F107 Block parties & cleanups · F108 Crisis response

### M. Nature & Animals
F109 Home gardening · F110 Birdwatching & wildlife · F111 Hiking & camping · F112 Fishing · F113 Foraging & seasons · F114 Strays & shelters · F115 Weather & seasons mood · F116 Parks & tree advocacy

### N. Everyday Quirks & Idiosyncrasies
F117 Superstitions · F118 Collections · F119 Phobias & fears · F120 Catchphrases & speech habits · F121 Stat-keeping & note-takers · F122 Lists & little systems · F123 Comfort objects · F124 Personal traditions · F125 Talkers & listeners · F126 Snoozers, night owls & the chronically late

### O. Travel, Migration & Roots
F127 Vacations · F128 Day trips & local exploring · F129 Road trips & driving culture · F130 Moving away & homesickness · F131 New arrivals & enclaves · F132 International threads · F133 Pilgrimage & meaningful journeys · F134 The hometown feeling

### P. Media & Entertainment
F135 TV & film habits · F136 Music taste eras · F137 Concerts & festivals · F138 News & radio · F139 Podcasts & talk culture · F140 Cinema culture · F141 Tabletop & long games

### Q. Rituals, Milestones & Traditions
F142 First car & license · F143 First apartment · F144 Graduations · F145 Career milestones · F146 Home milestones · F147 New Year & resolutions · F148 City & street anniversaries · F149 Farewells & going-away parties · F150 Reunions · F151 Coming-of-age traditions

## Part I — Core roster (F01–F61)

### The six engines (everything below emerges from these)

1. **Personality** — Big Five + quirks + talents per citizen; seeded, persistent.
2. **Mood & Affect** — valence/arousal + decaying affectors + salience-weighted memory.
3. **Relationship graph** — directed affinity edges (friend/rival/romance/family/trust) between citizens.
4. **Social transmission** — gossip, rumors, reputation, fame propagating along the graph.
5. **Daily-life scheduler** — routines, milestones, events on the game clock (day-sliced like vanilla).
6. **Echo layer** — where stories surface: Lifepath journal, Chirper, notifications, the city paper.
   Doubles as the drama engine — it is how the player *hears* about a life.

**Perf note:** every feature runs in the background statistical sim by default (batched,
day-sliced); full detail only for followed/nearby citizens. Every feature gets a `SoulConfig`
toggle so players can run the city at any level of madness.

---

### A. Romance, Family & Kin

- **F01 · Crushes & unrequited love** — attraction rolls from personality match + life-channel
  (style, status, age); most crushes are never confessed; rejection = mood hit + an awkwardness
  edge; a few lost loves are remembered for life as salient memories.
- **F02 · Courtship & dating** — vanilla partner-seeking extended into visible arcs: dates at
  cafés, parks, venues; courtship escalates over days; failed arcs end in heartbreak stories and
  a return to the pool.
- **F03 · Engagements & weddings** — accepted proposal → engagement → wedding event: venue,
  both friend circles invited, attendee mood spike, memories written, cost deducted (economy
  hook); young marriages get a divorce-risk grace period.
- **F04 · Anniversaries & gifts** — yearly anniversary moment; the gift is picked from the
  partner's personality (right gift = big mood, wrong = small ding); the date enters the journal.
- **F05 · Affairs & infidelity** — low agreeableness + opportunity (mood rut × attractive
  coworker) can spark an affair; secrecy tracked; discovery via gossip or caught-in-act → trust
  collapse, divorce spiral, drama propagates through both friend networks.
- **F06 · Divorce & split households** — sustained antipathy or infidelity → divorce: assets
  split, household splits, kids' mood hit with long recovery; exes usually settle as bitter
  rivals — and, years later, rarely as friends again.
- **F07 · Parenthood styles** — style is emergent from parents' traits (nurturing / strict /
  absent); children drift toward parents + peers over the years; a kid who loses a parent runs a
  grief arc with a resilience roll.
- **F08 · Childhood firsts** — first best friends, first crushes, first hobbies discovered;
  first-friend bonds persist for life if maintained and decay if not; reunions land harder (F61).
- **F09 · Grandfamily** — grandparents form with grandkids; grandparent childcare lowers the
  parents' stress; the family watches grandparents' decline (F17/F20 hooks).
- **F10 · Grief & widowhood** — death of a partner/kin/friend → strong salient memory + weeks of
  mourning behaviors (grave visits, withdrawal) until the support network catches them;
  anniversaries re-trigger it; resilient souls recover faster.

### B. Mind, Health & Coping

- **F11 · Trauma & resilience** — violence, disasters, house fires (even vanilla events) can
  leave trauma; reminders trigger it (that alley, that street); support + therapy + time heal;
  some souls get post-traumatic growth — a new purpose.
- **F12 · Therapy & counseling** — distressed citizens book sessions (clinic building / vanilla
  service hook); therapist–patient rapport is a real relationship edge; months-long arcs that
  actually move the mood; a therapist shortage means waitlists.
- **F13 · Depression & anxiety arcs** — sustained bad affect + thin support → depression:
  withdrawal from friends, performance crash, home all day; recovery paths: therapy, friendship,
  faith, a win at work; relapses possible. The anxiety variant avoids crowds, declines invitations.
- **F14 · Coping habits** — stress + low conscientiousness → a coping habit (drinking, late-night
  eating, doomscrolling Chirper); short-term relief, long-term cost; some habits stick for life.
- **F15 · Addiction & recovery** — habits can escalate to addiction; progression: secret →
  visible (late to work, missed school pickup) → crisis; rehab visits, sober streaks celebrated,
  relapses sting; recovered addicts can become counselors (rare).
- **F16 · Fitness & body arcs** — Fitness skill + routine → measurable healthiness (recovery
  speed, illness resistance); fitness fanatics are contagious; a death in the family can start —
  or kill — a personal fitness era.
- **F17 · Chronic conditions & disability** — age-scaled rolls for chronic illness; managed with
  routines (meds, clinic visits); life adjusts rather than stops: modified routines, adapted
  jobs, stronger support bonds; patients form a community.
- **F18 · Sleep & energy** — chronotype drives a real daily energy curve; bad sleep (nightlife
  F44, a newborn, stress) → clumsier, moodier next day with accident risk up; certain souls
  become park-bench nappers.
- **F19 · Meditation & inner life** — reflective practices for high-openness / high-
  conscientiousness souls: stress buffer, faster recovery; visible as the quiet park-bench
  regulars everyone recognizes.
- **F20 · Mortality awareness** — elderly souls feel time; friends dying and near-misses trigger
  bucket-list thinking (F57); some enter a "final chapter": warmer, more generous, tidier affairs.

### C. Work, Money & Ambition

- **F21 · Career ambitions** — teens form dream careers (nurtured by hobbies + mentors + skills);
  adults carry a dream-job tracker; working the dream = fulfillment; stuck elsewhere = slow-burn
  misery → mid-life pivot attempts (F56).
- **F22 · Performance, promotions & envy** — performance accrues from skills + mood +
  conscientiousness; promotions change title and pay; envious colleagues (low agreeableness)
  build rivalry edges; favoritism vibes if the boss's friend wins.
- **F23 · Firings, layoffs & job hunts** — persistent underperformance or company closure →
  fired / laid off; the job hunt is a phase: interviews (mood swings), rejections, desperation
  hires, or the moody household of holding out for the right offer.
- **F24 · Side hustles & street business** — gigs: food stalls, crafts from hobby skills,
  busking; real citizen-to-citizen commerce — a popular busker earns real money; hustles can
  grow into storefronts (F25).
- **F25 · Entrepreneurship & shopkeeping** — successful hustlers open shops; shopkeeping is its
  own life (long hours, pride, stress); shops fail (a real blow) or become institutions — the
  bakery that has been here 30 years.
- **F26 · Workplace social web** — coworker edges form on their own: lunch crews, work spouses
  (platonic), mentor chains, rivalries; office mood contagion — one toxic coworker sours a
  whole workplace.
- **F27 · Burnout & sabbaticals** — sustained overwork + low autonomy → burnout: performance
  crash, cynicism, withdrawal; choose: grind through (health cost), take a sabbatical (income
  dip, recovery), or quit the career entirely.
- **F28 · Strikes & labor moments** — toxic workplace + unfair pay → workers organize: union,
  strike events with visible picket lines, settlement or collapse; citywide sympathy mood waves.
- **F29 · Lotteries & gambling** — weekly tickets (tiny joy, almost never wins); poker rooms and
  gambling nights for thrill-seekers; an actual win = life upheaval (new "friends" appear);
  some spiral into gambling addiction (F15 link).
- **F30 · Debt, hardship & charity** — overspending vs income → debt; spiral behaviors (skip
  meals, second job, sell possessions); friends/family lend with strain if unpaid; wealthy souls
  can give — a rich citizen quietly paying a poor kid's tuition is a story the paper finds (F59).

### D. Crime, Justice & Redemption

- **F31 · Shoplifting & petty theft** — desperate or thrill-seeking souls lift goods; caught →
  fine + record; repeats escalate; shops can hire security (vanilla crime hooks).
- **F32 · Vandalism & turf** — taggers mark walls; gang turf shows on the streets; the city can
  clean and repaint; overpainting a rival's turf is a flashpoint.
- **F33 · Gangs & recruitment** — disaffected youth + no options → recruitment; gangs give
  belonging (mood up) at a moral cost; members' families stress; turf wars spike district crime.
- **F34 · Organized crime** — rare, late-stage: smuggling and protection rackets; a few
  citizens get quietly rich; police intelligence builds; takedowns ripple through the underworld.
- **F35 · Arrests, trial & prison** — violent offenders (including bad fight outcomes) can be
  arrested and sentenced by severity; prison = citizen off-map, family crisis, visits possible;
  release day is a reunion or an estrangement.
- **F36 · Records & second chances** — a record crushes job prospects; employers split on hiring
  ex-cons; a mentor, faith, or good job can flip a life (redemption arc); without support,
  recidivism; the paper tracks both (F59).
- **F37 · Corruption & scandal** — rare bribes among officials and police; exposure via
  journalist citizens × the gossip engine → scandal: firings, shaken elections, mood waves.
- **F38 · Probation, service & reform** — minor offenses → community service + check-ins;
  success softens the record, failure escalates; reform programs (GED classes, mentorship) give
  at-risk youth an off-ramp.

### E. Culture, Leisure & Daily Life

- **F39 · Routines & favorite spots** — daily loops (morning café, bench, route home) accrete
  emotional weight; disruptions (closures, construction) sting; each soul sits somewhere on a
  comfort-vs-novelty axis.
- **F40 · Food culture** — favorite dishes; restaurant regulars whom staff know; home cooking
  from the Cooking skill; foodies review spots; word of mouth moves demand.
- **F41 · Faith & spirituality** — faith carried or not (personality-weighted); services,
  bereavement groups, crisis help (a major resilience source); faith can grow in crisis or fade
  in doubt across a lifetime.
- **F42 · Birthdays & parties** — every soul has a date; friends and family (not always!)
  remember; parties sized by personality (big rager vs. quiet dinner); forgetting a close
  friend's birthday is a real ding.
- **F43 · Funerals & remembrance** — funerals assembled from the actual relationship graph (who
  shows up says everything); eulogies generated from real memories; graveyard anniversaries; an
  empty funeral is a tragedy the paper may note (F59).
- **F44 · Nightlife & afterparties** — bars and clubs, personality/age-weighted; great nights
  bond friendships and spark romance; next morning: hangover (productivity dip, grumpiness) and
  the occasional regret arc.
- **F45 · Fandoms & obsessions** — teams, bands, hobbyist followings; fans attend, buy merch,
  argue on Chirper, form clubs; a championship = a citywide mood wave for fans, misery for rivals.
- **F46 · Fashion & status** — style shifts with income, peers, and life phase (teen rebellion
  is loud, grief is muted); status symbols (car, watch) as aspiration arcs; broke souls can
  over-stretch for status (F30 link).
- **F47 · Pets & animals** — pets anchor routines and stabilize mood; pet illness or death is
  real grief (F10); dog-park walks seed friendships.
- **F48 · Holidays & seasons** — citywide holidays (parades, fireworks) and seasonal mood drift
  (winter coziness vs. summer frenzy); family drama spikes on holidays; decorations spread by
  neighborhood vibe (F49).
- **F49 · Neighborhood character** — districts accrete identity from their residents (arts
  quarter, quiet suburb, party street); vibes shift where people want to live; one anchor family
  moving in or out can turn a whole street.

### F. Social Fabric

- **F50 · Gossip engine** — rumors spawn from real events and distorted retellings; they travel
  the relationship graph with mutation (confidence decay, embellishment); gossipy souls (low
  conscientiousness + high extraversion) are super-spreaders; true/false flags matter for
  consequences.
- **F51 · Reputation** — public score vs. private self; built by visible acts (charity, scandal,
  championships); gates jobs, romance, invitations; falls fast, climbs slow.
- **F52 · Secrets & confidences** — souls hold their own and others' confided secrets; confiding
  is a trust act (bond up); a leaked confidence is a catastrophe and starts feud chains; some
  secrets are kept for life.
- **F53 · Feuds & reconciliation** — sustained negative edges → feuds: avoidance, badmouthing,
  pranks, escalation; reconciliation paths exist (forced proximity, mutual-friend mediation, a
  life event) — an old feud mended is one of the best stories the city can tell.
- **F54 · Mentorship & teaching** — experienced souls mentor crafts, careers, life skills; a
  protégé's success is a mentor's pride; chains across generations — the city's best baker was
  trained by the previous best baker.
- **F55 · Wingmen & matchmaking** — friends run set-ups, pep talks, wingman outings; busybody
  matchmakers emerge (high extraversion, low boundaries) — sometimes gifted, sometimes chaotic.

### G. Life Arc, Legacy & Memory

- **F56 · Midlife shakeups** — a midlife assessment (dreams vs. reality) manifests per
  personality: career pivot, sudden hobby obsession, the sports car (F46), existential
  conversations; growth chapter or crisis, depending on choices.
- **F57 · Bucket lists & last wishes** — private wish lists from midlife on (see the ocean,
  mentor a kid, make peace with an estranged sibling); completions are the city's most salient
  moments; unfinished lists echo in the journal.
- **F58 · Diaries & memoirs** — the LifePath journal becomes real and readable when following a
  citizen ("today I met someone…", generated from actual affectors + memories); famous
  citizens' memoirs get published, sold, and signed.
- **F59 · The City Chronicle** — an auto-generated local paper in the UI: weddings, deaths,
  scandals, sports, redemption stories, obituaries (built from life data), a gossip column —
  your city's drama digest. It finds the stories you missed.
- **F60 · Family trees & dynasties** — a persistent ancestry graph: surnames, inherited traits
  and talents, heirlooms passed down (a watch; grandmother's recipe = a cooking bonus), family
  feuds carried by descendants; a 100-year city becomes 4–5 real generations.
- **F61 · Reunions & returns** — citizens who left can return as visitors → re-movers; school
  reunions in long-lived cities; a soul comes back after 20 years, visits the old bench (F39),
  finds an old friend — the city's best-beat memories.

---

### New components implied (engineering appendix)

Beyond the v0.1 core set, the catalog needs roughly these — all `ISerializable`, version-guarded,
exact shapes pinned when each feature's milestone starts:

- `Reputation` — public score + confidence (bytes)
- `Fame` — reach tier + follower count (ushort)
- `SecretBuffer` — secret records (kind, severity, discovered flag)
- `TraumaBuffer` — trauma tags + intensity + since-day
- `Condition` — chronic illness / condition state (kind, managed)
- `HabitBuffer` — habit kind + strength + since (drinking, junk food, gambling, …)
- `Routine` — favorite-spot Entity refs + time-of-day weights
- `AspirationBuffer` — bucket-list entries (kind, target Entity?, status)
- `Faith` — devotion byte (+ tradition id)
- `Talent` — aptitude bitfield + discovery flags
- `CreatedWorkBuffer` — murals, books, songs (kind, quality, day) for arts/fame arcs
- `GamblingState` — lottery ticket rings + debt flags (may fold into `EconomyState`)
- **Service buildings to flag**: therapy clinic, rehab, prison, funeral/cemetery service,
  gambling venue (newsroom is UI-side).
- **Singletons**: `GossipNetwork` (rumor pool), `ChronicleFeed` (city event log feeding the
  paper), `EconomyState` (debt/gambling knobs), `CensusState` (aggregate tallies for the cheap
  background sim).

### How this ships — no big bang

Feature IDs map onto milestones (ordering follows dependency, not importance):

- **M1 Soul core** — personality, mood, learning + **F16** fitness, **F18** sleep, **F19** meditation
- **M2 Relations** — **F01–F04** romance arc, **F08**, **F26**, **F42**, **F52**, **F53**, **F55**
- **M3 Damage** — **F11–F15**, **F17**, **F31–F33**, **F35**, **F47**
- **M4 Story** — **F43**, **F58**, **F61** (the journal/panel doubles as the story surface)
- **M5 Depth & dynasties** — **F05–F07**, **F09**, **F10**, **F21–F25**, **F27**, **F30**,
  **F36**, **F38**, **F56**, **F57**, **F60**
- **M6 Ship** — config toggles, localization, perf gate
- **EP1 City Life** (post-ship) — **F20**, **F28**, **F29**, **F34**, **F37**, **F39–F41**,
  **F44–F46**, **F48–F51**, **F54**, **F59**

## Part II — Expansion roster (F62–F151)

### H. Arts, Craft & Creation

- **F62 · Latent talents** — every soul hides 1–2 undeveloped talents; a chance encounter (a
  friend's dare, a class, a jam session) unlocks one; an unlocked talent bends the life arc
  toward it.
- **F63 · Music & bands** — bedroom practice → chops; garage bands form; gigs at venues; band
  chemistry is its own relationship web (the drummer who quit mid-tour); breakout acts earn
  citywide fame.
- **F64 · Busking & street performance** — performers book pitches downtown; earnings scale
  with skill × foot traffic; regulars develop; a street legend draws crowds that venues notice.
- **F65 · Writing & books** — novelists, poets and diarists work evenings and coffee shops;
  finished works get published; a local bestseller changes income and social circle overnight;
  writers' groups form.
- **F66 · Painting, murals & sculpture** — painters decorate homes; the accomplished paint
  public murals neighborhoods brag about; sculptures get commissioned; arts districts hold
  art walks.
- **F67 · Photography** — hobbyists shoot city life (angles tie to favorite spots, F39);
  exhibits and a Chirper following; photojournalists catch events the Chronicle cites.
- **F68 · Theater & performance** — community productions cast real citizens; opening nights
  are social events; stage fright, divas, standing ovations; school plays pull in whole
  families.
- **F69 · Film & video** — amateur shorts; a viral one launches real careers; cinema dates
  (F02); documentary-makers chase local stories for the Chronicle.
- **F70 · Crafts, sewing & making** — knitting, woodworking, pottery, cosplay; craft fairs to
  sell (F24 hook); handmade gifts score big on F04; a legendary maker's furniture becomes an
  heirloom (F60).
- **F71 · Dancing** — studio dancers, street dancers, wedding dancers; dance partners build
  unique trust; studio competitions; a couple's first dance remembered forever.

### I. Sports & Competition

- **F72 · Pickup games** — courts, cages and skate spots; regulars form crews; winner-stays
  streaks become local legends; crews adopt their court as friendly turf.
- **F73 · Amateur leagues** — organized district teams; seasons with standings the Chronicle
  tracks; playoffs spike city mood; office leagues bond coworkers (F26).
- **F74 · Running & fitness culture** — joggers by chronotype (F18); running clubs; 5Ks to
  marathons; personal records as private milestones; runner's highs vs shin splints (M3).
- **F75 · Martial arts & boxing** — dojos teach discipline (mood regulation); sparring feeds
  real fight skill; gym rivalries; a knockout loss is either a grudge or growth.
- **F76 · Esports & gaming** — LAN cafés and ranked grinds; in-game and IRL rivalries; a local
  champion is a minor celebrity; crews overlap friend groups.
- **F77 · Chess & mind games** — park regulars, trash-talkers, prodigies; a nemesis across
  decades; puzzle and trivia culture feeds J.
- **F78 · School sports & kids' leagues** — parents on the sideline; young athletes get scouted
  (career seeds, F21); the kid who quits vs the kid who goes all the way.
- **F79 · Championship runs & parades** — deep runs trigger watch parties and district-wide
  mood; a title means a parade down Main Street; a loss is collective grief the Chronicle
  captures.
- **F80 · Sports rivalries** — fan sections and derby days; banter escalating to genuine feuds
  (F53); families split down the middle on game day.
- **F81 · Injuries & comebacks** — serious injuries (M3) force comebacks; physio arcs; the
  veteran's last season; retiring from a sport is an identity loss and a new chapter.

### J. Learning & Curiosity

- **F82 · Book clubs & libraries** — library regulars; book clubs as monthly social anchors;
  a club schism over the ending is excellent drama.
- **F83 · Night classes & adult learning** — career-changers take evening courses; retraining
  after layoffs (C); lifelong learners chase degrees; class cohorts bond.
- **F84 · Languages** — learned for travel, romance or roots; bilingual homes; kids translating
  for grandparents (F09); mispronunciation comedy.
- **F85 · Museums & galleries** — culture vultures vs once-a-year visitors; museum dates;
  school trips; gallery openings as social events (F66).
- **F86 · Trivia & game nights** — trivia teams, rival teams (friendly feuds); rotating game
  nights; Monopoly ending friendships (canon).
- **F87 · Stargazing & science nerds** — telescopes, weather-watchers, amateur radio; meteor
  showers as watch events; the telescope received as a kid's birthday gift is remembered
  forever.
- **F88 · Philosophy & late-night talks** — dorm-room existential debates that quietly reshape
  beliefs (slow personality drift); a great argument spawns a great friendship.
- **F89 · Apprenticeships (formal)** — structured master–protégé tracks (studio, garage,
  clinic); apprenticeship years; mastery day as a recognition moment.
- **F90 · Noticing** — random wonder: a flower, a poster, a fresh facade; tiny mood lifts that
  make cities feel alive and fill the journal (F58) with small poetry.

### K. Technology & Modern Life

- **F91 · Phones & screen habits** — screen time by personality; "you're always on your phone"
  fights; phone-heavy coping arcs (F14).
- **F92 · Social posting** — Chirper posts straight from mood and events; a viral post is a
  mood surge and town talk; followers scale with fame.
- **F93 · Group chats** — friend groups live in chats; inside jokes leak into real
  conversations; a silent chat signals drifting friends; left-on-read drama.
- **F94 · Streaming & binge culture** — post-work binges; watch parties; spoiler crimes as
  social offenses; "everyone's watching it" waves sweep districts.
- **F95 · Tech adoption & phobias** — early adopters queue for gadgets, late adopters grumble,
  technophobes abstain; new tech eras (drones, robotaxis) shift street life.
- **F96 · Remote work & the office era** — remote souls fill cafés at 10am (F39); commute
  rhythms shift; office friend webs (F26) thin out → loneliness risk.
- **F97 · Arcades & gaming cafés** — cabinets, ranked nights, the one kid who's unbeatable;
  birthday parties; a closing arcade is neighborhood grief (F39).
- **F98 · Forums & pen pals** — niche forums (fishkeeping, cars, modding); citizens who'd
  never meet IRL become friends; a forum friend visiting is a story beat.
- **F99 · Digital legacy** — a citizen's posts outlive them; their Chirper becomes a memorial;
  the city's data remembers (G).

### L. Community & Civic Life

- **F100 · Volunteering** — soup kitchens, shelters, animal rescues, youth coaching; volunteer
  cliques; volunteer fatigue; the quiet heroes the Chronicle spotlights.
- **F101 · Neighborhood watch** — groups form after incidents (D); patrol walks build trust;
  an overzealous watch creates friction; watched streets measurably cool.
- **F102 · Community gardens** — plots assigned, tomato rivalries; crops shared; the garden is
  neighborhood soul; a vandalized garden enrages the district (F32).
- **F103 · Protests & petitions** — citizens organize around issues (a dangerous corner, a
  closing school); marches are visible; officials respond or ignore (F37); a win changes the
  city.
- **F104 · Town halls & local politics** — meetings citizens actually attend; the regular vs
  the first-timer; rousing speeches plant political seeds.
- **F105 · Charity drives & giving** — food drives, fun runs, galas; giving grows with income;
  a district beating its goal is a mood wave.
- **F106 · Jury duty & civic duty** — summoned citizens mix across classes; the trial they sat
  on becomes a story they tell for life; foreman is an honor.
- **F107 · Block parties & cleanups** — streets close and grills come out; the annual block
  party is the neighborhood's heartbeat; blocks that party together stay together.
- **F108 · Crisis response** — after fires and floods, neighbors self-organize; heroes emerge;
  shared trauma becomes cohesion.

### M. Nature & Animals

- **F109 · Home gardening** — plots, balconies, windowsills; green thumbs vs plant murderers;
  sharing cuttings is a friendship ritual; the prized rose bush.
- **F110 · Birdwatching & wildlife** — birders keep lists; kids chase pigeons; fox sightings
  hit Chirper; a nesting eagle pair is a city event.
- **F111 · Hiking & camping** — weekend warriors; trail friendships; camping as family
  tradition; the annual trip that almost went wrong (story forever).
- **F112 · Fishing** — pier regulars; the one that got away; fish stories grow in the
  retelling (gossip synergy); quiet docks.
- **F113 · Foraging & seasons** — mushroom season genuinely felt; old-timers teach it; the
  occasional upset stomach (B).
- **F114 · Strays & shelters** — feeding street cats; shelter adoptions (beyond owned pets,
  F47); a rescue dog's glow-up becomes a neighborhood story.
- **F115 · Weather & seasons mood** — gray streaks dip moods; first snow is collective
  delight; heat waves bring crankiness and ice-cream runs; storms bring worry for loved ones
  en route.
- **F116 · Parks & tree advocacy** — planting drives, park cleanups, the citizen who fights to
  save a beloved old tree; a saved tree becomes a landmark.

### N. Everyday Quirks & Idiosyncrasies

- **F117 · Superstitions** — lucky socks for interviews, knocking wood, avoiding cracks; bad
  outcomes blamed on lapsed rituals; superstitions spread through friend groups.
- **F118 · Collections** — coins, records, figurines, stamps, bottle caps; the hunt for the
  missing piece; completing a set is a pure-joy beat; collections on display at home.
- **F119 · Phobias & fears** — heights, dogs, clowns, small spaces; real routing around fears;
  with support, exposure arcs can shrink a fear.
- **F120 · Catchphrases & speech habits** — verbal tics, favorite words, nicknames for
  everyone; phrases spread through circles; the city's slang evolves.
- **F121 · Stat-keeping & note-takers** — citizens who log everything (their team, the
  weather, late buses); their records become the Chronicle's archive (F59).
- **F122 · Lists & little systems** — to-do list people vs chaos goblins; a chaotic soul's
  rare organized day is a pride beat.
- **F123 · Comfort objects** — grandpa's watch, a lucky pen, a hidden childhood blanket;
  comfort items buffer stress; losing one is a genuine crisis.
- **F124 · Personal traditions** — Friday pizza, Sunday park loop, first-day photos; rituals
  are family glue; broken traditions are an early trouble sign.
- **F125 · Talkers & listeners** — the monologuer vs the quiet one; the listener who remembers
  everything serves as a secrets hub (F52); talker–listener duos are stable.
- **F126 · Snoozers, night owls & the chronically late** — sleep quirks (F18); the friend
  always 20 minutes late (accounted for); one eternally punctual soul who suffers for it.

### O. Travel, Migration & Roots

- **F127 · Vacations** — budget-tiered annual trips (camping → beach → abroad); countdown
  excitement, photos shared, post-vacation blues are real.
- **F128 · Day trips & local exploring** — weekend explorers: the zoo, the lake, the weird
  roadside attraction; kids' field-trip energy; a date-ideas bank.
- **F129 · Road trips & driving culture** — first cars, Sunday drives, the scenic-route cult;
  car people with washing rituals; traffic brings real road rage.
- **F130 · Moving away & homesickness** — young souls leave for school or work; homesickness
  waves; calls home; some return changed (F61), some never do — their letters still arrive.
- **F131 · New arrivals & enclaves** — new move-ins get welcome wagons or cold shoulders;
  enclaves form with food, faith and festivals the whole city learns to love.
- **F132 · International threads** — exchange students and workers; host families; friendships
  that hold for decades; pen pals (F98).
- **F133 · Pilgrimage & meaningful journeys** — returns to hometowns, spiritual trips (F41),
  visiting where grandparents are from; these trips spawn the deepest memories.
- **F134 · The hometown feeling** — lifers vs returners; longitudinal knowledge ("I remember
  when this was a field"); landmarks accrete their stories; the last original resident of a
  district.

### P. Media & Entertainment

- **F135 · TV & film habits** — must-watch nights; comfort rewatches as emotional anchors;
  cliffhanger Mondays; spoiler etiquette enforced socially.
- **F136 · Music taste eras** — teen phases to jazz-aged; tastes shift with mood and friends;
  the song that defines a summer; concert synergy (F137).
- **F137 · Concerts & festivals** — ticket chaos, merch booths, crowd euphoria; a festival is
  a citywide event with traffic chaos and magic; the encore everyone remembers.
- **F138 · News & radio** — morning-show citizens; news junkies whose moods track events; the
  shop radio running all day; traffic announcers as local celebrities.
- **F139 · Podcasts & talk culture** — citizen podcasters (F69 synergy); niche local shows;
  listening commutes; a hometown podcast blowing up.
- **F140 · Cinema culture** — premieres, critic citizens, the cinema as date spot and
  rainy-day refuge; a film shot in the city premiering at home is a pride event (F69).
- **F141 · Tabletop & long games** — campaigns spanning months; the DM as resident
  storyteller; a campaign finale is the friend group's event of the year.

### Q. Rituals, Milestones & Traditions

- **F142 · First car & license** — driver's ed, test nerves, first-car pride (usually a
  beloved beater); the car as freedom.
- **F143 · First apartment** — moving day with boxes and parent tears; housewarmings; the
  instant-noodle era that grows funnier every year.
- **F144 · Graduations** — real ceremonies with crowds and gowns; the kid who nearly dropped
  out walking anyway; family pride waves.
- **F145 · Career milestones** — the first paycheck, the first promotion toast, the retirement
  watch; thirty years at one job earns a plaque and a party.
- **F146 · Home milestones** — first dinner in the new place, the renovation era (and its
  stress), the garden's first bloom.
- **F147 · New Year & resolutions** — resolutions made (fitness! F74), kept by few; gyms full
  in January, empty by March; a resolution truly kept is character growth.
- **F148 · City & street anniversaries** — the city's founding day, a street's anniversary,
  the day the bridge opened; local pride markers.
- **F149 · Farewells & going-away parties** — departures get send-offs and speeches; the
  friend who drives them to the station; letters that follow.
- **F150 · Reunions** — school, team and family reunions organize in long-lived cities; the
  what-happened-to-everyone night; rekindled flames (A).
- **F151 · Coming-of-age traditions** — quinceañeras, bar/bat mitzvahs, sweet sixteens —
  traditions vary by citizen background (enclaves, F131); among the most memorable days in
  the journal.

### New components implied — Part II addendum

On top of Part I's list, this half needs roughly:

- `QuirkBuffer` — superstitions, fears, comfort objects (ids + intensity)
- `HobbyState` — active hobbies + gear + proficiency
- `CollectionState` — collection kind + completion
- `PhoneState` — screen habits + group-chat entity refs
- `RitualState` — personal traditions + calendar hooks
- `WorkGroup` / `Band` / `Studio` — formed groups as entities (bands, teams, productions)
- Venue entities: music venue, gym/dojo, library, museum, cinema, arcade, community garden,
  court/pitch, radio station.

(Shapes pinned per feature during its milestone; all `ISerializable`, version-guarded — same
as Part I.)

### Milestone mapping — Part II

Roughly, following dependencies again (not importance):

- **M1 Soul core** — F90, F115, F117, F126
- **M2 Relations** — F91–F94, F118, F121–F125, F135–F136
- **M3 Damage** — F75, F81, F111–F113, F119
- **M4 Story** — F65, F87, F99, F134, F150
- **M5 Depth** — F62–F64, F66–F74, F76–F80, F82–F86, F88–F89, F95–F98, F102,
  F105–F110, F114, F120, F127–F133, F142–F146, F149, F151
- **EP1 City Life** — F100–F101, F103–F104, F116, F137–F141, F147–F148
