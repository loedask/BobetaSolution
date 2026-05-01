using System.Collections.Frozen;

namespace Bobeta.Mobile.Services;

/// <summary>Supported locales: en, fr, kt, ln, sw. Stored in <see cref="State.AppState.SelectedLanguage"/>.</summary>
public class I18nService(AppStateService appState)
{
    private readonly AppStateService _appState = appState;

    public string Locale => _appState.State.SelectedLanguage;

    private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> Translations = BuildTranslations();

    public string T(string key)
    {
        var locale = _appState.State.SelectedLanguage ?? "en";
        if (Translations.TryGetValue(locale, out var dict) && dict.TryGetValue(key, out var v)) return v;
        if (Translations.TryGetValue("en", out var en) && en.TryGetValue(key, out var enV)) return enV;
        return key;
    }

    public static IReadOnlyList<(string Code, string Label, string ShortCode)> SupportedLocales { get; } = new[]
    {
        ("en", "English", "EN"),
        ("fr", "Français", "FR"),
        ("kt", "Kituba", "KT"),
        ("ln", "Lingala", "LN"),
        ("sw", "Kiswahili", "SW"),
    };

    private static FrozenDictionary<string, FrozenDictionary<string, string>> BuildTranslations()
    {
        var en = new Dictionary<string, string>
        {
            ["app_name"] = "BOBETA",
            ["tagline"] = "Play Smart. Win Real Money.",
            ["get_started"] = "Get Started",
            ["secure_momo"] = "Secure MoMo Transactions",
            ["secure"] = "Secure",
            ["fair_randomized"] = "Fair & Randomized",
            ["instant_payouts"] = "Instant Payouts",
            ["choose_language"] = "Choose Language",
            ["enter_number"] = "Enter Your Number",
            ["send_code_desc"] = "We'll send a verification code to your MoMo number",
            ["phone_placeholder"] = "0XX XXX XXX",
            ["send_code"] = "Send Code",
            ["verification_code"] = "Verification Code",
            ["enter_otp_desc"] = "Enter the 6-digit code sent to",
            ["verify"] = "Verify",
            ["resend_code"] = "Resend Code",
            ["choose_player_name"] = "Choose Player Name",
            ["player_name_desc"] = "This is how other players will see you",
            ["player_name_placeholder"] = "Enter your player name",
            ["create_account"] = "Create Account",
            ["welcome_back"] = "Welcome back,",
            ["wallet_balance"] = "Wallet Balance",
            ["deposit"] = "Deposit",
            ["withdraw"] = "Withdraw",
            ["create_game"] = "Create Game",
            ["join_game"] = "Join Game",
            ["history"] = "History",
            ["recent_activity"] = "Recent Activity",
            ["see_all"] = "See All",
            ["won_vs"] = "Won vs",
            ["deposit_label"] = "Deposit",
            ["tx_bet_lock"] = "Game stake",
            ["tx_bet_release"] = "Stake returned",
            ["tx_winnings"] = "Winnings",
            ["tx_commission"] = "Platform fee",
            ["bet_placed"] = "Bet placed",
            ["trust_message"] = "All transactions are secured. 75% payout guaranteed. Fair & randomized card system.",
            ["select_bet_amount"] = "Select Bet Amount",
            ["choose_bet_desc"] = "Choose how much you want to bet",
            ["your_bet"] = "Your bet",
            ["potential_win"] = "Potential win (75%)",
            ["platform_fee"] = "Platform fee (25%)",
            ["create_game_session"] = "Create Game Session",
            ["waiting_for_opponent"] = "Waiting for Opponent",
            ["looking_for_match"] = "Looking for a match...",
            ["cancel"] = "Cancel",
            ["bet"] = "Bet",
            ["open_game_sessions"] = "Open game sessions",
            ["refresh"] = "Refresh",
            ["live"] = "Live",
            ["join"] = "Join",
            ["waiting_for_opponent_short"] = "Waiting for opponent",
            ["game_history"] = "Game History",
            ["won"] = "Won",
            ["lost"] = "Lost",
            ["history_waiting"] = "Waiting for opponent",
            ["history_in_progress"] = "In progress",
            ["history_live_game"] = "Live game",
            ["history_you_hosted"] = "You started this game",
            ["history_you_joined"] = "You joined this game",
            ["history_live_hint"] = "Join only lists open lobbies. Open the table to keep playing.",
            ["history_continue"] = "Open table",
            ["history_cancelled"] = "Cancelled",
            ["profile"] = "Profile",
            ["games"] = "Games",
            ["wins"] = "Wins",
            ["win_rate"] = "Win Rate",
            ["wallet_settings"] = "Wallet Settings",
            ["security"] = "Security",
            ["sign_out"] = "Sign Out",
            ["sign_out_confirm"] = "Are you sure you want to sign out of your account?",
            ["current_balance"] = "Current Balance",
            ["amount_fcfa"] = "Amount (FCFA)",
            ["enter_amount"] = "Enter amount",
            ["payment_method_momo"] = "Mobile Money (MoMo)",
            ["instant_deposit_momo"] = "Instant deposit via MoMo",
            ["confirm_deposit"] = "Confirm Deposit",
            ["processing"] = "Processing…",
            ["deposit_successful"] = "Deposit Successful!",
            ["added_to_wallet"] = "added to wallet",
            ["invalid_amount"] = "Invalid amount",
            ["min_deposit_msg"] = "Minimum deposit is 100 FCFA",
            ["available_balance"] = "Available Balance",
            ["momo_number"] = "MoMo Number",
            ["confirm_withdrawal"] = "Confirm Withdrawal",
            ["withdrawal_successful"] = "Withdrawal Successful!",
            ["sent_to_momo"] = "sent to MoMo",
            ["min_withdrawal"] = "Minimum",
            ["max_withdrawal"] = "Max",
            ["insufficient_balance"] = "Insufficient balance",
            ["cannot_exceed_balance"] = "You cannot withdraw more than your balance",
            ["min_withdraw_msg"] = "Minimum withdrawal is",
            ["home"] = "Home",
            ["create"] = "Create",
            ["language"] = "Language",
            ["confirm"] = "Confirm",
            ["not_found"] = "Oops! Page not found",
            ["return_home"] = "Return to Home",
            ["done_short"] = "Done",
            ["game"] = "Game",
            ["pot"] = "Pot",
            ["pot_table"] = "Table pot",
            ["pot_seats"] = "Head-to-head",
            ["pot_activity_hint"] = "Activity lists only your seat (−{0} FCFA). This total is both stacks combined.",
            ["pot_chip_title"] = "{0} FCFA from each player · 2 seats",
            ["pot_opponent_lane"] = "Across from you",
            ["your_turn"] = "Your turn",
            ["opponent_turn"] = "Opponent's turn",
            ["invalid_move_follow_suit"] = "You must follow the led suit when you have a card in that suit. Choose a highlighted card.",
            ["trick_outcome_you"] = "You took this trick.",
            ["trick_outcome_opponent"] = "Opponent took this trick.",
            ["waiting_for_opponent"] = "Waiting for an opponent…",
            ["play_card"] = "Play card",
            ["take_card"] = "Take",
            ["take_card_hint_disabled"] = "Play a card of the led suit if you hold one.",
            ["take_card_hint_enabled"] = "No led suit — tap Take so the opponent gets their card back and you draw from stock.",
            ["makopa_how_to_play_title"] = "How to play Makopa",
            ["makopa_rules_body"] =
                "\u2022 Two players start with 4 cards each; the rest of the deck is a fair stock pile.\n\u2022 First leader is chosen at random for that game.\n\u2022 You must follow the led suit whenever you hold a card in that suit.\n\u2022 If you cannot follow, tap Take: the other's lead goes back into their hand, you draw one from stock if any, they lead again.\n\u2022 When both suits match, higher rank wins the trick (ties favour the leader).\n\u2022 After winning a trick, if only one card is left it's your win when you lead.\n\u2022 If the other holds a single card and you play that suit, you lose.",
            ["makopa_round_score"] = "Hands won: {0}\u2013{1} (first to 2 wins the match)",
            ["makopa_rules_link"] = "Rules",
        };

        var fr = new Dictionary<string, string>(en)
        {
            ["app_name"] = "BOBETA",
            ["tagline"] = "Jouez malin. Gagnez de l'argent réel.",
            ["get_started"] = "Commencer",
            ["choose_language"] = "Choisir la langue",
            ["send_code"] = "Envoyer le code",
            ["verify"] = "Vérifier",
            ["cancel"] = "Annuler",
            ["done_short"] = "Terminer",
            ["deposit"] = "Dépôt",
            ["withdraw"] = "Retrait",
            ["profile"] = "Profil",
            ["home"] = "Accueil",
            ["language"] = "Langue",
            ["waiting_for_opponent"] = "En attente d'un adversaire…",
            ["history_waiting"] = "En attente d'un adversaire",
            ["history_in_progress"] = "Partie en cours",
            ["history_live_game"] = "Partie en direct",
            ["history_you_hosted"] = "Vous avez créé cette partie",
            ["history_you_joined"] = "Vous avez rejoint cette partie",
            ["history_live_hint"] = "Rejoindre n'affiche que les salons ouverts. Ouvrez la table pour continuer.",
            ["history_continue"] = "Ouvrir la table",
            ["history_cancelled"] = "Annulée",
            ["tx_bet_lock"] = "Mise de jeu",
            ["tx_bet_release"] = "Mise rendue",
            ["tx_winnings"] = "Gains",
            ["tx_commission"] = "Frais plateforme",
            ["pot_table"] = "Pot (table)",
            ["pot_seats"] = "Duel",
            ["pot_activity_hint"] = "L'activité n'affiche que votre place (−{0} FCFA). Ici = les deux piles réunies.",
            ["pot_chip_title"] = "{0} FCFA par joueur · 2 places",
            ["pot_opponent_lane"] = "En face",
            ["invalid_move_follow_suit"] = "Vous devez suivre la couleur demandée si vous en avez. Choisissez une carte mise en évidence.",
            ["trick_outcome_you"] = "Vous remportez ce pli.",
            ["trick_outcome_opponent"] = "L'adversaire remporte ce pli.",
            ["take_card"] = "Prendre",
            ["take_card_hint_disabled"] = "Jouez une carte de la couleur demandée si vous en avez.",
            ["take_card_hint_enabled"] = "Sans la couleur : Prenez pour rendre la carte à l'autre joueur et piocher.",
            ["makopa_how_to_play_title"] = "Comment jouer (Makopa)",
            ["makopa_rules_body"] =
                "\u2022 Deux joueurs ; 6 cartes par main.\n\u2022 Match en 2 mains gagnantes sur 3 (premier à 2 remporte le pot).\n\u2022 Suivez la couleur lorsque vous pouvez.\n\u2022 Sans carte de la couleur demandée, jouez une carte quelconque ; seules les cartes de la couleur de tête décident.\n\u2022 La meilleure carte de la couleur de tête remporte la levée ; en cas d'égalité, le joueur ayant amené garde.\n\u2022 Le vainqueur d'une main entame la première levée suivante ; le joueur qui amène la première main est tiré au sort pour cette table.",
            ["makopa_round_score"] = "Mains gagnées : {0}\u2013{1} (2 pour gagner)",
            ["makopa_rules_link"] = "Règles",
        };

        var kt = new Dictionary<string, string>(en);
        var ln = new Dictionary<string, string>(en);
        var sw = new Dictionary<string, string>(en);

        return new Dictionary<string, FrozenDictionary<string, string>>
        {
            ["en"] = en.ToFrozenDictionary(),
            ["fr"] = fr.ToFrozenDictionary(),
            ["kt"] = kt.ToFrozenDictionary(),
            ["ln"] = ln.ToFrozenDictionary(),
            ["sw"] = sw.ToFrozenDictionary(),
        }.ToFrozenDictionary();
    }
}
