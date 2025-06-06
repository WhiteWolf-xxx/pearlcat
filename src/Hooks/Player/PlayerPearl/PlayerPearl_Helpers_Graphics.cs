﻿using RWCustom;

namespace Pearlcat;

public static class PlayerPearl_Helpers_Graphics
{
    public static int MaxPearlsWithEffects => Utils.RainWorld.options.quality == Options.Quality.LOW ? 3 : 9;


    public static void RealizedEffect(this PhysicalObject? physicalObject)
    {
        if (physicalObject?.room is null)
        {
            return;
        }

        physicalObject.room.AddObject(new Explosion.ExplosionLight(physicalObject.firstChunk.pos, 100.0f, 1.0f, 6, physicalObject.abstractPhysicalObject.GetObjectColor()));
        physicalObject.room.AddObject(new ShockWave(physicalObject.firstChunk.pos, 15.0f, 0.07f, 10));
    }

    public static void AbstractedEffect(this PhysicalObject? physicalObject)
    {
        if (physicalObject?.room is null)
        {
            return;
        }

        physicalObject.room.AddObject(new Explosion.ExplosionLight(physicalObject.firstChunk.pos, 100.0f, 1.0f, 3, physicalObject.abstractPhysicalObject.GetObjectColor()));
        physicalObject.room.AddObject(new ShockWave(physicalObject.firstChunk.pos, 25.0f, 0.07f, 10));
    }


    public static void DeathEffect(this PhysicalObject? physicalObject)
    {
        physicalObject?.room?.AddObject(new ShockWave(physicalObject.firstChunk.pos, 150.0f, 0.8f, 10));
    }


    public static void SwapEffect(this PhysicalObject? physicalObject, PhysicalObject? newObject)
    {
        if (physicalObject?.room is null || newObject is null)
        {
            return;
        }

        if (physicalObject.abstractPhysicalObject.TryGetSentry(out _) || newObject.abstractPhysicalObject.TryGetSentry(out _))
        {
            return;
        }

        var lightningBoltOld = new MoreSlugcats.LightningBolt(physicalObject.firstChunk.pos, newObject.firstChunk.pos, 0, Mathf.Lerp(0.8f, 1.0f, Random.value))
        {
            intensity = 0.35f,
            lifeTime = 7.0f,
            lightningType = Custom.RGB2HSL(physicalObject.abstractPhysicalObject.GetObjectColor()).x,
        };
        physicalObject.room.AddObject(lightningBoltOld);

        var lightningBoltNew = new MoreSlugcats.LightningBolt(newObject.firstChunk.pos, physicalObject.firstChunk.pos, 0, Mathf.Lerp(1.2f, 1.5f, Random.value))
        {
            intensity = 0.75f,
            lifeTime = 12.0f,
            lightningType = Custom.RGB2HSL(physicalObject.abstractPhysicalObject.GetObjectColor()).x,
        };
        physicalObject.room.AddObject(lightningBoltNew);
    }

    public static void SwapEffect(this PhysicalObject? physicalObject, Vector2 nextPos)
    {
        if (physicalObject?.room is null)
        {
            return;
        }

        if (physicalObject.abstractPhysicalObject.TryGetSentry(out _))
        {
            return;
        }

        var lightningBoltOld = new MoreSlugcats.LightningBolt(physicalObject.firstChunk.pos, nextPos, 0, Mathf.Lerp(0.8f, 1.0f, Random.value))
        {
            intensity = 0.35f,
            lifeTime = 7.0f,
            lightningType = Custom.RGB2HSL(physicalObject.abstractPhysicalObject.GetObjectColor()).x,
        };
        physicalObject.room.AddObject(lightningBoltOld);

        var lightningBoltNew = new MoreSlugcats.LightningBolt(nextPos, physicalObject.firstChunk.pos, 0, Mathf.Lerp(1.2f, 1.5f, Random.value))
        {
            intensity = 0.75f,
            lifeTime = 12.0f,
            lightningType = Custom.RGB2HSL(physicalObject.abstractPhysicalObject.GetObjectColor()).x,
        };
        physicalObject.room.AddObject(lightningBoltNew);
    }


    public static void ConnectEffect(this PhysicalObject? physicalObject, Vector2 pos, Color? overrideColor = null, bool syncOnline = false)
    {
        if (physicalObject?.room is null)
        {
            return;
        }

        if (physicalObject.abstractPhysicalObject.TryGetSentry(out _))
        {
            return;
        }

        var color = overrideColor ?? physicalObject.abstractPhysicalObject.GetObjectColor();

        var lightningBolt = new MoreSlugcats.LightningBolt(physicalObject.firstChunk.pos, pos, 0, Mathf.Lerp(1.2f, 1.5f, Random.value))
        {
            intensity = 0.75f,
            lifeTime = 12.0f,
            lightningType = Custom.RGB2HSL(color).x,
        };

        physicalObject.room.AddObject(lightningBolt);

        if (ModCompat_Helpers.RainMeadow_IsOnline && syncOnline)
        {
            MeadowCompat.RPC_ObjectConnectEffect(physicalObject, pos, color);
        }
    }

    public static void ConnectEffect(this Room? room, Vector2 startPos, Vector2 targetPos, Color color, float intensity = 0.75f, float lifeTime = 12.0f, bool syncOnline = false)
    {
        if (room is null)
        {
            return;
        }

        var lightningBolt = new MoreSlugcats.LightningBolt(startPos, targetPos, 0, Mathf.Lerp(1.2f, 1.5f, Random.value))
        {
            intensity = intensity,
            lifeTime = lifeTime,
            lightningType = Custom.RGB2HSL(color).x,
        };

        room.AddObject(lightningBolt);

        if (ModCompat_Helpers.RainMeadow_IsOnline && syncOnline)
        {
            MeadowCompat.RPC_RoomConnectEffect(room, startPos, targetPos, color, intensity, lifeTime);
        }
    }


    public static void DeflectEffect(this Room? room, Vector2 pos)
    {
        if (room is null)
        {
            return;
        }

        for (var i = 0; i < 5; i++)
        {
            room.AddObject(new Spark(pos, Custom.RNV(), Color.white, null, 16, 24));
        }

        room.AddObject(new Explosion.ExplosionLight(pos, 150f, 1f, 8, Color.white));
        room.AddObject(new ShockWave(pos, 60f, 0.1f, 8));

        room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pos, 0.6f, 1.5f + Random.value * 0.5f);
    }

    public static void ReviveEffect(this Room? room, Vector2 pos)
    {
        if (room is null)
        {
            return;
        }

        room.AddObject(new Explosion.ExplosionLight(pos, 100.0f, 1.0f, 3, Color.white));
        room.AddObject(new ShockWave(pos, 250.0f, 0.07f, 6));

        room.AddObject(new ShockWave(pos, 30.0f, 20.0f, 20));

        for (var i = 0; i < 4; i++)
        {
            var randVec = Custom.RNV() * 150.0f;
            room.ConnectEffect(pos, pos + randVec, Color.green, 1.5f, 80, true);
        }

        room.PlaySound(SoundID.UI_Slugcat_Die, pos, 1.0f, 1.0f);

        room.PlaySound(SoundID.Fire_Spear_Explode, pos, 0.5f, 0.7f);
        room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pos, 3.0f, 0.4f);
    }
}
