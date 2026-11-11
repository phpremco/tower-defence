// Round 37: sets the purse to the amount in Tools/PlayTest/r37_money_arg.txt (a believable number for a still with the HUD).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
int wanted = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r37_money_arg.txt").Trim());
if (context.Wallet.Money > wanted) context.Wallet.TrySpend(context.Wallet.Money - wanted); else context.Wallet.Add(wanted - context.Wallet.Money);
return "money " + context.Wallet.Money;
