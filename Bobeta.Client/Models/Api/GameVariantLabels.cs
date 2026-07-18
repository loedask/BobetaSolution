namespace Bobeta.Client.Models.Api;

public static class GameVariantLabels
{
    public static string Name(GameVariant variant) => variant switch
    {
        GameVariant.Kopo => "Kopo",
        GameVariant.Ngola => "Ngola",
        GameVariant.Domino => "Domino",
        _ => "Makopa"
    };
}
