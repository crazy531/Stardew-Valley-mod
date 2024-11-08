namespace Denifia.Stardew.BuyRecipes
{
    public sealed class ModConfig
    {
        public int maxNumberOfRecipesPerWeek { get; set; }
        public bool Pierre_store { get; set; } 
        public ModConfig()
        {
            this.maxNumberOfRecipesPerWeek = 5;
            this.Pierre_store = true;
        }
    }
}