using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace Evaluator.Format
{
    /// <summary>chyba ve vstupnich datech (neplatny JSON, chybejici nebo spatny udaj)</summary>
    public class ChybaDatException : Exception
    {
        public ChybaDatException(string zprava, Exception vnitrni = null) : base(zprava, vnitrni) { }
    }

    /// <summary>data v jinem formatu nebo novejsi verzi, nez evaluator zna</summary>
    public class NepodporovanyFormatException : Exception
    {
        public NepodporovanyFormatException(string zprava) : base(zprava) { }
    }

    /// <summary>
    /// pristup k JSON nactenemu JavaScriptSerializer (objekt = Dictionary, pole = object[], cislo = int, long nebo
    /// decimal); chyba typu ohlasi i cestu k udaji, napr. "audio.R.ac[3]"
    /// </summary>
    static class Json
    {
        public const string FormatCasu = "yyyy-MM-dd'T'HH:mm:ss";   //cas "2026-10-28T09:42:00" (created, exam.time)

        public static JavaScriptSerializer Serializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 64 };
        }

        public static IDictionary<string, object> Objekt(object hodnota, string cesta)
        {
            IDictionary<string, object> objekt = hodnota as IDictionary<string, object>;
            if (objekt == null)
                throw new ChybaDatException($"{cesta}: očekáván objekt");
            return objekt;
        }

        /// <summary>polozka objektu, null kdyz neni nebo je null</summary>
        public static object Polozka(IDictionary<string, object> objekt, string klic)
        {
            object hodnota;
            if (objekt == null || !objekt.TryGetValue(klic, out hodnota))
                return null;
            return hodnota;
        }

        public static IDictionary<string, object> ObjektPolozka(IDictionary<string, object> objekt, string klic, string cesta)
        {
            object hodnota = Polozka(objekt, klic);
            return (hodnota == null) ? null : Objekt(hodnota, cesta + "." + klic);
        }

        public static object[] PolePolozka(IDictionary<string, object> objekt, string klic, string cesta)
        {
            object hodnota = Polozka(objekt, klic);
            if (hodnota == null)
                return null;
            object[] pole = hodnota as object[];
            if (pole == null)
                throw new ChybaDatException($"{cesta}.{klic}: očekáván seznam");
            return pole;
        }

        public static string Text(IDictionary<string, object> objekt, string klic, string cesta)
        {
            object hodnota = Polozka(objekt, klic);
            if (hodnota == null)
                return null;
            string text = hodnota as string;
            if (text == null)
                throw new ChybaDatException($"{cesta}.{klic}: očekáván text");
            return text;
        }

        public static bool Logicka(IDictionary<string, object> objekt, string klic, string cesta)
        {
            object hodnota = Polozka(objekt, klic);
            if (hodnota == null)
                return false;
            if (!(hodnota is bool))
                throw new ChybaDatException($"{cesta}.{klic}: očekáváno true nebo false");
            return (bool)hodnota;
        }

        public static bool JeCislo(object hodnota)
        {
            return hodnota is int || hodnota is long || hodnota is decimal || hodnota is double;
        }

        public static decimal Cislo(object hodnota, string cesta)
        {
            if (!JeCislo(hodnota))
                throw new ChybaDatException($"{cesta}: očekáváno číslo");
            return Convert.ToDecimal(hodnota, CultureInfo.InvariantCulture);
        }

        public static int CeleCislo(object hodnota, string cesta)
        {
            decimal cislo = Cislo(hodnota, cesta);
            if (cislo != decimal.Truncate(cislo) || cislo < int.MinValue || cislo > int.MaxValue)
                throw new ChybaDatException($"{cesta}: očekáváno celé číslo");
            return (int)cislo;
        }

        public static int? CeleCislo(IDictionary<string, object> objekt, string klic, string cesta)
        {
            object hodnota = Polozka(objekt, klic);
            return (hodnota == null) ? (int?)null : CeleCislo(hodnota, cesta + "." + klic);
        }

        public static decimal? Cislo(IDictionary<string, object> objekt, string klic, string cesta)
        {
            object hodnota = Polozka(objekt, klic);
            return (hodnota == null) ? (decimal?)null : Cislo(hodnota, cesta + "." + klic);
        }

        public static List<int> CelaCisla(IDictionary<string, object> objekt, string klic, string cesta)
        {
            List<int> seznam = new List<int>();
            object[] pole = PolePolozka(objekt, klic, cesta);
            for (int i = 0; pole != null && i < pole.Length; i++)
                seznam.Add(CeleCislo(pole[i], $"{cesta}.{klic}[{i}]"));
            return seznam;
        }

        public static List<decimal> Cisla(IDictionary<string, object> objekt, string klic, string cesta)
        {
            List<decimal> seznam = new List<decimal>();
            object[] pole = PolePolozka(objekt, klic, cesta);
            for (int i = 0; pole != null && i < pole.Length; i++)
                seznam.Add(Cislo(pole[i], $"{cesta}.{klic}[{i}]"));
            return seznam;
        }

        /// <summary>souběžná pole musí mít stejnou délku (pravidlo formátu)</summary>
        public static void StejnaDelka(int delka1, int delka2, string cesta)
        {
            if (delka1 != delka2)
                throw new ChybaDatException($"{cesta}: souběžná pole mají různou délku ({delka1} a {delka2})");
        }
    }
}
