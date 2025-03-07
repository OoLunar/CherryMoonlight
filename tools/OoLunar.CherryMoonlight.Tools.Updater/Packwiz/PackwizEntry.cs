namespace OoLunar.CherryMoonlight.Tools.Updater.Packwiz
{
    public sealed record PackwizEntry
    {
        public string Name { get; init; } = null!;
        public string Filename { get; init; } = null!;
        public PackwizUpdateSystem Update { get; init; } = null!;
        public string IndexFile { get; set; } = null!;
    }
}
