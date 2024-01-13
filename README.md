# BOILING HOT
This is a small mod that introduces SUPERHOT (or Heat Signature)-esque mechanics to
[Unexplored](http://store.steampowered.com/app/506870/) - the time only moves when you do!

It is inspired by [a small video](https://twitter.com/PlayUnexplored/status/1095746581779890176)
from the game's official Twitter account showing some game footage in slow motion.
This got me thinking "Hey, that looks just about the pace the game goes in Heat Signature during combat.
I wonder how'd this work as an actual game mechanic". Pretty well, apparently!

**Extra links:** [itch.io page](https://yellowafterlife.itch.io/boiling-hot) · [small video](https://www.youtube.com/watch?v=sNpEsA_0j4U)

**Note:** as of 2023, this mod has also been integrated as an accessibility option into the game and can be currently found on the "unstable" branch on Steam.

## Mechanics
Unless you are moving (or trying to), attacking with a melee weapon,
or using the respective button to look forward, time slows down to 10% of normal.

For ethical reasons, leaderboard submission is disabled while this mod is active.

## Installing
Extract the binary to the game directory, run BoilingHot executable to patch.

Once that is done, you can remove the mod's files - you won't need them.

## Uninstalling
Replace Unexplored executable with Unexplored-Original (which the patcher makes for you automatically)

## Technical
This is a ridiculously simple mod. As in, it took me longer to write a patcher for it than to figure out the actual mod.

There is a condition in the game update loop that goes like this:
```cs
if (this.playState.Slowed) {
    // ease towards 10% gameplay speed
} else {
    // ease towards 100% gameplay speed
}
```
and is used for boss intros.

This mod uses [Mono.Cecil](https://github.com/jbevain/cecil/) to patch the condition to be
```cs
if (playState.Slowed || (Player.lurge <= 0f && Walk <= 0f && LookAheadFactor <= 0f))
```
While I have experimented with making the time stop completely, 10% speed is more than enough for tactical planning,
and not having to tip-toe around just to let an enemy move a few extra steps feels better. And it's less work too!
