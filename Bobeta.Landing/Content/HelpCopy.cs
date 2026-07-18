namespace Bobeta.Landing.Content;

/// <summary>FAQ and legal copy for the marketing site. Kept here so product i18n stays lean.</summary>
public static class HelpCopy
{
    public sealed record FaqItem(string Question, string Answer);
    public sealed record LegalSection(string Title, string Body);
    public sealed record LegalDoc(string Title, string Intro, IReadOnlyList<LegalSection> Sections);

    public static IReadOnlyList<FaqItem> Faq(string locale) => locale switch
    {
        "fr" => FaqFr,
        "kt" => FaqKt,
        "ln" => FaqLn,
        "sw" => FaqSw,
        _ => FaqEn,
    };

    public static LegalDoc Terms(string locale) => Doc(locale, TermsEn, TermsFr, TermsKt, TermsLn, TermsSw);
    public static LegalDoc Privacy(string locale) => Doc(locale, PrivacyEn, PrivacyFr, PrivacyKt, PrivacyLn, PrivacySw);
    public static LegalDoc Responsible(string locale) => Doc(locale, ResponsibleEn, ResponsibleFr, ResponsibleKt, ResponsibleLn, ResponsibleSw);
    public static LegalDoc Rules(string locale) => Doc(locale, RulesEn, RulesFr, RulesKt, RulesLn, RulesSw);
    public static LegalDoc WhatIs(string locale) => Doc(locale, WhatIsEn, WhatIsFr, WhatIsKt, WhatIsLn, WhatIsSw);

    private static LegalDoc Doc(string locale, LegalDoc en, LegalDoc fr, LegalDoc kt, LegalDoc ln, LegalDoc sw) =>
        locale switch
        {
            "fr" => fr,
            "kt" => kt,
            "ln" => ln,
            "sw" => sw,
            _ => en,
        };

    private static readonly FaqItem[] FaqEn =
    [
        new("What is Bobeta?",
            "A skill-gaming platform with a short list of games and fast 1v1 matches for real MoMo stakes. Read the full pitch on the What is Bobeta page."),
        new("How do I create an account?",
            "Open Play now, enter your MoMo number, and confirm the OTP we send. That number becomes your Bobeta login."),
        new("How do deposits and withdrawals work?",
            "Add money from your MoMo wallet, then withdraw winnings the same way. Keep enough balance on MoMo before you deposit or cash out."),
        new("What games can I play?",
            "Makopa, Kopo, Ngola, and Domino. Each match is one-vs-one skill play for a stake you choose."),
        new("What happens to the pot when I win?",
            "Both players' stakes form the pot. Bobeta takes a platform fee. The rest goes to the winner's wallet."),
        new("Is the game fair?",
            "Yes. Deals and first turns use randomization built into the game engines. Outcomes come from play, not hidden odds."),
        new("How do I get help?",
            "Start with this FAQ and the Game rules page. For account or payment issues, reach support through the channels published on Bobeta."),
    ];

    private static readonly FaqItem[] FaqFr =
    [
        new("Qu'est-ce que Bobeta ?",
            "Une plateforme de jeux d'adresse avec une courte liste de jeux et des duels rapides à enjeux MoMo. Lisez la page Qu'est-ce que Bobeta pour le détail."),
        new("Comment créer un compte ?",
            "Appuyez sur Jouer maintenant, entrez votre numéro MoMo, puis validez le code OTP. Ce numéro devient votre connexion Bobeta."),
        new("Comment marchent les dépôts et retraits ?",
            "Rechargez depuis votre portefeuille MoMo, puis retirez vos gains de la même façon. Gardez un solde MoMo suffisant avant de déposer ou d'encaisser."),
        new("Quels jeux puis-je jouer ?",
            "Makopa, Kopo, Ngola et Domino. Chaque partie est un duel d'adresse avec une mise que vous choisissez."),
        new("Que devient le pot si je gagne ?",
            "Les mises des deux joueurs forment le pot. Bobeta prélève des frais de plateforme. Le reste va sur le portefeuille du gagnant."),
        new("Le jeu est-il équitable ?",
            "Oui. Les distributions et le premier tour utilisent le tirage au sort des moteurs de jeu. Le résultat vient du jeu, pas d'une cote cachée."),
        new("Comment obtenir de l'aide ?",
            "Commencez par cette FAQ et la page Règles des jeux. Pour un compte ou un paiement, contactez le support via les canaux indiqués sur Bobeta."),
    ];

    private static readonly FaqItem[] FaqKt =
    [
        new("Bobeta kele nini?",
            "Plateforme ya ba jeux ya adresse ti liste ya kukusa mpi ba duels ya nswalu na ba enjeux MoMo. Tala page Bobeta kele nini? sambu na mambu mingi."),
        new("Mutindu ya kusala compte?",
            "Fina Jouer maintenant, kota numéro MoMo na nge, mpi confirma OTP. Numéro yina kele login na nge na Bobeta."),
        new("Mutindu dépôt mpi retrait ke salama?",
            "Yika mbongo kutuka na MoMo, mpi katula ba gains na mutindu mosi. Bika solde ya MoMo yina me kuka."),
        new("Ba jeux nini nge lenda sakana?",
            "Makopa, Kopo, Ngola mpi Domino. Ketesani kele duel ya adresse ti mise nge me solula."),
        new("Soki nge me longa, pot ke kwenda wapi?",
            "Ba mises zole ke sala pot. Bobeta ke baka frais ya plateforme. Ya ntama ke kwenda na wallet ya nani me longa."),
        new("Jeu kele ya botondi?",
            "Ee. Ba cartes mpi nani ke bandisa ke solamaka na chance ya moteur. Résultat kele ya kusakana, ve ya cote ya kubumba."),
        new("Mutindu ya kubaka lusadisu?",
            "Tala FAQ yai mpi page ya ba règles. Sambu na compte to paiement, boka support na ba canaux ya Bobeta."),
    ];

    private static readonly FaqItem[] FaqLn =
    [
        new("Bobeta ezali nini?",
            "Plateforme ya ba jeux ya adresse na liste mokuse mpe ba duels ya mbangu na ba enjeux MoMo. Tala lokasa Bobeta ezali nini? mpo na makambo mingi."),
        new("Ndenge nini nakoki kosala compte?",
            "Finá Jouer maintenant, kotia nimero MoMo na yo, mpe valida OTP. Nimero wana ekoma login na yo na Bobeta."),
        new("Dépôt mpe retrait esalaka ndenge nini?",
            "Bakisa mbongo utángá na MoMo, mpe bímisa ba gains na ndenge moko. Tika solde ya MoMo ekoka."),
        new("Ba jeux nini nakoki kosakana?",
            "Makopa, Kopo, Ngola mpe Domino. Mokano mokomoko ezali duel ya adresse na mise oponi."),
        new("Soki nalóngi, pot ekendaka wapi?",
            "Ba mises mibale esalá pot. Bobeta ezwi frais ya plateforme. Oyo etikali ekenda na wallet ya oyo elongi."),
        new("Mosala ezali ya bosembo?",
            "Ee. Ba cartes mpe nani abandeli epesamaka na chance ya moteur. Résultat eutí na mosala, te na cote ya kobomba."),
        new("Ndenge nini nazwa lisalisi?",
            "Bandá na FAQ oyo mpe lokasa ya ba règles. Mpo na compte to paiement, benga support na ba canaux ya Bobeta."),
    ];

    private static readonly FaqItem[] FaqSw =
    [
        new("Bobeta ni nini?",
            "Jukwaa la michezo ya ustadi lenye orodha fupi na mechi za haraka za dau za MoMo. Soma ukurasa Bobeta ni nini? kwa maelezo kamili."),
        new("Ninawezaje kufungua akaunti?",
            "Gusa Play now, weka nambari yako ya MoMo, kisha thibitisha OTP. Nambari hiyo inakuwa login yako ya Bobeta."),
        new("Amana na uondoaji hufanyaje kazi?",
            "Ongeza pesa kutoka MoMo, kisha toa ushindi kwa njia hiyo hiyo. Hakikisha MoMo ina salio la kutosha."),
        new("Naweza kucheza michezo gani?",
            "Makopa, Kopo, Ngola, na Domino. Kila mechi ni pambano la ustadi la mtu mmoja dhidi ya mwingine kwa dau unalochagua."),
        new("Poti inaenda wapi nikishinda?",
            "Dau za wachezaji wote mbili huunda poti. Bobeta inachukua ada ya jukwaa. Salio huenda kwenye pochi ya mshindi."),
        new("Je mchezo ni wa haki?",
            "Ndiyo. Ugawaji na zamu ya kwanza hutumia nasibu ya injini. Matokeo yanatokana na uchezaji, si odds zilizofichwa."),
        new("Ninawezaje kupata msaada?",
            "Anza na FAQ hii na ukurasa wa sheria za michezo. Kwa akaunti au malipo, wasiliana na support kupitia njia zilizoonyeshwa kwenye Bobeta."),
    ];

    private static readonly LegalDoc TermsEn = new(
        "Terms of use",
        "These terms cover how you use Bobeta: skill games for real stakes paid through mobile money.",
        [
            new("Agreement",
                "By creating an account or playing on Bobeta, you accept these terms, our privacy policy, responsible play guidelines, and game rules. If you do not agree, do not use the service."),
            new("Who can play",
                "You must be 18 or older and able to use mobile money legally where you play. One person, one account. Do not share your OTP or account access."),
            new("Your account",
                "Sign-in uses your MoMo phone number and a one-time code. Keep your number and device secure. Tell us if you lose access so we can help lock the account."),
            new("Money in and out",
                "Deposits and withdrawals go through supported MoMo channels. Amounts, limits, and timing can depend on your operator. Only use money you can afford to stake."),
            new("Matches and fees",
                "When you join a match, your stake is locked. The pot is both stakes combined. Bobeta takes a platform fee from the pot. The winner receives the rest in their Bobeta wallet."),
            new("Fair play",
                "Cheating, multi-accounting, abuse of bugs, or harassment can lead to stake voids, wallet holds, or account closure. We may pause play while we review a dispute."),
            new("Changes",
                "We may update these terms as Bobeta grows. The latest version always sits on this page. Keeping your account after a change means you accept the update."),
        ]);

    private static readonly LegalDoc TermsFr = new(
        "Conditions générales",
        "Ces conditions couvrent l'usage de Bobeta : jeux d'adresse à enjeux réels, payés via mobile money.",
        [
            new("Accord",
                "En créant un compte ou en jouant sur Bobeta, vous acceptez ces conditions, la politique de confidentialité, les règles de jeu responsable et les règles des jeux. Sinon, n'utilisez pas le service."),
            new("Qui peut jouer",
                "Vous devez avoir 18 ans ou plus et pouvoir utiliser le mobile money légalement là où vous jouez. Une personne, un compte. Ne partagez ni OTP ni accès."),
            new("Votre compte",
                "La connexion utilise votre numéro MoMo et un code à usage unique. Protégez votre numéro et votre appareil. Prévenez-nous en cas de perte d'accès."),
            new("Argent entrant et sortant",
                "Dépôts et retraits passent par les canaux MoMo supportés. Montants, plafonds et délais dépendent de votre opérateur. Ne misez que de l'argent que vous pouvez vous permettre."),
            new("Parties et frais",
                "Quand vous rejoignez une partie, votre mise est bloquée. Le pot = les deux mises. Bobeta prélève des frais de plateforme sur le pot. Le gagnant reçoit le reste sur son portefeuille Bobeta."),
            new("Jeu loyal",
                "Triche, multi-comptes, abus de bugs ou harcèlement peuvent entraîner annulation de mise, blocage du portefeuille ou fermeture du compte. Nous pouvons suspendre une partie le temps d'un contrôle."),
            new("Modifications",
                "Nous pouvons mettre à jour ces conditions. La version en vigueur est toujours sur cette page. Continuer à utiliser Bobeta vaut acceptation."),
        ]);

    private static readonly LegalDoc TermsKt = new(
        "Ba conditions générales",
        "Ba conditions yai ke landa mutindu ya kusadila Bobeta: ba jeux ya adresse ti ba enjeux ya kyeleka na MoMo.",
        [
            new("Lukanu",
                "Soki nge me sala compte to me sakana na Bobeta, nge me ndima ba conditions yai, politique ya confidentialité, jeu responsable mpi ba règles. Soki ve, sadila service ve."),
            new("Nani lenda sakana",
                "Nge fweti zabaka bamvula 18 to kuluta, mpi sadila MoMo na nsiku. Muntu mosi, compte mosi. Kabula OTP ve."),
            new("Compte na nge",
                "Login ke sadila numéro MoMo mpi code OTP. Bika téléphone na nge ya kyeleka. Soki nge me vidisa accès, yikisa biso."),
            new("Mbongo ya kotisa mpi ya katula",
                "Dépôt mpi retrait ke salama na MoMo. Ba limites ke landa opérateur. Sadila mbongo nge lenda fwaka."),
            new("Ba parties mpi ba frais",
                "Soki nge me kota partie, mise ke zibama. Pot = ba mises zole. Bobeta ke baka frais ya plateforme na pot. Nani me longa ke bakaka ya ntama na wallet."),
            new("Sakana ya botondi",
                "Triche, ba comptes mingi to kuzanga bantu lenda kanga compte. Biso lenda zibisa partie ntangu ya kusosa."),
            new("Ba changements",
                "Biso lenda soba ba conditions. Version ya nsuka kele na page yai. Kusadila Bobeta na nima = kundima."),
        ]);

    private static readonly LegalDoc TermsLn = new(
        "Ba conditions générales",
        "Ba conditions oyo elandaka ndenge ya kosalela Bobeta: ba jeux ya adresse na ba enjeux ya solo na MoMo.",
        [
            new("Boyokani",
                "Soki osali compte to osakani na Bobeta, ondimi ba conditions oyo, politique ya confidentialité, jeu responsable mpe ba règles. Soki te, kosalela service te."),
            new("Nani akoki kosakana",
                "Ofuti kozala na mbula 18 to koleka, mpe kosalela MoMo na mobeko. Moto moko, compte moko. Kákabola OTP te."),
            new("Compte na yo",
                "Login esaleli nimero MoMo mpe code OTP. Batela téléphone na yo. Soki opoti accès, yebisa biso."),
            new("Mbongo ya kotisa mpe kobimisa",
                "Dépôt mpe retrait esalama na MoMo. Ba limites elandaka opérateur. Salela mbongo okoki kobungisa."),
            new("Ba parties mpe ba frais",
                "Soki okoti partie, mise ezibani. Pot = ba mises mibale. Bobeta ezwi frais ya plateforme na pot. Oyo elongi azwi oyo etikali na wallet."),
            new("Mosala ya bosembo",
                "Triche, ba comptes ebele to kobunga bato ekoki koziba compte. Tokoki koziba partie ntango ya bolukiluki."),
            new("Ba changements",
                "Tokoki kobongola ba conditions. Version ya sika ezali na lokasa oyo. Kosalela Bobeta nsima = kondima."),
        ]);

    private static readonly LegalDoc TermsSw = new(
        "Masharti ya matumizi",
        "Masharti haya yanaeleza matumizi ya Bobeta: michezo ya ustadi yenye dau halisi kupitia mobile money.",
        [
            new("Makubaliano",
                "Kwa kufungua akaunti au kucheza Bobeta, unakubali masharti haya, sera ya faragha, mchezo unaowajibika, na sheria za michezo. Usipokubali, usitumie huduma."),
            new("Nani anaweza kucheza",
                "Lazima uwe na umri wa miaka 18 au zaidi na uweze kutumia MoMo kisheria. Mtu mmoja, akaunti moja. Usishiriki OTP."),
            new("Akaunti yako",
                "Ingia kwa nambari ya MoMo na OTP. Linda simu yako. Tupigie ikiwa umepoteza ufikiaji."),
            new("Kuweka na kutoa pesa",
                "Amana na uondoaji hupitia MoMo. Vipimo hutegemea mtandao wako. Tumia tu pesa unazoweza kuhatarisha."),
            new("Mechi na ada",
                "Unapoingia mechi, dau lako linafungwa. Poti = dau zote mbili. Bobeta inachukua ada ya jukwaa kutoka poti. Mshindi anapata salio kwenye pochi."),
            new("Uchezaji wa haki",
                "Udanganyifu, akaunti nyingi, au unyanyasaji vinaweza kufunga akaunti. Tunaweza kusimamisha mechi wakati wa uchunguzi."),
            new("Mabadiliko",
                "Tunaweza kusasisha masharti. Toleo jipya liko kwenye ukurasa huu. Kuendelea kutumia Bobeta ni kukubali."),
        ]);

    private static readonly LegalDoc PrivacyEn = new(
        "Privacy policy",
        "We collect only what we need to run accounts, matches, and MoMo payouts.",
        [
            new("What we collect",
                "Phone number used for login, player display name, game history, wallet transactions, basic device and app diagnostics, and messages you send to support."),
            new("Why we collect it",
                "To authenticate you, run matches, settle pots, prevent fraud, improve the product, and meet legal duties where they apply."),
            new("Payments",
                "MoMo deposits and withdrawals are processed with your mobile money provider. We do not store your MoMo PIN."),
            new("Sharing",
                "We share data with payment partners and infrastructure providers only as needed to operate Bobeta. We do not sell your personal data."),
            new("Retention and security",
                "We keep account and transaction records as long as your account stays open and as required for dispute or legal needs. Access is limited and protected with standard security controls."),
            new("Your choices",
                "You can update your player name in the app and ask support about access, correction, or account closure."),
        ]);

    private static readonly LegalDoc PrivacyFr = new(
        "Politique de confidentialité",
        "Nous collectons seulement ce qu'il faut pour les comptes, les parties et les paiements MoMo.",
        [
            new("Ce que nous collectons",
                "Numéro de téléphone pour la connexion, nom d'affichage, historique de jeu, transactions du portefeuille, diagnostics basiques, et messages envoyés au support."),
            new("Pourquoi",
                "Pour vous authentifier, lancer les parties, régler les pots, limiter la fraude, améliorer le produit et respecter les obligations légales applicables."),
            new("Paiements",
                "Dépôts et retraits MoMo passent par votre opérateur. Nous ne stockons pas votre code PIN MoMo."),
            new("Partage",
                "Nous partageons des données avec partenaires de paiement et hébergeurs uniquement pour faire tourner Bobeta. Nous ne vendons pas vos données personnelles."),
            new("Conservation et sécurité",
                "Nous gardons les comptes et transactions tant que le compte existe et selon les besoins de litige ou de loi. L'accès est limité et protégé."),
            new("Vos choix",
                "Vous pouvez changer votre nom de joueur dans l'app et contacter le support pour accès, correction ou fermeture de compte."),
        ]);

    private static readonly LegalDoc PrivacyKt = new(
        "Politique ya confidentialité",
        "Biso ke balula kaka biloko ya ntina sambu na compte, ba parties mpi MoMo.",
        [
            new("Nini biso ke balula",
                "Numéro ya téléphone, kombo ya joueur, historique ya jeu, ba transactions ya wallet, diagnostics ya fioti, mpi ba messages na support."),
            new("Sambu nini",
                "Sambu na login, ba parties, kufuta pot, kubuya triche, kusoba produit mpi nsiku."),
            new("Ba paiements",
                "Dépôt mpi retrait ke salama na opérateur MoMo. Biso ke bumba PIN ya MoMo ve."),
            new("Kubakisa ba banques",
                "Biso ke kabula ba données kaka na ba partenaires ya ntina. Biso ke teka ba données na nge ve."),
            new("Kubumba mpi sécurité",
                "Biso ke bumba ba comptes ntangu compte kele ouvert mpi ntangu nsiku ke lomba. Accès kele ya kufunga."),
            new("Ba choix na nge",
                "Nge lenda soba kombo na app mpi yikisa support sambu na fermeture ya compte."),
        ]);

    private static readonly LegalDoc PrivacyLn = new(
        "Politique ya confidentialité",
        "Tobimisi kaka makambo ya ntina mpo na compte, ba parties mpe MoMo.",
        [
            new("Nini tobimisi",
                "Nimero ya téléphone, nkombo ya joueur, historique ya jeu, ba transactions ya wallet, diagnostics moke, mpe ba messages na support."),
            new("Mpo nini",
                "Mpo na login, ba parties, kofuta pot, koboya triche, kobongisa produit mpe mobeko."),
            new("Ba paiements",
                "Dépôt mpe retrait esalama na opérateur MoMo. Tobombaka PIN ya MoMo te."),
            new("Kokabola",
                "Tokabolaka ba données kaka na ba partenaires ya mosala. Totekaka ba données na yo te."),
            new("Kobomba mpe sécurité",
                "Tobombaka ba comptes ntango compte ezali mpe ntango mobeko elingi. Accès ezali ya kobatela."),
            new("Ba choix na yo",
                "Okoki kobongola nkombo na app mpe koyebisa support mpo na kokanga compte."),
        ]);

    private static readonly LegalDoc PrivacySw = new(
        "Sera ya faragha",
        "Tunakusanya tu yanayohitajika kwa akaunti, mechi, na malipo ya MoMo.",
        [
            new("Tunachokusanya",
                "Nambari ya simu, jina la mchezaji, historia ya michezo, miamala ya pochi, uchunguzi wa kawaida, na ujumbe kwa support."),
            new("Kwa nini",
                "Kuthibitisha utambulisho, kuendesha mechi, kulipa poti, kuzuia udanganyifu, kuboresha huduma, na kutii sheria."),
            new("Malipo",
                "Amana na uondoaji hupitia mtandao wa MoMo. Hatuwehifadhi PIN yako ya MoMo."),
            new("Kushiriki data",
                "Tunashiriki data na washirika wa malipo na miundombinu inavyohitajika tu. Hatuzzi data yako ya kibinafsi."),
            new("Uhifadhi na usalama",
                "Tunaweka rekodi za akaunti na miamala wakati akaunti ipo na inavyohitajika kisheria. Ufikiaji ni mdogo na salama."),
            new("Chaguo zako",
                "Unaweza kubadilisha jina la mchezaji kwenye app na kuomba support kuhusu kufunga akaunti."),
        ]);

    private static readonly LegalDoc ResponsibleEn = new(
        "Responsible play",
        "Bobeta is skill gaming with real stakes. Stay in control.",
        [
            new("Play within your means",
                "Only stake money you can afford to lose. A match should stay fun, not stressful."),
            new("Take breaks",
                "Step away after wins or losses. Chasing money after a bad run usually makes things worse."),
            new("Age limit",
                "Bobeta is for adults 18+. Do not let minors use your phone or MoMo account to play."),
            new("Warning signs",
                "If you hide play from family, borrow to stake, or feel you cannot stop, pause and get help from people you trust or local support services."),
            new("Account controls",
                "You can stop playing anytime. Ask support if you need help limiting or closing your account."),
        ]);

    private static readonly LegalDoc ResponsibleFr = new(
        "Jeu responsable",
        "Bobeta, c'est du jeu d'adresse avec de vrais enjeux. Gardez le contrôle.",
        [
            new("Jouez selon vos moyens",
                "Ne misez que ce que vous pouvez perdre. Une partie doit rester un plaisir, pas une pression."),
            new("Faites des pauses",
                "Arrêtez-vous après une série de gains ou de pertes. Courir après l'argent aggrave souvent la situation."),
            new("Âge minimum",
                "Bobeta est réservé aux adultes de 18 ans et plus. Ne laissez pas un mineur jouer avec votre téléphone ou votre MoMo."),
            new("Signes d'alerte",
                "Si vous cachez votre jeu, empruntez pour miser, ou n'arrivez plus à vous arrêter, faites une pause et demandez de l'aide."),
            new("Contrôles du compte",
                "Vous pouvez arrêter quand vous voulez. Contactez le support pour limiter ou fermer votre compte."),
        ]);

    private static readonly LegalDoc ResponsibleKt = new(
        "Jeu responsable",
        "Bobeta kele jeu ya adresse ti ba enjeux ya kyeleka. Bika nge muntu ya kusolula.",
        [
            new("Sakana na mbongo nge kele na yo",
                "Misa kaka mbongo nge lenda fwaka. Partie fweti bikala ya kiese, ve ya mpasi."),
            new("Baka ba pauses",
                "Yimisa nima ya ba longues to ba pertes. Kulanda mbongo mbala mingi ke yika mpasi."),
            new("Bamvula",
                "Bobeta sambu na bantu ya bamvula 18+. Bika bana ve sakana na téléphone to MoMo na nge."),
            new("Ba signes",
                "Soki nge ke bumba jeu, ke kukumina mbongo to ke kuka ve kuyimisa, yimisa mpi yikisa lusadisu."),
            new("Compte",
                "Nge lenda yimisa ntangu yonso. Yikisa support sambu na kufunga compte."),
        ]);

    private static readonly LegalDoc ResponsibleLn = new(
        "Jeu responsable",
        "Bobeta ezali mosala ya adresse na ba enjeux ya solo. Bika yo moko okamba.",
        [
            new("Sakana na mbongo ozali na yango",
                "Misa kaka mbongo okoki kobungisa. Partie esengeli kozala esengo, te mpasi."),
            new("Zwa ba pauses",
                "Tika nsima ya ba elonga to ba kobunga. Kolanda mbongo mbala mingi ebakisaka mpasi."),
            new("Mbula",
                "Bobeta ezali mpo na bato ya mbula 18+. Tika bana te basakana na téléphone to MoMo na yo."),
            new("Ba signes",
                "Soki obombaka mosala, okózwa mbongo to okoki te kotika, tika mpe zwa lisalisi."),
            new("Compte",
                "Okoki kotika ntango nyonso. Yebisa support mpo na kokanga compte."),
        ]);

    private static readonly LegalDoc ResponsibleSw = new(
        "Mchezo unaowajibika",
        "Bobeta ni mchezo wa ustadi wenye dau halisi. Kaa na udhibiti.",
        [
            new("Cheza kwa uwezo wako",
                "Weka tu dau unaloweza kupoteza. Mechi iwe furaha, si msongo."),
            new("Pumzika",
                "Simama baada ya ushindi au hasara. Kukimbizia hasara mara nyingi huongeza tatizo."),
            new("Umri",
                "Bobeta ni kwa watu wa miaka 18+. Usiwaache watoto wacheze kwa simu au MoMo yako."),
            new("Ishara za tahadhari",
                "Ukificha uchezaji, kukopa kwa dau, au kushindwa kuacha, simama na tafuta msaada."),
            new("Akaunti",
                "Unaweza kuacha wakati wowote. Wasiliana na support ili kupunguza au kufunga akaunti."),
        ]);

    private static readonly LegalDoc RulesEn = new(
        "Game rules",
        "Bobeta matches are one-vs-one skill games. Stakes are real. Here is the short version.",
        [
            new("How a match works",
                "You create or join a game, lock a stake, and play until there is a winner. The pot is both stakes. Bobeta keeps a platform fee. The winner gets the rest."),
            new("Makopa",
                "Two players, four cards each from a shuffled deck. Follow suit when you can. Highest card of the led suit wins the trick. Win by reducing your hand under the game's win conditions."),
            new("Kopo",
                "10×10 checkers with flying kings. Captures are mandatory and you must take the maximum capture path when more than one exists."),
            new("Ngola",
                "Two rows of eight pits. Sow seeds counterclockwise. Capture from occupied opponent pits according to the Ngola rules in play."),
            new("Domino",
                "Double-six for two players. Match either open end. Draw if you cannot play. Empty your hand to win, or win on lowest pips if both are blocked."),
            new("Disconnects and disputes",
                "If a match stops because of a disconnect or suspected abuse, Bobeta may resume, void, or settle based on the game state and our review. In-game rule panels show the detailed rules for each title."),
        ]);

    private static readonly LegalDoc RulesFr = new(
        "Règles des jeux",
        "Les parties Bobeta sont des duels d'adresse à enjeux réels. Voici la version courte.",
        [
            new("Déroulement d'une partie",
                "Vous créez ou rejoignez une partie, bloquez une mise, et jouez jusqu'à un gagnant. Le pot = les deux mises. Bobeta garde des frais de plateforme. Le gagnant reçoit le reste."),
            new("Makopa",
                "Deux joueurs, quatre cartes chacun. Suivez la couleur si vous le pouvez. La plus haute carte de la couleur demandée gagne le pli."),
            new("Kopo",
                "Dames 10×10 avec dames volantes. Les prises sont obligatoires et la prise maximale s'applique."),
            new("Ngola",
                "Deux rangées de huit trous. Semez à l'envers des aiguilles d'une montre et capturez selon les règles Ngola en vigueur."),
            new("Domino",
                "Double-six à deux. Matchez une extrémité libre. Piochez si vous ne pouvez pas jouer. Videz votre main pour gagner, ou gagnez au moins de points si les deux sont bloqués."),
            new("Déconnexions et litiges",
                "En cas de coupure ou d'abus suspecté, Bobeta peut reprendre, annuler ou régler selon l'état de la partie et notre contrôle. Les panneaux de règles dans le jeu donnent le détail."),
        ]);

    private static readonly LegalDoc RulesKt = new(
        "Ba règles ya ba jeux",
        "Ba parties Bobeta kele ba duels ya adresse ti ba enjeux ya kyeleka. Yai kele ya kukusa.",
        [
            new("Mutindu partie ke salama",
                "Sala to kota partie, zibisa mise, sakana tii na nani me longa. Pot = ba mises zole. Bobeta ke baka frais ya plateforme. Nani me longa ke bakaka ya ntama."),
            new("Makopa",
                "Bantu zole, ba cartes iya. Landa couleur soki nge lenda. Carte ya nene ya couleur yina ke longa pli."),
            new("Kopo",
                "Dames 10×10 ti ba rois volantes. Kubaka kele obligatoire mpi kubaka ya nene."),
            new("Ngola",
                "Ba lignes zole ya ba trous iya. Mena na nima ya montre mpi baka na nsiku ya Ngola."),
            new("Domino",
                "Double-six ya bantu zole. Fwanisa nsuka. Baka kana nge lenda ve. Bika ba tuile nyonso sambu na kununga."),
            new("Kukata connexion mpi ba litiges",
                "Soki partie me yimisa, Bobeta lenda banda diaka, kangisa to futa na mutindu ya état. Ba règles na kati ya jeu ke monisa mambu ya nene."),
        ]);

    private static readonly LegalDoc RulesLn = new(
        "Ba règles ya ba jeux",
        "Ba parties Bobeta ezali ba duels ya adresse na ba enjeux ya solo. Oyo ezali ya moke.",
        [
            new("Ndenge partie esalama",
                "Sala to kota partie, ziba mise, sakana tii na oyo elongi. Pot = ba mises mibale. Bobeta ezwi frais ya plateforme. Oyo elongi azwi oyo etikali."),
            new("Makopa",
                "Bato mibale, ba cartes minei. Landa couleur soki okoki. Carte ya monene ya couleur wana elonga pli."),
            new("Kopo",
                "Dames 10×10 na ba rois volantes. Kokanga ezali obligatoire mpe kokanga ya monene."),
            new("Ngola",
                "Ba molongo mibale ya mabulu mwambe. Kabola na ngambo ya loboko ya mwasi mpe kanga na mibeko ya Ngola."),
            new("Domino",
                "Double-six ya bato mibale. Kokanisa nsuka. Zwa soki okoki te. Longola ba tuile nionso mpo na kolonga."),
            new("Kokata connexion mpe ba litiges",
                "Soki partie etiki, Bobeta ekoki kobanda lisusu, koziba to kofuta na boyangeli. Ba règles na kati ya jeu emonisi makambo ya mibu."),
        ]);

    private static readonly LegalDoc RulesSw = new(
        "Sheria za michezo",
        "Mechi za Bobeta ni pambano la ustadi lenye dau halisi. Hii ni muhtasari.",
        [
            new("Jinsi mechi inavyofanya kazi",
                "Unda au jiunge, funga dau, cheza hadi mshindi. Poti = dau zote mbili. Bobeta inachukua ada ya jukwaa. Mshindi anapata salio."),
            new("Makopa",
                "Wachezaji wawili, kadi nne kila mmoja. Fuata aina ukiweza. Kadi ya juu ya aina iliyoongoza inashinda."),
            new("Kopo",
                "Dame 10×10 wenye wafalme wanaoruka. Kuchukua ni lazima na kuchukua kwa kiwango cha juu kunatumika."),
            new("Ngola",
                "Safu mbili za mashimo manane. Sambaza kinyume cha saa na teka kulingana na sheria za Ngola."),
            new("Domino",
                "Double-six kwa wawili. Linganisha ncha. Chota ukiwa huwezi. Maliza mkono ili ushinde, au pointi chache ikiwa wote wamefungwa."),
            new("Kukatika na migogoro",
                "Mechi ikisimama, Bobeta inaweza kuendelea, batilisha, au maliza baada ya ukaguzi. Paneli za sheria ndani ya mchezo zina maelezo kamili."),
        ]);

    private static readonly LegalDoc WhatIsEn = new(
        "What is Bobeta?",
        "Bobeta is a skill-gaming platform for real stakes on mobile money. A short list of games you already know. Fast matches. Clear payouts.",
        [
            new("A short list, on purpose",
                "Big platforms stack hundreds of titles and leave you scrolling. Bobeta does the opposite. You get a small set of classic skill games: Makopa, Kopo, Ngola, and Domino. Pick one, stake, and play. No endless catalog to dig through."),
            new("Built for quick wins",
                "Every match is one-vs-one and paced for short sessions. You lock a stake, play a real opponent, and settle when the game ends. Win, then take the payout to your wallet. Ideal when you want a sharp match, not a long night of browsing lobbies."),
            new("Real stakes. Real cash-out.",
                "Deposit and withdraw with MoMo. Both stakes form the pot. Bobeta takes a platform fee. The rest goes to the winner's wallet. What you see in the match is what you play for."),
            new("Skill play, not a slot wall",
                "Outcomes come from how you play. Deals and first turns use fair randomization in the game engines. There is no hidden odds board behind a thousand lookalike games."),
            new("How to start",
                "Open Play now, confirm your MoMo number with an OTP, add funds, then create or join a match. The FAQ and Game rules pages cover the details if you need them."),
        ]);

    private static readonly LegalDoc WhatIsFr = new(
        "Qu'est-ce que Bobeta ?",
        "Bobeta est une plateforme de jeux d'adresse à enjeux réels via mobile money. Une courte liste de jeux que vous connaissez déjà. Des parties rapides. Des paiements clairs.",
        [
            new("Une liste courte, volontairement",
                "Les grandes plateformes empilent des centaines de titres et vous font défiler. Bobeta fait l'inverse. Vous avez un petit set de jeux classiques: Makopa, Kopo, Ngola et Domino. Choisissez, misez, jouez. Pas de catalogue infini."),
            new("Pensé pour des parties rapides",
                "Chaque partie est un duel, cadencé pour des sessions courtes. Vous bloquez une mise, affrontez un adversaire, et le règlement suit la fin de partie. Gagnez, puis encaissez sur votre portefeuille. Idéal pour un match net, pas pour une soirée à chercher un jeu."),
            new("Enjeux réels. Retrait réel.",
                "Déposez et retirez avec MoMo. Les deux mises forment le pot. Bobeta prélève des frais de plateforme. Le reste va au gagnant. Ce que vous voyez dans la partie est ce que vous jouez."),
            new("De l'adresse, pas un mur de machines",
                "Le résultat vient du jeu. Les distributions et le premier tour utilisent un tirage équitable dans les moteurs. Pas de cote cachée derrière mille jeux qui se ressemblent."),
            new("Comment commencer",
                "Ouvrez Jouer maintenant, confirmez votre numéro MoMo avec un OTP, rechargez, puis créez ou rejoignez une partie. La FAQ et les Règles des jeux donnent le détail si besoin."),
        ]);

    private static readonly LegalDoc WhatIsKt = new(
        "Bobeta kele nini?",
        "Bobeta kele plateforme ya ba jeux ya adresse ti ba enjeux ya kyeleka na mobile money. Liste ya kukusa ya ba jeux nge me zaba. Ba parties ya nswalu. Ba paiements ya polele.",
        [
            new("Liste ya kukusa, na lukanu",
                "Ba plateformes ya nene ke yika ba titres nkama mpi nge ke landa. Bobeta ke sala na nima. Nge ke zwaka set ya kukusa: Makopa, Kopo, Ngola mpi Domino. Sola, yika mise, sakana. Ve catalogue ya ndambu ve."),
            new("Sala sambu na ba parties ya nswalu",
                "Konso partie kele duel ya kukusa. Yika mise, sakana ti mbeni, mpi règlement ke landa nsuka. Longa, baka mbongo na wallet. Yai kele sambu na match ya nswalu, ve sambu na kulanda ba jeux ntangu ya nene."),
            new("Ba enjeux ya kyeleka. Retrait ya kyeleka.",
                "Yika mpi katula na MoMo. Ba mises zole ke sala pot. Bobeta ke baka frais ya plateforme. Ya ntama ke kwenda na nani me longa. Yo nge me mona na partie kele yo nge ke sakana."),
            new("Adresse, ve mur ya ba machines",
                "Résultat kele ya kusakana. Ba cartes mpi nani ke bandisa ke solamaka na botondi. Ve cote ya kubumba nima ya ba jeux nkama."),
            new("Mutindu ya kubanda",
                "Fina Jouer maintenant, confirma numéro MoMo na OTP, yika mbongo, sala to kota partie. FAQ mpi ba règles ke monisa ba détails."),
        ]);

    private static readonly LegalDoc WhatIsLn = new(
        "Bobeta ezali nini?",
        "Bobeta ezali plateforme ya ba jeux ya adresse na ba enjeux ya solo na mobile money. Liste mokuse ya ba jeux oyebi. Ba parties ya mbangu. Ba paiements ya polele.",
        [
            new("Liste mokuse, na makanisi",
                "Ba plateformes ya monene ezali na ba titres nkama mpe ozali kolanda. Bobeta ezali kosala na nsima. Ozwi set mokuse: Makopa, Kopo, Ngola mpe Domino. Pona, tya mise, sakana. Ezali te catalogue ya seko."),
            new("Salemi mpo na ba parties ya mbangu",
                "Mokano mokomoko ezali duel mokuse. Tya mise, sakana na monguna, règlement elandi suka. Longa, zwá mbongo na wallet. Ezali mpo na match ya mbangu, ezali te mpo na koluka ba jeux na butu mobimba."),
            new("Ba enjeux ya solo. Retrait ya solo.",
                "Tya mpe katola na MoMo. Ba mises mibale esali pot. Bobeta ezwi frais ya plateforme. Oyo etikalí ekendé na oyo alongi. Oyo omoni na partie ezali oyo osakanaka."),
            new("Adresse, ezali te mur ya ba machines",
                "Résultat ezali ya kosakana. Ba cartes mpe nani abandi esalemaka na bolamu. Ezali te cote ya kobomba nsima ya ba jeux nkama."),
            new("Ndenge ya kobanda",
                "Fina Jouer maintenant, confirma numéro MoMo na OTP, tya mbongo, sala to kota partie. FAQ mpe ba règles emonisi ba détails."),
        ]);

    private static readonly LegalDoc WhatIsSw = new(
        "Bobeta ni nini?",
        "Bobeta ni jukwaa la michezo ya ustadi yenye dau halisi kupitia mobile money. Orodha fupi ya michezo unayoijua. Mechi za haraka. Malipo wazi.",
        [
            new("Orodha fupi, kwa makusudi",
                "Majukwaa makubwa yanaweka mamia ya michezo na unakuwa unaskroli. Bobeta inafanya kinyume. Unapata seti ndogo: Makopa, Kopo, Ngola, na Domino. Chagua, weka dau, cheza. Hakuna katalogi isiyoisha."),
            new("Imejengwa kwa ushindi wa haraka",
                "Kila mechi ni mtu mmoja dhidi ya mwingine, kwa muda mfupi. Funga dau, cheza, maliza. Shinda, kisha chukua malipo kwenye pochi. Inafaa kwa mechi kali, si usiku mzima wa kutafuta mchezo."),
            new("Dau halisi. Utoaji halisi.",
                "Weka na toa kupitia MoMo. Dau zote mbili zinaunda poti. Bobeta inachukua ada ya jukwaa. Salio huenda kwa mshindi. Unachokiona kwenye mechi ndicho unachochezea."),
            new("Ustadi, si ukuta wa slot",
                "Matokeo yanatokana na uchezaji. Ugawaji na mwanzo hutumia bahati ya haki kwenye injini. Hakuna odds za siri nyuma ya michezo elfu zinazofanana."),
            new("Jinsi ya kuanza",
                "Fungua Play now, thibitisha nambari yako ya MoMo kwa OTP, weka fedha, kisha unda au jiunge. FAQ na Sheria za michezo zina maelezo zaidi."),
        ]);
}
