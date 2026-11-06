// Round 16: confirms the editor runs the new code (the furrow's open ramp and the trail's body reach exist).
bool ramp = typeof(Game.Bosses.CrackLineView).GetMethod("SetOpenRamp") != null;
bool reach = typeof(Game.Bosses.LavaTrailView).GetMethod("ReachUnder") != null;
return "SetOpenRamp " + ramp + ", ReachUnder " + reach;
