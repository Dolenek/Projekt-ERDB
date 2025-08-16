# Captcha Solver — Design, Operation, Configuration, and Offline Evaluation

This document describes the integrated captcha solver that enables fully automatic answers to Epic RPG’s in-chat guard prompt.

Related source files:
- [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1)
- [EpicRPGBot.UI/Services/CaptchaClassifier.cs](EpicRPGBot.UI/Services/CaptchaClassifier.cs:1)
- [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1)
- [EpicRPGBot.UI/Env.cs](EpicRPGBot.UI/Env.cs:1)

Folder for reference images:
- [EpicRPGBot.UI/CaptchaRefs](EpicRPGBot.UI/CaptchaRefs:1)


## Overview

When the bot encounters the guard message in Discord chat (e.g., “Select the item of the image above or respond with the item name”), the engine:
- Detects the guard text via the normal last-message polling.
- Pauses scheduled hunt/work/farm timers (chat polling remains active).
- Locates the captcha image element inside the same Discord chat message (same list item).
- Downloads the image bytes using an HTTP client (no browser-origin restrictions).
- Classifies the image by comparing it against 16 known references (user-provided) using perceptual hashes (dHash + pHash).
- If the match is confident (distance ≤ threshold), submits the matching label as text in chat (exact filename without extension).
- Resumes timers immediately afterward.

The solver is robust to small variations: grayscale, thin line overlays, small scaling/squashing changes, and slight contrast/brightness differences.


## Triggers (how detection starts)

The solver triggers when either of the following appears in the last two messages (rolling window) of the channel:

- Exact Epic RPG phrase:
  - “Select the item of the image above or respond with the item name”
- Alternate testing phrase (for manual tests):
  - “EPIC GUARD: stop there,”

Details:
- The engine caches the previous message text and id; if the phrase is present in either the newest or the immediately previous message, the solver starts.
- If the image is not found in the primary message, the solver falls back to check the adjacent message (to support cases where the prompt and the image are posted separately).
- Implementation is in the engine’s message handler in [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1).


## Reference Images

Provide 16 reference images corresponding to all possible captcha items:
- Place PNG/JPG files in [EpicRPGBot.UI/CaptchaRefs](EpicRPGBot.UI/CaptchaRefs:1).
- Filenames must be the exact answer string expected in chat, all lowercase with spaces as in-game.
  - Examples: `apple.png`, `fish.png`, `banana.png`, `iron sword.png` (if multi-word answers exist).
- No mapping file is required. The classifier uses the filename (without extension) as the chat answer.

Recommendations:
- Reasonable size (e.g., 64–256 px). Any size works; the classifier scales for hashing.
- Use clean, representative images of each distinct item.
- Replace images if they materially change in-game; keep names identical.


## Runtime Behavior

Detection
- The engine continuously polls the last Discord message’s text.
- On finding a trigger phrase in the last two messages, the solver pipeline begins.

Image extraction and download
- The image source (src) is retrieved from the same chat message DOM node that contains the guard text.
- If no image is found there, the solver tries the adjacent message (the other of the last two).
- The absolute image URL is downloaded via an HTTP client on the .NET side.

Classification
- Preprocessing: letterbox to 64x64 on a black square, grayscale conversion, light 3x3 median filter to reduce thin overlay noise.
- Two 64-bit perceptual hashes are computed:
  - dHash (difference hash)
  - pHash (DCT-based)
- Each reference is pre-hashed. For the input image, we compute distances (Hamming) to every reference for both methods and pick the smallest distance overall.

Decision and response
- If best distance ≤ threshold (default 12), the bot sends the corresponding label (filename without extension) to the chat as the answer.
- If uncertain (distance > threshold), the solver logs the uncertainty and does not reply.
- Timers resume after the solver completes.

Logging and telemetry
- The engine emits solver log lines (e.g., detection, URL fetch, classification result, timing), which the UI Console displays with a “[solver] …” prefix.


## Configuration (.env keys)

The UI reads .env keys using the loader in [EpicRPGBot.UI/Env.cs](EpicRPGBot.UI/Env.cs:1). All keys are optional; defaults apply if unspecified.

- CAPTCHA_REFS_DIR
  - Directory for the reference images.
  - Default: a folder named `CaptchaRefs` under the application base directory.

- CAPTCHA_HASH_THRESHOLD
  - Integer in 1..64. Accept match if best Hamming distance ≤ threshold.
  - Default: 12

- CAPTCHA_SELFTEST
  - If set to `1`, an optional offline classifier self-test runs on app load (detailed below).
  - Default: unset (no self-test)

- CAPTCHA_TIMEOUT_MS
  - Informational and for future guardrails (current pipeline is designed to complete well under 15 seconds).
  - Default: 15000 (if used)


## Operating the Solver (Runtime)

1) Ensure images exist in [EpicRPGBot.UI/CaptchaRefs](EpicRPGBot.UI/CaptchaRefs:1) with correct names.
2) Start the UI app and navigate to your target Discord channel as usual.
3) Start the bot (Start button).
4) When a guard captcha appears:
   - The Console panel will show “[solver] Captcha detected (guard message).” or “(alt phrase).”
   - It logs the image URL discovery, download success, classification method/distance, and final answer send if confident.
   - Timers resume automatically.

Manual testing in Discord
- Post the text “EPIC GUARD: stop there,” in the channel, followed by the captcha image (or vice-versa).
- The solver checks the last two messages, so the trigger and the image can be in either order.
- Watch the Console panel for “[solver] …” logs.


## Offline Self-Test (Optional)

Purpose
- Validate that your 16 reference images are classified correctly.
- Simulate mild distortions (squash and thin line overlays) to check robustness and tune the threshold.

How to run
- Set `CAPTCHA_SELFTEST=1` in `.env`.
- Launch the UI app normally (no need to interact with Discord).
- The Console panel logs:
  - Reference count and threshold.
  - For each reference image:
    - Classification result on the original image.
    - Classification result on a synthetic variant (grayscale, slightly squashed, with thin white lines).

Testing other images in self-test
- The self-test iterates over all files in [EpicRPGBot.UI/CaptchaRefs](EpicRPGBot.UI/CaptchaRefs:1). To test an additional image offline, temporarily drop it into this folder; its filename will be treated as the expected label. Review the logged distances and method.

Expected output
- For each reference, correct label on original.
- For variants, nearly always correct; report distance and method (dhash or phash).
- If you see systematic misclassifications, consider:
  - Lowering the threshold if there are frequent near-ties but correct labels still have comfortably low distances.
  - Raising the threshold slightly (e.g., 14–16) if certain items always classify just above 12 despite being visually similar.

Tuning tip
- Inspect distances in the self-test logs; pick a threshold that comfortably separates correct-from-incorrect under typical noise.


## Troubleshooting

“Solver initialized … failed”
- Ensure the reference directory exists.
- Verify there are images with supported extensions (.png/.jpg/.jpeg).
- Check permission to read the folder.

“Captcha image URL not found”
- The solver tries the primary message id and then the adjacent one (last-two window). If both miss, confirm the image is visible in the viewport and Discord hasn’t changed the DOM structure. The DOM code is in [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1).

“Download failed”
- Temporary CDN hiccups or network connectivity issues.
- Try again; the solver runs fast enough to reattempt on next guard.

“Classifier uncertain (dist=…, method=…)”
- The solver declines to answer when not confident.
- Review self-test distances and consider small threshold adjustments via `.env`.
- Verify reference images match the in-chat captcha visual identity.

Performance concerns
- Typical end-to-end solve time is ~2–4 seconds (HTTP + hashing + send).
- If your system is slower, consider disabling the median filter (requires code change) or ensuring references are not excessively large (scaling cost).


## Maintenance and Updates

Updating references
- Replace any reference image if the in-game asset changes materially.
- Keep the filename exactly the same as the intended chat answer string.
- Re-run offline self-test with `CAPTCHA_SELFTEST=1` to confirm distances.

Adjusting behavior
- Hash threshold: adjust via `.env` (CAPTCHA_HASH_THRESHOLD).
- Reference folder: move images and point CAPTCHA_REFS_DIR to the new path.

Extending beyond 16 items
- Add additional references with the correct filenames.
- The classifier automatically includes any images in the refs directory.

DOM changes (Discord markup)
- If image lookup breaks, update the DOM query that retrieves the image source (see [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1)).
- The current logic prefers visible `img` elements inside the same chat message node and falls back to the adjacent message.


## Notes and Considerations

- The solver only responds when confident to reduce incorrect answers.
- Timers are paused during solving to avoid interleaving unrelated bot messages.
- Logs with “[solver] …” can be found in the Console panel (left sidebar → Console).
- Keeping WebView2 runtime and dependencies up to date is recommended for best compatibility.