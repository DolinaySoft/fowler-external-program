using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Evaluator.Model;

namespace Evaluator.Format
{
    /// <summary>
    /// Sestaveni odpovedi (fmt "fowler-eval", popis v docs/format.md) z vysledku hodnoceni (Model.Vysledek).
    /// Hodnoty upravi na tvar, ktery prijmou pole okna ve Fowleru (pocet desetinnych mist a znaku, delka textu);
    /// pri doplnovani hodnoceni neni treba menit.
    /// </summary>
    public static class ZapisOdpovedi
    {
        public const string Fmt = "fowler-eval";
        public const int Verze = 1;
        const int MaxZnakuTextu = 100;  //textova pole okna Hodnotit
        const int MaxZnakuCisla = 5;    //ciselna pole okna Hodnotit audiogramu (napr. 12.25, -10.5, 100)

        /// <summary>zapise odpoved do souboru v UTF-8 bez BOM</summary>
        public static void Zapis(string soubor, Vstup vstup, Vysledek vysledek, string app)
        {
            File.WriteAllText(soubor, Sestav(vstup, vysledek, app), new UTF8Encoding(false));
        }

        public static string Sestav(Vstup vstup, Vysledek vysledek, string app)
        {
            List<string> poznamky = new List<string>(vysledek.Poznamky);
            Dictionary<string, object> odpoved = new Dictionary<string, object>();
            odpoved["fmt"] = Fmt;
            odpoved["v"] = Verze;
            odpoved["app"] = app;
            odpoved["created"] = DateTime.Now.ToString(Json.FormatCasu, CultureInfo.InvariantCulture);
            //identifikace vysetreni presne tak, jak byla predana - podle ni Fowler pozna, ze odpoved patri k vysetreni
            Dictionary<string, object> pacient = new Dictionary<string, object>();
            if (vstup.Pacient.Pid != null)
                pacient["pid"] = vstup.Pacient.Pid;
            if (vstup.Pacient.Rc != null)
                pacient["rc"] = vstup.Pacient.Rc;
            odpoved["patient"] = pacient;
            odpoved["exam"] = new Dictionary<string, object>
            {
                { "time", vstup.CasVysetreni.ToString(Json.FormatCasu, CultureInfo.InvariantCulture) }
            };
            //jen cast, ktera byla predana
            if (vstup.Audio != null && vysledek.Audio != null)
                PridejNeprazdne(odpoved, "audio", Audio(vysledek.Audio, poznamky));
            if (vstup.Tymp != null && vysledek.Tymp != null)
                PridejNeprazdne(odpoved, "tymp", Tympanometrie(vysledek.Tymp));
            if (vstup.Slovni != null && vysledek.Slovni != null)
                PridejNeprazdne(odpoved, "speech", Slovni(vysledek.Slovni, poznamky));
            if (poznamky.Count > 0)
                odpoved["note"] = string.Join("\n", poznamky);
            return Json.Serializer().Serialize(odpoved);
        }

        static void PridejNeprazdne(Dictionary<string, object> objekt, string klic, Dictionary<string, object> hodnota)
        {
            if (hodnota != null && hodnota.Count > 0)
                objekt[klic] = hodnota;
        }

        /// <summary>
        /// cislo s nejvyse maxDesetin desetinnymi misty a nejvyse maxZnaku znaky (zaokrouhli se, kdyz je delsi,
        /// napr. prumer 106.67 dB na 106.7 dB); null = nelze zapsat
        /// </summary>
        static decimal? Cislo(decimal? hodnota, int maxDesetin, int maxZnaku)
        {
            if (hodnota == null)
                return null;
            for (int desetin = maxDesetin; desetin >= 0; desetin--)
            {
                decimal zaokrouhleno = Math.Round(hodnota.Value, desetin, MidpointRounding.AwayFromZero);
                zaokrouhleno = zaokrouhleno / 1.000000000000000000000000000000000m; //bez nul na konci (20.10 -> 20.1)
                if (zaokrouhleno.ToString(CultureInfo.InvariantCulture).Length <= maxZnaku)
                    return zaokrouhleno;
            }
            return null;
        }

        /// <summary>text bez ridicich znaku a mezer na okrajich, nejvyse MaxZnakuTextu; null = prazdny</summary>
        static string Text(string text)
        {
            if (text == null)
                return null;
            text = new string(text.Where(c => !char.IsControl(c)).ToArray()).Trim();
            if (text.Length > MaxZnakuTextu)
                text = text.Substring(0, MaxZnakuTextu).TrimEnd();
            return (text.Length == 0) ? null : text;
        }

        //cislo do pole okna; hodnota mimo rozsah pole by Fowler odmitl celou odpoved, proto se vynecha s poznamkou
        static void PridejCislo(Dictionary<string, object> objekt, string klic, decimal? hodnota, int maxDesetin, int maxZnaku,
            decimal min, decimal max, string co, List<string> poznamky)
        {
            decimal? cislo = Cislo(hodnota, maxDesetin, maxZnaku);
            if (cislo != null && cislo >= min && cislo <= max)
                objekt[klic] = cislo.Value;
            else if (hodnota != null)
                poznamky.Add($"{co}: hodnotu {hodnota.Value.ToString(CultureInfo.InvariantCulture)} nelze zapsat do pole okna " +
                    $"(rozsah {min.ToString(CultureInfo.InvariantCulture)} až {max.ToString(CultureInfo.InvariantCulture)}).");
        }

        static void PridejText(Dictionary<string, object> objekt, string klic, string hodnota)
        {
            string text = Text(hodnota);
            if (text != null)
                objekt[klic] = text;
        }

        static Dictionary<string, object> Trojice(Trojice t, decimal min, decimal max, string co, List<string> poznamky)
        {
            Dictionary<string, object> o = new Dictionary<string, object>();
            PridejCislo(o, "R", t.R, 2, MaxZnakuCisla, min, max, co + " vpravo", poznamky);
            PridejCislo(o, "L", t.L, 2, MaxZnakuCisla, min, max, co + " vlevo", poznamky);
            PridejCislo(o, "total", t.Celkem, 2, MaxZnakuCisla, min, max, co + " celkem", poznamky);
            return o;
        }

        static Dictionary<string, object> Audio(VysledekAudia v, List<string> poznamky)
        {
            Dictionary<string, object> o = new Dictionary<string, object>();
            PridejNeprazdne(o, "fowler", Trojice(v.ZtrataDleFowlera, 0, 100, "Ztráta dle Fowlera", poznamky));
            PridejNeprazdne(o, "acAvg", Trojice(v.PrumerVzduch, -10, 130, "Průměr vzduchu", poznamky));
            PridejNeprazdne(o, "bcAvg", Trojice(v.PrumerKost, -10, 130, "Průměr kosti", poznamky));
            PridejText(o, "who", v.Who);
            PridejText(o, "group", v.Skupina);
            Dictionary<string, object> sisi = new Dictionary<string, object>();
            PridejText(sisi, "R", v.SisiR);
            PridejText(sisi, "L", v.SisiL);
            PridejNeprazdne(o, "sisi", sisi);
            return o;
        }

        static object Podnet(Podnet podnet)
        {
            if (podnet.JeSum)
                return podnet.Sum;
            return podnet.Hz;
        }

        static Dictionary<string, object> Tympanometrie(VysledekTympanometrie v)
        {
            Dictionary<string, object> o = new Dictionary<string, object>();
            PridejNeprazdne(o, "R", TympUcho(v.R));
            PridejNeprazdne(o, "L", TympUcho(v.L));
            return o;
        }

        static Dictionary<string, object> TympUcho(VysledekTympUcha v)
        {
            if (v == null)
                return null;
            Dictionary<string, object> o = new Dictionary<string, object>();
            string typ = Text(v.Typ);
            if (typ != null && typ.Length <= 10)
                o["type"] = typ;
            PridejSeznam(o, "ipsi", Reflexy(v.Ipsi));
            PridejSeznam(o, "contra", Reflexy(v.Contra));
            PridejSeznam(o, "decayIpsi", Decay(v.DecayIpsi));
            PridejSeznam(o, "decayContra", Decay(v.DecayContra));
            return o;
        }

        static void PridejSeznam(Dictionary<string, object> objekt, string klic, List<object> seznam)
        {
            if (seznam.Count > 0)
                objekt[klic] = seznam;
        }

        //kazdy podnet nejvyse jednou (Fowler opakovany podnet odmitne)
        static List<object> Reflexy(List<VysledekReflexu> reflexy)
        {
            return reflexy.GroupBy(r => r.Podnet).Select(g => (object)new Dictionary<string, object>
            {
                { "stim", Podnet(g.Key) },
                { "res", g.First().Vybavny ? "+" : "-" }
            }).ToList();
        }

        static List<object> Decay(List<VysledekDecay> seznam)
        {
            return seznam.GroupBy(d => d.Podnet).Select(g => (object)new Dictionary<string, object>
            {
                { "stim", Podnet(g.Key) },
                { "pct", g.First().Procent.HasValue ? (object)g.First().Procent.Value : "-" }
            }).ToList();
        }

        static Dictionary<string, object> Slovni(VysledekSlovni v, List<string> poznamky)
        {
            Dictionary<string, object> o = Srt(v.BezSluchadla, "", poznamky);
            Dictionary<string, object> aided = Srt(v.SeSluchadlem, " se sluchadlem", poznamky);
            if (aided.Count > 0)
                o["aided"] = aided;
            return o;
        }

        //SRT: nejvyse 1 desetinne misto (pole SRT ve fslovni)
        static Dictionary<string, object> Srt(SrtKrivek s, string co, List<string> poznamky)
        {
            Dictionary<string, object> srt = new Dictionary<string, object>();
            PridejCislo(srt, "R", s.R, 1, 6, -10, 120, "SRT vpravo" + co, poznamky);
            PridejCislo(srt, "L", s.L, 1, 6, -10, 120, "SRT vlevo" + co, poznamky);
            PridejCislo(srt, "bin", s.Bin, 1, 6, -10, 120, "SRT binaurálně" + co, poznamky);
            Dictionary<string, object> o = new Dictionary<string, object>();
            if (srt.Count > 0)
                o["srt"] = srt;
            return o;
        }
    }
}
