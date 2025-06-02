using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace IntoxiDrunkOverhaul
{
    public sealed class ModEntry : Mod
    {
        private int drunkLevel;
        private int wobbleTick;
        private ModConfig cfg = null!;

        private const int TipsyId = 17;
        private const int WobbleInterval = 60;

        public override void Entry(IModHelper helper)
        {
            cfg = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.UpdateTicked      += OnUpdate;
            helper.Events.GameLoop.DayStarted        += (_, _) => drunkLevel = 0;
            helper.Events.GameLoop.SaveLoaded        += (_, _) => drunkLevel = 0;
            helper.Events.GameLoop.ReturnedToTitle   += (_, _) => drunkLevel = 0;
        }

        private void OnUpdate(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            var player = Game1.player;

            if (player.hasBuff(TipsyId))
            {
                var buff = player.buffsDisplay.otherBuffs.FirstOrDefault(b => b.which == TipsyId);
                if (buff != null && buff.millisecondsDuration < 29000)
                {
                    drunkLevel = Math.Min(drunkLevel + 1, cfg.MaxDrunkLevel);
                    ApplyDrunkBuff(player);
                    DropBottle(player);
                    Game1.addHUDMessage(new HUDMessage("你觉得有点晕……", 3));
                }
            }

            if (++wobbleTick >= WobbleInterval && drunkLevel > 0)
            {
                wobbleTick = 0;
                Game1.screenGlowHold = 0.05f * drunkLevel;
                if (cfg.EnableScreenFlash && drunkLevel >= 4)
                    Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite(6, Game1.random.Next(120, 200)));
            }

            if (drunkLevel >= cfg.ClinicPassOutLevel && !Game1.eventUp)
            {
                player.exhausted.Value = true;
                Game1.addHUDMessage(new HUDMessage("你醉得不省人事，被抬去哈维诊所……", HUDMessage.error_type));
                Game1.endOfNightMenus.Push(new SleepMenu());
                drunkLevel = 0;
            }
        }

        private void ApplyDrunkBuff(Farmer p)
        {
            p.removeBuff(TipsyId);
            int minutes = 30 * drunkLevel;
            var buff = new Buff(
                speed: -cfg.SlowPerLevel * drunkLevel,
                minutesDuration: minutes,
                source: "酒精",
                displaySource: "Drunk"
            ) { which = TipsyId };

            p.buffsDisplay.addOtherBuff(buff);
            p.addedSpeed = -cfg.SlowPerLevel * drunkLevel;
        }

        private void DropBottle(Farmer p)
        {
            if (Game1.random.NextDouble() >= cfg.BottleDropChance) return;
            var loc  = p.currentLocation;
            var tile = p.getTileLocation();
            var bottle = new SObject(parentSheetIndex: 346, stack: 1)
            {
                Name = "Empty Bottle",
                Price = 0
            };
            loc.dropObject(bottle, tile * 64f, p);
        }
    }

    internal sealed class ModConfig
    {
        public int    MaxDrunkLevel      { get; set; } = 6;
        public int    SlowPerLevel       { get; set; } = 1;
        public double BottleDropChance   { get; set; } = 0.4;
        public int    ClinicPassOutLevel { get; set; } = 6;
        public bool   EnableScreenFlash  { get; set; } = true;
    }
}
