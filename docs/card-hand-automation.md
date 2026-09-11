# Card Hand Automation

Card hand is a tracked, opt-out automation. While `Start Bot` is active and `Auto Card Hand` is enabled, the scheduler reads `card hand` from `rpg cd` and sends `rpg card hand` when ready.

## Game model

- The draw pool contains the 52 standard cards and one Joker.
- Button labels use suit first, such as `H2`, `DK`, and `SA`; `pass` is an action and `hands` is ignored.
- A pass keeps the visible hand and draws one unseen card.
- Selecting a card discards it permanently and draws two distinct unseen cards.
- Joker is suitless and non-wild. It only has special meaning in Four Aces plus Joker.
- Final hands are classified in the documented EPIC RPG reward priority, so the highest-priority overlapping category wins.

## Decision policy

Every visible action is compared by expected configured reward value. Final reward quantities are individually floored after applying ownership and rank multipliers, then multiplied by the nine saved reward weights.

The ownership multiplier is `0.15 + 0.17 × owned cards in the final hand`. Goldened cards count as owned without an extra multiplier. Matching-rank bonuses are averaged for two-pair hands; a full house uses `(2 × triple bonus + pair bonus) / 3`, matching EPIC RPG's observed payout examples.

The decision engine uses a five-second sampled full-horizon expectimax search. It exactly enumerates affordable final-round outcomes, samples larger earlier chance nodes without replacement, memoizes canonical states, and seeds sampling from the frozen game state and settings. Effective ties prefer a higher chance of positive weighted payout, then `pass`, then stable card order.

Ownership and reward weights are frozen when a game starts. Every round logs the cards, chosen action, completed terminal sample count, estimated value, and positive-payout probability.

## Discord coordination

The complete three-action game holds the global send lane. Other scheduled commands, event responses, and deck imports cannot send into the active hand.

The bot accepts either a new EPIC RPG message or an update to the current prompt. Round correlation enforces the `2 → 3 → 4 → result` progression, prefers the newest valid message, and ignores transient edits to already-consumed prompts. If solving, button lookup, or the preferred click fails, it tries the visible `pass` button. If that fallback fails or no next prompt/result arrives within the bounded wait, automation stops and raises a desktop alert for manual recovery.

An accepted game arms a 24-hour fallback. A `wait at least` response overrides that delay, and later `rpg cd` snapshots remain authoritative.

## Settings and ownership import

`Settings > Card Hand` contains:

- `Auto Card Hand`, enabled by default.
- Non-negative decimal weights for time capsules, round cards, ETERNAL/GODLY/OMEGA lootboxes, flasks, time cookies, guild rings, and arena cookies.
- The owned-card count, last successful UTC load time, and `Load card deck` action.

Deck loading sends `rpg card deck` through the engine send lane when running, or the confirmed-command sender when stopped. The native attachment is normalized to the fixed 52-card grid plus Joker and classified with OpenCV. Dark cells are unowned; bright and goldened cells are owned.

Persisted ownership is replaced only when all 53 cells have a valid, confident classification. A failed download, layout check, or ambiguous cell preserves the previous deck and alerts the user. Before the first valid import, all cards are treated as unowned and the baseline multiplier is logged.

Default weights are: time capsule `100`, round card `50`, ETERNAL lootbox `50`, GODLY lootbox `20`, OMEGA lootbox `2`, flask `2`, time cookie `1`, guild ring `0`, and arena cookie `0`.
