using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewMods
{
    /// <summary>The mod entry point.</summary>
    public class Overlay
    {
        ITranslationHelper translate;
        private IMonitor Monitor;
        private IModHelper Helper;
        private IManifest ModManifest;

        private bool hideText = false;    //world fish preview data 
        private Farmer who;
        //private int screen;
        private int totalPlayersOnThisPC;


        private List<Item> fishHere;
        private Dictionary<string, int> fishChances;
        private Dictionary<string, int> fishChancesSlow;
        private int fishChancesModulo;
        private List<string> oldGeneric;
        private Dictionary<string, int> fishFailed;
        private bool isMinigameOther = false;

        private bool isMinigame = false;    //minigame fish preview data, Reflection
        private string miniFish;
        private bool minibossFish;

        private float miniFishPos;
        private int miniXPositionOnScreen;
        private int miniYPositionOnScreen;
        private Vector2 miniFishShake;
        private Vector2 miniEverythingShake;
        private Vector2 miniBarShake;
        private Vector2 miniTreasureShake;
        private float miniScale;
        private bool miniBobberInBar;
        private float miniBobberBarPos;
        private float miniBobberBarHeight;
        private float miniTreasurePosition;
        private float miniTreasureScale;
        private float miniTreasureCatchLevel;
        private bool miniTreasureCaught;
       // private bool flipBubble;
        private float reelRotation;
        private float distanceFromCatching;
        private bool goldenTreasure;


        public static Dictionary<string, LocationData> locationData;
        public static Dictionary<string, string> fishData;
        public static Texture2D[] background = new Texture2D[2];
        public static Color colorBg;
        public static Color colorText;


        public static int miniMode = new int();   //config values
        public static bool barCrabEnabled ;
        public static Vector2 barPosition = new Vector2();
        public static int iconMode = new int();
        public static float barScale = new float();
        public static int maxIcons = new int();
        public static int maxIconsPerRow = new int();
        public static int backgroundMode = new int();
        public static int extraCheckFrequency;
        public static int scanRadius = new int();
        public static bool showTackles = new bool();
        public static bool showPercentages = new bool();
        public static int sortMode = new int();
        public static bool uncaughtDark = new bool();
        public static bool onlyFish = new bool();
        public static bool skipfish = new bool();


        public Overlay(ModEntry entry)
        {
            this.Helper = entry.Helper;
            this.Monitor = entry.Monitor;
            this.ModManifest = entry.ModManifest;
            this.translate = entry.Helper.Translation;
        }

        public void Rendered(object sender, RenderedEventArgs e)
        {
            //screen = Context.ScreenId;
            who = Game1.player;
            try { 
            if (Game1.eventUp || who.CurrentItem == null || who.CurrentItem.Name == null ||
                !((who.CurrentItem is FishingRod) || (who.CurrentItem.Name.Equals("Crab Pot", StringComparison.Ordinal) && barCrabEnabled))) return;//code stop conditions

            totalPlayersOnThisPC = 1;
            foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsSplitScreen) totalPlayersOnThisPC++;
            }

            if (Game1.player.CurrentItem is FishingRod)  //dummy workaround for preventing player from getting special items
            {
                who = new Farmer();


                foreach (string mail in Game1.player.mailReceived)
                {
                    who.mailReceived.Add(mail);
                }
                who.mailReceived.Add("CalderaPainting");
                who.currentLocation = Game1.player.currentLocation;
                var local_fish = locationData["Default"];
                who.setTileLocation(Game1.player.Tile);
                // who.FishingLevel = Game1.player.FishingLevel;
                Buff buff = new Buff(
                    id: "Example.ModId_ZoomZoom",
                    displayName: "Zoom Zoom", // can optionally specify description text too
                   // iconTexture: this.Helper.ModContent.Load<Texture2D>("assets/zoom.png"),
                    //iconSheetIndex: 0,
                    duration: Buff.ENDLESS, // 30 seconds
                    effects: new BuffEffects()
                    {
                        FishingLevel = { Game1.player.FishingLevel},
                        LuckLevel = { Game1.player.LuckLevel }// shortcut for buff.Speed.Value = 10
                    }
                    );
                who.UniqueMultiplayerID = Game1.player.UniqueMultiplayerID;
                who.applyBuff(buff);
                //if there's ever any downside of referencing player rod directly, use below + add bait/tackle to it
                //FishingRod rod = (FishingRod)(Game1.player.CurrentTool as FishingRod).getOne();
                //who.CurrentTool = rod;
                who.CurrentTool = Game1.player.CurrentTool;
               // who.LuckLevel = Game1.player.LuckLevel;
                foreach (var item in Game1.player.fishCaught) who.fishCaught.Add(item);
                foreach (var item in Game1.player.secretNotesSeen) who.secretNotesSeen.Add(item);
              //  who.secretNotesSeen.Add() = Game1.player.secretNotesSeen.g;
            }
            }
            catch
            {
                return;

            }

            SpriteFont font = Game1.smallFont;                                                          //UI INIT
            Rectangle source = GameLocation.getSourceRectForObject(who.CurrentItem.ParentSheetIndex);      //for average icon size
            SpriteBatch batch = Game1.spriteBatch;

            batch.End();    //stop current UI drawing and start mode where where layers work from 0f-1f
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            //MINIGAME PREVIEW
            // var a = FishingRod.bossFish;
            //var fishall =   locationData  .Fish;
            if (isMinigame && miniMode < 2 && miniScale == 1f)//scale == 1f when moving elements appear
            {



                //     batch.Draw(Game1.mouseCursors, new Vector2((float)(this.miniXPositionOnScreen - (this.flipBubble ? 44 : 20) + 404), (float)(this.miniYPositionOnScreen - 16 + 314)) + this.miniEverythingShake,
                //          new Rectangle?(new Rectangle(652, 1685, 52, 157)), Color.White * 0.6f * this.miniScale, 0f, new Vector2(26f, 78.5f) * this.miniScale, 4f * this.miniScale, this.flipBubble ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0.001f);
                //       batch.Draw(Game1.mouseCursors, new Vector2((float)(this.miniXPositionOnScreen + 370), (float)(this.miniYPositionOnScreen + 296)) + this.miniEverythingShake, 
                //         new Rectangle?(new Rectangle(644, 1999, 38, 150)), Color.White * this.miniScale, 0f, new Vector2(18.5f, 74f) * this.miniScale, 4f * this.miniScale, SpriteEffects.None, 0.01f);


                if (miniMode == 0) //Full minigame
                { 


                batch.Draw(Game1.mouseCursors, new Vector2((float)(miniXPositionOnScreen + 64), (float)(miniYPositionOnScreen + 12 + (int)this.miniBobberBarPos)) + this.miniBarShake + this.miniEverythingShake, 
                    new Rectangle?(new Rectangle(685, 1998, 9, 2)), this.miniBobberInBar ? Color.White : (Color.White * 0.25f * ((float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.0), 2) + 2f)), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.89f);
                batch.Draw(Game1.mouseCursors, new Vector2((float)(this.miniXPositionOnScreen + 64), (float)(this.miniYPositionOnScreen + 12 + (int)this.miniBobberBarPos + 8)) + this.miniBarShake + this.miniEverythingShake, new Rectangle?(new Rectangle(682, 2081, 9, 1)),
                    this.miniBobberInBar ? Color.White : (Color.White * 0.25f * ((float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.0), 2) + 2f)), 0f, Vector2.Zero, new Vector2(4f, (float)(this.miniBobberBarHeight - 16)), SpriteEffects.None, 0.89f);
                batch.Draw(Game1.mouseCursors, new Vector2((float)(this.miniXPositionOnScreen + 64), (float)(this.miniYPositionOnScreen + 12 + (int)this.miniBobberBarPos + this.miniBobberBarHeight - 8)) + this.miniBarShake + this.miniEverythingShake, new Rectangle?(new Rectangle(682, 2085, 9, 2)),
                    this.miniBobberInBar ? Color.White : (Color.White * 0.25f * ((float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.0), 2) + 2f)), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.89f);
                batch.Draw(Game1.staminaRect, new Rectangle(this.miniXPositionOnScreen + 124, this.miniYPositionOnScreen + 4 + (int)(580f * (1f - this.distanceFromCatching)), 16, (int)(580f * this.distanceFromCatching)), Utility.getRedToGreenLerpColor(this.distanceFromCatching));
                batch.Draw(Game1.mouseCursors, new Vector2((float)(this.miniXPositionOnScreen + 18), (float)(this.miniYPositionOnScreen + 514)) + this.miniEverythingShake, new Rectangle?(new Rectangle(257, 1990, 5, 10)), Color.White, this.reelRotation, new Vector2(2f, 10f), 4f, SpriteEffects.None, 0.89f);

                    //treasure
                        if (goldenTreasure)
                    {
                        batch.Draw(Game1.mouseCursors_1_6, new Vector2((float)(this.miniXPositionOnScreen + 64 + 18), (float)(this.miniYPositionOnScreen + 12 + 24) + this.miniTreasurePosition) + this.miniTreasureShake + this.miniEverythingShake,
                            new Rectangle?(new Rectangle(256, 51, 20, 24)), Color.White, 0f, new Vector2(10f, 10f), 2f * this.miniTreasureScale, SpriteEffects.None, 0.9f);
                    }
                    else
                    {
                        batch.Draw(Game1.mouseCursors, new Vector2((float)(this.miniXPositionOnScreen + 64 + 18), (float)(this.miniYPositionOnScreen + 12 + 24) + this.miniTreasurePosition) + this.miniTreasureShake + this.miniEverythingShake, 
                            new Rectangle?(new Rectangle(638, 1865, 20, 24)), Color.White, 0f, new Vector2(10f, 10f), 2f * this.miniTreasureScale, SpriteEffects.None, 0.9f);
                    }

                    if (this.miniTreasureCatchLevel > 0f && !this.miniTreasureCaught)
                    {
                        batch.Draw(Game1.staminaRect, new Rectangle(this.miniXPositionOnScreen + 64, this.miniYPositionOnScreen + 12 + (int)this.miniTreasurePosition, 40, 8), null,Color.DimGray * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                        batch.Draw(Game1.staminaRect, new Rectangle(this.miniXPositionOnScreen + 64, this.miniYPositionOnScreen + 12 + (int)this.miniTreasurePosition, (int)(this.miniTreasureCatchLevel * 40f), 8),null, Color.Orange, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                    }
               
                }
                else batch.Draw(Game1.mouseCursors, new Vector2((float)(this.miniXPositionOnScreen + 64 + 18), (float)(this.miniYPositionOnScreen + 12 + 24) + this.miniFishPos) + this.miniFishShake + this.miniEverythingShake,
                    new Rectangle?(new Rectangle(614 + (this.minibossFish ? 20 : 0), 1840, 20, 20)), Color.Black, 0f, new Vector2(10f, 10f), 2.05f, SpriteEffects.None, 0.9f);

                source = ItemRegistry.GetData(miniFish).GetSourceRect();

                batch.Draw(ItemRegistry.GetData(miniFish).GetTexture(), new Vector2((float)(this.miniXPositionOnScreen + 64 + 18), (float)(this.miniYPositionOnScreen + 12 + 24) + this.miniFishPos) + this.miniFishShake + this.miniEverythingShake,
                    source, (!uncaughtDark || who.fishCaught.ContainsKey("(O)" + miniFish)) ? Color.White : Color.DarkSlateGray, 0f, new Vector2(9.5f, 9f),
                    Utility.ModifyCoordinateForUIScale(3f), SpriteEffects.FlipHorizontally, 1f);

            }



            if (iconMode != 3)
            {
                float iconScale = Game1.pixelZoom / 2f * barScale;
                int iconCount = 0;
                float boxWidth = 0;
                float boxHeight = 0;
                Vector2 boxTopLeft = barPosition;
                Vector2 boxBottomLeft = barPosition;


                //this.Monitor.Log("\n", LogLevel.Debug);
                
                if (who.currentLocation is MineShaft && who.CurrentItem.Name.Equals("Crab Pot", StringComparison.Ordinal))//crab pot
                {
                    string warning = translate.Get("Bar.CrabMineWarning");
                    DrawStringWithBorder(batch, font, warning, boxBottomLeft + new Vector2(source.Width * iconScale, 0), Color.Red, 0f, Vector2.Zero, 1f * barScale, SpriteEffects.None, 1f, colorBg); //text
                    batch.End();
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
                    return;
                }

                try
                {
                    if (showTackles && who.CurrentItem is FishingRod)    //BAIT AND TACKLE (BOBBERS) PREVIEW
                    {
                        string bait = (who.CurrentItem as FishingRod).GetBait()?.QualifiedItemId;
                        var a = (who.CurrentItem as FishingRod).GetTackle();
                        bool tackle = false;
                        int tacklenum = 0;
                        ParsedItemData itemData;
                        if (bait != null)
                        {
                            //source = GameLocation.getSourceRectForObject(ItemRegistry.GetData(bait).SpriteIndex);
                            source = ItemRegistry.GetData(bait).GetSourceRect();
                            if (backgroundMode == 0) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);

                            int baitCount = (who.CurrentItem as FishingRod).attachments[0].Stack;
                            batch.Draw(ItemRegistry.GetData(bait).GetTexture(), boxBottomLeft, source, Color.White, 0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.9f);

                            if ((who.CurrentItem as FishingRod).attachments[0].Quality == 4) batch.Draw(Game1.mouseCursors, boxBottomLeft + (new Vector2(13f, (showPercentages ? 24 : 16)) * barScale),
                                new Rectangle(346, 392, 8, 8), Color.White, 0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 1f);
                            else Utility.drawTinyDigits(baitCount, batch, boxBottomLeft + new Vector2((source.Width * iconScale) - Utility.getWidthOfTinyDigitString(baitCount, 2f * barScale),
                                (showPercentages ? 26 : 19) * barScale), 2f * barScale, 1f, colorText);

                            if (iconMode == 1) boxBottomLeft += new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                            else boxBottomLeft += new Vector2(source.Width * iconScale, 0);
                            iconCount++;
                        }

                        for (int i = 0; i < a.Count; i++)
                        {

                            //tackle = a[i]?.ItemId;
                            if (a[i] != null)
                            {

                                //source = GameLocation.getSourceRectForObject(tackle);
                                itemData = ItemRegistry.GetData(a[i]?.ItemId);
                                source = itemData.GetSourceRect();
                                tackle = true;
                                if (backgroundMode == 0) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);

                                int tackleCount = FishingRod.maxTackleUses - a[i].uses.Value;
                                batch.Draw(itemData.GetTexture(), boxBottomLeft, source, Color.White, 0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.9f);

                                if (a[i].Quality == 4) batch.Draw(Game1.mouseCursors, boxBottomLeft + (new Vector2(13f, (showPercentages ? 24 : 16)) * barScale),
                                    new Rectangle(346, 392, 8, 8), Color.White, 0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 1f);
                                else Utility.drawTinyDigits(tackleCount, batch, boxBottomLeft + new Vector2((source.Width * iconScale) - Utility.getWidthOfTinyDigitString(tackleCount, 2f * barScale),
                                    (showPercentages ? 26 : 19) * barScale), 2f * barScale, 1f, colorText);

                                if (iconMode == 1) boxBottomLeft += new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                                else boxBottomLeft += new Vector2(source.Width * iconScale, 0);
                                iconCount++;
                                tacklenum++;
                            }


                        }
                        if (iconMode == 2 && (bait != null || tackle))
                        {
                            boxBottomLeft = boxTopLeft + new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                            boxWidth = (iconCount * source.Width * iconScale) + boxTopLeft.X;
                            boxHeight += (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0);
                            if (bait != null && tackle) iconCount -= tacklenum;
                        }


                    }
                }
                catch { 
                
                }


                bool foundWater = false;
                Vector2 nearestWaterTile = new Vector2(99999f, 99999f);      //any water nearby + nearest water tile check
                if (who.currentLocation.canFishHere())
                {
                    Vector2 scanTopLeft = who.Tile - new Vector2(scanRadius + 1);
                    Vector2 scanBottomRight = who.Tile + new Vector2(scanRadius + 2);
                    for (int x = (int)scanTopLeft.X; x < (int)scanBottomRight.X; x++)
                    {
                        for (int y = (int)scanTopLeft.Y; y < (int)scanBottomRight.Y; y++)
                        {
                            if (who.currentLocation.isTileFishable(x, y) && !who.currentLocation.isTileBuildingFishable(x, y))
                            {
                                Vector2 tile = new Vector2(x, y);
                                float distance = Vector2.DistanceSquared(who.Tile, tile);
                                float distanceNearest = Vector2.DistanceSquared(who.Tile, nearestWaterTile);
                                if (distance < distanceNearest || (distance == distanceNearest &&  Game1.player.GetGrabTile() == tile)) nearestWaterTile = tile;
                                foundWater = true;
                            }
                        }
                    }
                }

                if (foundWater)
                {   
                    if (who.CurrentItem is FishingRod) who.setTileLocation(nearestWaterTile);
                    string locationName = who.currentLocation.Name;    //LOCATION FISH PREVIEW
                        if (who.CurrentItem is FishingRod)
                    {
                        if (!isMinigame)
                        {
                            if (oldGeneric == null)
                            {
                                oldGeneric = new List<string>();
                                fishFailed = new Dictionary<string, int>();
                                fishHere = new List<Item> { ItemRegistry.Create("(O)168") };
                                fishChances = new Dictionary<string, int> { { "-1", 0 }, { "(O)168", 0 } };
                                fishChancesSlow = new Dictionary<string, int>();
                                fishChancesModulo = 1;
                            }
                                
                            AddGenericFishToList(locationName);
                        }
                    }
                    else AddCrabPotFish();
                    //for (int i = 0; i < 20; i++)    //TEST ITEM INSERT
                    //{
                    //    fishHere.Add(100 + i);
                    //    fishChances.Add(100 + i, 1000);
                    //}
                    //  List<string> Jelly = new List<string>() { "CaveJelly", "RiverJelly", "SeaJelly" };
                    int percent=0;
                    foreach (var fish in fishHere)
                    {

                        //if (fish == null || fish == "")
                        //{
                        //    continue;
                        //}
                        
                        try
                        {
                            if (onlyFish && fish.QualifiedItemId != "(O)168" && !fishData.ContainsKey(fish.ItemId)) continue;//skip if not fish, except trash
                            try {

                                if (fishChancesSlow.ContainsKey(fish.QualifiedItemId) && fishChancesSlow[fish.QualifiedItemId]>1) percent = (int)Math.Round((float)fishChancesSlow[fish.QualifiedItemId] / fishChancesSlow["-1"] * 100f);
                                else percent = 0; //chance of this fish
                            }
                            catch {

                                percent = 0;


                            }
                        if (iconCount < maxIcons && percent > 0)
                        {
                            bool caught = (!uncaughtDark || who.fishCaught.ContainsKey(fish.QualifiedItemId)|| !fishData.ContainsKey(fish.ItemId));
                            if (fish.QualifiedItemId == "(O)168") caught = true;

                            iconCount++;
                            string fishNameLocalized = "???";
                                // ParsedItemData item1 = ItemRegistry.GetData("RiverJelly");
                          // var a = fishHere[0].Type;

                                // if (ItemRegistry.Create(fish.QualifiedItemId, 1).Name.Equals("Error Item", StringComparison.Ordinal) && fish.CompareTo("900078") != 0)  //Furniture
                                if (fish.GetItemTypeId() == ItemRegistry.type_furniture || fish.GetItemTypeId() == ItemRegistry.type_hat )
                            {
                                //Monitor.Log(fish, LogLevel.Warn);
                               
                                ParsedItemData item = ItemRegistry.GetData(fish.QualifiedItemId);
                                // ParsedItemData item1 = ItemRegistry.GetData("RiverJelly");

                                // if (caught) fishNameLocalized = item.DisplayName;
                                Texture2D texture = item.GetTexture();
                                fishNameLocalized = item.DisplayName;


                                batch.Draw(texture, boxBottomLeft, item.GetSourceRect(),
                                     Color.White , 0f, Vector2.Zero,((fish.GetItemTypeId() == ItemRegistry.type_furniture)? 0.95f:1.5f ) * barScale, SpriteEffects.None, 0.98f);//icon
                             

                            }
                            //else if(fish.GetItemTypeId() == ItemRegistry.type_hat)//Hat (workaround)
                            //{
                            //    ParsedItemData item = ItemRegistry.GetData(fish.QualifiedItemId);

                            //    if (caught) fishNameLocalized = item.DisplayName;
                            //    Texture2D texture = item.GetTexture();
                            //    batch.Draw(texture, boxBottomLeft, item.GetSourceRect(),
                            //        Color.White, 0f, Vector2.Zero, 1.5f * barScale[screen], SpriteEffects.None, 0.98f);//icon
                            //}

                           
                            else                                                                                        //Item
                            {


                               // Monitor.Log(fish.QualifiedItemId);
                                ParsedItemData item = ItemRegistry.GetData(fish.QualifiedItemId);




                                if (caught) fishNameLocalized = item.DisplayName;

                                //  Monitor.Log(fish, LogLevel.Warn);
                                //source = GameLocation.getSourceRectForObject(int.Parse(fish));

                          
                                if (fish.QualifiedItemId == "(O)168") batch.Draw(Game1.objectSpriteSheet, boxBottomLeft + new Vector2(2 * barScale, -5 * barScale), item.GetSourceRect(), (caught) ? Color.White : Color.DarkSlateGray,
                                    0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.98f);//icon trash
                                else batch.Draw(item.GetTexture(), boxBottomLeft, item.GetSourceRect(), (caught) ? Color.White : Color.DarkSlateGray,
                                    0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.98f);//icon
                            }

                            if (showPercentages)
                            {
                                DrawStringWithBorder(batch, font, percent + "%", boxBottomLeft + new Vector2((source.Width * iconScale / 2f), 27f * barScale),
                                    (caught) ? colorText : colorText * 0.8f, 0f, new Vector2(font.MeasureString(percent + "%").X / 2f, 0f), 0.58f * barScale, SpriteEffects.None, 1f, colorBg);//%
                            }

                            if (fish.QualifiedItemId == ("(O)"+miniFish) && miniMode < 3) batch.Draw(background[0], new Rectangle((int)boxBottomLeft.X - 1, (int)boxBottomLeft.Y - 1, (int)(source.Width * iconScale) + 1, (int)((source.Width * iconScale) + (showPercentages ? 10 * barScale : 0) + 1)),
                                null, Color.GreenYellow, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);  //minigame outline

                            if (backgroundMode == 0) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);


                            if (iconMode == 0)      //Horizontal Preview
                            {
                                if (iconCount % maxIconsPerRow == 0) boxBottomLeft = new Vector2(boxTopLeft.X, boxBottomLeft.Y + (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0)); //row switch
                                else boxBottomLeft += new Vector2(source.Width * iconScale, 0);
                            }
                            else                    //Vertical Preview
                            {
                                if (iconMode == 2 && !hideText)  // + text
                                {
                                    DrawStringWithBorder(batch, font, fishNameLocalized, boxBottomLeft + new Vector2(source.Width * iconScale, 0), (caught) ? colorText : colorText * 0.8f, 0f, new Vector2(0, -3), 1f * barScale, SpriteEffects.None, 0.98f, colorBg); //text
                                    boxWidth = Math.Max(boxWidth, boxBottomLeft.X + (font.MeasureString(fishNameLocalized).X * barScale) + (source.Width * iconScale));
                                }

                                if (iconCount % maxIconsPerRow == 0) //row switch
                                {
                                    if (iconMode == 2) boxBottomLeft = new Vector2(boxWidth + (20 * barScale), boxTopLeft.Y);
                                    else boxBottomLeft = new Vector2(boxBottomLeft.X + (source.Width * iconScale), boxTopLeft.Y);
                                }
                                else boxBottomLeft += new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                                if (iconMode == 2 && iconCount <= maxIconsPerRow) boxHeight += (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0);
                            }
                        }
                        }
                        catch
                        {
                            continue;

                        }
                    }

                    if (backgroundMode == 1) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);
                }
                else if (backgroundMode == 1) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);
            }

            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        }


        public void OnMenuChanged(object sender, MenuChangedEventArgs e)   //Minigame data
        {
            if (e.NewMenu is BobberBar)
            { isMinigame = true;
                BobberBar Bobber = e.NewMenu as BobberBar;
            }
            else
            {
                isMinigame = false;
                if (e.OldMenu is BobberBar) miniFish = "-1";
            }
        }
        public void OnRenderMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is BobberBar bar  && isMinigame)
            {
                miniFish = Helper.Reflection.GetField<string>(bar, "whichFish").GetValue();

                if (miniMode < 2)
                {
                    miniScale = Helper.Reflection.GetField<float>(bar, "scale").GetValue();
                    miniFishPos = Helper.Reflection.GetField<Single>(bar, "bobberPosition").GetValue();
                    miniXPositionOnScreen = Helper.Reflection.GetField<int>(bar, "xPositionOnScreen").GetValue();
                    miniYPositionOnScreen = Helper.Reflection.GetField<int>(bar, "yPositionOnScreen").GetValue();
                    miniFishShake = Helper.Reflection.GetField<Vector2>(bar, "fishShake").GetValue();
                    miniEverythingShake = Helper.Reflection.GetField<Vector2>(bar, "everythingShake").GetValue();
                    reelRotation = Helper.Reflection.GetField<float>(bar, "reelRotation").GetValue();
                    minibossFish = Helper.Reflection.GetField<bool>(bar, "bossFish").GetValue();
                    distanceFromCatching = Helper.Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();
                 //   flipBubble = Helper.Reflection.GetField<bool>(bar, "flipBubble").GetValue() || false;


                }
                if (miniMode == 0)
                {
                    miniBarShake = Helper.Reflection.GetField<Vector2>(bar, "barShake").GetValue();
                    miniTreasureShake = Helper.Reflection.GetField<Vector2>(bar, "treasureShake").GetValue();
                    miniBobberInBar = Helper.Reflection.GetField<bool>(bar, "bobberInBar").GetValue();
                    miniBobberBarPos = Helper.Reflection.GetField<float>(bar, "bobberBarPos").GetValue();
                    miniBobberBarHeight = Helper.Reflection.GetField<int>(bar, "bobberBarHeight").GetValue();
                    miniTreasurePosition = Helper.Reflection.GetField<float>(bar, "treasurePosition").GetValue();
                    miniTreasureScale = Helper.Reflection.GetField<float>(bar, "treasureScale").GetValue();
                    miniTreasureCatchLevel = Helper.Reflection.GetField<float>(bar, "treasureCatchLevel").GetValue();
                    miniTreasureCaught = Helper.Reflection.GetField<bool>(bar, "treasureCaught").GetValue();
                    miniTreasureCatchLevel = Helper.Reflection.GetField<float>(bar, "treasureCatchLevel").GetValue();
                    miniTreasureCaught = Helper.Reflection.GetField<bool>(bar, "treasureCaught").GetValue();
                    goldenTreasure = Helper.Reflection.GetField<bool>(bar, "goldenTreasure").GetValue();
                }
            }
        }
        public void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == "barteke22.FishingMinigames")
            {
                if (e.Type == "whichFish")
                {
                    miniFish = e.ReadAs<string>();
                    if (miniFish == "-1") isMinigameOther = false;
                    else isMinigameOther = true;
                }
                if (e.Type == "hideText") hideText = e.ReadAs<bool>();
            }
        }

        private void AddGenericFishToList(string locationName)         //From GameLocation.cs getFish()
        {
            List<string> tempFish = new List<string>();
            //bool magicBait = who.currentLocation.IsUsingMagicBait(who);
            bool magicBait =false;
            if (who.CurrentTool is FishingRod rod) magicBait = rod.HasMagicBait();
            List<SpawnFishData> rawFishData = new List<SpawnFishData>();

            List<string> all_season_fish = new List<string>();
            if (locationName.Equals("BeachNightMarket", StringComparison.Ordinal)) locationName = "Beach";
            if (locationName.Equals("DesertFestival", StringComparison.Ordinal)) locationName = "Desert";

            LocationData data2 = who.currentLocation.GetData();


            if (data2?.Fish!=null)
            {

                foreach (SpawnFishData item in data2.Fish)
                {


                    if (magicBait) { rawFishData.Add(item); }

                    else if (!magicBait && item.Season.ToString().Equals(Game1.currentSeason, StringComparison.OrdinalIgnoreCase))
                    {
                        rawFishData.Add(item);
                      //  rawFishData.Add(Regex.Replace(item.Id, @"\D", ""));

                    }
                    else if (item.Season == null)
                    {   
                        //magic bait = all fish
                       
                       rawFishData.Add(item);
                    }

                   
                }
                //if (!magicBait) rawFishData = locationData[locationName].Split('/')[4 + Utility.getSeasonNumber(Game1.currentSeason)].Split(' '); //fish by season
                

                Dictionary<string, Rectangle?> rawFishDataWithLocation = new Dictionary<string, Rectangle?>();

                if (rawFishData.Count > 1)

                {
                    for (int j = 0; j < rawFishData.Count; j += 1) {
                        var posttion = rawFishData[j].PlayerPosition;
                        if (posttion is not null && rawFishData[j].RandomItemId is null)
                        {

                            rawFishDataWithLocation.Add(rawFishData[j].ItemId, posttion.Value);

                        }
                        else if (rawFishData[j].RandomItemId?.Count > 0)
                        {

                            var a1 = rawFishData[j].RandomItemId;

                            foreach (var fishid in a1)
                            {
                                rawFishDataWithLocation.Add(fishid, posttion is not null? posttion.Value: new Rectangle(0, 0, 0, 0));


                            }


                        }
                        else if (rawFishDataWithLocation.ContainsKey(rawFishData[j].ItemId)) continue;

                        else if (rawFishData[j].ItemId != null) rawFishDataWithLocation.Add(rawFishData[j].ItemId, posttion is not null ? posttion.Value: new Rectangle(0, 0, 0, 0));






                }
            }
                  

                string[] keys = rawFishDataWithLocation.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    Point location = new Point(-1,-1);
                    try
                    {
                       var  fish = ItemRegistry.Create(keys[i]);
                        bool fail = true;
                        string str = Regex.Replace(keys[i], @".*\)", "");
                        if (fish.HasTypeObject())
                        {



                        }




                    if (!Regex.IsMatch(keys[i], @"\([O]\)") || str  is null || str == "") {


                       // Monitor.Log($"errro: {keys[i]}",LogLevel.Warn);
                        rawFishDataWithLocation.Remove(keys[i]);
                        continue;

                    }
                    //int.TryParse(str,out int key);

                    if (fishData.ContainsKey(str)) { 
                    string[] specificFishData = fishData[str]?.Split('/');
                            if (specificFishData[1] == "trap")   //  fix  Beach Farm fishing trap
                            {
                                tempFish.Add(keys[i]);
                                continue;

                            }
                            //    who.currentLocation.b;
                            string[] timeSpans = specificFishData[5]?.Split(' ');
                    if (!rawFishDataWithLocation[keys[i]].Equals(Rectangle.Empty))
                    {
                        location = rawFishDataWithLocation[keys[i]].Value.Location;
                     //   var a = Game1.player.TilePoint;

                    };
                    if (location.X==-1|| Game1.player.TilePoint ==  location)
                    {
                        for (int l = 0; l < timeSpans?.Length; l += 2)
                        {

                            try{

                                if (Game1.timeOfDay >= Convert.ToInt32(timeSpans[l]) && Game1.timeOfDay < Convert.ToInt32(timeSpans[l + 1]))
                                {
                                    fail = false;
                                    break;
                                }


                            }
                            catch {
                                        fail = true;

                                        break;
                            
                            }
                         
                        }
                    }
                    if (!specificFishData[7].Equals("both", StringComparison.Ordinal))
                    {
                       
                        if (specificFishData[7].Equals("rainy", StringComparison.Ordinal) && !Game1.IsRainingHere(who.currentLocation)) fail = true;
                        else if (specificFishData[7].Equals("sunny", StringComparison.Ordinal) && Game1.IsRainingHere(who.currentLocation)) fail = true;
                    }
                    if (magicBait) fail = false; //I guess magic bait check comes at this exact point because it overrides all conditions except rod and level?

                    bool beginnersRod = who != null && who.CurrentItem != null && who.CurrentItem is FishingRod && (int)who.CurrentTool.UpgradeLevel == 1;

                    if (Convert.ToInt32(specificFishData[1]) >= 50 && beginnersRod) fail = true;
                    if (who.FishingLevel < Convert.ToInt32(specificFishData[12])) fail = true;


                    }
                    else fail = false;

                    if (!fail && !tempFish.Contains(keys[i]))
                    {
                        tempFish.Add(keys[i]);
                    }

                    }
                    catch { }
                }
                if ((tempFish.Count == 0 && oldGeneric.Count != 0) || tempFish.Count > 0 && (!(new HashSet<string>(oldGeneric).SetEquals(tempFish))))//reset lists if generic list changed
                {
                    oldGeneric = tempFish.ToList();
                    fishFailed = new Dictionary<string, int>();
                    fishHere = new List<Item> { ItemRegistry.Create("(O)168")};
                    fishChances = new Dictionary<string, int> { { "-1", 0 }, { "(O)168", 0 } };
                    fishChancesSlow = new Dictionary<string, int>();
                    fishChancesModulo = 1;

                    foreach (var key in oldGeneric)
                    {   
                       

                        if (sortMode == 0) SortItemIntoListByDisplayName(key);
                        else fishHere.Add(ItemRegistry.Create(key));

                        if (!fishChances.ContainsKey(key)) fishChances.Add(key, 0);
                    }
                }
            }
            AddFishToListDynamic();
        }
        private void AddFishToListDynamic()                            //very performance intensive check for fish fish available in this area - simulates fishing
        {
            int freq = (isMinigame || isMinigameOther) ? 6 / totalPlayersOnThisPC : extraCheckFrequency / totalPlayersOnThisPC; //minigame lowers frequency
            //HashSet<string>  fishname = new HashSet<string>();
            int nuts = 0;
            Item item;
            skipfish = false;
            for (int i = 0; i < freq; i++)
            {   
                item = null;
                nuts = (Game1.player.team.limitedNutDrops.ContainsKey("IslandFishing")) ? Game1.player.team.limitedNutDrops["IslandFishing"] : 0;

                Game1.stats.TimesFished++;
                 int fish = AddHardcoded();
                string fishid = fish.ToString();
                Game1.stats.TimesFished--;
                if (skipfish) break;

                if (fish != -2)//not fully hardcoded
                {
                    if (fish == -1)//dynamic
                    {
                        //int nuts = 5;                                                                           //"fix" for preventing player from not getting specials       ----start1
                    //    bool mail1 = false;
                    //    bool mail2 = false;
                   //     bool caughtIridiumKrobus = Game1.player.mailReceived.Contains("caughtIridiumKrobus");



                        //item = (Object)who.currentLocation.getFish(0, "1", 5, who, 100, who.Tile, who.currentLocation.Name);

             

            
                        if (item == null)
                        {
                            Game1.stats.TimesFished++;
                            item = who.currentLocation.getFish(0, "1", 5, who, 100, who.Tile, who.currentLocation.Name);
                            Game1.stats.TimesFished--;


                        }
                        if (who.currentLocation is IslandLocation)
                        {

                            if (nuts < 5) Game1.player.team.limitedNutDrops["IslandFishing"] = nuts;
                        }

                    }
                    else {

                        item = ItemRegistry.Create(fishid);
                    
                    
                    }

                    if (item == null) { continue; }
                    try
                    {
                        // if (item.ItemId=="73") Monitor.Log("Skipped incorrect Object of type" + item.GetType() + ", ID: " + item.ItemId + ", QualifiedItemId: " + item.QualifiedItemId + ", CodeName: " + item.Name + ". DisplayName is \"Error Item\".\nContinue  game", LogLevel.Error);

                        //  var a = Game1.player.team.limitedNutDrops;

                        if (item.Name.Equals("Error Item", StringComparison.Ordinal))
                        {
                            Monitor.LogOnce("Skipped incorrect Object of type" + item.GetType() + ", ID: " + item.ItemId + ", QualifiedItemId: " + item.QualifiedItemId + ", CodeName: " + item.Name + ". DisplayName is \"Error Item\".\nContinue  game", LogLevel.Error);
                            continue;
                        }
                        //   fishname.Add(item.DisplayName);
                        //     if ((item.ParentSheetIndex == 134 || item.ParentSheetIndex == 133 || item.ParentSheetIndex == 132))
                        //       {


                        //      Monitor.Log(item.DisplayName, LogLevel.Error);



                        //    }


                        fishid = item.QualifiedItemId;  // string

                        fish = item.ParentSheetIndex;  // int

                        // Monitor.Log(fish + ":"+fishid,LogLevel.Info);
                    }


                    catch (Exception)
                    {
                        Monitor.LogOnce("Skipped Object of type" + item.GetType() + ", ID: " + item.ParentSheetIndex + ", CodeName: " + item.Name + ", Catefory: " + item.Category + ". Missing DisplayName.", LogLevel.Error);
                        continue;
                    }

                    int val;
                    if (fishChances["-1"] < int.MaxValue && fishid != null && item != null) //percentages, slow version (the one shown) is updated less over time
                    {
                        if (fish >= 167 && fish <= 172)
                        {
                            fishChances.TryGetValue("(O)168", out val);
                            fishChances["(O)168"] = val + 1;
                        }
                        else if ( fishHere.All(f => f.QualifiedItemId !=item?.QualifiedItemId))
                        {
                            fishChances = new Dictionary<string, int> { { "-1", 0 } };//reset % on new fish added
                            foreach (var f in fishHere)
                            { if ( f is not null) fishChances.TryAdd(f.QualifiedItemId, 1);
                            }
                            fishChancesSlow = new Dictionary<string, int>();
                            fishChancesModulo = 1;

                            if (sortMode == 0) SortItemIntoListByDisplayName(fishid); //sort by name
                            else fishHere.Add(item);
                            fishChances.Add(fishid, 1);
                        }
                        else
                        {
                            fishChances.TryGetValue(fishid, out val);
                            fishChances[fishid] = val + 1;
                        }
                    }
                    fishChances.TryGetValue("-1", out val);
                    fishChances["-1"] = val + 1;
                    if (fishChances["-1"] % fishChancesModulo == 0)
                    {
                        if (fishChancesModulo < 10000) fishChancesModulo *= 10;
                        fishChancesSlow = fishChances.ToDictionary(entry => entry.Key, entry => entry.Value);
                    }
                    if (sortMode == 1) SortListByPercentages(); //sort by %



                    //if fish not in last X attempts, redo lists
                    if ((fish < 167 || fish > 172))
                    {
                        fishChances.TryGetValue(fishid, out val);
                        float chance = (float)val / fishChances["-1"] * 100f;
                        if (chance < 0.5f) fishFailed[fishid] = 5000;
                        else if (chance < 1f) fishFailed[fishid] = 3500;
                        else if (chance < 2f) fishFailed[fishid] = 3000;
                        else if (chance < 3f) fishFailed[fishid] = 2500;
                        else if (chance < 4f) fishFailed[fishid] = 1500;
                        else fishFailed[fishid] = 1000;
                    }
                }
                foreach (var key in fishFailed.Keys.ToList())
                {
                    fishFailed[key]--;
                    if (fishFailed[key] < 1) oldGeneric = null;
                }
            }
       //     Monitor.Log("[" + string.Join(", ", fishname) + "]", LogLevel.Error);

        }

        private int AddHardcoded()//-2 skip dynamic, -1 dynamic, above -1 = item to add to dynamic
        {
            //if (who.currentLocation is Caldera)
            //{
            //    if (Game1.random.NextDouble() < 0.05 && !Game1.player.mailReceived.Contains("CalderaPainting")) return 2732;//physics 101
            //    return -1;
            //}
            //if (who.currentLocation is Forest)
            //{
            //    if (who.Tile.Y > 108f && !Game1.player.mailReceived.Contains("caughtIridiumKrobus")) return 2396;//iridium krobus
            //    return -1;
            //}
            IModInfo modInfo = this.Helper.ModRegistry.Get("NermNermNerm.QuestableTractor");
            //object api = Helper.ModRegistry.GetApi("NermNermNerm.QuestableTractor");

            if (modInfo !=null && who.currentLocation == Game1.getFarm() )
            {
                skipfish=true;
                Monitor.LogOnce("QuestableTractor detected, disable Farm fish forecast",LogLevel.Info) ;

                if (((who.CurrentTool != null) ? who.CurrentTool.QualifiedItemId : null) == "(T)NermNermNerm.QuestableTractor.Harpoon")
                {
                    fishHere = new List<Item>() {  };
                    fishChancesSlow = new Dictionary<string, int>() { };
                }
                else
                {

                 



                    try
                    {
                        var a = modInfo.GetType().GetProperty("Mod").GetValue(modInfo);
                        var b = a.GetType().GetField("QuestControllers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(a);

                        //  Type elementType = typeof(IEnumerable).GetMethod("GetType").Invoke(enumerable, null) as Type; ;

                        var count = b.GetType().GetProperty("Count").GetValue(b);
                        //      var getItemMethod = b.GetType().GetProperty("Item").GetValue(b);
                        // MethodInfo getItemMethod = itemProperty.GetGetMethod();
                        foreach (var item in b as IEnumerable)
                        {
                            // 使用反射调用get_Item方法来获取元素
                            //      object element = getItemMethod.Invoke(b, new object[] { i });
                            //        if (element == null) continue;
                            //       else if ((string)element.GetType().GetProperty("Count").GetValue(element) == "QuestableTractor.BorrowHarpoonQuestStatus")
                            var OverallQuestState = item.GetType().GetProperty("OverallQuestState", BindingFlags.Public | BindingFlags.Instance).GetValue(item);
                            string ModDataKey = (string)item.GetType().GetProperty("ModDataKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item);
                            //string aaa = OverallQuestState.ToString() as string;
                            if (ModDataKey == "QuestableTractor.BorrowHarpoonQuestStatus" && OverallQuestState.ToString() == "InProgress")
                            {
                                var State = item.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance).GetValue(item);

                                if (State.ToString() != "CatchTheBigOne")
                                {
                                    skipfish = false;

                                    return -1; }
                            }
                            else if (ModDataKey == "QuestableTractor.BorrowHarpoonQuestStatus" && OverallQuestState.ToString() == "Completed")
                            {
                                skipfish = false;
                                Monitor.LogOnce("QuestableTractor's BorrowHarpoonQuest Completed , enable Farm fish forecast", LogLevel.Info);
                                return -1;

                            }

                            // 输出元素
                            //       Console.WriteLine(element);
                        }
                        fishHere = new List<Item>() { }; ;
                        fishChancesSlow = new Dictionary<string, int>() {  };
                        //  bool  hh =     b.ANY(a => a.ModDataKey == "QuestableTractor.BorrowHarpoonQuestStatus");



                    }
                    catch { }

                }

                oldGeneric = null;
                return -1;
            }


            if (who.currentLocation is IslandLocation)
            {
              //  if (Utility.CreateRandom(Game1.stats.DaysPlayed, Game1.stats.TimesFished, Game1.uniqueIDForThisGame).NextDouble() < 0.15 && (!Game1.player.team.limitedNutDrops.ContainsKey("IslandFishing") || Game1.player.team.limitedNutDrops["IslandFishing"] < 5)) return 73;

                //if (who.currentLocation is IslandFarmCave)
                //{
                //    if (Game1.random.NextDouble() < 0.1) return 900078;//frog hat + 900000
                //    else if (who.currentLocation.HasUnlockedAreaSecretNotes(Game1.player) && Game1.random.NextDouble() < (0.08) && who.currentLocation.tryToCreateUnseenSecretNote(Game1.player) != null) return 842;//journal
                //    else return -1;
                //}

                //if (who.currentLocation is IslandNorth)
                //{
                //    if ((bool)(Game1.getLocationFromName("IslandNorth") as IslandNorth).bridgeFixed.Value &&
                //        (new Random((int)(who.Tile.X * 2000 + who.Tile.Y * 777 + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + (int)Game1.stats.TimesFished))).NextDouble() < 0.1) return 821;
                //    return -1;
                //}

                if (who.currentLocation is IslandSouthEast && who.Tile.X >= 17 && who.Tile.X <= 21 && who.Tile.Y >= 19 && who.Tile.Y <= 23)
                {
                    if (!(Game1.player.currentLocation as IslandSouthEast).fishedWalnut.Value)
                    {
                        fishHere = new List<Item>() { ItemRegistry.Create("(O)73") };
                        fishChancesSlow = new Dictionary<string, int>() { { "-1", 10 }, { "(O)73", 10 }, { "(O)168", 1 } };
                    }
                    else
                    {
                        fishHere = new List<Item>() { ItemRegistry.Create("(O)168") };;
                        fishChancesSlow = new Dictionary<string, int>() { { "-1", 10 }, { "(O)168", 10 } };
                    }
                    oldGeneric = null;
                    return -2;
                }

                //if (who.currentLocation is IslandWest)
                //{
                //    if (Game1.player.hasOrWillReceiveMail("islandNorthCaveOpened") &&
                //        (new Random((int)(who.Tile.X * 2000 + who.Tile.Y * 777 + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + (int)Game1.stats.TimesFished))).NextDouble() < 0.1) return 825;
                //    return -1;
                //}
            }
            if (who.currentLocation is Railroad)
            {
                if (Game1.currentSeason.Equals("winter")) return -2;
                else if (Game1.player.secretNotesSeen.Contains(GameLocation.NECKLACE_SECRET_NOTE_INDEX) && !Game1.player.hasOrWillReceiveMail(GameLocation.CAROLINES_NECKLACE_MAIL)) return 191;  //return int.Parse(Regex.Replace(GameLocation.CAROLINES_NECKLACE_ITEM_QID, @"\D", "")) ;
                                                                                                                                                                                                  //   else if (!who.mailReceived.Contains("gotSpaFishing")) return 2423;
                                                                                                                                                                                                  //     else if (Game1.random.NextDouble() < 0.08) return 2423;
                                                                                                                                                                                                  //else return 168;
            }
            return -1;
        }

        private void AddCrabPotFish()
        {
            fishHere = new List<Item>();
            bool isMariner = who.professions.Contains(10);
            if (!isMariner) fishHere.Add(ItemRegistry.Create("(O)168"));//trash
            fishChancesSlow = new Dictionary<string, int>();
            float failChance = 0;

            bool ocean = who.currentLocation is Beach;
            var PotType = who.currentLocation.GetCrabPotFishForTile(who.Tile);
            if (PotType != null && PotType.Count>0) {

                ocean = PotType[0].Equals("ocean",StringComparison.Ordinal)?true:false;
            }
            //Monitor.Log(a[0], LogLevel.Info);
            if (who.currentLocation.GetData()?.FishAreas != null && who.currentLocation.GetData()?.FishAreas.Count >0)
            {
                foreach (KeyValuePair<string, FishAreaData> fishArea in who.currentLocation.GetData()?.FishAreas)
                {
                    FishAreaData value = fishArea.Value;
                    bool? flag = value.Position?.Contains((int)who.Tile.X, (int)who.Tile.Y);
                    if (flag.HasValue)
                    {
                        if (flag.GetValueOrDefault())
                        {
                            failChance = (isMariner ? 1f : 0.8f - (float)value.CrabPotJunkChance);
                            break;
                        }
                    }
                    else if (failChance == 0) {
                        failChance = (isMariner ? 1f : 0.8f - (float)value.CrabPotJunkChance);


                    }


                }
            }
            if (failChance == 0)
            {
                failChance = 0.8f - 0.2f;
            }



            foreach (var fish in fishData)
            {
                if (!fish.Value.Contains("trap")) continue;

                string[] rawSplit = fish.Value.Split('/');
                if ((rawSplit[4].Equals("ocean", StringComparison.Ordinal) && ocean) || (rawSplit[4].Equals("freshwater", StringComparison.Ordinal) && !ocean))
                {
                    if (fishHere.All(f => f.ItemId != fish.Key))
                    {
                        if (sortMode == 0) SortItemIntoListByDisplayName(fish.Key);
                        else fishHere.Add(ItemRegistry.Create("(O)" + fish.Key));

                        if (showPercentages || sortMode == 1)
                        {
                            float rawChance = float.Parse(rawSplit[2]);
                            fishChancesSlow.Add("(O)"+fish.Key, (int)Math.Round(rawChance * failChance * 100f));
                            failChance *= (1f - rawChance);
                        }
                    }
                }
            }
            if (isMariner) fishChancesSlow.Add("-1", fishChancesSlow.Sum(x => x.Value));
            else
            {
                fishChancesSlow.Add("(O)168", 100 - fishChancesSlow.Sum(x => x.Value));
                fishChancesSlow.Add("-1", 100);
            }
            if (sortMode == 1) SortListByPercentages();
        }



        private void SortItemIntoListByDisplayName(string itemId)
        {

            //    if (!int.TryParse(itemId, out var _itemId)) return;

            //if (itemId == "900078")

            string name = (itemId == "900078") ? new Hat(("78").ToString()).Name :  ItemRegistry.Create(itemId)?.Name;
            if ( name.Equals("Error Item", StringComparison.Ordinal) ){

               // var a = new Object(itemId, 1);
                
                return; }
            for (int j = 0; j < fishHere.Count; j++)
            {   

              //  if (!int.TryParse(fishHere[j], out var a)) return;
                string name2 = (itemId == "900078") ? new Hat($"78").Name : fishHere[j].Name;
                if (string.Compare(name, name2, StringComparison.CurrentCulture) <= 0)
                {
                    fishHere.Insert(j, ItemRegistry.Create(itemId));
                    return;
                }
            }
            fishHere.Add(ItemRegistry.Create(itemId));
        }

        private void SortListByPercentages()
        {
            int index = 0;
            foreach (var item in fishChancesSlow.OrderByDescending(d => d.Value).ToList())
            {
                if (fishHere.FindIndex(f => f.QualifiedItemId == item.Key)> -1)
                {
                    fishHere.RemoveAt(fishHere.FindIndex(f => f.QualifiedItemId == item.Key));
                    fishHere.Insert(index, ItemRegistry.Create(item.Key));
                    index++;
                }
            }
        }


        /// <summary>Makes text a tiny bit bolder and adds a border behind it. The border uses text colour's alpha for its aplha value. 6 DrawString operations, so 6x less efficient.</summary>
        private void DrawStringWithBorder(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, Color? borderColor = null)
        {
            Color border = borderColor.HasValue ? borderColor.Value : Color.Black;
            border.A = color.A;
            batch.DrawString(font, text, position + new Vector2(-1.2f * scale, -1.2f * scale), border, rotation, origin, scale, effects, layerDepth - 0.00001f);
            batch.DrawString(font, text, position + new Vector2(1.2f * scale, -1.2f * scale), border, rotation, origin, scale, effects, layerDepth - 0.00001f);
            batch.DrawString(font, text, position + new Vector2(-1.2f * scale, 1.2f * scale), border, rotation, origin, scale, effects, layerDepth - 0.00001f);
            batch.DrawString(font, text, position + new Vector2(1.2f * scale, 1.2f * scale), border, rotation, origin, scale, effects, layerDepth - 0.00001f);

            batch.DrawString(font, text, position + new Vector2(-0.2f * scale, -0.2f * scale), color, rotation, origin, scale, effects, layerDepth);
            batch.DrawString(font, text, position + new Vector2(0.2f * scale, 0.2f * scale), color, rotation, origin, scale, effects, layerDepth);
        }
        private void AddBackground(SpriteBatch batch, Vector2 boxTopLeft, Vector2 boxBottomLeft, int iconCount, Rectangle source, float iconScale, float boxWidth, float boxHeight)
        {
            if (backgroundMode == 0)
            {
                batch.Draw(background[backgroundMode], new Rectangle((int)boxBottomLeft.X - 1, (int)boxBottomLeft.Y - 1, (int)(source.Width * iconScale) + 1, (int)((source.Width * iconScale) + 1 + (showPercentages ? 10 * barScale : 0))),
                    null, colorBg, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }
            else if (backgroundMode == 1)
            {
                if (iconMode == 0) batch.Draw(background[backgroundMode], new Rectangle((int)boxTopLeft.X - 2, (int)boxTopLeft.Y - 2, (int)(source.Width * iconScale * Math.Min(iconCount, maxIconsPerRow)) + 5,
               (int)(((source.Width * iconScale) + (showPercentages ? 10 * barScale : 0)) * Math.Ceiling(iconCount / (maxIconsPerRow * 1.0))) + 5), null, colorBg, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                else if (iconMode == 1) batch.Draw(background[backgroundMode], new Rectangle((int)boxTopLeft.X - 2, (int)boxTopLeft.Y - 2, (int)(source.Width * iconScale * Math.Ceiling(iconCount / (maxIconsPerRow * 1.0))) + 5,
                    (int)(((source.Width * iconScale) + (showPercentages ? 10 * barScale : 0)) * Math.Min(iconCount, maxIconsPerRow)) + 5), null, colorBg, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                else if (iconMode == 2) batch.Draw(background[backgroundMode], new Rectangle((int)boxTopLeft.X - 2, (int)boxTopLeft.Y - 2, (int)(boxWidth - boxTopLeft.X + 6), (int)boxHeight + 4),
                    null, colorBg, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }
        }
    }
}
