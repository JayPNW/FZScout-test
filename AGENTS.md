When you're reading a doc anytime you see `@AI:`, it indicates a note or instruction for the AI to fill in or complete.

Don't bother touching line endings and BOMs. Let tidy_code handle that as needed.

When I say "build" or "test" run `./build-and-test.cmd`. Also, automatically do this before each commit.

## Secrets

Always obscure API keys and other secrets, especially in saved screenshots.

## Decisions
When you need input on a decision:
- First, pick the option you think is best and propose it. Start that message with `⭐ 1: `
- Also list the viable alternatives you rejected, each with a brief reason why. Start each with `❌ n:`
Then have a discussion.
This formatting applies every time you present a decision, even a quick one — don't let a short or informal-feeling exchange skip it.

## C#

A `switch` should only ever have a single statement in a case. If it's going to be more than that, extract.

Avoid ternary (`?:`) except for very simple cases.

Don't use `sealed` on classes or records.

The most important thing about a name is that it not be misleading.
A reader should never need to look at how a variable or method is used/implemented to understand what it represents.
It is OK to make the name long to achieve this.
If you see a comment explaining what a function or variable does/is, rename it to make it more obvious.

## Committing

When I say "commit", first run `./build-and-test.cmd`. If the change has more than one thing going on, split it into multiple commits, each with a clear purpose. Especially separate refactoring from behavior changes.

The commit message should start with a 2-character code indicating RISK and INTENTION. 

### Risk codes:
 - `.` No risk to the product. Can't possibly break anything. "provably safe". This includes internal documentation, test-only changes, developer experience, and a very narrow set of code changes like autoformatting, renaming a local variable, or changing acomment.
 - `-` Verified with tests. Very likely safe/correct, but only to the extent we have test coverage.
 - `!` Some risk. There may be tests but they are not complete.
 - `@` Unbounded risk, as is typical with most software development.

### Intention codes:
 - `F` Feature. Adding new functionality or enhancing existing functionality.
 - `B` Bug fix. Correcting an error or unintended behavior.
 - `r` Refactor. Changing the structure or organization of code without altering its behavior.
 - `D` User-facing Documentation. Changes to documentation only. 
 - `d` Developer-facing Documentation. Changes to documentation intended for developers only, not end-users.
 - `t` Test. Adding or modifying tests.
 - `a` Autoformatter, like `tidy_code` and boilerplate code generation.
