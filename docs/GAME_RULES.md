# Game Rules

Authoritative rule logic lives in `Bobeta.Application/Games/`. This document mirrors that implementation so rules stay stable when code changes. Each section lists the source files and regression tests that lock the behavior in place.

**When you change game rules:** update the code, this document, the i18n strings (`makopa_rules_body`, `kopo_rules_body`), and the tests listed below.

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
| Normal win | It is your turn to lead (no trick in progress, you are the designated leader) **and** you hold **exactly 1 card** |
| Instant loss | Before trick resolution: exactly one player holds a singleton (1 card), that player is **not** the responder, and the responder plays a card whose **suit matches** the singleton holder's remaining card → singleton holder wins immediately |
| Hand wins | Each completed trick increments the trick winner's hand-win tally (shown in UI as "Hands won") |

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

### Economy

| Rule | Detail |
|------|--------|
| Win | Same 25% platform commission on the pot as Makopa |
| Draw | Both players' locked bets are released |

---

## Platform / session rules (both games)

**Source:** `CreateGameRequestValidator.cs`, `GameSessionService.cs`, `GameInactivityCoordinator.cs`  
**Tests:** `Bobeta.Application.Tests/Validators/CreateGameRequestValidatorTests.cs`, `Bobeta.API.Tests/Services/GameInactivityCoordinatorTests.cs`

| Rule | Detail |
|------|--------|
| Bet range | 200–500 FCFA per player |
| Create game | Locks creator's bet; status = Waiting |
| Join game | Locks opponent's bet; game auto-starts |
| Inactivity | 60 s idle → first warning (10 s to Continue or Cancel); after Continue, 40 s idle → second warning; deadline expires → game cancelled, bets released |
| Real moves | Reset idle timer and dismiss warnings |
| API routing | `play-card` / `void-follow` → Makopa only; `kopo/move` → Kopo only |

---

## User-facing copy

Player-facing rule text is in:

- `Bobeta.Web.Shared/Services/I18nService.cs` — keys `makopa_rules_body`, `kopo_rules_body`
- `Bobeta.Mobile/Services/I18nService.cs` — same keys (EN + FR)

Displayed in `Bobeta.Web/Pages/GamePlay.razor` and `Bobeta.Mobile/Pages/GamePlayPage.xaml.cs`.

Keep i18n strings aligned with this document when rules change.
