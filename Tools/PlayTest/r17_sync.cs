// Round 17: syncs only the re-exported FireLarva and SealedOne (not the whole catalog: ShardFlyer stays as it is).
int copied = Game.Editor.BlenderSync.Sync(new[] { "FireLarva", "SealedOne" });
return copied + " file(s) copied";
