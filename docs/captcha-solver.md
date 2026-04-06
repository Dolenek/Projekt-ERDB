# Captcha Solver

The captcha solver is integrated into the `.UI` runtime.

Trigger:
- The current implementation triggers on the alternate guard phrase `EPIC GUARD: stop there,`.
- The engine checks both the newest and immediately previous chat messages to support split text/image posts.
- Guard incidents stay active until the chat later contains `EPIC GUARD: Everything seems fine ... keep playing`.
- That clear message immediately resumes tracked timers and cancels any in-flight solver attempt.

Solve flow:
1. Pause hunt/adventure/work/farm timers.
2. Try to capture the captcha image from the selected Discord message through a DevTools screenshot.
3. The screenshot capture picks the largest visible content image in the message and ignores small inline images such as avatars, badges, or emoji-like assets.
4. If no image is captured, fall back to the message image URL, then the adjacent message.
5. Load or reuse the OpenAI captcha answer provider.
6. Send the image and the fixed item catalog to OpenAI and require a strict JSON answer with a catalog index or `unknown`.
7. For full-color captchas, treat color as a strong signal together with shape; only fall back to grayscale cues when the captcha is clearly desaturated, grayscale, or black-and-white.
8. If a guard solve is active, scheduled tracked commands and queued `rpg cd` sends are skipped so the answer lane stays reserved for the guard reply.
9. If the first OpenAI answer is invalid or `unknown`, retry once with the configured retry model and an enlarged retry image.
10. If a screenshot-based solve is still uncertain, retry once more from the underlying message image URL before giving up.
11. If the result is confident, send the matched canonical item name back to Discord.
12. If the provider is uncertain, skip sending instead of guessing.
13. Resume timers after the attempt completes.
14. Show one desktop alert on first detection, then at most one reminder every 10 seconds while the same guard incident stays active.

Configuration:
- `CAPTCHA_OPENAI_API_KEY`
- `CAPTCHA_OPENAI_MODEL` default `gpt-5.4-mini`
- `CAPTCHA_OPENAI_RETRY_MODEL` default `gpt-5.4`
- `CAPTCHA_ITEM_NAMES_FILE` points to either:
- a JSON array of `{ name, outline, grayscale_cues, disambiguation }` items, or
- a plain text file with one canonical item name per line
- `CAPTCHA_API_TIMEOUT_SECONDS` default `10`
- `CAPTCHA_DEBUG_CAPTURE` set to `1` to save the exact captcha image bytes given to the solver
- `CAPTCHA_DEBUG_DIR` default `artifacts/captcha-debug`

Telemetry:
- Solver events are written to the Console tab with a `[solver]` prefix.
- Typical lines include solver initialization, image acquisition source, resolved attachment URLs, saved debug artifact paths, OpenAI retry use, skipped sends during active guard handling, uncertain classifications, and the final chosen answer.
- Guard notifications use `[guard]`; the first detection is a warning, later reminders and clear events are info logs.

Offline self-test:
- Enable with `CAPTCHA_SELFTEST=1` in `.env`.
- Set `CAPTCHA_SELFTEST_REPLAY_DIR` to a folder of labeled screenshots.
- Replay filenames must match a canonical item name, or use `Item Name__anything.png`.
- The replay self-test logs expected item, predicted item, pass/fail, and a summary count.
