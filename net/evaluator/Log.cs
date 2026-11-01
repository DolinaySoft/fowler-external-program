using System;
using System.IO;

namespace Evaluator
{
    /// <summary>
    /// zaznam prubehu do evaluator.log ve slozce programu (bez osobnich udaju); nad 1 MB se prejmenuje na .old
    /// </summary>
    static class Log
    {
        static readonly string soubor = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "evaluator.log");

        public static void Zapis(string zprava)
        {
            try
            {
                FileInfo info = new FileInfo(soubor);
                if (info.Exists && info.Length > 1024 * 1024)
                {
                    File.Copy(soubor, soubor + ".old", true);
                    File.Delete(soubor);
                }
                File.AppendAllText(soubor, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {zprava}{Environment.NewLine}");
            }
            catch (Exception)
            {
                //log neni nutny pro funkci (napr. slozka jen pro cteni)
            }
        }
    }
}
