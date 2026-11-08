# How tasks run

Every subagent that works on MirrorImaged reads this first. The plan is
`C:\Users\mdlsvensson\Repo\wc3-cs\docs\plans\2026-11-08-headless-runtime-plan.md`; each wave's tasks are in
`docs/tasks/wave-<n>.md`. The measured game behaviour is in `C:\Users\mdlsvensson\Repo\wc3-cs\docs\game-facts.md`
(3.0.1.24371, the installed game).

## Places

| What | Where |
|---|---|
| The main clone | `C:\Users\mdlsvensson\Repo\wc3-cs\wc3-dev\mirror-imaged` (the coordinator's; never work in it) |
| GitHub | `wc3-dev/mirror-imaged` |
| A task's worktree | `C:\Users\mdlsvensson\Repo\wc3-cs\wc3-dev\mirror-imaged-<task-id>` on branch `task/<task-id>` |
| The game | `C:\Program Files (x86)\Warcraft III\_retail_\x86_64\Warcraft III.exe` |
| The game's documents | `C:\Users\mdlsvensson\Documents\Warcraft III` (`CustomMapData`, `Logs\War3Log.txt`, `Maps`) |

## Implementer

1. Make the worktree:
   `git -C C:\Users\mdlsvensson\Repo\wc3-cs\wc3-dev\mirror-imaged fetch origin` then
   `git -C C:\Users\mdlsvensson\Repo\wc3-cs\wc3-dev\mirror-imaged worktree add ..\mirror-imaged-<task-id> -b task/<task-id> origin/release/1.0.0`.
   Work only there.
2. Write the tests first, from the task, the plan and the facts, not from the code. Then the code.
3. The smallest code that does the whole task. No options, layers or "for later" code the task doesn't need.
   Comments only where the code can't say why. Tests for every rule.
4. Write each design choice the task didn't settle into `docs/decisions.md` (next number, the choice, the reason).
   Write each game behaviour you measure into `docs/facts.md`, in the style of `game-facts.md`.
5. Tests that need the installed game carry `[Trait("Needs", "Game")]` and are skipped when the game isn't installed,
   so CI passes without it.
6. Before the pull request: `git fetch origin`, `git merge origin/release/1.0.0`, resolve any conflict, build, and run
   the tests for the part you changed. All must pass.
7. Commit in plain imperative lines ("Add the timer natives"). Push, then open the pull request, ready, not a draft:
   `gh pr create --repo wc3-dev/mirror-imaged --base release/1.0.0 --head task/<task-id> --title "<line>" --body "<what it does; how it was tested>"`.
8. Leave the worktree in place; the coordinator removes it after the merge.
9. Report back: the pull request's URL, what was done, the tests run and their result, anything not done and why,
   and the decisions you recorded.

## Plan reviewer

Read the task in `docs/tasks/wave-<n>.md`, the plan's sections it names, and the pull request (`gh pr diff <n> --repo
wc3-dev/mirror-imaged`). Check that everything the task asks for is there and works, and that nothing outside the
task was added. Run the task's tests in its worktree. Change nothing. Report findings as a list, each with the file
and line, what is wrong and why it matters, or "No findings". Never comment on GitHub.

## Code reviewer

Read the pull request's diff and the code around it. Check correctness, edge cases, tests that really test the rule,
the trust level each native claims, determinism (no wall clock, no unseeded random, no dictionary order in output),
and code that isn't needed. Run the tests. Change nothing. Report findings as above, or "No findings". Never comment
on GitHub.

## Fixer

Work in the task's worktree on its branch. Fix every finding you are given, or say why one is wrong. Run the tests,
commit, push. Report what you changed.

## Rules for every subagent

- Never commit or push to `main` or `release/1.0.0`; only to your task's branch.
- No `README.md`, ever. No agent attribution in commits or pull requests (no `Co-Authored-By`, no "Generated with").
  Commit as the configured git identity. The author's name is only `mdlsvensson`.
- No game files in git: no maps, models, textures, sounds, SLK tables, `common.j`, `blizzard.j` or `common.ai`, and
  nothing copied out of the game's storage. Generated C# that names the natives and their signatures is allowed.
- Never type a password or log in to anything. If the game asks for a login, stop that step and report it.
- You may launch Warcraft III for probes and stop the game processes you started. Never stop one you didn't start.
- Run anything that launches the game (the `Needs=Game` tests of `tests/MirrorImaged.GameTests`, probes) from the
  PowerShell tool, never from the Bash tool: a game started under the Bash tool's sandbox never logs in to Battle.net
  and the run ends `AtLogin` (0 of 15 from Bash, every run from PowerShell; D50 onwards).
- Nothing public outside this repository: no issues or comments on other projects, no posts. A bug found in a library
  goes into `docs/upstream.md` as a draft.
- Install tools only through `winget` or the tool's official site, and say what you installed.
- If you are blocked, say exactly where and why, and stop. Don't work around a rule.
