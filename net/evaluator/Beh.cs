using System;
using System.Reflection;
using Evaluator.Format;
using Evaluator.Hodnoceni;
using Evaluator.Model;

namespace Evaluator
{
    /// <summary>
    /// Prubeh programu: evaluator.exe "vstupni soubor" "vystupni soubor" (Fowler predava "%VSTUP%" "%VYSTUP%").
    /// Precte predana data, ohodnoti je (Hodnoceni/Hodnotitel.cs) a zapise odpoved. Navratovy kod
    /// 0 = odpoved je zapsana; jiny kod Fowler ohlasi a nic nenacte. Chyba pri hodnoceni se oznami v odpovedi
    /// (note), aby ji uzivatel videl.
    /// </summary>
    public static class Beh
    {
        public const int KodOk = 0;
        public const int KodParametry = 1;      //chybi vstupni nebo vystupni soubor
        public const int KodVstup = 2;          //vstupni soubor nelze precist nebo neobsahuje platna data
        public const int KodFormat = 3;         //data v jinem formatu nebo novejsi verzi, nez evaluator zna
        public const int KodZapis = 4;          //odpoved nelze zapsat

        public static string Nazev()
        {
            Version verze = Assembly.GetExecutingAssembly().GetName().Version;
            return $"evaluator {verze.Major}.{verze.Minor}.{verze.Build}";
        }

        public static int Spust(string[] parametry)
        {
            if (parametry.Length < 2)
            {
                Log.Zapis("Chybi parametry: evaluator.exe \"vstupni soubor\" \"vystupni soubor\"");
                return KodParametry;
            }
            string souborVstup = parametry[0];
            string souborVystup = parametry[1];

            Vstup vstup;
            try
            {
                vstup = CteniVstupu.Precti(souborVstup);
            }
            catch (NepodporovanyFormatException ex)
            {
                Log.Zapis("Nepodporovany format dat: " + ex.Message);
                return KodFormat;
            }
            catch (Exception ex)
            {
                Log.Zapis("Vstupni data nelze precist: " + ex.Message);
                return KodVstup;
            }
            Log.Zapis($"Data z {vstup.App}: {Casti(vstup)}");

            Vysledek vysledek;
            try
            {
                Hodnotitel hodnotitel = Hodnotitel.Vytvor();
                Log.Zapis("Hodnotitel " + hodnotitel.GetType().FullName);
                vysledek = hodnotitel.Hodnot(vstup);
            }
            catch (Exception ex)
            {
                Log.Zapis("Chyba pri hodnoceni: " + ex);
                vysledek = new Vysledek();
                vysledek.Poznamky.Add("Hodnocení se nepodařilo: " + ex.Message);
            }

            try
            {
                ZapisOdpovedi.Zapis(souborVystup, vstup, vysledek, Nazev());
            }
            catch (Exception ex)
            {
                Log.Zapis("Odpoved nelze zapsat: " + ex.Message);
                return KodZapis;
            }
            Log.Zapis("Odpoved zapsana");
            return KodOk;
        }

        static string Casti(Vstup vstup)
        {
            return (vstup.Audio != null ? "audio " : "") + (vstup.Tymp != null ? "tymp " : "") + (vstup.Slovni != null ? "speech" : "");
        }
    }
}
