# Citizen Soul — Feature Catalog
**v0.2 — 2026-10-10 · 61 features, 7 categories · continued in FEATURES-2.md — 90 more (F62–F151)**

The core five (friends · learning · injury · fights · emotions) are the spine. This catalog is
the full "real human" expansion: **61 additional behaviors**, each an *emergent combination of
six shared engines* — not 61 bespoke systems. That is the whole trick to shipping this at
100k+ citizens. Where vanilla already has a primitive (partner-seeking, births, schooling,
animals), a feature **extends the arc around it** instead of replacing the vanilla system.

## The six engines (everything below emerges from these)

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

## A. Romance, Family & Kin

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

## B. Mind, Health & Coping

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

## C. Work, Money & Ambition

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

## D. Crime, Justice & Redemption

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

## E. Culture, Leisure & Daily Life

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

## F. Social Fabric

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

## G. Life Arc, Legacy & Memory

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

## New components implied (engineering appendix)

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

## How this ships — no big bang

Feature IDs map onto milestones (ordering follows dependency, not importance):

- **M1 Soul core** — personality, mood, learning + **F16** fitness, **F18** sleep, **F19** meditation
- **M2 Relations** — **F01–F04** romance arc, **F08**, **F26**, **F42**, **F52**, **F53**, **F55**
- **M3 Damage** — **F11–F15**, **F17**, **F31–F33**, **F35**, **F47**
- **M4 Story** — **F43**, **F58**, **F61** (the journal/panel doubles as the story surface)
- **M5 Depth & dynasties** — **F05–F07**, **F09**, **F10**, **F21–F25**, **F27**, **F30**,
  **F36**, **F38**, **F56**, **F57**, **F60**
- **M6 Ship** — config toggles, localization, perf gate
- **EP1 City Life** — **F20**, **F28**, **F29**, **F34**, **F37**, **F39–F41**,
  **F44–F46**, **F48–F51**, **F54**, **F59**
