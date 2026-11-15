# Story review: where Deepwake's story stands, and what Spencer does about being dumped

Read-only review, 2026-11-15. No story data was changed.

Sources read:

- `Docs/STORY.md` (Remco's S-69-CRYS script, the source of truth)
- `Docs/STORY_ROADMAP.md`, `Docs/CUTSCENES_CONTINENTS.md`, `Docs/BOSS_INTROS.md`, `Docs/BOSSES.md`
- The story section of `Docs/GAME_DESIGN.md` and the story notes in `Docs/STATUS.md`
- All data under `Assets/_Project/Data/Story/` and the level briefings in `Assets/_Project/Data/Levels/`

## Short answer to Remco's question

**As written, Spencer does not accept it and is not bribed. He becomes a whistleblower.** In the script he slowly starts to doubt NTI. He finds proof in continent 4 that NTI knew about the life forms. He backs Voss when she refuses orders in continent 5. In The Core he refuses a direct order with one word ("No."), has ORA delete the gag order and sends all the evidence to Earth. In the epilogue NTI loses the mission and a new team takes over.

**But the thread you remember is open.** The angry line "No life forms, they said. No complications, they said!" was added to the opening cutscene in round 63. The script was written before that line and never answers it:

- Nothing in the built levels 1 to 6 picks the anger up again. Spencer is cheerful there.
- When the truth comes out in continent 4, Voss gets the line about being sent here knowingly. Spencer, the man who shouted, does not.
- The company never speaks to Spencer personally. There is no offer, no threat and no bribe anywhere.
- The epilogue says what happens to NTI, Voss and ORA. It does not say what happens to Spencer.

None of this ending is built. Everything after level 6 of continent 1 exists only on paper.

One correction to the question: the script has **six** continents, not five. There are five on the surface plus The Core underground.

---

## 1. The story as it stands, in order

Three labels are used below:

- **BUILT**: in the game data now.
- **WRITTEN**: in the docs, not in the game.
- **MISSING**: exists nowhere.

### 1.1 Prologue and opening cutscene: BUILT

`Cutscene_Opening.asset` with the `Dlg_Cut_*` dialogues.

1. **Earth, 2053.** ORA narrates: Earth is mined out, with no lithium, cobalt or rare earths left.
2. **The survey.** AI telescopes find S-69-CRYS: metals, and crystals that "never run dry". It has air, water and a stable climate, but no lights and no signals. "A few strange heat sources below the surface were written off as volcanic activity."
3. **Orbit.** Nova Terra Industries (NTI) sends Operation New Dawn. After three years of cryosleep the mothership arrives. ORA: "No life forms detected. Expected complications: none. Welcome to S-69-CRYS, Commander."
4. **Landing.** A salvo of capsules comes down, with cargo for several continents. Spencer's capsule is last. "Pathfinder here. Ground's stable, crystals everywhere. This is our spot."
5. **Base and drill.** The base unfolds and the drill goes down. ORA: "Extraction started. Yield above expectations." Drones bring in the first crystal.
6. **Underground.** The crystals tremble with the drill. Eyes open in the dark.
7. **The tremor.** Spencer: "ORA... what is this?" ORA: "Correction. Life forms detected. Many life forms."
8. **The attack.** The Queen claws out of her own burrow and screams. A few creatures stand and glare.
9. **Spencer's anger** (added in round 63). "No life forms, they said. No complications, they said!"
10. **The gate.** Voss: "We can't go back. Earth is counting on us. Spencer, hold those gates!" ORA offers the AI turrets. Title card "Defend the base", and level 1 starts.

### 1.2 Continent 1, Crystal Plains, levels 1 to 6: BUILT

Levels `Level_C1_01` to `Level_C1_06`: Landing Site, Glass Meadows, Shard Ridge, Fault Line, Quartz Flats, Crysium Vein. Each has a briefing and dialogue triggers.

| Level | Story content in the game |
| --- | --- |
| 1 Landing Site | ORA: "Place turrets along the path." Spencer: "And whatever gets through is mine." Wave 2: "They're not testing us anymore." After the win: **"We woke something up down there."** |
| 2 Glass Meadows | Voss brings the Laser against swarms. Repair drone tutorial. |
| 3 Shard Ridge | Voss built anti-air "just to be safe". Flyers appear: "They fly too?!" After the win Spencer thanks her; Voss: "Honestly, I was hoping I'd be wrong." Harvest drone: "So the crystals buy the bullets. Fine by me." |
| 4 Fault Line | Mixed ground and air, attack drone, a flying boss. |
| 5 Quartz Flats | Teaser of the fast enemy: "What was that?! I couldn't even track it." |
| 6 Crysium Vein | Voss: "The crystals never run dry. Never. This changes everything." Land Mine and Chrono Field. |

Also built:

- Voss's scan hints per new species (`Dlg_Scan_*`) and her tips (gate upgrade, high ground, move Spencer, rear rift).
- The ORA and Voss lines for all six continent boss intros (`Dlg_Intro_*`, `Intro_*`). The bosses themselves are only playable in the developer boss arena, not in the campaign.
- The planet globe with all six regions. Continents 2 to 6 are "coming soon". Levels 7 to 20 of continent 1 are placeholder names in the catalog ("Crystal Plains 7" and so on).
- Speakers `Speaker_Nti` and `Speaker_Pioneer1`. **No dialogue line uses them.** NTI has never spoken in the game.
- Blender models for later story beats: the Pioneer-1 wreck, the Core seal in three states, the Sealed One, corrupted skins.

### 1.3 Continent 1, levels 7 to 20: WRITTEN

From `STORY.md`. Tesla (level 9), armoured enemies and the Railgun (11 to 13), "they live inside the elements, that is why our scans saw nothing" (14), healers and the Acid Sprayer (15 and 16), Lava Ball (18), R.A.T. (19), and Gravemaw (20).

Closing lines: ORA: "Crystal Plains secured. Extraction at full capacity." Spencer: "There are five more continents, Doc." Voss: "I know. And over there, they're different."

NTI and Spencer's grievance are not mentioned once in continent 1.

### 1.4 Continents 2 to 6: WRITTEN

Level tables and key dialogues are in `STORY.md`. A draft cutscene after each continent boss is in `CUTSCENES_CONTINENTS.md`; that draft is not approved yet.

| # | Continent | Story beats | Boss and closing line |
| --- | --- | --- | --- |
| 2 | Ashen Reach (fire, sand) | Confidence. Level 10: ORA stumbles ("No life forms detec... Correction."). Level 12: NTI raises the quota by 40%. Spencer: "They don't even ask how we're doing." | Cinder Matriarch. Voss: "Too easy. They're pulling back to the north. Why?" |
| 3 | Northern Crown (ice) | Hard; tower combos. Level 9: the crystal veins all point down. Level 10: intercepted NTI message, "Do not inform personnel. Protocol [REDACTED] remains active." ORA: "No access." | Frost Warden. "A Warden. A guardian. But of what?" |
| 4 | Mirewood (bog) | **Twist A.** Level 8: the wreck of Pioneer-1, an NTI probe from 2047. Level 9: its log reports life. Level 10: ORA admits Directive 7, "do not report life forms to personnel". Level 11: NTI: "The wreck is not relevant. Continue extraction." | Rotbloom, fought at the wreck. Spencer: "So they're not attacking us. We attacked them." Voss: Pioneer-1 drilled too, and then the attack began. |
| 5 | Southern Crown (ice, mixed) | NTI forces superweapons on the team. Level 14: an overcharged crystal blows up part of the base. Level 15: Voss refuses to overcharge anything more. Spencer: "I'm with you on that." | The Shattered. "That used to be a Burrower. Our weapons turned it into this." The ground splits open to The Core. |
| 6 | The Core | **Twist B.** Level 5: the veins are a seal. Level 8: NTI orders maximum extraction; Spencer: "No." Level 9: Spencer deletes Directive 7 and sends everything to Earth; ORA: "Commander? Thank you." Level 10: the seal breaks and the Burrowers fight beside us. Level 15: a ring of overcharged crystals will press the seal shut. | The Sealed One. ORA: "Seal restored." A Burrower looks at Spencer and digs away. Spencer: "Pretty sure that was a thank-you." |

### 1.5 The ending: WRITTEN

Epilogue in `STORY.md`:

- The logs reach Earth. NTI loses the mission and a new team takes over.
- Mining continues at the surface only, far from the veins, by agreement with the Burrowers.
- Voss: "Earth ran empty because nobody looked after it. Here, we do it differently."
- ORA: "All systems nominal. Life forms detected: many. Status: neighbours."

The cutscene draft adds proposals: the thanking Burrower is the Queen from the opening, the big drill is capped, and the game shows its first daylight.

### 1.6 MISSING

- Any answer to Spencer's anger in the opening.
- Any personal exchange between NTI and Spencer or Voss. NTI only sends orders.
- Why NTI hid the truth, said out loud.
- Spencer's fate after the ending.
- What the Sealed One is, and who or what made the seal.
- Per-level dialogue for most levels in continents 2 to 6. Only the key beats are written.

---

## 2. Characters and where each arc ends

**Cole "Pathfinder" Spencer** (field scout; ORA calls him "Commander")

- Character note in the script: level-headed, dry humour, loyal to his team, "starts to doubt NTI the more he discovers".
- Built: jokes, competence, and one burst of anger in the opening.
- Written arc: doubt in continent 2, questions in 3, proof in 4, backs Voss in 5, refuses and blows the whistle in 6.
- Ends on the thank-you line. He is not in the epilogue. His arc is the least finished of the cast.

**Dr. Elena Voss** (chief scientist)

- Starts as the one who pushes: "We can't go back. Earth is counting on us."
- Builds every weapon, grows curious about the Burrowers, then doubts.
- Refuses NTI in continent 5: "NTI can fire me if they want."
- Decodes the seal and designs the fix.
- Has the closing moral of the game. Hers is the most complete arc.

**ORA** (base AI)

- Dry and factual. Lies by order from the first line.
- Glitches from continent 2, refuses access in 3, confesses Directive 7 in 4.
- Freed by Spencer in 6: "Commander? Thank you."
- Her last line mirrors her first. A complete arc.

**NTI** (the company)

- A faceless voice: a quota message, redacted memos, "Continue.", "Deployment mandatory.", "This is an order."
- Its motive is never stated.
- Its end is one sentence in the epilogue.
- It never appears in the built game except as a logo, a flag and a name in the prologue.

**The Burrowers** (the planet's life)

- From "infestation that attacks us" to "defenders we attacked" to allies.
- The Queen screams in the opening and, in the cutscene draft, gives the thank-you at the end. A good bookend.

**The Sealed One**

- The thing under the seal. It summons corrupted versions of known enemies.
- It has no motive, no origin and no voice. It is a final boss, not a character.

**Pioneer-1**

- An unmanned NTI probe from 2047. It found life, drilled, was attacked and crashed.
- It is the evidence for twist A.

---

## 3. Open threads and contradictions

### 3.1 Spencer's grievance (the main one)

1. **The seed has no follow-up.** The anger line is new, and nothing after it knows it exists. In levels 1 to 6 he says things like "So the crystals buy the bullets. Fine by me."
2. **"They" is unclear.** Is it ORA, who literally said the words, or NTI, who briefed him? The line works best if it means NTI. ORA being the one who was ordered to lie is then the later sting.
3. **The payoff goes to the wrong person.** In continent 4, level 10, Voss says "They sent us here knowing there was life here." Spencer only asks "ORA. Did you know?" The natural callback, his own words thrown back quietly, is not there.
4. **The opening spoils twist A a little.** The player hears about "heat sources written off as volcanic" and hears Spencer shout "they said". Most players will suspect NTI by level 1. That is fine as dramatic irony, but the reveal in continent 4 then has to give more than "they knew". The script does have more: Pioneer-1 drilled too, so we struck first. That part should carry the scene.
5. **No temptation and no cost.** Spencer's "No." in The Core costs him nothing visible. NTI never tried to keep him on side, so refusing is easy.
6. **No fate.** He does not go home, stay or get a new role. The story just stops for him.

### 3.2 NTI's role and motive

- Why hide the life forms? The obvious answers are that no crew would have signed up, that Earth's authorities would not have approved the mission, or that Earth is too desperate to care. None is ever said.
- "A new team takes over": who? The cutscene draft has a rule of no new named people, so this stays abstract.
- Voss says "We can't go back." Why not, with a mothership in orbit? One line would fix it, for example that the ship only flies home with full holds.
- NTI is both the villain and the supplier of every weapon the player enjoys. The superweapons in continent 5 are forced on the team in the story, but they are a reward in gameplay.

### 3.3 The secret under the planet

- `GAME_DESIGN.md` and `CLAUDE.md` still describe the old version: five levels, with the secret revealed in "the last level of the first world" as an opening to a next world. The script moved it to a six-continent arc. Those sections are stale.
- `GAME_DESIGN.md` says technology is found "in the remains of an older civilisation". In the script Voss develops everything, and Crysium powers it.
- `BOSSES.md` has The Shattered come out of "old ruins" through a sealed gate. `STORY.md` says it is a Burrower overloaded by our weapons. Ruins imply builders, which the script does not have. This is already flagged as question 5 in the cutscene draft.
- Who made the seal? Are the Burrowers its makers or only its keepers? Unanswered.
- Hints arrive earlier in the built game than in the script. "We woke something up down there" is in level 1, and "I think it heard us digging" is in the Gravemaw intro. The script only names the hint in continent 3. This is not a problem, but the later reveals must not pretend it is new.
- `SealState`, cosmetic or with consequences, is still an open decision in the roadmap.

### 3.4 Resources versus the living planet

- The ending is a compromise: mining continues, gently. It never says whether that saves Earth. The prologue's stakes ("Earth is empty") are not answered.
- Crysium "never runs dry" is set up as a wonder in level 6 and never explained. Reserve twist D (the Burrowers grow the Crysium) would explain it and would give the compromise a reason: you need them alive to have crystals at all. It is unused.
- The player kills Burrowers for about 110 levels and is told at level 80 that we struck first. Gameplay does not change until The Core, level 10. This is common in the genre, but Spencer and Voss need to voice the discomfort, or it reads as the game not noticing.

### 3.5 Smaller points

- **Rank.** The prologue calls Spencer a "field scout", ORA calls him "Commander", and `GAME_DESIGN.md` says the player is the commander of the base. This can be turned into a feature; see option 2.
- **Dates.** The prologue says it is 2053 and then mentions three years of cryosleep. Voss later says 2047 is "six years before our mission". It is unclear whether 2053 is the launch or the arrival.
- **Mechanics.** Boss mechanics differ between `BOSSES.md` and `STORY.md` for the Matriarch, the Warden and the Sealed One. This is already listed in the cutscene draft and does not touch the story.
- **Scope.** The script is 120 levels and six are built. Remco said on 2026-11-12 that this must not become a mega project. If the game ships with fewer continents or levels, the ending and Spencer's arc must still land. Each option below lists its minimum beats for that reason.

---

## 4. Options for Spencer's arc and the ending

All four keep twists A and B, the bosses and the seal ending. They differ in what Spencer does with his anger. All are text-box work plus at most one or two cutscene shots. None needs a new system.

### Option 1: The Whistleblower (the script as written, with the thread tied off)

Spencer never wavers. His anger turns into suspicion, then proof, then exposure.

How it plays out per continent:

- **1 Crystal Plains.** Two or three dry callbacks keep the grievance alive. For example, after the flyers: "Let me guess. Nobody mentioned those either." At Gravemaw: "File that under 'no complications'."
- **2 Ashen Reach.** As written: the quota message and "They don't even ask how we're doing."
- **3 Northern Crown.** As written: the redacted memo.
- **4 Mirewood.** Spencer gets the callback at Directive 7, quietly this time: "No life forms, they said." Voss's line follows.
- **5 Southern Crown.** As written.
- **6 The Core.** As written, plus one epilogue beat for Spencer. For example, the new team asks for a pathfinder who knows the neighbours, and he stays.

Reuses: everything. Changes: about six new lines, one reassigned line and one epilogue beat.

- **For:** cheapest, and nothing in the script moves.
- **Against:** NTI stays a faceless order machine, Spencer's "No." stays free of cost, and it does not use the "bribed to rise up the ranks" idea.

### Option 2: The Offer (recommended)

NTI answers Spencer's anger the way a company would: with rank, pay and a ticket home. He is bought one step at a time, almost without noticing. The "No." in The Core then turns down something real.

How it plays out per continent:

- **1 Crystal Plains.** The same callbacks as option 1. After Gravemaw comes the first NTI message in the game: congratulations and a promotion. "Field Scout Spencer is hereby appointed Site Commander. Hazard bonus approved." Spencer: "Hazard bonus. For the hazards that weren't there." This also explains why ORA calls him Commander.
- **2 Ashen Reach.** The quota message in level 12 comes with another step up and a share of the yield. Spencer takes it: "Fine. Somebody should get paid for this." Voss notices.
- **3 Northern Crown.** The redacted memo. Spencer has more to lose now and lets ORA's "No access" stand a little too easily. Voss: "You used to ask twice."
- **4 Mirewood.** The wreck and Directive 7, with Spencer's quiet callback. In level 11, NTI's "the wreck is not relevant" becomes the real offer: "Director of Surface Operations. A seat on the first ship home. The log stays where it is." He does not answer. NTI takes silence as a yes. The existing beat "NTI: continue; the team doubts" for levels 11 to 14 now has content.
- **5 Southern Crown.** The superweapons arrive addressed to "Director Spencer". He deploys them, as the player does. In level 14 the crystal destroys part of the base on his watch. In level 15, when Voss refuses, "I'm with you on that" becomes the first crack. The Shattered is what he made, not only what NTI made.
- **6 The Core.** In level 8, NTI's order comes with the last and largest offer. "No." In level 9 he sends the logs, including the offers, so he exposes himself too. Epilogue: NTI loses the mission, and Spencer hands back the title: "Pathfinder will do."

Reuses:

- Every existing scene.
- The unused `Speaker_Nti`.
- The level 12, 10, 11 and 8 NTI messages that are already scripted.
- ORA's "Commander".
- The hero unlocks at levels 5, 10 and 15, which can read as kit that comes with the rank.

Changes:

- About 12 to 15 new or extended text-box lines.
- NTI addresses Spencer by name.
- One extra line in the epilogue.
- No new characters, levels or systems.

Minimum beats if the game ships shorter: the promotion after the first boss, the offer after the wreck, and the "No.".

- **For:** it answers Remco's question with drama and not a shrug. It gives NTI a voice and a readable motive (they buy silence because the truth would end the mission). It makes the player's own use of the superweapons part of the story. It makes the already-written "No." the climax of his arc.
- **Against:** Spencer is less clean for a while. He must stay likeable through continents 4 and 5, which is a matter of tone: he is tired and stalling, not greedy.

### Option 3: The Planet's Pathfinder (he turns earlier and stays)

Spencer breaks with NTI at the wreck in continent 4, not in The Core. From then on he and Voss work against the company from inside. At the end he does not go home.

How it plays out per continent:

- **1 to 3.** As option 1.
- **4 Mirewood.** After "We attacked them" he stops reporting to NTI. ORA covers for him, which is her first disobedience and comes earlier than in the script.
- **5 Southern Crown.** The superweapons are forced through over his objection. He uses them to protect the crew, not the quota. The base blast in level 14 proves him right.
- **6 The Core.** As written. The ending changes: the Warden's role passes to him. The frozen guardian of continent 3 is mirrored by a human who stays at the seal. Last image: Spencer at the capped drill when the new team's capsules land. "Pathfinder here. Ground's stable. Mind where you dig."

Reuses: the Frost Warden's "guarding something" beat, the capsule landing from the opening, the epilogue mining shot.

Changes: the dialogue of continents 4 to 6 is rewritten around an earlier break, ORA's arc moves forward, and the epilogue gets a new last shot.

- **For:** the strongest theme payoff, and a memorable final image that mirrors his first line.
- **Against:** the player still kills Burrowers for 30 or more levels after Spencer has taken their side, which grates. NTI's pressure in continent 5 needs a reason he cannot simply refuse. It needs the most rewriting.

### Option 4: The Canary (a twist on why he was sent)

The grievance gets worse before it is resolved. Spencer was not misinformed by neglect. The Pathfinder role exists to draw the first attack, so that NTI can measure the response before committing the expensive crews. "No complications" was the bait briefing.

How it plays out per continent:

- **1 to 3.** As option 1. The redacted memo in continent 3 gets one more visible word: "Protocol PATHFINDER remains active."
- **4 Mirewood.** The Pioneer-1 log ends with NTI's conclusion, not the probe's: send one scout, drill, and record what wakes up. Spencer reads his own call sign in a document written six years ago. Callback: "No life forms, they said. They wrote that line before I ever signed."
- **5 Southern Crown.** NTI has its data and escalates with the superweapons. Spencer and Voss both know they are the experiment.
- **6 The Core.** As written. The "No." and the upload now also release the Pathfinder file. Epilogue as in option 1 or 2.

Reuses: the wreck, the log, the redacted memos and the call sign that is already his name.

Changes: the contents of the Pioneer-1 log and one memo, and about eight lines.

- **For:** it makes the grievance personal and gives the twist teeth even for players who guessed that NTI knew.
- **Against:** it is darker than the current tone. NTI becomes cartoonishly evil unless it is handled dryly. It adds a third reveal to continent 4, which already carries twist A and "we struck first".

Option 4 can be combined with 1 or 2. Combined with 2, it becomes the reason he finally says no.

### Recommendation

**Option 2, The Offer.**

- It is the only option that turns the question into story: what does he do about it, and what does the company do about him?
- It costs only text boxes, which fits the scope limit.
- It keeps the whole script, both twists and the ending.
- It gives the faceless company the one thing it lacks: a way of talking to its people.
- If it feels too grey for Spencer, option 1 is the safe fallback and uses the same continent 1 callbacks. Those callbacks can be written now without committing to either.

This is Remco's decision. Nothing here has been built or changed.

### Decisions needed, whichever option is chosen

1. Who "they" are in the angry line: NTI's briefing (suggested) or ORA.
2. Whether Spencer gets the callback line at Directive 7.
3. Spencer's fate in the epilogue: he stays, goes home, or it is left open for a sequel.
4. NTI's stated motive for hiding the life forms, in one sentence.
5. Whether twist D (the Burrowers grow the Crysium) is used to explain "never runs dry" and to justify the compromise ending.
6. The Shattered's origin: old ruins (`BOSSES.md`) or a Burrower we overloaded (`STORY.md`).
7. Whether to update the stale story sections in `GAME_DESIGN.md` and `CLAUDE.md` (five levels, older civilisation, secret at the end of world 1).
