# Captcha Solver

The captcha solver is integrated into the `.UI` runtime and uses local image references plus perceptual hashes.

Trigger:
- The current implementation triggers on the alternate guard phrase `EPIC GUARD: stop there,`.
- The engine checks both the newest and immediately previous chat messages to support split text/image posts.
- Guard incidents stay active until the chat later contains `EPIC GUARD: Everything seems fine ... keep playing`.
- That clear message immediately resumes tracked timers and cancels any in-flight solver attempt.

Solve flow:
1. Pause hunt/adventure/work/farm timers.
2. Try to capture the captcha image from the selected Discord message through a DevTools screenshot.
3. If no image is captured, fall back to the message image URL, then the adjacent message.
4. Load or reuse a `CaptchaClassifier`.
5. Classify the image against the local references.
6. If the match is confident, send the matched filename label back to Discord.
7. Resume timers after the attempt completes.
8. Show one desktop alert on first detection, then at most one reminder every 10 seconds while the same guard incident stays active.

Reference data:
- Default folder: `EpicRPGBot.UI/CaptchaRefs`
- Override with `.env` key `CAPTCHA_REFS_DIR`
- Filenames are the exact text sent back to chat, without the file extension

Configuration:
- `CAPTCHA_HASH_THRESHOLD` controls the maximum accepted Hamming distance.
- The default threshold is `12`.

Telemetry:
- Solver events are written to the Console tab with a `[solver]` prefix.
- Typical lines include solver initialization, image acquisition source, uncertain classifications, and the final chosen answer.
- Guard notifications use `[guard]`; the first detection is a warning, later reminders and clear events are info logs.

Offline self-test:
- Enable with `CAPTCHA_SELFTEST=1` in `.env`.
- On startup, the app classifies every reference image and a generated distorted variant.
- The self-test logs the winning label, distance, method, and top matches to the Console tab.
