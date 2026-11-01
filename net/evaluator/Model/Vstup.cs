using System;
using System.Collections.Generic;

namespace Evaluator.Model
{
    //Predana data vysetreni (fmt "fowler-exam", popis v docs/format.md). Plni je Format/CteniVstupu.cs,
    //hodnoceni (potomek Hodnoceni/Hodnotitel.cs) pracuje jen s temito tridami a nemusi resit JSON.
    //Co v datech nebylo, je null (u seznamu prazdny seznam).

    /// <summary>predana data jednoho vysetreni jednoho pacienta</summary>
    public class Vstup
    {
        public string App { get; set; }                 //program, ktery data vytvoril, napr. "Fowler 3.3.0.0"
        public DateTime? Vytvoreno { get; set; }        //created
        public DateTime CasVysetreni { get; set; }      //exam.time (odpoved ho musi zopakovat)
        public Pacient Pacient { get; set; } = new Pacient();
        //predava se jen cast podle okna, ze ktereho uzivatel program spustil; ostatni jsou null
        public TonovaAudiometrie Audio { get; set; }
        public Tympanometrie Tymp { get; set; }
        public SlovniAudiometrie Slovni { get; set; }
    }

    public class Pacient
    {
        public string Pid { get; set; }                 //GUID pacienta ve Fowleru (odpoved ho musi zopakovat)
        public string Rc { get; set; }                  //rodne cislo bez lomitka (odpoved ho musi zopakovat)
        public string Jmeno { get; set; }
        public string Prijmeni { get; set; }
        public DateTime? DatumNarozeni { get; set; }
        public string Pojistovna { get; set; }
        public int? VekRoky { get; set; }               //vek v den vysetreni: dokoncene roky (exam.age)
        public int? VekMesice { get; set; }             //a mesice (0-11)
        public string Pohlavi { get; set; }             //"M", "F", null = neznamo (exam.sex)
    }

    #region Tonova audiometrie (audio)

    public class TonovaAudiometrie
    {
        //true = vysetreni ulozene verzi Fowleru pred MDR; kostni vedeni mohlo byt doplnene odhadem
        public bool Legacy { get; set; }
        public string Pristroj { get; set; }
        public string Poznamka { get; set; }
        public List<int> Hz { get; } = new List<int>(); //frekvence, na kterych je aspon jedna hodnota
        public AudioUcho R { get; set; }                //null = ucho bez hodnot
        public AudioUcho L { get; set; }
    }

    /// <summary>hodnoty jednoho ucha podle frekvence [Hz]; frekvence bez hodnoty ve slovniku neni</summary>
    public class AudioUcho
    {
        public SortedDictionary<int, Prah> Vzduch { get; } = new SortedDictionary<int, Prah>();       //ac [dB HL]
        public bool VzduchMaskovan { get; set; }                                                      //acMasked
        public SortedDictionary<int, int> VzduchMaskovani { get; } = new SortedDictionary<int, int>(); //acMask [dB]
        public SortedDictionary<int, Prah> Kost { get; } = new SortedDictionary<int, Prah>();         //bc [dB HL]
        public SortedDictionary<int, int> KostMaskovani { get; } = new SortedDictionary<int, int>();  //bcMask [dB]
        public SortedDictionary<int, int> Ucl { get; } = new SortedDictionary<int, int>();            //prah nepohodli [dB]
        public SortedDictionary<int, int> Mcl { get; } = new SortedDictionary<int, int>();            //nejprijemnejsi hlasitost [dB]
        public SortedDictionary<int, int> Sisi { get; } = new SortedDictionary<int, int>();           //SISI [%]

        public Prah VzduchNa(int hz)
        {
            Prah prah;
            return Vzduch.TryGetValue(hz, out prah) ? prah : Prah.Nezmereno;
        }

        public Prah KostNa(int hz)
        {
            Prah prah;
            return Kost.TryGetValue(hz, out prah) ? prah : Prah.Nezmereno;
        }
    }

    /// <summary>prah sluchu na jedne frekvenci: hodnota v dB HL, nebo neslysi ("NR", ve Fowleru "-")</summary>
    public struct Prah
    {
        public static readonly Prah Nezmereno = new Prah();
        public static readonly Prah NeslysiPrah = new Prah(true, 0);

        Prah(bool neslysi, int db)
        {
            Zmereno = true;
            Neslysi = neslysi;
            Db = db;
        }

        public static Prah Hodnota(int db)
        {
            return new Prah(false, db);
        }

        public bool Zmereno { get; }
        public bool Neslysi { get; }
        public int Db { get; }                          //plati jen kdyz Zmereno a ne Neslysi

        public override string ToString()
        {
            return !Zmereno ? "" : (Neslysi ? "NR" : Db.ToString());
        }
    }

    #endregion

    #region Tympanometrie (tymp)

    public class Tympanometrie
    {
        public bool Legacy { get; set; }
        public string Pristroj { get; set; }
        public TympUcho R { get; set; }                 //null = ucho bez dat
        public TympUcho L { get; set; }
    }

    public class TympUcho
    {
        public List<Tympanogram> Tympanogramy { get; } = new List<Tympanogram>(); //tg
        public List<Reflex> Ipsi { get; } = new List<Reflex>();
        public List<Reflex> Contra { get; } = new List<Reflex>();
        public List<Decay> DecayIpsi { get; } = new List<Decay>();
        public List<Decay> DecayContra { get; } = new List<Decay>();
        public List<EtfTympanogram> Etf { get; } = new List<EtfTympanogram>();
        public EtfPerforovany EtfPerf { get; set; }
    }

    /// <summary>
    /// tympanogram; poddajnost a objem u sondy 226 Hz v ml, u 1000 Hz v mmho; Poddajnost je hodnota pristroje
    /// vcetne objemu zvukovodu (graf Fowleru ukazuje Poddajnost - ObjemZvukovodu)
    /// </summary>
    public class Tympanogram
    {
        public int? SondaHz { get; set; }               //226 nebo 1000; null = hodnoty zapsane rucne v panelu (bez krivky)
        public int? VrcholTlak { get; set; }            //peakP [daPa]
        public decimal? VrcholPoddajnost { get; set; }  //peakY
        public decimal? ObjemZvukovodu { get; set; }    //ecv
        public int? GradientDaPa { get; set; }          //gradP
        public decimal? GradientMl { get; set; }        //gradY (tympanometr udava gradient v ml)
        public List<int> Tlak { get; } = new List<int>();               //p [daPa]
        public List<decimal> Poddajnost { get; } = new List<decimal>(); //y
        public bool MaKrivku { get { return Tlak.Count > 0; } }
    }

    /// <summary>podnet reflexu: frekvence [Hz], nebo sum "LP", "HP", "WB"</summary>
    public class Podnet : IEquatable<Podnet>
    {
        public int Hz { get; set; }                     //0 u sumu
        public string Sum { get; set; }                 //null u tonu

        public static Podnet Ton(int hz) { return new Podnet { Hz = hz }; }
        public static Podnet Sumovy(string sum) { return new Podnet { Sum = sum }; }
        public bool JeSum { get { return Sum != null; } }

        public bool Equals(Podnet jiny) { return jiny != null && jiny.Hz == Hz && jiny.Sum == Sum; }
        public override bool Equals(object obj) { return Equals(obj as Podnet); }
        public override int GetHashCode() { return JeSum ? Sum.GetHashCode() : Hz; }
        public override string ToString() { return JeSum ? Sum : Hz + " Hz"; }
    }

    public class Reflex
    {
        public Podnet Podnet { get; set; }
        public int? Tlak { get; set; }                  //tlak v uchu behem mereni [daPa]
        public string Vysledek { get; set; }            //"+", "-" nebo null - z panelu Fowleru (zadal uzivatel nebo pristroj)
        public string Zdroj { get; set; }               //"dev" = pristroj, "user" = uzivatel
        public List<ReflexIntenzita> Intenzity { get; } = new List<ReflexIntenzita>();
    }

    /// <summary>zaznam jedne intenzity podnetu: Cas je poloha v zaznamu 0-1000 (u MAICO ms), Zmena poddajnosti v ml (kladna = pokles poddajnosti)</summary>
    public class ReflexIntenzita
    {
        public int Db { get; set; }
        public List<int> Cas { get; } = new List<int>();        //t
        public List<decimal> Zmena { get; } = new List<decimal>(); //y
        public bool? NalezenPristrojem { get; set; }    //found - jen kdyz vysledek posila pristroj
    }

    public class Decay
    {
        public Podnet Podnet { get; set; }
        public int? Tlak { get; set; }
        public int? Db { get; set; }
        public int? Procent { get; set; }               //decay v % z pristroje (null = pristroj hodnotu nedal)
        public List<int> Cas { get; } = new List<int>();        //0-10000
        public List<decimal> Zmena { get; } = new List<decimal>();
    }

    public class EtfTympanogram
    {
        public int? TlakTestu { get; set; }             //press [daPa]
        public decimal? ObjemZvukovodu { get; set; }
        public List<int> Tlak { get; } = new List<int>();
        public List<decimal> Poddajnost { get; } = new List<decimal>();
    }

    public class EtfPerforovany
    {
        public List<int> Cas { get; } = new List<int>();        //ms
        public List<int> Tlak { get; } = new List<int>();       //daPa
    }

    #endregion

    #region Slovni audiometrie (speech)

    public class SlovniAudiometrie
    {
        public bool Legacy { get; set; }
        public string Pristroj { get; set; }
        public string Poznamka { get; set; }
        public SlovniKrivky BezSluchadla { get; } = new SlovniKrivky();
        public SlovniKrivky SeSluchadlem { get; } = new SlovniKrivky(); //aided
    }

    public class SlovniKrivky
    {
        public SlovniKrivka R { get; set; }             //null = krivka nebyla zmerena
        public SlovniKrivka L { get; set; }
        public SlovniKrivka Bin { get; set; }           //binauralne
    }

    /// <summary>body krivky serazene podle intenzity</summary>
    public class SlovniKrivka
    {
        public List<SlovniBod> Body { get; } = new List<SlovniBod>();
    }

    public class SlovniBod
    {
        public int Db { get; set; }                     //intenzita podnetu [dB]
        public int Procent { get; set; }                //srozumitelnost [%]
        public bool Maskovano { get; set; }
        public int? MaskovaniDb { get; set; }           //uroven maskovani, null = neuvedena
    }

    #endregion
}
