using System.Windows;

namespace Evaluator
{
    /// <summary>
    /// WPF aplikace bez okna: hodnoceni probehne pri startu a program skonci s navratovym kodem (Beh.Spust).
    /// Okno se ukaze jen pri spusteni bez parametru (napr. poklepanim), aby bylo zrejme, k cemu program je.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (e.Args.Length == 0)
            {
                MessageBox.Show(Beh.Nazev() + " - program pro hodnocení měření pro Fowler.\n\n" +
                    "Spouští ho Fowler tlačítkem Externí program... v oknech Hodnotit a slovní audiometrie.\n" +
                    "Ve Fowleru: Nastavení programu, tlačítko Extra..., program = cesta k evaluator.exe, parametry prázdné.\n\n" +
                    "Ruční spuštění: evaluator.exe \"vstupní soubor\" \"výstupní soubor\"",
                    "evaluator", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown(Beh.KodParametry);
                return;
            }
            Shutdown(Beh.Spust(e.Args));
        }
    }
}
