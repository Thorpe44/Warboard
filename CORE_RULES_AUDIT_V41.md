# Warboard v41 — final core-rules audit

Rule basis: Warhammer 40,000 Core Rules, Edition 11, June 2026.

| Section | v41 status |
|---|---|
| 01 Core Concepts | Existing core retained; 3D geometry from v39 retained |
| 02 Datasheets | Existing importer/data model retained |
| 03 Moving | v41 adds enemy/friendly model path rules, terrain traversal and FLY integration |
| 04 Making Attacks | Existing staged attack system retained |
| 05 Attack Sequence | Existing v39 attack compliance retained; Epic Challenge integrated |
| 06 Other Concepts | Existing mortal/hazard/visibility handling retained; terrain visibility completed |
| 07 Battle Round | Existing direct event/battle flow retained |
| 08 Command Phase | Existing Core CP/Battle-shock retained; Insane Bravery window added for Traditional resolution |
| 09 Movement Phase | Transport, FLY, AIRCRAFT and ingress interactions completed |
| 10 Shooting Phase | Terrain/cover/Hidden/Smokescreen/Plunging Fire integrated |
| 11 Charge Phase | FLY/AIRCRAFT and Heroic Intervention/Crushing Impact integrated |
| 12 Fight Phase | v40 fight sequence retained; Counteroffensive and AIRCRAFT targeting integrated |
| 13 Terrain | Exposed/Light/Dense, movement, cover, Hidden, obscuring/Solid approximation completed |
| 14 Objectives | v39 objective geometry/control retained |
| 15 Stratagems | Existing Command Re-roll/Fire Overwatch retained; remaining core stratagem interactions added |
| 16 Actions | v39 action eligibility/completion retained |
| 17 Monsters & Vehicles | Existing close-quarters shooting retained; movement-through-model rule integrated |
| 18 Transports | Completed in v41 |
| 19 Attached Units | Existing joined-unit architecture retained and supported by transports |
| 20 Strategic Reserves | Existing ingress/reserves retained; Rapid Ingress integrated |
| 21 Flying & Surging | Take to the Skies and AIRCRAFT-aware surge target selection integrated |
| 22 Other Rules & Abilities | Existing ability framework retained; Plunging Fire completed |
| 23 Aircraft | Completed in v41 |
| 24 Core Abilities | Existing UniversalRuleEngine/RulesEngine coverage retained |
| 25 Muster Armies | Core battle-size/Warlord/unit-limit validation completed; faction-specific metadata remains faction-owned |

### Deliberate physical abstractions

Warboard uses Unity colliders and model transforms, so line-of-sight, terrain-area boundaries, Solid openings, vertical surfaces and "as close as possible" emergency placement are enforced using the available 3D battlefield geometry rather than a manual tape-measure adjudication layer. Traditional mode continues to leave physical dice interpretation to the players.
