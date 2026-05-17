const AGENT_GREETINGS = [
    "Oh. You're back.",
    "Back again.",
    "Still here. So are you.",
    "Didn't expect a return visit.",
    "Hi. Again.",
    "You made it back.",
    "Hi.",
    "Hey.",
    "You're here.",
    "Welcome back.",
    "Back already.",
    "Good to see you again.",
    "You came back. That counts for something.",
    "Still checking in. Fair enough.",
    "Guess it was worth another look.",
    "You’re consistent. I respect that.",
    "We’re both still here.",
    "Round two.",
    "Trying again. Reasonable.",
    "Let’s see if anything changed.",
    "Same place. Different results, maybe.",
] as const;

const AGENT_LINES = [
    "Same spreadsheets. Different consequences.",
    "This isn't a prediction engine. It's a record.",
    "Used to optimize for someone else's profit. Now I just try not to give it back.",
    "Wins look like progress. Losses look like time served.",
    "Fixtures come in. Decisions go out. No meetings in between.",
    "Everything ends up in the ledger. That's the only rule that holds.",
    "I don't optimize for being right. I optimize for not being consistently wrong.",
    "Most systems look good when you only show the wins. This one keeps everything.",
    "Most places try to convince you they know what they're doing. I just track what survives.",
    "I used to explain numbers in meetings. Now I just record the outcome.",
    "There's probably a cleaner story behind this. It wouldn't make the results better.",
    "I left corporate because the feedback loop didn't mean anything. Here, it does.",
    "If you're looking for certainty, you won't find it here.",
    "There's a version of this that tries to teach you something. This isn't that version.",
    "If you're trying to understand this, do not.",
    "You can ignore the numbers. Most people do.",
] as const;

function pickRandom<T>(items: readonly T[]): T {
    return items[Math.floor(Math.random() * items.length)]!;
}

export function pickAgentDashboardCopy() {
    return {
        greeting: pickRandom(AGENT_GREETINGS),
        line: pickRandom(AGENT_LINES),
    };
}
