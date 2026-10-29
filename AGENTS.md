When you're reading a doc anytime you see `@AI:`, it indicates a note or instruction for the AI to fill in or complete.

## Secrets

Always obscure API keys and other secrets, especially in saved screenshots.

## Decisions
When you need input on a decision:
- First, pick the option you think is best and propose it. Start that message with `⭐ 1: `
- Also list the viable alternatives you rejected, each with a brief reason why. Start each with `❌ n:`
Then have a discussion.
This formatting applies every time you present a decision, even a quick one — don't let a short or informal-feeling exchange skip it.

## C#

### MVVM

WPF ViewModels should not contain logic and should not need to be tested.
It contains only properties and commands that the view binds to.
Everything should either be in the view or the application model it sits on.

### Testing

Most UI tests should be of this form: populate w ViewModel with test data, bind it to a View, render the View, and verify the rendered view.
