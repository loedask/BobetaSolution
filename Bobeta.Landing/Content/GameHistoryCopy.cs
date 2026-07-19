namespace Bobeta.Landing.Content;

/// <summary>
/// Short history blurbs for landing game modals.
/// Ethnographic citations live in docs/game-history-sources.md (not shown on the public site).
/// </summary>
public static class GameHistoryCopy
{
    public sealed record Entry(
        string GameId,
        string Title,
        string Lead,
        IReadOnlyList<string> Paragraphs,
        string? Note = null);

    public static Entry? For(string gameId, string locale)
    {
        var catalog = locale switch
        {
            "fr" => Fr,
            _ => En,
        };
        return catalog.TryGetValue(gameId, out var entry) ? entry : En.GetValueOrDefault(gameId);
    }

    private static readonly Dictionary<string, Entry> En = new(StringComparer.OrdinalIgnoreCase)
    {
        ["makopa"] = new(
            "makopa",
            "Makopa",
            "On Bobeta, Makopa is our 1v1 four-card trick-taking game.",
            [
                "We named the seat Makopa after a familiar East African card word. In Swahili, makopa means the suit of hearts.",
                "The trick-taking rules you play on Bobeta are a Bobeta design for short MoMo matches. They are not a reconstruction of one named village tradition under that title.",
                "In Congolese speech, players often call this kind of card play Congolese poker, and sometimes even kopo. On Bobeta we keep the product name Makopa so it stays distinct from our checkers seat.",
                "Central Africa has many popular card games with local names (for example Cameroon's Kos, related to Briscola-style play). Makopa sits in that wider culture of cards, without claiming a single ethnographic ruleset as ours.",
            ],
            "If we later rename or retie Makopa to a specific documented game, we will update this note."),

        ["kopo"] = new(
            "kopo",
            "Kopo",
            "Kopo is Bobeta's name for a 10×10 flying-kings checkers match.",
            [
                "The rules follow the international draughts family: large board, flying kings, and compulsory capture lines. That family is widely played in Francophone Central Africa, often simply called dames.",
                "In Congolese speech, kopo is a familiar name for that board game. Bobeta uses it for this 1v1 stakes version.",
            ]),

        ["ngola"] = new(
            "ngola",
            "Ngola",
            "Ngola belongs to the Central African mancala family: sow seeds, capture, out-count your opponent.",
            [
                "Ethnographers recorded Ngola as a four-row sowing game in the Republic of Congo (Klepzig, 1972). Related games such as Kisolo (also Chisolo / Cisolo) were documented among Luba, Lulua, and Songye communities in the Congo basin (Townshend, 1977).",
                "Counters were often tree seeds. Kisolo descriptions mention seeds from local trees sometimes called ngola, which is one reason the name travels with the game.",
                "Bobeta's Ngola is a simplified two-row, eight-pit 1v1 ruleset for fast matches. It keeps the sowing-and-capture idea, not every regional board size or capture nuance.",
            ]),

        ["domino"] = new(
            "domino",
            "Domino",
            "Dominoes are a worldwide tile game, and double-six tables are common across Central Africa.",
            [
                "Players match open ends, draw when stuck, and race to empty a hand. Bobeta uses a standard 1v1 double-six draw game (avec pioche).",
                "Unlike Ngola or Abbia, Domino is not a Bobeta invention or a single ethnic title. It is a shared popular pastime we ship with clear house rules for stakes.",
            ]),

        ["abbia"] = new(
            "abbia",
            "Abbia",
            "Abbia (also Abia, Abiè) was a Central African game of chance played with carved seed tokens.",
            [
                "It is best documented among Beti / Fang / Boulou communities of southern Cameroon, with related play noted historically in northern Gabon, Equatorial Guinea, and parts of Congo.",
                "Players used halved fruit seeds, polished and engraved on one face. Tokens went into a flat woven basket with plain calabash discs (sa). A mediator flipped the basket. Outcomes followed carved-side up or down patterns, closer to many two-faced dice than to cubic 1–6 dice.",
                "Stakes could be extreme in the historical record, which is one reason colonial authorities suppressed the game. Carved abbia tokens survive today mostly as art objects.",
                "Bobeta's Abbia is a simplified 1v1 version: each seat throws five tokens, higher carved-up count wins. It keeps the token-flip idea inside Bobeta's two-player stake model.",
            ]),

        ["nzengue"] = new(
            "nzengue",
            "Nzengué",
            "Nzengué (also Nzengué, Nzengue) is a Congo alignment game documented on a nine-point board: square corners, edge midpoints, center, and diagonals.",
            [
                "Ludii catalogs Small and Large move phases after placement. Klepzig (1972) describes related play in the Congo region.",
                "Each player starts with three stones. Seats alternate placing on empty points, then slide stones along lines to adjacent empty points.",
                "First player to make three in a row wins. Lines follow the board edges, midlines, and diagonals through the center.",
                "Bobeta's Nzengué is a simplified 1v1 version for MoMo stakes: the Small board topology with the Large move phase after placement.",
            ]),

        ["yote"] = new(
            "yote",
            "Yoté",
            "Yoté (also Yote) is a West African capture game played for stakes in Senegal, Mali, and Guinea. Sources include Wikipedia and GamesCrafters rule sets.",
            [
                "Each player starts with twelve pieces in hand on an empty 5×6 board. Turns alternate: place from hand, slide orthogonally, or jump-capture.",
                "A jump removes the jumped piece. If any opponent pieces remain on the board, the capturer removes one more of their choice.",
                "Captures are optional. No diagonal moves. One jump per turn.",
                "Win by leaving the opponent with no pieces in hand or on the board. If both sides have three or fewer pieces total, the match is a draw.",
                "Bobeta's Yoté follows these common house rules for 1v1 MoMo stakes.",
            ]),
    };

    private static readonly Dictionary<string, Entry> Fr = new(StringComparer.OrdinalIgnoreCase)
    {
        ["makopa"] = new(
            "makopa",
            "Makopa",
            "Sur Bobeta, Makopa est notre jeu de prises a deux, quatre cartes chacun.",
            [
                "Le nom Makopa reprend un mot de cartes d'Afrique de l'Est. En swahili, makopa designe la couleur des coeurs.",
                "Les regles de prises que vous jouez ici sont une creation Bobeta pour des matchs MoMo courts. Ce n'est pas la reconstitution d'une tradition villageoise unique sous ce titre.",
                "En parler congolais, on appelle souvent ce genre de jeu de cartes poker congolais, et parfois meme kopo. Sur Bobeta, le nom produit reste Makopa pour le distinguer de notre siege de dames.",
                "L'Afrique centrale connait beaucoup de jeux de cartes locaux (par exemple Kos au Cameroun, proche de la Briscola). Makopa s'inscrit dans cette culture des cartes, sans pretendre coller a une seule fiche ethnographique.",
            ],
            "Si nous rattacheons plus tard Makopa a un jeu documente precis, nous mettrons cette note a jour."),

        ["kopo"] = new(
            "kopo",
            "Kopo",
            "Kopo est le nom Bobeta pour un damier 10×10 a dames volantes.",
            [
                "Les regles suivent la famille des dames internationales: grand plateau, dames volantes, prises obligatoires. Cette famille est tres jouee en Afrique centrale francophone, souvent juste sous le nom de dames.",
                "En parler congolais, kopo est un nom familier pour ce jeu de plateau. Bobeta l'utilise pour cette version 1v1 misee.",
            ]),

        ["ngola"] = new(
            "ngola",
            "Ngola",
            "Ngola appartient a la famille des mankala d'Afrique centrale: semer, capturer, compter.",
            [
                "Les ethnographes ont note Ngola comme jeu de semis a quatre rangees en Republique du Congo (Klepzig, 1972). Des jeux proches comme Kisolo (aussi Chisolo / Cisolo) sont documentes chez les Luba, Lulua et Songye du bassin du Congo (Townshend, 1977).",
                "Les pions etaient souvent des graines d'arbres. Les descriptions de Kisolo citent parfois des graines d'arbres appeles ngola, d'ou le voyage du nom.",
                "Le Ngola Bobeta est une version simplifiee a deux rangees de huit cases pour des matchs rapides. On garde l'idee semer-capturer, pas chaque variante regionale.",
            ]),

        ["domino"] = new(
            "domino",
            "Domino",
            "Les dominos sont un jeu de tuiles mondial, et les tables double-six sont courantes en Afrique centrale.",
            [
                "On matche les extremites, on pioche si on est bloque, on vide sa main. Bobeta utilise un double-six a deux avec pioche.",
                "Contrairement a Ngola ou Abbia, Domino n'est ni une invention Bobeta ni un titre ethnique unique. C'est un passe-temps populaire avec des regles maison pour les enjeux.",
            ]),

        ["abbia"] = new(
            "abbia",
            "Abbia",
            "L'Abbia (aussi Abia, Abie) etait un jeu de hasard d'Afrique centrale joue avec des jetons de graines sculptees.",
            [
                "Il est surtout documente chez les Beti / Fang / Boulou du sud Cameroun, avec des pratiques proches notees historiquement au nord Gabon, en Guinee equatoriale et dans des zones du Congo.",
                "On utilisait des noyaux de fruits coupes en deux, polis et graves sur une face. Les jetons allaient dans un panier plat avec des disques de calebasse (sa). Un mediateur retournait le panier. Le resultat suivait face sculptee ou non, plus proche de des a deux faces que de cubes 1-6.",
                "Les enjeux pouvaient etre extremes dans les recits historiques, ce qui explique en partie les interdictions coloniales. Les jetons abbia survivent surtout comme objets d'art.",
                "L'Abbia Bobeta est une version 1v1 simplifiee: chaque joueur lance cinq jetons, le plus de faces sculptees gagne. On garde l'idee du lancer de jetons dans le modele a deux sieges de Bobeta.",
            ]),

        ["nzengue"] = new(
            "nzengue",
            "Nzengué",
            "Le Nzengué est un jeu d'alignement du Congo sur un plateau a neuf points: coins, milieux des cotes, centre et diagonales.",
            [
                "Ludii recense une phase de placement puis une phase de deplacement (Small / Large). Klepzig (1972) decrit des pratiques proches dans la region congolaise.",
                "Chaque joueur commence avec trois pierres. On place a tour de role sur des points vides, puis on fait glisser une pierre le long d'une ligne vers un point voisin libre.",
                "Le premier a aligner trois pierres gagne. Les lignes suivent les cotes, les medianes et les diagonales passant par le centre.",
                "Le Nzengué Bobeta est une version 1v1 simplifiee pour les enjeux MoMo: topologie Small avec phase de deplacement Large apres le placement.",
            ]),

        ["yote"] = new(
            "yote",
            "Yoté",
            "Le Yoté (aussi Yote) est un jeu de capture d'Afrique de l'Ouest, joue pour des enjeux au Senegal, au Mali et en Guinee. Sources: Wikipedia et GamesCrafters.",
            [
                "Chaque joueur commence avec douze pieces en main sur un plateau 5×6 vide. A tour de role: placer, glisser orthogonalement, ou sauter pour capturer.",
                "Un saut retire la piece sautee. S'il reste des pieces adverses sur le plateau, le capteur en retire une autre au choix.",
                "Les captures ne sont pas obligatoires. Pas de diagonales. Un seul saut par tour.",
                "Gagne celui qui laisse l'adversaire sans piece en main ni sur le plateau. Match nul si chaque camp a trois pieces ou moins au total.",
                "Le Yoté Bobeta suit ces regles maison courantes pour des enjeux MoMo 1v1.",
            ]),
    };
}
