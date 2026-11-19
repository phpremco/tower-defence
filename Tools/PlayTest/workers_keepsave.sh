#!/usr/bin/env bash
# Round 85: workers.sh itself never restores the save any more, so this is workers.sh under its old name (the chains of
# rounds 80b to 84 call it).
exec bash "$(dirname "$0")/workers.sh" "$@"
