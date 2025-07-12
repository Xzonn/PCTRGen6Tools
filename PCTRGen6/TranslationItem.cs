namespace PCTRGen6;

internal class TranslationItem
{
    public int Index { get; set; }
    public string? Key { get; set; }
    public string? Original { get; set; }
    public string? Translation { get; set; }

    public bool? Trash { get; set; }
}
