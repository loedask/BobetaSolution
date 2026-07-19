# Game Rules

Authoritative rule logic lives in `Bobeta.Application/Games/`. This document mirrors that implementation so rules stay stable when code changes. Each section lists the source files and regression tests that lock the behavior in place.

**When you change game rules:** update the code, this document, the i18n strings (`makopa_rules_body`, `kopo_rules_body`, `ngola_rules_body`, `domino_rules_body`), and the tests listed below.

---

## Makopa (trick-taking card game)

**Source:** `Bobeta.Application/Games/Makopa/MakopaRules.cs`, `MakopaGameEngine.cs`  
**Tests:** `Bobeta.Application.Tests/Games/MakopaRulesTests.cs`, `MakopaGameEngineRulesTests.cs`, `Services/GameEngineServiceTests.cs`

### Setup

| Rule | Detail |
|------|--------|
| Players | Exactly 2 |
| Deck | Full 52-card deck (4 suits × ranks 2–14; Ace = 14, highest) |
| Deal | 4 cards each; remaining 44 cards are **never used** (no drawing) |
| First leader | Chosen at random (session-seeded) |
| Card encoding | `"Suit_Rank"`, e.g. `"Spade_12"` (Queen) |

### Trick flow

| Rule | Detail |
|------|--------|
| Trick composition | Lead + response (2 plays) |
| Leading | Leader plays any card from hand; that card's suit becomes the trick suit |
| Follow suit | If the responder holds the led suit, they **must** play a card of that suit |
| Void / Take | If the responder has **no** card in the led suit, they **cannot** play a card — they must call **Take** (`VoidFollowDraw`). Attempting to play a card while void returns `must_follow_suit`. |
| Take effect | The lead card is added to the responder's hand; the trick is cleared; the **leader leads again** |
| After lead only | Turn passes to the responder |

### Trick resolution (both cards played)

| Rule | Detail |
|------|--------|
| Comparison | Only cards matching the **led suit** count; off-suit responses rank as 0 |
| Winner | Higher rank on the led suit wins |
| Tie | Leader (first play) wins |
| After trick | Both trick cards leave play permanently; trick winner leads the next trick |

### Win and instant loss

| Rule | Detail |
|------|--------|
| Empty-hand win | After a play (lead, response, or Take resolution), if either seat has **0 cards**, that player wins immediately. Takes priority over the one-card-to-lead win. |
| Normal win | It is your turn to lead (no trick in progress, you are the designated leader) **and** you hold **exactly 1 card** |
| Instant loss | Before trick resolution: exactly one player holds a singleton (1 card), that player is **not** the responder, and the responder plays a card whose **suit matches** the singleton holder's remaining card → singleton holder wins immediately |
| Hand wins | Each completed trick increments the trick winner's hand-win tally (shown in UI as "Hands won") |
| Forfeit (leave) | Leaving an in-progress match after confirm settles the pot to the opponent (25% platform fee). Inactivity cancel still refunds both and uses the inactivity message. |

### Economy

| Rule | Detail |
|------|--------|
| Pot | `betAmount × 2` |
| Platform fee | 25% of the pot |
| Winner payout | Pot minus commission |

---

## Kopo (10×10 flying-kings checkers)

**Source:** `Bobeta.Application/Games/Kopo/KopoBoard.cs`, `KopoRules.cs`, `KopoGameEngine.cs`  
**Tests:** `Bobeta.Application.Tests/Games/KopoRulesTests.cs`, `KopoGameEngineRulesTests.cs`

### Board setup

| Rule | Detail |
|------|--------|
| Size | 10×10 |
| Playable squares | Dark cells where `(row + col) % 2 == 1` |
| Pieces | 40 total — 20 per player on rows 0–3 (opponent) and 6–9 (creator) |
| Forward direction | Creator moves up (row decreases); opponent moves down |

### Movement

| Rule | Detail |
|------|--------|
| Men | Move diagonally forward one square to an empty playable cell |
| Captures (men) | Jump over an adjacent enemy to an empty landing square two steps away |
| Kings (flying) | Slide any distance diagonally on empty squares, or capture by jumping over **exactly one** enemy along the ray |
| Promotion | Man becomes king on reaching the opponent's back row (creator → row 0, opponent → row 9), applied when a capture chain ends |

### Capture rules

| Rule | Detail |
|------|--------|
| Compulsory capture | If any capture is available, quiet (non-capture) moves are forbidden |
| Maximum capture | Must choose a path that captures the **most** pieces |
| Multi-jump chain | After a capture, if further captures are possible with the same piece, that piece **must continue**; turn does not switch until the chain ends |

### Outcomes

| Rule | Detail |
|------|--------|
| Win | Opponent has 0 pieces |
| Loss | You have 0 pieces |
| Draw (stalemate) | Neither player has any legal move → bets are **released** (no winner settlement) |
| Blocked player | If only one side has legal moves, the other side loses |
| Forfeit (leave) | Leaving an in-progress match after confirm settles the pot to the opponent (same 25% fee as a normal win). Distinct from inactivity cancel, which refunds both. |

### Economy

| Rule | Detail |
|------|--------|
| Win | Same 25% platform commission on the pot as Makopa |
| Draw | Both players' locked bets are released |

---

## Ngola (2×8 seed-sowing / Mancala)

**Source:** `Bobeta.Application/Games/Ngola/NgolaRules.cs`, `NgolaGameEngine.cs`  
**Tests:** `Bobeta.Application.Tests/Games/NgolaRulesTests.cs`, `NgolaGameEngineRulesTests.cs`

Bobeta Ngola is a custom **two-row, eight-pit** ruleset (not the 4×8 Ludii variant).

### Board setup

| Rule | Detail |
|------|--------|
| Players | Exactly 2 |
| Board | 16 pits in two parallel rows of 8 |
| Ownership | Creator owns pits `0–7`; opponent owns pits `8–15` |
| Initial seeds | 4 seeds in every pit (64 total) |
| First turn | Chosen at random (session-seeded) |
| Viewer layout | Each seat sees its own row at the bottom, left-to-right as local pit indices `0–7` |

### Movement (sowing)

| Rule | Detail |
|------|--------|
| Legal start | On your turn, choose one of **your** pits containing **at least 2** seeds |
| Illegal start | Pits with 0 or 1 seed cannot start a move |
| Sowing | Empty the chosen pit, then drop one seed into each following pit **counter-clockwise** around the oval (`index → (index + 1) % 16`) |
| Turn switch | After a completed sow (and optional capture), turn passes to the opponent |

### Capture

| Rule | Detail |
|------|--------|
| Trigger | The **last** sown seed lands in an **opponent** pit |
| Occupied | That opponent pit already had seeds before the landing seed was placed (`count after sow > 1`) |
| Effect | Capture **all** seeds now in that landing pit (previous seeds + landing seed); add them to the mover's score; empty the pit |
| Empty landing | Landing in an empty opponent pit captures nothing |
| Own landing | Landing in your own row never captures |

### Outcomes

| Rule | Detail |
|------|--------|
| End condition | After a move, the player about to move has **no** pit with ≥ 2 seeds |
| Remaining seeds | Each side adds the seeds still sitting in its own row to its capture score; pits are cleared |
| Win | Higher capture score wins; loser is the other seat |
| Draw | Equal capture scores → bets are **released** (no winner settlement) |

### Economy

| Rule | Detail |
|------|--------|
| Win | Same 25% platform commission on the pot as Makopa / Kopo |
| Draw | Both players' locked bets are released |

---

## Domino (double-six draw game)

**Source:** `Bobeta.Application/Games/Domino/DominoRules.cs`, `DominoGameEngine.cs`  
**Tests:** `Bobeta.Application.Tests/Games/DominoRulesTests.cs`, `DominoGameEngineRulesTests.cs`

1v1 Domino with a double-six set (28 tiles), draw from the boneyard when stuck (*avec pioche*).

### Setup

| Rule | Detail |
|------|--------|
| Players | Exactly 2 |
| Set | Double-six (tiles `0-0` … `6-6`) |
| Deal | 7 tiles each; remaining 14 form the boneyard |
| Opening | Highest double in either hand (else highest tile); that seat must open with that tile |

### Play

| Rule | Detail |
|------|--------|
| Attach | Match either open end (`left` / `right`) |
| Draw | If no legal play and the boneyard still has tiles, draw one and keep the turn |
| Pass | If no legal play and the boneyard is empty, pass |
| Win | Empty your hand |
| Blocked | Both seats blocked with empty boneyard: lowest remaining pip count wins; equal pips → draw |

### Economy

| Rule | Detail |
|------|--------|
| Win | Same 25% platform commission on the pot as other games |
| Draw | Both players' locked bets are released |

---

## Abbia (1v1 token-flip chance game)

**Source:** `Bobeta.Application/Games/Abbia/AbbiaRules.cs`, `AbbiaGameEngine.cs`  
**Tests:** `Bobeta.Application.Tests/Games/AbbiaRulesTests.cs`, `AbbiaGameEngineRulesTests.cs`

Simplified 1v1 Abbia: each seat throws five two-faced tokens. Carved side up scores. Higher count wins.

### Setup

| Rule | Detail |
|------|--------|
| Players | Exactly 2 |
| Tokens | 5 per seat |
| First throw | Random seat (session-seeded) |

### Play

| Rule | Detail |
|------|--------|
| Throw | On your turn, throw once; server flips your five tokens |
| Reveal | Your tokens stay visible after you throw; opponent tokens stay hidden until they throw |
| Win | After both throws, higher carved-up count wins |
| Draw | Equal carved-up counts release both bets |

### Economy

| Rule | Detail |
|------|--------|
| Win | Same 25% platform commission on the pot as other games |
| Draw | Both players' locked bets are released |

---

## Nzengué (1v1 alignment game)

**Source:** `Bobeta.Application/Games/Nzengue/NzengueRules.cs`, `NzengueGameEngine.cs`

Simplified 1v1 Nzengué for MoMo stakes: nine-point board (square corners, edge midpoints, center, diagonals), three stones per seat, placement phase then slide phase.

### Setup

| Rule | Detail |
|------|--------|
| Players | Exactly 2 |
| Board | 9 points connected by edges, midlines, and diagonals through the center |
| Stones | 3 per seat |
| First turn | Random seat (session-seeded) |

### Play

| Rule | Detail |
|------|--------|
| Place | Alternate placing one stone on an empty point until each seat has placed three |
| Move | After placement, alternate sliding one of your stones along a line to an adjacent empty point |
| Win | First player to get three stones in a row wins |
| Draw | If the current player has no legal move in the move phase, the match is a draw |

### Economy

| Rule | Detail |
|------|--------|
| Win | Same 25% platform commission on the pot as other games |
| Draw | Both players' locked bets are released |

---

## Yoté (1v1 West African capture game)

**Source:** `Bobeta.Application/Games/Yote/YoteRules.cs`, `YoteGameEngine.cs`

Bobeta house rules for Yoté: 5×6 board, 12 pieces per seat in hand, empty board at start. Place from hand, slide orthogonally, or jump-capture. After a jump, remove one more opponent piece if any remain on the board.

### Setup

| Rule | Detail |
|------|--------|
| Players | Exactly 2 |
| Board | 5 rows × 6 columns, starts empty |
| Pieces | 12 per seat in hand |
| First turn | Random seat (session-seeded) |

### Play

| Rule | Detail |
|------|--------|
| Place | Put one piece from hand on any empty cell |
| Slide | Move one of your board pieces one orthogonal step to an adjacent empty cell |
| Capture | Jump orthogonally over one opponent piece to an empty landing cell; remove the jumped piece |
| Bonus remove | After a jump, if any opponent pieces remain on the board, remove one more of your choice |
| Optional captures | Captures are never mandatory |
| One jump | At most one jump per turn; no diagonals |
| Win | Leave the opponent with zero pieces in hand and on the board |
| Draw | Both sides have 3 or fewer total pieces (hand + board), or stalemate with equal totals |

### Economy

| Rule | Detail |
|------|--------|
| Win | Same 25% platform commission on the pot as other games |
| Draw | Both players' locked bets are released |

---

## Platform / session rules (all games)

**Source:** `CreateGameRequestValidator.cs`, `GameSessionService.cs`, `GameInactivityCoordinator.cs`  
**Tests:** `Bobeta.Application.Tests/Validators/CreateGameRequestValidatorTests.cs`, `Bobeta.API.Tests/Services/GameInactivityCoordinatorTests.cs`

| Rule | Detail |
|------|--------|
| Bet range | 200–500 FCFA per player |
| Concurrent seats | At most one open Waiting table you created, and at most one live InProgress match |
| Create game | Locks creator's bet; status = Waiting |
| Join game | Locks opponent's bet; game auto-starts |
| Inactivity | 60 s idle → first warning (10 s to Continue or Cancel); after Continue, 40 s idle → second warning; deadline expires → game cancelled, bets released |
| Real moves | Reset idle timer and dismiss warnings |
| API routing | `play-card` / `void-follow` → Makopa only; `kopo/move` → Kopo only; `ngola/move` → Ngola only; `domino/move` → Domino only; `abbia/throw` → Abbia only; `nzengue/move` → Nzengue only; `yote/move` → Yote only |

---

## User-facing copy

Player-facing rule text is in:

- `Bobeta.Web.Shared/Services/I18nService.cs` — keys `makopa_rules_body`, `kopo_rules_body`, `ngola_rules_body`, `domino_rules_body`, `abbia_rules_body`, `nzengue_rules_body`, `yote_rules_body`
- `Bobeta.Mobile/Services/I18nService.cs` — same keys

Displayed in `Bobeta.Web/Pages/GamePlay.razor` and `Bobeta.Mobile/Pages/GamePlayPage.xaml.cs`.

Keep i18n strings aligned with this document when rules change.
