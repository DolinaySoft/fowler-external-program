# evaluator – šablona v C#

Externí program pro Fowler v C# (.NET Framework 4.8, Visual Studio 2022, projekt WPF bez okna). Bez dalších
knihoven: JSON čte a zapisuje `System.Web.Extensions` (JavaScriptSerializer). Odpovědnost za hodnocení a celkový
postup jsou v [README v kořeni repozitáře](../README.md), formát v [docs/format.md](../docs/format.md).

## Překlad a použití ve Fowleru

1. Přeložte `evaluator.sln` (Release).
2. Zkopírujte obsah `evaluator\bin\Release` (`evaluator.exe`, `evaluator.exe.config` a případné soubory `.config`
   vlastního hodnocení) do složky, např. `C:\Fowler\evaluator\`.
3. Ve Fowleru: **Nastavení programu → Extra... → Externí program pro hodnocení měření**:
   - program: cesta k `evaluator.exe` (relativní cesta je vůči složce Fowleru, např. `evaluator\evaluator.exe`),
   - parametry: prázdné (= `"%VSTUP%" "%VYSTUP%"`),
   - případně „Spouštět hned po stisku Hodnotit“.

Ruční spuštění, např. na vzorových datech:

```
evaluator.exe ..\..\..\..\priklady\audio.json odpoved.json
```

Spuštěný bez parametrů program jen ukáže informaci o sobě. Průběh zapisuje do `evaluator.log` ve své složce.

## Struktura

```
evaluator.sln
evaluator/
  App.xaml, App.xaml.cs      start bez okna, návratový kód
  Beh.cs                     průběh: přečíst vstup → ohodnotit → zapsat odpověď
  Log.cs                     evaluator.log ve složce programu (bez osobních údajů)
  Model/
    Vstup.cs                 předaná data (čas vyšetření, pacient, audio, tympanometrie, slovní)
    Vysledek.cs              výsledek hodnocení = pole oken Fowleru
  Format/                    formát souborů
    CteniVstupu.cs           JSON fowler-exam → Model.Vstup
    ZapisOdpovedi.cs         Model.Vysledek → JSON fowler-eval (úprava na tvar polí Fowleru)
    Json.cs                  pomocné funkce pro JSON
  Hodnoceni/
    Hodnotitel.cs            základní třída hodnocení: HodnotAudio, HodnotTympano, HodnotSlovni (nic neurčí)
  Vlastni/                   vlastní hodnocení pracoviště (není v gitu; překládá se automaticky, když existuje)
```

## Jak doplnit hodnocení

Základní třída `Hodnotitel` (`Hodnoceni/Hodnotitel.cs`) má pro každou část vyšetření jednu metodu. Žádná z nich
nic neurčí:

| metoda | dostane | vrátí (pole ve Fowleru) |
|---|---|---|
| `HodnotAudio` | `TonovaAudiometrie` – všechna předaná data audiogramu | `VysledekAudia` (okno Hodnotit audiogramu) |
| `HodnotTympano` | `Tympanometrie` – všechna předaná data tympanometrie | `VysledekTympanometrie` (okno Hodnotit tympanometrie) |
| `HodnotSlovni` | `SlovniAudiometrie` – všechna předaná data slovní audiometrie | `VysledekSlovni` (SRT ve slovní audiometrii) |

Každá metoda navíc dostane údaje pacienta (`Pacient`, mj. věk v den vyšetření a pohlaví) a seznam `poznamky`
pro zprávu uživateli. Data jsou popsaná v `Model/Vstup.cs`, výsledky v `Model/Vysledek.cs`, význam a rozsahy
v [docs/format.md](../docs/format.md).

1. Ve složce `evaluator/Vlastni` (nebo kdekoli v projektu) vytvořte potomka třídy `Hodnotitel` a přepište
   (`override`) jen metody částí, které chcete hodnotit. Údaj, který nelze nebo se nemá určit, nechte `null`,
   pole v okně Fowleru pak zůstane, jak je.
2. Připojte ho do programu metodou `Pripoj`:

```csharp
using System.Collections.Generic;
using Evaluator.Model;

namespace Evaluator.Hodnoceni
{
    public class MujHodnotitel : Hodnotitel
    {
        public override VysledekAudia HodnotAudio(TonovaAudiometrie audio, Pacient pacient, List<string> poznamky)
        {
            VysledekAudia vysledek = new VysledekAudia();
            //postup pracoviště: audio.R a audio.L obsahují všechny předané hodnoty ucha (Vzduch, Kost, Ucl, Mcl,
            //Sisi...), pacient.VekRoky je věk v den vyšetření
            return vysledek;
        }
    }

    public partial class Hodnotitel
    {
        static partial void Pripoj(ref Hodnotitel hodnotitel)
        {
            hodnotitel = new MujHodnotitel();
        }
    }
}
```

- Metoda, kterou potomek nepřepíše, vrátí `null`: program tuto část nehodnotí, odpověď to uvede v poznámce
  (např. „Program tympanometrii nehodnotí.“) a Fowler do okna nic nevloží.
- Dokud program metodu `Pripoj` nemá (nebo když potomka i s jeho soubory odstraníte), použije se základní třída.
  Program pak funguje dál, jen nic neurčí. Kterou třídu použil, zapíše `evaluator.log`.
- Soubory `.cs` ve složce `Vlastni` se přeloží automaticky, soubory `.config` se zkopírují k programu (např. pro
  vlastní nastavení, které si hodnocení přečte samo). `evaluator.exe.config` obsahuje jen nastavení .NET.
- Zbytek programu (`Format`, `Model`, `Beh.cs`) při doplňování hodnocení není třeba měnit.
- Poznámka pro uživatele: `poznamky.Add("...")`. Fowler poznámky zobrazí po vložení odpovědi.
- Výjimka při hodnocení se nepřenese do Fowleru jako chyba: odpověď se zapíše bez hodnocení a s poznámkou
  „Hodnocení se nepodařilo: …“, podrobnosti jsou v `evaluator.log`.
- Hodnoty není nutné zaokrouhlovat na tvar polí Fowleru, `ZapisOdpovedi` je upraví (audiogram nejvýše
  2 desetinná místa a 5 znaků, SRT 1 desetinné místo). Hodnota mimo rozsah pole se vynechá a uvede v poznámce,
  aby Fowler neodmítl celou odpověď.
- Čas vyšetření je `Vstup.CasVysetreni`, věk v den vyšetření a pohlaví jsou u pacienta (`Pacient.VekRoky`,
  `VekMesice`, `Pohlavi`).
