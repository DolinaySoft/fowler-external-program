using System.Collections.Generic;

namespace Evaluator.Model
{
    //Vysledek hodnoceni = pole oken Fowleru, do kterych Fowler odpoved vlozi ke kontrole uzivatelem. Z nej
    //Format/ZapisOdpovedi.cs sestavi odpoved (fmt "fowler-eval", popis v docs/format.md v koreni repozitare).
    //U kazdeho udaje je popisek pole v okne a hodnota, kterou pole prijme; jak udaj urcit, je vec pracoviste.
    //null = udaj se nehodnoti, Fowler pole v okne necha, jak je.

    public class Vysledek
    {
        public VysledekAudia Audio { get; set; }
        public VysledekTympanometrie Tymp { get; set; }
        public VysledekSlovni Slovni { get; set; }
        //zprava pro uzivatele (note): proc nektery udaj neni, upozorneni k datum apod., kazda polozka na radek
        public List<string> Poznamky { get; } = new List<string>();
    }

    /// <summary>hodnota pro prave ucho, leve ucho a celkem</summary>
    public class Trojice
    {
        public decimal? R { get; set; }
        public decimal? L { get; set; }
        public decimal? Celkem { get; set; }
    }

    /// <summary>pole okna Hodnotit audiogramu (odpoved "audio")</summary>
    public class VysledekAudia
    {
        public Trojice ZtrataDleFowlera { get; } = new Trojice(); //"ztrata sluchu dle Fowlera" (fowler): cislo 0 az 100 [%]
        public Trojice PrumerVzduch { get; } = new Trojice();     //"prumer vzdusneho vedeni" (acAvg): cislo -10 az 130 [dB]
        public Trojice PrumerKost { get; } = new Trojice();       //"prumer kostniho vedeni" (bcAvg): cislo -10 az 130 [dB]
        public string Who { get; set; }                           //"ztrata sluchu dle WHO" (who): text
        public string Skupina { get; set; }                       //"skupina ztraty sluchu dle veku" (group): text
        public string SisiR { get; set; }                         //"vysledek SISI testu" (sisi): text, napr. jako v nabidce okna
        public string SisiL { get; set; }                         //  "negativní", "neurčitý", "pozitivní"
    }

    /// <summary>pole okna Hodnotit tympanometrie (odpoved "tymp")</summary>
    public class VysledekTympanometrie
    {
        public VysledekTympUcha R { get; set; }
        public VysledekTympUcha L { get; set; }
    }

    public class VysledekTympUcha
    {
        public string Typ { get; set; }                           //"typ krivky" (type): text do 10 znaku, nabidka okna
                                                                  //  A, B, C, C1, C2, "-" (nelze zmerit)
        public List<VysledekReflexu> Ipsi { get; } = new List<VysledekReflexu>();       //"reflexy IPSI"
        public List<VysledekReflexu> Contra { get; } = new List<VysledekReflexu>();     //"reflexy CONTRA"
        public List<VysledekDecay> DecayIpsi { get; } = new List<VysledekDecay>();      //"decay IPSI"
        public List<VysledekDecay> DecayContra { get; } = new List<VysledekDecay>();    //"decay CONTRA"
    }

    public class VysledekReflexu
    {
        public Podnet Podnet { get; set; }
        public bool Vybavny { get; set; }                         //true = "+", false = "-"
    }

    public class VysledekDecay
    {
        public Podnet Podnet { get; set; }
        public int? Procent { get; set; }                         //cele cislo [%]; null = "-" (nemeritelne)
    }

    /// <summary>pole SRT ve slovni audiometrii (odpoved "speech")</summary>
    public class VysledekSlovni
    {
        public SrtKrivek BezSluchadla { get; } = new SrtKrivek();
        public SrtKrivek SeSluchadlem { get; } = new SrtKrivek();
    }

    public class SrtKrivek
    {
        public decimal? R { get; set; }                           //"SRT": cislo -10 az 120 [dB]
        public decimal? L { get; set; }
        public decimal? Bin { get; set; }
    }
}
