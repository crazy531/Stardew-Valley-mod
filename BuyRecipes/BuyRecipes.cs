using System;
using System.Collections.Generic;
using System.Linq;
using Denifia.Stardew.BuyRecipes.Domain;
using Denifia.Stardew.BuyRecipes.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Denifia.Stardew.BuyRecipes
{
    /// <summary>The mod entry class.</summary>
    public class BuyRecipes : Mod
    {
        private bool _savedGameLoaded = false;
        private List<CookingRecipe> _cookingRecipes;
        private List<CookingRecipe> _thisWeeksRecipes;
        private int _seed;
        private ModConfig Config = null; 
        public static List<IRecipeAquisitionConditions> RecipeAquisitionConditions;
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            RecipeAquisitionConditions = new List<IRecipeAquisitionConditions>()
            {
                new FriendBasedRecipeAquisition(),
                new SkillBasedRecipeAquisition(),
                new LevelBasedRecipeAquisition()
            };

            helper.ConsoleCommands
                .Add("buyrecipe", helper.Translation.Get("Command_1"), HandleCommand)
                .Add("showrecipes", helper.Translation.Get("Command_2"), HandleCommand);
            //.Add("buyallrecipes", $"Temporary. \n\nUsage: buyallrecipes", HandleCommand);


        }



        private void HandleCommand(string command, string[] args)
        {
            args = new List<string> { string.Join(" ", args) }.ToArray();

            if (!_savedGameLoaded)
            {
                Monitor.Log(Helper.Translation.Get("Need_load"), LogLevel.Warn);
                return;
            }

            switch (command)
            {
                case "buyrecipe":
                    BuyRecipe(args);
                    break;
                case "showrecipes":
                    ShowWeeklyRecipes();
                    break;
                case "buyallrecipes":
                    BuyAllRecipes();
                    break;
                default:
                    throw new NotImplementedException($"Send Items received unknown command '{command}'.");
            }
        }

        private void BuyAllRecipes()
        {
            foreach (var recipe in _cookingRecipes.Where(x => !x.IsKnown).ToList())
            {
                BuyRecipe(new string[] { recipe.Name }, false);
            }
        }

        private void BuyRecipe(string[] args, bool checkInWeeklyRecipes = true)
        {
            if (args.Length == 1)
            {
                var recipeName = args[0].Trim('"');

                //var recipe = _cookingRecipes.FirstOrDefault(x => x.Name.Equals(recipeName, StringComparison.OrdinalIgnoreCase));
                CookingRecipe recipe = _cookingRecipes.FirstOrDefault(x => x.DisplayName.Equals(recipeName, StringComparison.OrdinalIgnoreCase));
                if (recipe == null)
                {
                    Monitor.Log(Helper.Translation.Get("Purchase_status1",new { recipe = recipeName }), LogLevel.Info);
                    return;
                }

                // Use the explicit name
                recipeName = recipe.Name;

                if (recipe.IsKnown || Game1.player.cookingRecipes.ContainsKey(recipeName))
                {
                    recipe.IsKnown = true;
                    Monitor.Log(Helper.Translation.Get("Purchase_status2"), LogLevel.Info);
                    return;
                }

                if (checkInWeeklyRecipes && !_thisWeeksRecipes.Any(x => x.Name.Equals(recipeName)))
                {
                    Monitor.Log(Helper.Translation.Get("Purchase_status3"), LogLevel.Info);
                    return;
                }

                if (Game1.player.Money < recipe.AquisitionConditions.Cost)
                {
                    Monitor.Log(Helper.Translation.Get("Purchase_status4"), LogLevel.Info);
                    return;
                }

                Game1.player.cookingRecipes.Add(recipeName, 0);
                Game1.player.Money -= recipe.AquisitionConditions.Cost;
                _thisWeeksRecipes.Remove(recipe);
                //Monitor.Log($"{recipeName} bought for {ModHelper.GetMoneyAsString(recipe.AquisitionConditions.Cost)}!", LogLevel.Alert);
                Monitor.Log(Helper.Translation.Get("Purchase_status5",new { recipe = recipe.DisplayName,cost = GetMoneyAsString(recipe.AquisitionConditions.Cost) }), LogLevel.Alert);
            }
            else
            {
                LogArgumentsInvalid("buy");
            }
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            _savedGameLoaded = false;
            _cookingRecipes = null;
            _thisWeeksRecipes = null;
            _seed = -1;
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            _savedGameLoaded = true;
            DiscoverRecipes();
            GenerateWeeklyRecipes();
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            GenerateWeeklyRecipes();

        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
            mod: this.ModManifest,
            reset: () => this.Config = new ModConfig(),
            save: () => OnConfigUpdate()
            );
            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => Helper.Translation.Get("maxNumberOfRecipesPerWeek.name"),
            tooltip: () => Helper.Translation.Get("maxNumberOfRecipesPerWeek.description"),
            getValue: () => Config.maxNumberOfRecipesPerWeek,
            setValue: value => Config.maxNumberOfRecipesPerWeek = value,
            min: 1,
            max: 10
            );
            configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => Helper.Translation.Get("Pierre_store.name"),
            tooltip: () => Helper.Translation.Get("Pierre_store.description"),
            getValue: () => Config.Pierre_store,
            setValue: value => Config.Pierre_store = value,
            fieldId: "Pierre_store"

            );
/*            configMenu.OnFieldChanged(
            mod: this.ModManifest,
            OnConfigUpdate

            );*/



        }

        private void OnConfigUpdate()
        {
            this.Helper.WriteConfig(this.Config);
            if (Config.Pierre_store)
            {
                Helper.Events.Display.MenuChanged += OnMenuChanged;

                Monitor.Log(Helper.Translation.Get("Pierre_store_status1"), LogLevel.Info);






            }
            else {

                Helper.Events.Display.MenuChanged -= OnMenuChanged;
                Monitor.Log(Helper.Translation.Get("Pierre_store_status2"), LogLevel.Info);

            }




        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is not  ShopMenu && e.OldMenu is not ShopMenu )
            {

                return;

            }

            ShopMenu shopMenu;
            if (e.OldMenu is ShopMenu) {

                 shopMenu = e.OldMenu as ShopMenu;
                if (shopMenu.ShopId == "SeedShop")
                {

                    UpdateRecipes();
                }
                return;
            }
             shopMenu = e.NewMenu as ShopMenu;
            if (shopMenu.ShopId == "SeedShop")
            {

                foreach (var a in  _thisWeeksRecipes) {

                   Item b = ItemRegistry.Create(a.ResultingItem.Id);
                    b.IsRecipe = true;

                    shopMenu.AddForSale(b);

                }

            }

        }

        private void UpdateRecipes()
        {
            for (int i = _thisWeeksRecipes.Count -1; i >= 0; i--) {

                if (Game1.player.cookingRecipes.ContainsKey(_thisWeeksRecipes[i].Name))
                {
                    Monitor.Log(Helper.Translation.Get("Purchase_status6", new { recipe = _thisWeeksRecipes[i].DisplayName }), LogLevel.Info);
                    _cookingRecipes.Remove(_thisWeeksRecipes[i]);
                    _thisWeeksRecipes.RemoveAt(i);


                }

            }
         

        }

        private void GenerateWeeklyRecipes()
        {
            var gameDateTime = new GameDateTime(Game1.timeOfDay, Game1.dayOfMonth, Game1.currentSeason, Game1.year);
            var startDayOfWeek = (((gameDateTime.DayOfMonth / 7) + 1) * 7) - 6;
            var seed = int.Parse($"{startDayOfWeek}{gameDateTime.Season}{gameDateTime.Year}");
            var random = new Random(seed);



            //       Game1.addHUDMessage(Message);

            if (_seed == seed) return;
            _seed = seed;

            _thisWeeksRecipes = new List<CookingRecipe>();
            //var maxNumberOfRecipesPerWeek = 5;
            var unknownRecipes = _cookingRecipes.Where(x => !x.IsKnown).ToList();
            var unknownRecipesCount = unknownRecipes.Count;

            if (unknownRecipesCount == 0)
            {
                ShowNoRecipes();
                return;
            }

            for (int i = 0; i < Config.maxNumberOfRecipesPerWeek; i++)
            {
                var recipe = unknownRecipes[random.Next(unknownRecipesCount)];

                if (!_thisWeeksRecipes.Any(x => x.Name.Equals(recipe.Name)))
                {
                    _thisWeeksRecipes.Add(recipe);
                }
            }

            ShowWeeklyRecipes();
            if (Config.Pierre_store) Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("Recipe_status1"), 0)
            {
                noIcon = true,
                timeLeft = 5250f
            });
        }

        private void ShowNoRecipes()
        {
            Monitor.Log(Helper.Translation.Get("Purchase_status7"), LogLevel.Info);
        }

        private void ShowWeeklyRecipes()
        {
            if (_thisWeeksRecipes.Count == 0)
            {
                ShowNoRecipes();
                return;
            }

            Monitor.Log(Helper.Translation.Get("Purchase_status8"), LogLevel.Alert);
            foreach (var item in _thisWeeksRecipes)
            {
                Monitor.Log($"{GetMoneyAsString(item.AquisitionConditions.Cost)} - {item.DisplayName}", LogLevel.Info);
            }
            Monitor.Log(Helper.Translation.Get("Command_1"), LogLevel.Info);


        }

        private void DiscoverRecipes()
        {
            _cookingRecipes = new List<CookingRecipe>();
            foreach (var recipe in CraftingRecipe.cookingRecipes)
            {
                try
                {
                    CookingRecipe cookingRecipe = new CookingRecipe(recipe.Key, recipe.Value);
                    if (Game1.player.knowsRecipe(cookingRecipe.Name))
                    {
                        cookingRecipe.IsKnown = true;
                    }
                    _cookingRecipes.Add(cookingRecipe);
                }
                catch {

                    Monitor.Log(Helper.Translation.Get("Purchase_status9",new { recipe = recipe.Key }), LogLevel.Info);
                    continue;

                }




             
            }
        }

 

        private void LogUsageError(string error, string command)
        {
            Monitor.Log($"{error} Type 'help {command}' for usage.", LogLevel.Error);
        }

        private void LogArgumentsInvalid(string command)
        {
            LogUsageError("The arguments are invalid.", command);
        }
        private static string GetMoneyAsString(int money)
        {
            return $"G{money.ToString("#,##0")}";
        }
    }
}
