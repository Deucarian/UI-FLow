namespace Deucarian.UIFlow.Samples.SimpleUsage
{
    [ScreenKeySet]
    public static class ScreenKeys
    {
        public static ScreenKey Settings => new Definition();
        private sealed class Definition : ScreenKey
        {
            public Definition() : base("settings") { }
        }
    }
}
