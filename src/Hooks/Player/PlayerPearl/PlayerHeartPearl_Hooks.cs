﻿using RWCustom;

namespace Pearlcat;

public static class PlayerHeartPearl_Hooks
{
    public static void ApplyHooks()
    {
        On.DataPearl.Update += DataPearl_Update_PearlpupPearl;
        
        On.DataPearl.InitiateSprites += DataPearl_InitiateSprites_PearlpupPearl;

        On.DataPearl.DrawSprites += DataPearl_DrawSprites_PearlpupPearl;
        On.DataPearl.PlaceInRoom += DataPearl_PlaceInRoom;
    }


    private static void DataPearl_PlaceInRoom(On.DataPearl.orig_PlaceInRoom orig, DataPearl self, Room placeRoom)
    {
        orig(self, placeRoom);

        if (!self.AbstractPearl.TryGetHeartPearlModule(out var module))
        {
            return;
        }

        module.Umbilical?.Reset(self.firstChunk.pos);
    }

    private static void DataPearl_InitiateSprites_PearlpupPearl(On.DataPearl.orig_InitiateSprites orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);

        if (!self.AbstractPearl.TryGetHeartPearlModule(out var module))
        {
            return;
        }


        // Umbilical
        module.Umbilical = new UmbilicalGraphics(sLeaser.sprites.Length);
        Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + module.Umbilical.TotalSprites);

        module.Umbilical.InitiateSprites(sLeaser, rCam);

        var umbilical = module.Umbilical;
        var mgContainer = rCam.ReturnFContainer("Midground");
        var fgContainer = rCam.ReturnFContainer("Foreground");

        for (var i = 0; i < umbilical.SmallWires.GetLength(0); i++)
        {
            sLeaser.sprites[umbilical.SmallWireSprite(i)].RemoveFromContainer();
            mgContainer.AddChild(sLeaser.sprites[umbilical.SmallWireSprite(i)]);
        }

        // Possession
        var spriteIndex = sLeaser.sprites.Length;

        module.PossessLaserSprite = spriteIndex++;
        module.PossessProgressSprite = spriteIndex++;
        module.PossessCircleSprite = spriteIndex++;

        Array.Resize(ref sLeaser.sprites, spriteIndex);

        var possessLaserSprite = sLeaser.sprites[module.PossessLaserSprite] = new FSprite("pixel")
        {
            shader = Utils.Shaders["GateHologram"],
        };

        var progressSprite = sLeaser.sprites[module.PossessProgressSprite] = new FSprite("Futile_White")
        {
            shader = Utils.Shaders["HoldButtonCircle"],
        };

        var circleSprite = sLeaser.sprites[module.PossessCircleSprite] = new FSprite("pearlcat_possesscircle")
        {
            shader = Utils.Shaders["GateHologram"],
        };


        possessLaserSprite.RemoveFromContainer();

        fgContainer.AddChild(possessLaserSprite);
        fgContainer.AddChild(progressSprite);
        fgContainer.AddChild(circleSprite);

        sLeaser.sprites[0].color = module.AliveMainColor;
    }

    private static void DataPearl_DrawSprites_PearlpupPearl(On.DataPearl.orig_DrawSprites orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (!self.AbstractPearl.TryGetHeartPearlModule(out var module))
        {
            return;
        }

        if (self.slatedForDeletetion)
        {
            return;
        }

        if (self.room is null)
        {
            return;
        }


        PlayerModule? playerModule = null;

        if (module.OwnerRef?.TryGetTarget(out var player) == true)
        {
            player.TryGetPearlcatModule(out playerModule);
        }


        var mainSprite = sLeaser.sprites[0];
        var highlightSprite = sLeaser.sprites[1];
        var glimmerSprite = sLeaser.sprites[2];

        module.CurrentMainColor = Color.Lerp(module.CurrentMainColor, module.IsPlayerAlive ? module.AliveMainColor : module.DeadMainColor, 0.001f);
        mainSprite.color = module.CurrentMainColor;

        highlightSprite.color = Custom.hexToColor("ffffff");
        glimmerSprite.color = Color.white;

        highlightSprite.SetPosition(mainSprite.GetPosition());


        if (module.HeartFirstBeatTimer == 0.0f || module.HeartSecondBeatTimer == 0.0f)
        {
            mainSprite.scale = 2.5f;
            highlightSprite.scale = 2.0f;
            glimmerSprite.scale = 1.5f;

            var sparkVel = Custom.RNV() * 2.5f;
            self.room.AddObject(new Spark(self.firstChunk.pos, sparkVel, Color.red, null, 3, 5));

            self.room.AddObject(new ExplosionSpikes(self.room, self.firstChunk.pos, 3, 10.0f, 10.0f, 5.0f, 10.0f, Color.red));

            if (module.HeartSecondBeatTimer == 0)
            {
                var vol = self.AbstractPearl.TryGetSentry(out _) ? 0.25f : 0.15f;

                self.room.PlaySound(Enums.Sounds.Pearlcat_Heartbeat, self.firstChunk.pos, vol, 1.0f);
            }
        }

        mainSprite.scale = Custom.LerpBackEaseOut(mainSprite.scale, 0.9f, 0.02f);
        highlightSprite.scale = Custom.LerpBackEaseOut(highlightSprite.scale, 0.9f, 0.02f);
        glimmerSprite.scale = Custom.LerpBackEaseOut(glimmerSprite.scale, 0.9f, 0.02f);
        
        module.Umbilical?.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        module.Umbilical?.ApplyPalette(sLeaser);

        
        var possessLaserSprite = sLeaser.sprites[module.PossessLaserSprite];
        var possessProgressSprite = sLeaser.sprites[module.PossessProgressSprite];
        var possessCircleSprite = sLeaser.sprites[module.PossessCircleSprite];

        if (playerModule is not null && playerModule.PossessionTarget?.TryGetTarget(out var target) == true)
        {
            var possessionColor = target.abstractPhysicalObject.GetObjectColor();

            // Laser
            possessLaserSprite.isVisible = true;
            
            var startPos = Vector2.Lerp(self.firstChunk.lastPos, self.firstChunk.pos, timeStacker) - camPos;
            var targetPos = Vector2.Lerp(target.firstChunk.lastPos, target.firstChunk.pos, timeStacker) - camPos;

            // Offset by the radius of the possess circle
            targetPos -= Custom.DirVec(startPos, targetPos) * 85.0f;

            // Offset by the radius of the heart pearl
            startPos += Custom.DirVec(startPos, targetPos) * 15.0f;

            var dir = Custom.DirVec(startPos, targetPos);

            var laserWidth = 3.0f;
            var laserLength = Custom.Dist(startPos, targetPos);

            possessLaserSprite.rotation = Custom.VecToDeg(dir);
            possessLaserSprite.scaleX = laserWidth;
            possessLaserSprite.scaleY = laserLength;

            possessLaserSprite.color = possessionColor;

            possessLaserSprite.SetPosition(startPos + dir * laserLength / 2.0f);


            // Progress
            possessProgressSprite.isVisible = true;
            
            possessProgressSprite.alpha = Custom.LerpMap(playerModule.StoreObjectTimer, 0.1f, Player_Helpers.POSSESSION_DELAY, 0.0f, 1.0f);
            possessProgressSprite.scale = Custom.LerpMap(playerModule.StoreObjectTimer, 0.0f, Player_Helpers.POSSESSION_DELAY, 3.5f, 10.0f);

            possessProgressSprite.color = possessionColor;

            possessProgressSprite.SetPosition(target.mainBodyChunk.pos - camPos);


            // Reticle
            possessCircleSprite.isVisible = true;
            possessCircleSprite.SetPosition(target.mainBodyChunk.pos - camPos);

            possessCircleSprite.color = possessionColor;
        }
        else
        {
            possessLaserSprite.isVisible = false;
            possessProgressSprite.isVisible = false;
            possessCircleSprite.isVisible = false;
        }
    }

    private static void DataPearl_Update_PearlpupPearl(On.DataPearl.orig_Update orig, DataPearl self, bool eu)
    {
        orig(self, eu);

        if (!self.AbstractPearl.TryGetHeartPearlModule(out var module))
        {
            return;
        }

        if (module.HeartFirstBeatTimer > module.HeartFirstBeatTime)
        {
            module.HeartFirstBeatTimer = -1.0f;
            module.HeartSecondBeatTimer = 0.0f;
        }
        else if (module.HeartFirstBeatTimer >= 0.0f)
        {
            module.HeartFirstBeatTimer += module.HeartRateMult;
        }
        else if (module.HeartSecondBeatTimer > module.HeartSecondBeatTime)
        {
            module.HeartSecondBeatTimer = -1.0f;
            module.HeartFirstBeatTimer = 0.0f;
        }
        else if (module.HeartSecondBeatTimer >= 0.0f)
        {
            module.HeartSecondBeatTimer += module.HeartRateMult;
        }


        // Heartrate
        if (module.OwnerRef?.TryGetTarget(out var player) == true && module.IsPlayerAlive)
        {
            var threatLevel = 0.0f;

            if (self.abstractPhysicalObject.world.game.manager.musicPlayer is not null)
            {
                threatLevel = self.abstractPhysicalObject.world.game.manager.musicPlayer.threatTracker
                    .currentMusicAgnosticThreat;
            }

            // Heart rate based on how threatened the player feels
            var threatMult = player.dangerGraspTime > 0 ? 3.0f : Custom.LerpMap(threatLevel, 0.0f, 1.0f, 1.0f, 2.5f);

            // Mushroom effect on heartrate
            var adrenalineMult = Custom.LerpMap(player.Adrenaline, 0.0f, 1.0f, 1.0f, 1.5f);

            // Heartrate slows when sleeping
            var sleepMult = player.Sleeping || player.sleepCurlUp > 0.0f ? 0.65f : 1.0f;

            // Slows when starving
            var starveMult = player.Malnourished ? 0.8f : 1.0f;


            // Heart rate depending on possessed creature (lower mass = faster)
            var creatureMult = 1.0f;

            if (player.TryGetPearlcatModule(out var playerModule) &&
                playerModule.PossessedCreature?.TryGetTarget(out var creature) == true &&
                creature.realizedCreature is not null)
            {
                threatMult = 1.0f;
                starveMult = 1.0f;

                creatureMult = Custom.LerpMap(creature.realizedCreature.TotalMass, 0.1f, 10.0f, 3.0f, 0.5f);
            }

            module.HeartRateMult = threatMult * adrenalineMult * sleepMult * creatureMult * starveMult;
        }
        else if (!module.IsPlayerAlive)
        {
            module.HeartRateMult = Custom.LerpExpEaseIn(module.HeartRateMult, 0.0f, 0.15f);
        }


        // Umbilical
        // Disconnected from player, i.e. pearlpup is dead
        // Also hide when cosmetics are disabled
        if (module.OwnerRef is null || !module.OwnerRef.TryGetTarget(out player) || !self.abstractPhysicalObject.IsPlayerPearl() || ModOptions.DisableCosmetics_New)
        {
            if (module.Umbilical is not null)
            {
                module.Umbilical.IsVisible = false;
            }
        }
        // Is attached to the player
        else
        {
            if (!player.TryGetPearlcatModule(out var playerModule))
            {
                return;
            }

            var umbilicalStartPos = playerModule.ScarPos;
            var umbilicalEndPos = self.firstChunk.pos;

            if (module.Umbilical is not null)
            {
                module.Umbilical.IsVisible = true;
                module.Umbilical.Update(umbilicalStartPos, umbilicalEndPos, self.room);
            }
        }
    }
}
