namespace Sportarr.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Maps Sportarr league (series) names to the short league codes used in Game Thumbs URLs
    /// (e.g. Sportarr "NCAA Division 1" -> Game Thumbs "ncaaf"). Game Thumbs paths are code-based
    /// (/{code}/cover, /{code}/{team1}/{team2}/thumb), so a league is only "supported" by Game
    /// Thumbs when it appears in this table; unsupported leagues fall back to the Sportarr API.
    ///
    /// Sportarr exposes no league code/slug (those fields are null), so this table is hand-curated
    /// and verified against the live /api/public/v1/leagues data. Names are matched case-insensitively.
    /// </summary>
    public static class GameThumbsLeagues
    {
        /// <summary>
        /// A Game Thumbs league code, optionally guarded by sport to disambiguate Sportarr leagues
        /// that share a name across sports (e.g. "Bangladesh Premier League" exists as both Soccer
        /// and Cricket; only the Cricket one maps to Game Thumbs).
        /// </summary>
        private readonly struct LeagueCode
        {
            public LeagueCode(string code, string? requiredSport = null)
            {
                this.Code = code;
                this.RequiredSport = requiredSport;
            }

            /// <summary>Gets the Game Thumbs league code used as the URL path segment.</summary>
            public string Code { get; }

            /// <summary>Gets the Sportarr sport this mapping requires, or null if any sport matches.</summary>
            public string? RequiredSport { get; }
        }

        /// <summary>
        /// Sportarr league name -> Game Thumbs code. Keyed case-insensitively. Only leagues with a
        /// verified Game Thumbs counterpart are listed; leagues Game Thumbs does not support
        /// (e.g. PWHL, IIHF, Olympics, most MiLB sub-levels, NCAA minor sports) are intentionally absent.
        /// </summary>
        private static readonly Dictionary<string, LeagueCode> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            // Basketball
            ["NBA"] = new("nba"),
            ["WNBA"] = new("wnba"),
            ["NBA G League"] = new("nbag"),
            ["Unrivaled Basketball"] = new("ubl"),

            // American football
            ["NFL"] = new("nfl"),
            ["UFL"] = new("ufl"),
            ["CFL"] = new("cfl"),

            // Baseball
            ["MLB"] = new("mlb"),
            ["World Baseball Classic"] = new("wbc"),
            ["Korean KBO League"] = new("kbo"),

            // Hockey
            ["NHL"] = new("nhl"),
            ["UK Elite Ice Hockey League"] = new("eihl"),
            ["American USHL"] = new("ushl"),
            ["American AHL"] = new("ahl"),
            ["American ECHL"] = new("echl"),
            ["Canadian WHL"] = new("whl"),
            ["Canadian OHL"] = new("ohl"),
            ["Canadian QMJHL"] = new("qmjhl"),

            // Lacrosse
            ["National Lacrosse League"] = new("nll"),

            // Soccer
            ["English Premier League"] = new("epl"),
            ["FA Cup"] = new("engfa"),
            ["English League Championship"] = new("championship"),
            ["English League 1"] = new("league-one"),
            ["English League 2"] = new("league-two"),
            ["English National League"] = new("english-national-league"),
            ["English National League South"] = new("english-national-league-south"),
            ["English National League North"] = new("english-national-league-north"),
            ["EFL Cup"] = new("engleaguecup"),
            ["FA Community Shield"] = new("engcommunityshield"),
            ["FA Trophy"] = new("engfatrophy"),
            ["Spanish La Liga"] = new("laliga"),
            ["Copa del Rey"] = new("copadelrey"),
            ["Spanish La Liga 2"] = new("segunda"),
            ["German Bundesliga"] = new("bundesliga"),
            ["German 2. Bundesliga"] = new("2-bundesliga"),
            ["Italian Serie A"] = new("seriea"),
            ["Italian Serie B"] = new("serieb"),
            ["French Ligue 1"] = new("ligue1"),
            ["French Ligue 2"] = new("ligue2"),
            ["Scottish Premier League"] = new("spfl"),
            ["Scottish FA Cup"] = new("scup"),
            ["Scottish Championship"] = new("sch"),
            ["Scottish League 1"] = new("sl1"),
            ["Scottish League 2"] = new("sl2"),
            ["Scottish League Cup"] = new("slcup"),
            ["American Major League Soccer"] = new("mls"),
            ["UEFA Champions League"] = new("uefa"),
            ["UEFA Womens Champions League"] = new("uefa.wchampions"),
            ["UEFA Europa League"] = new("europa"),
            ["UEFA Conference League"] = new("conference"),
            ["UEFA European Championships"] = new("euros"),
            ["UEFA European Championships Qualifying"] = new("euroq"),
            ["UEFA Nations League"] = new("uefanationsleague"),
            ["African Cup of Nations"] = new("afcon"),
            ["African Cup of Nations Qualifying"] = new("afconq"),
            ["CONCACAF Nations League"] = new("concacafnationsleague"),
            ["FIFA World Cup"] = new("worldcup"),
            ["World Cup Qualifying UEFA"] = new("wcq-uefa"),
            ["World Cup Qualifying CONMEBOL"] = new("wcq-conmebol"),
            ["World Cup Qualifying CONCACAF"] = new("wcq-concacaf"),
            ["World Cup Qualifying AFC"] = new("wcq-afc"),
            ["World Cup Qualifying CAF"] = new("wcq-caf"),
            ["World Cup Qualifying OFC"] = new("wcq-ofc"),

            // Cricket
            ["The Hundred"] = new("the100m"),
            ["The Hundred Women"] = new("the100w"),
            ["English t20 Blast"] = new("t20blast"),
            ["Indian Premier League"] = new("ipl"),
            ["Australian Big Bash League"] = new("bbl"),
            ["Bangladesh Premier League"] = new("bpl", "Cricket"),
            ["Caribbean Premier League"] = new("cpl"),
            ["Major League Cricket"] = new("mlc"),
            ["New Zealand Super Smash"] = new("nzss"),
            ["SA20"] = new("sa20"),
            ["Nepal Premier League"] = new("npl"),
            ["Pakistan Super League"] = new("psl"),

            // Rugby
            ["Six Nations Championship"] = new("6n"),
            ["Six Nations Women"] = new("6nw"),
            ["English Prem Rugby"] = new("epr"),
            ["United Rugby Championship"] = new("urc"),
            ["European Rugby Challenge Cup"] = new("ercc"),
            ["Rugby World Cup"] = new("rwc"),
            ["Womens Rugby World Cup"] = new("wrwc"),
            ["English Rugby League Super League"] = new("erlsp"),
            ["Rugby League World Cup"] = new("rlwc"),
            ["Major League Rugby"] = new("mlr"),

            // Combat sports (MMA)
            ["UFC"] = new("ufc"),
            ["Professional Fighters League"] = new("pfl"),
            ["Bellator"] = new("bellator"),

            // Tennis
            ["ATP World Tour"] = new("atp"),
            ["WTA Tour"] = new("wta"),

            // NCAA (Sportarr only carries these four with a Game Thumbs equivalent)
            ["NCAA Division 1"] = new("ncaaf"),
            ["NCAA Division 1 Ice Hockey"] = new("ncaah"),
            ["NCAA Division I Basketball Mens"] = new("ncaam"),
            ["NCAA Division I Basketball Women"] = new("ncaaw"),
        };

        /// <summary>
        /// Resolves the Game Thumbs league code for a Sportarr league name.
        /// </summary>
        /// <param name="leagueName">The Sportarr league (series) name, e.g. "NCAA Division 1".</param>
        /// <param name="sport">The Sportarr sport name, used only to disambiguate names shared across
        /// sports. Pass null when the sport is unknown (the sport guard is then skipped).</param>
        /// <param name="code">The resolved Game Thumbs code when this returns true.</param>
        /// <returns>True if the league is supported by Game Thumbs; otherwise false.</returns>
        public static bool TryGetLeagueCode(string? leagueName, string? sport, out string code)
        {
            code = string.Empty;

            if (string.IsNullOrWhiteSpace(leagueName))
            {
                return false;
            }

            if (!Map.TryGetValue(leagueName.Trim(), out var mapping))
            {
                return false;
            }

            // Enforce the sport guard only when both a requirement and a known sport are present.
            // When the sport is unknown (e.g. resolved from an episode), fall back to name-only matching.
            if (mapping.RequiredSport != null && !string.IsNullOrEmpty(sport) &&
                !string.Equals(mapping.RequiredSport, sport, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            code = mapping.Code;
            return true;
        }
    }
}
