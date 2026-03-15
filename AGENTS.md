\# Agent Notes



\## file\_length\_and\_structure



Never allow a file to exceed 500 lines.

If a file approaches 400 lines, break it up immediately.

Treat 1000 lines as unacceptable, even temporarily.

Use folders and naming conventions to keep small files logically grouped. 



\## function and class size



Keep functions under 30–40 lines.

If a class is over 200 lines, assess splitting into smaller helper classes.



\## naming\_and\_readability



All class, method, and variable names must be descriptive and intention-revealing.

Avoid vague names like `data`, `info`, `helper`, or `temp`. 



\## documentation discipline



Whenever an agent adds a feature or significant refactor, record it in a focused file under `docs/` (max \~120 lines each) and link it from `documentation.md`. Do not let the docs sprawl into one giant page.



\## documentation\_governance



`docs/` is the canonical documentation source for this repository.



\### core\_rules

\- Canonical docs must describe current behavior only. Do not keep changelog/history narrative in canonical pages.

\- Do not duplicate full specs across files. Link to the canonical page instead.



\## scalability mindset 



Always code as if someone else will scale this.

Include extension points (e.g., protocol conformance, dependency injection) from day one.



\### minimum\_doc\_impact\_review

For each substantial code task, review whether updates are needed in:

