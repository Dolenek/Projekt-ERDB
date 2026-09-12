# Pet menu

The Control Center's **Pets** button opens a modal pet management window.
Opening the window reserves the bot's exclusive workflow slot, drains the current
send cycle and pauses scheduled automation. Close the window to release the slot.
An active interactive command prevents opening the window.
The default mode is **Auto merge material**: selected material is fused in TT-recommended
pairs until the requested total pet count is reached or eligible pairs run out.
Each successful step reloads all pet IDs before the next pair is planned.

## Inventory and selection

- Refresh sends `rpg pets` and reads every page, including edits of the same Discord message.
- Pet parsing prefers the live message text, which retains embed line breaks; detached
  rendered-text clones can concatenate pet headers and fields. Pagination recognizes
  EPIC RPG's custom `epic_arrow_right` emoji as well as standard arrow labels.
- The configured profile player name identifies the owner; when absent, `rpg p` supplies it.
- Inventory must contain the advertised number of unique IDs before fusion is available.
- Rows show ID, species, tier, score, status, skills and the reason a pet is protected.
- Search and species, tier and skill filters narrow the view. Tier accepts numbers or Roman numerals.
- Column headers support sorting. Material checkboxes select manual parents or the automatic material pool.
- **Select all unprotected** selects eligible pets across the whole inventory, including filtered-out rows.
- **Target ID** selects the pet to upgrade. Targets do not have to be selected as material.
- Preview describes the next fusion; subsequent steps depend on actual game results.
- Manual mode explicitly fuses the whole selection together. Selecting more than
  three pets requires a Yes/No confirmation showing count and IDs; No is the default.
  Declining sends no command. Automatic modes continue to execute pairs sequentially.

## Protections

All modes honor manual locks and exclude non-idle pets and unrecognized details.
Default enabled protections preserve one best pet of each species, special species,
and pets with EPIC, ASCENDED or PERFECT. Each protection has its own switch.
Best means highest tier, then highest game-reported score, then ordinal ID.
Best protection is recalculated after every refresh and fusion and also applies to
an explicitly selected upgrade target; disable the relevant protection to use it.

The recognition catalog currently accepts Cat, Dog, Dragon, Bunny, Pony, Worker and
Snowman, ordinary ranked skills and the unranked Farmer, Leader and Gifter skills.
Unknown species/skills remain visible but ineligible; extend the recognition catalog
and parser fixtures when adding verified game formats. Non-cat/dog/dragon species
are treated as special.

## Saved preferences and identity

`%LocalAppData%/EpicRPGBot.UI/settings/pet-settings.ini` stores TT, protection switches,
species mixing and owner-scoped lock fingerprints. The strategy is TT recommended
pairs; material and target selections last only for the open window.

Pet IDs are not persistent identities. Fingerprints use species, tier, score and
skills. Ambiguous mappings block fusion instead of following an old ID. Identical
pets with different selections/locks cannot be distinguished safely. **Reset
selections and locks** explicitly discards those assignments and reloads inventory;
the protection switches remain enabled as configured. Review and reselect pets afterward.

## Execution controls

- **Start fusion** starts the operation shown in the preview.
- **Stop** prevents further steps and waits for an already sent fusion to settle.
- Closing during a run requests Stop; close again after the operation finishes.
- Errors are shown in the window and Console under `[pets]`.
- Automation resumes on close only if it was running before opening Pets and there
  was no error or user Stop. An unresolved result keeps the bot stopped, even after refresh.

See [Pet fusion](pet-fusion.md) for planning and execution rules.
