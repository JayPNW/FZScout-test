When you're reading a doc anytime you see `@AI:`, it indicates a note or instruction for the AI to fill in or complete.

Don't bother touching line endings and BOMs. Let tidy_code handle that as needed.

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
