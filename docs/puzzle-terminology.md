# Puzzle terminology

`Puzzle` is the code domain for the in-game image question. The app recognizes
an item attachment and submits its canonical catalog name when eligible.

| Responsibility | Code |
| --- | --- |
| Solver orchestration | `Services/PuzzleSolverService.cs` |
| Answer provider | `Puzzle/IPuzzleAnswerProvider.cs` |
| Local recognition | `Puzzle/Local/` |
| Attachment retrieval | `Services/PuzzleImageSource.cs` |
| Chat answer contract | `Services/IPuzzleAnswerChatClient.cs` |
| Engine integration | `BotEngine.Puzzle.cs` |
| Replay CLI | `tools/PuzzleReplay/` |
| Dataset and evaluation tools | `tools/puzzle/` |

The production bundle, environment variables and lifecycle are documented only
in [the solver contract](puzzle-solver.md). Recognition interfaces are described
in [recognition internals](puzzle-multisource-recognition.md).
Dataset, model and diagnostic artifacts use `puzzle`-prefixed directories under
`artifacts/`; their manifests retain provenance and labels. Measured evidence is
summarized in [validation evidence](puzzle-campaign-results.md).
