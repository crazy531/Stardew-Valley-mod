﻿using System;
using System.Collections.Generic;
using System.Linq;
using Denifia.Stardew.BuyRecipes.Framework;
using StardewValley;
using StardewModdingAPI;
using StardewValley.ItemTypeDefinitions;
using System.Threading;

namespace Denifia.Stardew.BuyRecipes.Domain
{
    public class CookingRecipe 
    {
        public string Name { get; set; }
        
        public string DisplayName { get; set; }
        public List<GameItemWithQuantity> Ingredients { get; set; }
        public GameItemWithQuantity ResultingItem { get; set; }
        public IRecipeAquisitionConditions AquisitionConditions { get; set; }
        public bool IsKnown { get; set; }

        public CookingRecipe(string name, string data)
        {
            Name = name;

            var dataParts = data.Split('/');

            var ingredientsData = dataParts[0];
            Ingredients = DeserializeIngredients(ingredientsData);

            var unknownData = dataParts[1];

            //string resultingItemData = dataParts[2];
            try
            {
            string resultingItemData = ItemRegistry.QualifyItemId(dataParts[2]);

                if (resultingItemData != null || resultingItemData == "")
                {
                    ResultingItem = DeserializeResultingItem(resultingItemData);

                }
                else
                {

                    ResultingItem = DeserializeResultingItem(dataParts[2]);
                }



            }
            catch { }

            if (ResultingItem != null && ResultingItem.DisplayName != null)
            {
                DisplayName = ResultingItem.DisplayName;
            }
            else
            {
                DisplayName = name;
            }

            var aquisitionData = dataParts[3];
            var aquisitionConditions = BuyRecipes.RecipeAquisitionConditions.FirstOrDefault(x => x.AcceptsConditions(aquisitionData));
            if (aquisitionConditions == null)
            {
                AquisitionConditions = new DefaultRecipeAquisition(aquisitionData);
            }
            else
            {
                AquisitionConditions = (IRecipeAquisitionConditions)Activator.CreateInstance(aquisitionConditions.GetType(), new object[] { aquisitionData });
            }
        }

        private List<GameItemWithQuantity> DeserializeIngredients(string data)
        {
            var ingredients = new List<GameItemWithQuantity>();
            var dataParts = data.Split(' ');
            for (int i = 0; i < dataParts.Count(); i++)
            {
                try
                {
                    var ingredientData = DeserializeItemWithQuantity(dataParts[i], dataParts[i + 1]);
                    ingredients.Add(ingredientData);

                    i++; // Skip in pairs
                }
                catch
                {
                }
            }
            return ingredients;
        }

        private GameItemWithQuantity DeserializeResultingItem(string data)
        {
            var dataParts = data.Split(' ');
            if (dataParts.Count() == 1)
            {
                // Default amount of an item is 1
                return DeserializeItemWithQuantity(dataParts[0], "1");
            }
            return DeserializeItemWithQuantity(dataParts[0], dataParts[1]);
        }

        private GameItemWithQuantity DeserializeItemWithQuantity(string itemId, string quantity)
        {
            var itemWithQuantity = new GameItemWithQuantity
            {
                Id = itemId,
                Quantity = int.Parse(quantity),
            };

            // var gameItem = ModHelper.GameObjects.FirstOrDefault(x => x.Id == itemWithQuantity.Id);
            ParsedItemData gameItem = ItemRegistry.GetData(itemId);
            if (gameItem != null)
            {
                //itemWithQuantity.Name = gameItem.Name;
                itemWithQuantity.Name = gameItem.InternalName;
                itemWithQuantity.DisplayName = gameItem.DisplayName;
            }

            return itemWithQuantity;
        }
    }
}
