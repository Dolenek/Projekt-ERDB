# Pet fusion

The [Pet menu](pet-menu.md) supports manual fusion, upgrading one target lineage
and reducing the total pet count. Runs start with a button; there is no background
fusion trigger and no adventure, ascend or release workflow.

## Manual mode

Select at least two material pets. The preview identifies consumed pets and possible
result species. The command is `rpg pets fusion <IDs>` and needs no subsequent game
confirmation. Three or more parents are supported in manual mode.

Common species use the highest parent count, with ties allowing multiple outcomes.
A special species takes priority. Different special species or different special
skills cannot be combined. Skill inheritance and tier-up are not guaranteed.
Manual selections of more than three pets require explicit confirmation before
any command is sent. The warning states that all selected pets become one pet.

## Automatic planning

- TT must be entered as a nonnegative integer.
- `PetFusionRecipes` encodes the supplied recommendation chart for result tiers II–XX
  and TT bands 0–9, 10–24, 25–40, 41–60, 61–90, 91–120 and 121+.
- Empty chart entries have no automatic recipe. Chart annotations do not create
  probability estimates or additional recipes. Manual parsing supports tiers I–XXV.
- Automatic steps always use two pets. The higher parent is result tier minus one;
  the lower parent comes from the chart. There is no fallback to unsupported pairs.
- Upgrade chooses a ready donor first, otherwise recursively prepares missing
  material from the explicitly selected pool. The result inherits the target role.
- Reduction prefers the lowest available recommended tier-up and stops when the
  total inventory count reaches the requested count or no pair remains.
- Equal candidate donors prefer lower score, then ordinal ID.
- Species mixing defaults off. Automatic pairs must preserve the target species,
  so two different common species cannot be mixed automatically. An unprotected
  special target may consume a compatible common donor when mixing is enabled.
- Actual inventory and protections are reevaluated after each result, including
  results that do not increase tier. Preparing material can stop if it becomes protected.

## Execution and recovery

`PetFusionPlanner` and `PetIdentityMapper` are independent of WPF and Discord.
`PetFusionWorkflow` uses an injected `IPetGateway`; `DiscordPetGateway` implements
the live protocol through the engine's shared send lane and minimum command gap.

Before the first fusion, inventory is reloaded and selections are reconciled.
The live success reply is recognized by its owner-matched pets heading, `You have
got a new pet!`, result ID and tier; the reply need not contain the word fusion.
After each recognized fusion reply, all pages are reloaded. The expected surviving
fingerprints, count delta, result tier and possible species must agree before the
next step is planned. IDs can change for both the result and surviving pets.

Fusion is excluded from blind resend in `DiscordCommandSendPolicy`. Missing or
unrecognized replies, unexpected inventory changes and ambiguous identity stop the
run. No speculative retry is sent. A verified EPIC RPG author alone is not proof
of successful fusion. Unknown reply formats require parser review with a real
message sample before automation can continue reliably.

Author verification tolerates Discord APP/BOT badges, Unicode spacing and invisible
formatting in the author name. If author metadata is absent, only a leading EPIC RPG
message header is accepted; mentions in the body and explicit other authors are rejected.
Missing-reply diagnostics include outgoing/reply IDs and the extracted author.
Discord's `Verified AppAPP` decoration is also accepted. Reply message snowflakes
must be newer than the outgoing message in the same channel. If the sender returns
an older quoted message, Pets polls for a new reply for up to ten seconds without
resending the command; an old inventory cannot satisfy this check.

Tests under `EpicRPGBot.Tests/Pets` cover parser, catalog boundaries, protections,
planning, identity reconciliation, persistence, paginated message edits, cancellation,
concurrent workflow rejection and failure handling without sending real game commands.
