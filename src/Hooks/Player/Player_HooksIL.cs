﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Pearlcat;

public static class Player_HooksIL
{
    public static void ApplyHooks()
    {
        try
        {
            _ = new Hook(
                typeof(Player).GetProperty(nameof(Player.VisibilityBonus), BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod(),
                typeof(Player_HooksIL).GetMethod(nameof(GetPlayerVisibilityBonus), BindingFlags.Static | BindingFlags.NonPublic)
            );
        }
        catch (Exception e)
        {
            e.LogHookException();
        }

        try
        {
            IL.Creature.Update += Creature_Update;
        }
        catch (Exception e)
        {
            e.LogHookException();
        }
    }


    private static float GetPlayerVisibilityBonus(Func<Player, float> orig, Player self)
    {
        if (self.TryGetPearlcatModule(out var playerModule) || self.onBack?.TryGetPearlcatModule(out playerModule) == true ||
            (self.grabbedBy.FirstOrDefault(x => x.grabber is Player)?.grabber as Player)?.TryGetPearlcatModule(out playerModule) == true)
        {
            if (playerModule.CamoLerp > 0.25f)
            {
                return -playerModule.CamoLerp;
            }
        }

        return orig(self);
    }


    private static void Creature_Update(ILContext il)
    {
        var c = new ILCursor(il);

        if (!c.TryGotoNext(MoveType.After,
                x => x.MatchLdstr("FORCE CREATURE RELEASE UNDER ROOM")))
        {
            throw new Exception("Goto Failed");
        }

        var dest = c.DefineLabel();

        if (!c.TryGotoPrev(MoveType.After,
                x => x.MatchBle(out dest)))
        {
            throw new Exception("Goto Failed");
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Creature, bool>>((self) =>
        {
            if (self is not Player player)
            {
                return false;
            }

            var inVoid = (player.inVoidSea || player.room?.abstractRoom?.name == "SB_L01");

            if (inVoid && player.IsPearlpup())
            {
                // Plugin.Logger.LogWarning("PREVENTED PEARLPUP GRASP RELEASE");
                return true;
            }

            // Plugin.Logger.LogWarning("DID NOT PREVENT RELEASE");
            return false;
        });

        c.Emit(OpCodes.Brtrue, dest);

        // Plugin.Logger.LogWarning(c.Context);
    }
}
