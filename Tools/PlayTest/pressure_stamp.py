#!/usr/bin/env python3
"""The stamp of a pressure record (round 76, Docs/BALANCE_CRITIC_2026-11-17b.md) and the rule the report tools hold to.

A record written since round 76 ends, before the worker and the batch, with "bot=<version>:<hash>;data=<hash>;game=<hash>"
(Assets/Tests/PlayMode/ProbeStamp.cs): the version of the reference players with a hash of their code, recorded plans and
searched strategies; a hash of Assets/_Project/Data; a hash of Assets/_Project/Scripts. Older records carry no stamp.

The probe is deterministic per code, data and seed: every difference between two records of the same unit is an effect of a
change in one of those, never noise. So:

  - records measured with different bots are not compared (a difference could be either the bot or the data), and
    unstamped records are of an unknown bot;
  - a batch changes the bot or the data, never both.

The tools refuse the first unless called with --mixed-bots, and say so when the second was broken.
"""
import sys

UNSTAMPED = "unstamped"
ALLOW_FLAG = "--mixed-bots"


def parse(fields):
    """The stamp of a split record line as (bot, data, game); UNSTAMPED parts for a record of before round 76."""
    for field in fields[25:]:
        if field.startswith("bot="):
            parts = dict(item.split("=", 1) for item in field.split(";") if "=" in item)
            return parts.get("bot", UNSTAMPED), parts.get("data", UNSTAMPED), parts.get("game", UNSTAMPED)
    return UNSTAMPED, UNSTAMPED, UNSTAMPED


def describe(runs):
    """One line: the bot, data and game stamps a set of runs was measured with."""
    def show(key):
        values = sorted({r[key] for r in runs})
        return ", ".join(values) if values else "-"
    return f"bot {show('bot')}; data {show('data')}; game code {show('game')}"


def refuse(message):
    print(f"REFUSED: {message}\n(the probe is deterministic, so a difference between bots is not noise; pass {ALLOW_FLAG} to print it anyway)", file=sys.stderr)
    sys.exit(2)


def check_one(runs, name, allow):
    """One records file must hold one bot."""
    bots = sorted({r["bot"] for r in runs})
    if len(bots) > 1 and not allow:
        refuse(f"{name} holds records of {len(bots)} bots ({', '.join(bots)}); keep one records file per bot and data state")
    return bots


def check_pair(now, before, allow):
    """Two records files are compared only when one bot measured both; a change of bot and data at once is called out."""
    bots_now = {r["bot"] for r in now}
    bots_before = {r["bot"] for r in before}
    same_bot = bots_now == bots_before and len(bots_now) == 1 and UNSTAMPED not in bots_now
    same_data = {r["data"] for r in now} == {r["data"] for r in before} and UNSTAMPED not in {r["data"] for r in now}
    if same_bot:
        return
    if UNSTAMPED in bots_now | bots_before:
        reason = "at least one side is unstamped (measured before round 76, bot unknown)"
    elif same_data:
        reason = "the bot differs (the data is the same: this is a bot comparison, read it as one)"
    else:
        reason = "the bot AND the data differ: such a batch cannot say which of the two moved a cell"
    if not allow:
        refuse(f"before: {describe(before)}\n         after:  {describe(now)}\n         {reason}")
    print(f"WARNING ({ALLOW_FLAG}): {reason}")
