namespace StardewMods
{
	internal class ModConfig
	{
		public int[] BarIconMode { get; set; } = new int[] { 2, 0, 0, 0 };
		public string Comment_BarIconMode { get; set; } = "Above BarIconMode values: 0= Horizontal Icons, 1= Vertical Icons, 2= Vertical Icons + Text, 3= Off. The arrays represent splitscreen screens, with first element being the default user.";
		public int[] BarTopLeftLocationX { get; set; } = { 20, 20, 20, 20 };
		public int[] BarTopLeftLocationY { get; set; } = { 20, 20, 20, 20 };
		public float[] BarScale { get; set; } = { 1.0f, 1.0f, 1.0f, 1.0f };
		public int[] BarMaxIcons { get; set; } = { 30, 30, 30, 30 };
		public int[] BarMaxIconsPerRow { get; set; } = { 10, 10, 10, 10 };
		public int[] BarBackgroundMode { get; set; } = { 0, 0, 0, 0 };
		public string Comment_BarBackgroundMode { get; set; } = "Above BarBackgroundMode values: 0= Circles (behind each icon), 1= Rectangle (behind everything), 2= Off";
        public int[] BarBackgroundColorRGBA { get; set; } = { 0, 0, 0, 128 };
        public int[] BarTextColorRGBA { get; set; } = { 255, 255, 255, 255 };
		public bool[] BarShowBaitAndTackleInfo { get; set; } = { true, true, true, true };
		public bool[] BarShowPercentages { get; set; } = { true, true, true, true };
		public int[] BarSortMode { get; set; } = { 0, 0, 0, 0 };
		public string Comment_BarSortMode { get; set; } = "Above BarSortMode values: 0= Sort Icons by Name (text mode only), 1= Sort icons by catch chance (Extra Check Frequency based), 2= Off";
		public int BarExtraCheckFrequency { get; set; } = 100;
		public int[] BarScanRadius { get; set; } = { 5, 5, 5, 5 };
		public bool[] BarCrabPotEnabled { get; set; } = { true, true, true, true };
		public bool[] UncaughtFishAreDark { get; set; } = { true, true, true, true };
		public bool[] OnlyFish { get; set; } = { false, false, false, false };
		public int[] MinigamePreviewMode { get; set; } = { 3, 3, 3, 3 };
		public string Comment_MinigamePreviewMode { get; set; } = "0= Copies multiple layers to look better. 1= Looks worse, but might perform/work better. 2= Only outlines it in the Bar (BarEnabled needed), 3= Off.";

	}
}
