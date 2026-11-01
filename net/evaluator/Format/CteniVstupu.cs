using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Evaluator.Model;

namespace Evaluator.Format
{
    /// <summary>
    /// Cteni predanych dat (fmt "fowler-exam", popis v docs/format.md) do tridy Model.Vstup.
    /// Pri doplnovani hodnoceni neni treba menit - hodnoceni pracuje jen s modelem.
    /// </summary>
    public static class CteniVstupu
    {
        public const string Fmt = "fowler-exam";
        public const int Verze = 1;     //nejvyssi verze formatu, kterou evaluator zna

        public static Vstup Precti(string soubor)
        {
            string text;
            try
            {
                text = File.ReadAllText(soubor, new UTF8Encoding(false, true));
            }
            catch (DecoderFallbackException ex)
            {
                throw new ChybaDatException("soubor není v kódování UTF-8", ex);
            }
            return PrectiText(text);
        }

        public static Vstup PrectiText(string json)
        {
            object koren;
            try
            {
                koren = Json.Serializer().DeserializeObject(json);
            }
            catch (ArgumentException ex)
            {
                throw new ChybaDatException("neplatný JSON: " + ex.Message, ex);
            }
            IDictionary<string, object> data = Json.Objekt(koren, "data");
            string fmt = Json.Text(data, "fmt", "");
            if (fmt != Fmt)
                throw new NepodporovanyFormatException($"data nejsou ve formátu {Fmt} (fmt: {fmt ?? "chybí"})");
            int? verze = Json.CeleCislo(data, "v", "");
            if (verze == null || verze < 1)
                throw new ChybaDatException("chybí verze formátu (v)");
            if (verze > Verze)
                throw new NepodporovanyFormatException($"data mají novější verzi formátu ({verze}), než evaluator zná ({Verze})");

            Vstup vstup = new Vstup();
            vstup.App = Json.Text(data, "app", "");
            vstup.Vytvoreno = Cas(Json.Text(data, "created", ""));
            CtiPacienta(Json.ObjektPolozka(data, "patient", ""), vstup.Pacient);
            CtiVysetreni(Json.ObjektPolozka(data, "exam", ""), vstup);
            IDictionary<string, object> cast;
            if ((cast = Json.ObjektPolozka(data, "audio", "")) != null)
                vstup.Audio = CtiAudio(cast);
            if ((cast = Json.ObjektPolozka(data, "tymp", "")) != null)
                vstup.Tymp = CtiTympanometrii(cast);
            if ((cast = Json.ObjektPolozka(data, "speech", "")) != null)
                vstup.Slovni = CtiSlovni(cast);
            if (vstup.Audio == null && vstup.Tymp == null && vstup.Slovni == null)
                throw new ChybaDatException("data neobsahují žádnou část vyšetření (audio, tymp, speech)");
            return vstup;
        }

        //cas "2026-10-28T09:42:00" a datum "1957-04-15"
        static DateTime? Cas(string text)
        {
            DateTime cas;
            if (text != null && DateTime.TryParseExact(text, new[] { Json.FormatCasu, "yyyy-MM-dd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out cas))
                return cas;
            return null;
        }

        static void CtiPacienta(IDictionary<string, object> o, Pacient pacient)
        {
            if (o == null)
                return;
            pacient.Pid = Json.Text(o, "pid", "patient");
            pacient.Rc = Json.Text(o, "rc", "patient");
            pacient.Jmeno = Json.Text(o, "firstName", "patient");
            pacient.Prijmeni = Json.Text(o, "lastName", "patient");
            pacient.DatumNarozeni = Cas(Json.Text(o, "birthDate", "patient"));
            pacient.Pojistovna = Json.Text(o, "insurance", "patient");
        }

        //exam: cas vysetreni do vstupu, vek v den vysetreni a pohlavi k pacientovi; cas musi byt presne ve tvaru
        //FormatCasu, aby ho odpoved zopakovala tak, jak byl predan (Fowler ho porovnava jako text)
        static void CtiVysetreni(IDictionary<string, object> o, Vstup vstup)
        {
            string text = Json.Text(o, "time", "exam");
            DateTime cas;
            if (string.IsNullOrEmpty(text))
                throw new ChybaDatException("chybí čas vyšetření (exam.time)");
            if (!DateTime.TryParseExact(text, Json.FormatCasu, CultureInfo.InvariantCulture, DateTimeStyles.None, out cas))
                throw new ChybaDatException($"exam.time: neplatný čas vyšetření \"{text}\" (očekáváno RRRR-MM-DDThh:mm:ss)");
            vstup.CasVysetreni = cas;
            IDictionary<string, object> vek = Json.ObjektPolozka(o, "age", "exam");
            if (vek != null)
            {
                vstup.Pacient.VekRoky = Json.CeleCislo(vek, "y", "exam.age");
                vstup.Pacient.VekMesice = Json.CeleCislo(vek, "m", "exam.age");
            }
            vstup.Pacient.Pohlavi = Json.Text(o, "sex", "exam");
        }

        #region Tonova audiometrie

        static TonovaAudiometrie CtiAudio(IDictionary<string, object> o)
        {
            TonovaAudiometrie audio = new TonovaAudiometrie();
            audio.Legacy = Json.Logicka(o, "legacy", "audio");
            audio.Pristroj = Json.Text(o, "device", "audio");
            audio.Poznamka = Json.Text(o, "note", "audio");
            audio.Hz.AddRange(Json.CelaCisla(o, "hz", "audio"));
            audio.R = CtiAudioUcho(Json.ObjektPolozka(o, "R", "audio"), audio.Hz, "audio.R");
            audio.L = CtiAudioUcho(Json.ObjektPolozka(o, "L", "audio"), audio.Hz, "audio.L");
            return audio;
        }

        static AudioUcho CtiAudioUcho(IDictionary<string, object> o, List<int> hz, string cesta)
        {
            if (o == null)
                return null;
            AudioUcho ucho = new AudioUcho();
            CtiPrahy(o, "ac", hz, cesta, ucho.Vzduch);
            ucho.VzduchMaskovan = Json.Logicka(o, "acMasked", cesta);
            CtiHodnoty(o, "acMask", hz, cesta, ucho.VzduchMaskovani);
            CtiPrahy(o, "bc", hz, cesta, ucho.Kost);
            CtiHodnoty(o, "bcMask", hz, cesta, ucho.KostMaskovani);
            CtiHodnoty(o, "ucl", hz, cesta, ucho.Ucl);
            CtiHodnoty(o, "mcl", hz, cesta, ucho.Mcl);
            CtiHodnoty(o, "sisi", hz, cesta, ucho.Sisi);
            return ucho;
        }

        //krivka prahu podle hz: cislo [dB], "NR" = neslysi, null = nezmereno
        static void CtiPrahy(IDictionary<string, object> o, string klic, List<int> hz, string cesta, SortedDictionary<int, Prah> prahy)
        {
            object[] pole = Json.PolePolozka(o, klic, cesta);
            if (pole == null)
                return;
            Json.StejnaDelka(pole.Length, hz.Count, $"{cesta}.{klic} a hz");
            for (int i = 0; i < pole.Length; i++)
            {
                if (pole[i] == null)
                    continue;
                if (pole[i] as string == "NR")
                    prahy[hz[i]] = Prah.NeslysiPrah;
                else
                    prahy[hz[i]] = Prah.Hodnota(Json.CeleCislo(pole[i], $"{cesta}.{klic}[{i}]"));
            }
        }

        static void CtiHodnoty(IDictionary<string, object> o, string klic, List<int> hz, string cesta, SortedDictionary<int, int> hodnoty)
        {
            object[] pole = Json.PolePolozka(o, klic, cesta);
            if (pole == null)
                return;
            Json.StejnaDelka(pole.Length, hz.Count, $"{cesta}.{klic} a hz");
            for (int i = 0; i < pole.Length; i++)
            {
                if (pole[i] != null)
                    hodnoty[hz[i]] = Json.CeleCislo(pole[i], $"{cesta}.{klic}[{i}]");
            }
        }

        #endregion

        #region Tympanometrie

        static Tympanometrie CtiTympanometrii(IDictionary<string, object> o)
        {
            Tympanometrie tymp = new Tympanometrie();
            tymp.Legacy = Json.Logicka(o, "legacy", "tymp");
            tymp.Pristroj = Json.Text(o, "device", "tymp");
            tymp.R = CtiTympUcho(Json.ObjektPolozka(o, "R", "tymp"), "tymp.R");
            tymp.L = CtiTympUcho(Json.ObjektPolozka(o, "L", "tymp"), "tymp.L");
            return tymp;
        }

        static TympUcho CtiTympUcho(IDictionary<string, object> o, string cesta)
        {
            if (o == null)
                return null;
            TympUcho ucho = new TympUcho();
            object[] pole;
            int i;
            if ((pole = Json.PolePolozka(o, "tg", cesta)) != null)
            {
                for (i = 0; i < pole.Length; i++)
                    ucho.Tympanogramy.Add(CtiTympanogram(Json.Objekt(pole[i], $"{cesta}.tg[{i}]"), $"{cesta}.tg[{i}]"));
            }
            CtiReflexy(o, "ipsi", cesta, ucho.Ipsi);
            CtiReflexy(o, "contra", cesta, ucho.Contra);
            CtiDecay(o, "decayIpsi", cesta, ucho.DecayIpsi);
            CtiDecay(o, "decayContra", cesta, ucho.DecayContra);
            if ((pole = Json.PolePolozka(o, "etf", cesta)) != null)
            {
                for (i = 0; i < pole.Length; i++)
                {
                    string c = $"{cesta}.etf[{i}]";
                    IDictionary<string, object> e = Json.Objekt(pole[i], c);
                    EtfTympanogram etf = new EtfTympanogram();
                    etf.TlakTestu = Json.CeleCislo(e, "press", c);
                    etf.ObjemZvukovodu = Json.Cislo(e, "ecv", c);
                    etf.Tlak.AddRange(Json.CelaCisla(e, "p", c));
                    etf.Poddajnost.AddRange(Json.Cisla(e, "y", c));
                    Json.StejnaDelka(etf.Tlak.Count, etf.Poddajnost.Count, c + ".p a y");
                    ucho.Etf.Add(etf);
                }
            }
            IDictionary<string, object> perf = Json.ObjektPolozka(o, "etfPerf", cesta);
            if (perf != null)
            {
                ucho.EtfPerf = new EtfPerforovany();
                ucho.EtfPerf.Cas.AddRange(Json.CelaCisla(perf, "t", cesta + ".etfPerf"));
                ucho.EtfPerf.Tlak.AddRange(Json.CelaCisla(perf, "p", cesta + ".etfPerf"));
                Json.StejnaDelka(ucho.EtfPerf.Cas.Count, ucho.EtfPerf.Tlak.Count, cesta + ".etfPerf.t a p");
            }
            return ucho;
        }

        static Tympanogram CtiTympanogram(IDictionary<string, object> o, string cesta)
        {
            Tympanogram tg = new Tympanogram();
            tg.SondaHz = Json.CeleCislo(o, "probeHz", cesta);
            tg.VrcholTlak = Json.CeleCislo(o, "peakP", cesta);
            tg.VrcholPoddajnost = Json.Cislo(o, "peakY", cesta);
            tg.ObjemZvukovodu = Json.Cislo(o, "ecv", cesta);
            tg.GradientDaPa = Json.CeleCislo(o, "gradP", cesta);
            tg.GradientMl = Json.Cislo(o, "gradY", cesta);
            tg.Tlak.AddRange(Json.CelaCisla(o, "p", cesta));
            tg.Poddajnost.AddRange(Json.Cisla(o, "y", cesta));
            Json.StejnaDelka(tg.Tlak.Count, tg.Poddajnost.Count, cesta + ".p a y");
            return tg;
        }

        //podnet: frekvence v Hz, nebo sum "LP", "HP", "WB"
        static Podnet CtiPodnet(IDictionary<string, object> o, string cesta)
        {
            object stim = Json.Polozka(o, "stim");
            string sum = stim as string;
            if (sum == "LP" || sum == "HP" || sum == "WB")
                return Podnet.Sumovy(sum);
            if (stim != null && Json.JeCislo(stim))
                return Podnet.Ton(Json.CeleCislo(stim, cesta + ".stim"));
            throw new ChybaDatException($"{cesta}.stim: očekávána frekvence nebo \"LP\", \"HP\", \"WB\"");
        }

        static void CtiReflexy(IDictionary<string, object> o, string klic, string cesta, List<Reflex> reflexy)
        {
            object[] pole = Json.PolePolozka(o, klic, cesta);
            for (int i = 0; pole != null && i < pole.Length; i++)
            {
                string c = $"{cesta}.{klic}[{i}]";
                IDictionary<string, object> r = Json.Objekt(pole[i], c);
                Reflex reflex = new Reflex();
                reflex.Podnet = CtiPodnet(r, c);
                reflex.Tlak = Json.CeleCislo(r, "p", c);
                reflex.Vysledek = Json.Text(r, "res", c);
                reflex.Zdroj = Json.Text(r, "src", c);
                object[] intenzity = Json.PolePolozka(r, "lv", c);
                for (int k = 0; intenzity != null && k < intenzity.Length; k++)
                {
                    string ci = $"{c}.lv[{k}]";
                    IDictionary<string, object> lv = Json.Objekt(intenzity[k], ci);
                    ReflexIntenzita intenzita = new ReflexIntenzita();
                    intenzita.Db = Json.CeleCislo(lv, "db", ci) ?? 0;
                    intenzita.Cas.AddRange(Json.CelaCisla(lv, "t", ci));
                    intenzita.Zmena.AddRange(Json.Cisla(lv, "y", ci));
                    Json.StejnaDelka(intenzita.Cas.Count, intenzita.Zmena.Count, ci + ".t a y");
                    if (Json.Polozka(lv, "found") != null)
                        intenzita.NalezenPristrojem = Json.Logicka(lv, "found", ci);
                    reflex.Intenzity.Add(intenzita);
                }
                reflexy.Add(reflex);
            }
        }

        static void CtiDecay(IDictionary<string, object> o, string klic, string cesta, List<Decay> seznam)
        {
            object[] pole = Json.PolePolozka(o, klic, cesta);
            for (int i = 0; pole != null && i < pole.Length; i++)
            {
                string c = $"{cesta}.{klic}[{i}]";
                IDictionary<string, object> d = Json.Objekt(pole[i], c);
                Decay decay = new Decay();
                decay.Podnet = CtiPodnet(d, c);
                decay.Tlak = Json.CeleCislo(d, "p", c);
                decay.Db = Json.CeleCislo(d, "db", c);
                decay.Procent = Json.CeleCislo(d, "pct", c);
                decay.Cas.AddRange(Json.CelaCisla(d, "t", c));
                decay.Zmena.AddRange(Json.Cisla(d, "y", c));
                Json.StejnaDelka(decay.Cas.Count, decay.Zmena.Count, c + ".t a y");
                seznam.Add(decay);
            }
        }

        #endregion

        #region Slovni audiometrie

        static SlovniAudiometrie CtiSlovni(IDictionary<string, object> o)
        {
            SlovniAudiometrie slovni = new SlovniAudiometrie();
            slovni.Legacy = Json.Logicka(o, "legacy", "speech");
            slovni.Pristroj = Json.Text(o, "device", "speech");
            slovni.Poznamka = Json.Text(o, "note", "speech");
            CtiKrivky(o, "speech", slovni.BezSluchadla);
            IDictionary<string, object> aided = Json.ObjektPolozka(o, "aided", "speech");
            if (aided != null)
                CtiKrivky(aided, "speech.aided", slovni.SeSluchadlem);
            return slovni;
        }

        static void CtiKrivky(IDictionary<string, object> o, string cesta, SlovniKrivky krivky)
        {
            krivky.R = CtiKrivku(Json.ObjektPolozka(o, "R", cesta), cesta + ".R");
            krivky.L = CtiKrivku(Json.ObjektPolozka(o, "L", cesta), cesta + ".L");
            krivky.Bin = CtiKrivku(Json.ObjektPolozka(o, "bin", cesta), cesta + ".bin");
        }

        static SlovniKrivka CtiKrivku(IDictionary<string, object> o, string cesta)
        {
            if (o == null)
                return null;
            List<int> db = Json.CelaCisla(o, "db", cesta);
            List<int> pct = Json.CelaCisla(o, "pct", cesta);
            Json.StejnaDelka(db.Count, pct.Count, cesta + ".db a pct");
            object[] mask = Json.PolePolozka(o, "mask", cesta);
            if (mask != null)
                Json.StejnaDelka(db.Count, mask.Length, cesta + ".db a mask");
            SlovniKrivka krivka = new SlovniKrivka();
            for (int i = 0; i < db.Count; i++)
            {
                SlovniBod bod = new SlovniBod { Db = db[i], Procent = pct[i] };
                object m = (mask == null) ? null : mask[i];
                if (m != null)
                {
                    bod.Maskovano = true;
                    if (!(m as string == "M"))
                        bod.MaskovaniDb = Json.CeleCislo(m, $"{cesta}.mask[{i}]");
                }
                krivka.Body.Add(bod);
            }
            krivka.Body.Sort((a, b) => a.Db.CompareTo(b.Db));
            return krivka;
        }

        #endregion
    }
}
