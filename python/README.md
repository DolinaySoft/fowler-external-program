# evaluator – šablona v Pythonu

Externí program pro Fowler v Pythonu 3.8 a novějším, jen se standardní knihovnou. Dělá totéž jako verze v C#
a dává stejné odpovědi. Odpovědnost za hodnocení a celkový postup jsou v
[README v kořeni repozitáře](../README.md), formát v [docs/format.md](../docs/format.md).

## Spuštění

Ruční spuštění, např. na vzorových datech:

```
python evaluator.py ..\priklady\audio.json odpoved.json
```

Průběh se zapisuje do `evaluator.log` vedle `evaluator.py` (bez osobních údajů).

**Ve Fowleru:** Nastavení programu → Extra... → Externí program pro hodnocení měření:

- program: `pythonw.exe` z instalace Pythonu, např. `C:\Program Files\Python312\pythonw.exe`
  (`pythonw.exe` neotevírá okno konzole),
- parametry: `"C:\Fowler\evaluator-python\evaluator.py" "%VSTUP%" "%VYSTUP%"`. Cestu k `evaluator.py` uveďte
  celou: Fowler spouští program ve složce interpretu a relativní cesty v parametrech nepřevádí.

## Struktura

```
evaluator.py         průběh: přečíst vstup → ohodnotit → zapsat odpověď, návratový kód, log
model.py             předaná data (Vstup, Pacient, TonovaAudiometrie...) a výsledek (Vysledek, VysledekAudia...)
cteni_vstupu.py      JSON fowler-exam → model.Vstup
zapis_odpovedi.py    model.Vysledek → JSON fowler-eval (úprava na tvar polí Fowleru)
hodnotitel.py        základní třída Hodnotitel: hodnot_audio, hodnot_tympano, hodnot_slovni (nic neurčí)
vlastni/             vlastní hodnocení pracoviště (není v gitu; použije se automaticky, když existuje)
```

## Jak doplnit hodnocení

Základní třída `Hodnotitel` (`hodnotitel.py`) má pro každou část vyšetření jednu metodu. Žádná z nich nic neurčí:

| metoda | dostane | vrátí (pole ve Fowleru) |
|---|---|---|
| `hodnot_audio` | `TonovaAudiometrie` – všechna předaná data audiogramu | `VysledekAudia` (okno Hodnotit audiogramu) |
| `hodnot_tympano` | `Tympanometrie` – všechna předaná data tympanometrie | `VysledekTympanometrie` (okno Hodnotit tympanometrie) |
| `hodnot_slovni` | `SlovniAudiometrie` – všechna předaná data slovní audiometrie | `VysledekSlovni` (SRT ve slovní audiometrii) |

Každá metoda navíc dostane údaje pacienta (`Pacient`, mj. věk v den vyšetření a pohlaví) a seznam `poznamky`
pro zprávu uživateli. Data i výsledky jsou popsané v `model.py`, význam a rozsahy v
[docs/format.md](../docs/format.md).

Vytvořte balíček `vlastni` (složka `vlastni` vedle `evaluator.py`) se souborem `__init__.py`. V něm napište
potomka třídy `Hodnotitel`, přepište jen metody částí, které chcete hodnotit, a funkcí `pripoj()` ho vraťte:

```python
# vlastni/__init__.py
from hodnotitel import Hodnotitel
from model import VysledekAudia


class MujHodnotitel(Hodnotitel):
    def hodnot_audio(self, audio, pacient, poznamky):
        vysledek = VysledekAudia()
        # postup pracoviště: audio.R a audio.L obsahují všechny předané hodnoty ucha (vzduch, kost, ucl, mcl,
        # sisi...) jako slovník podle frekvence, pacient.vek_roky je věk v den vyšetření
        return vysledek


def pripoj():
    return MujHodnotitel()
```

- Metoda, kterou potomek nepřepíše, vrátí `None`: program tuto část nehodnotí, odpověď to uvede v poznámce
  (např. „Program tympanometrii nehodnotí.“) a Fowler do okna nic nevloží.
- Bez balíčku `vlastni` se použije základní třída. Program pak funguje dál, jen nic neurčí. Kterou třídu použil,
  zapíše `evaluator.log`.
- Údaj, který nelze nebo se nemá určit, nechte `None`, pole v okně Fowleru pak zůstane, jak je. Poznámka pro
  uživatele: `poznamky.append("...")`.
- Výjimka při hodnocení se nepřenese do Fowleru jako chyba: odpověď se zapíše bez hodnocení a s poznámkou
  „Hodnocení se nepodařilo: …“, podrobnosti jsou v `evaluator.log`.
- Čísla mohou být `int`, `float` nebo `Decimal` a není nutné je zaokrouhlovat, `zapis_odpovedi.py` je upraví
  na tvar polí Fowleru. Hodnota mimo rozsah pole se vynechá a uvede v poznámce.
- Desetinná čísla ve vstupu (ml, mmho) jsou `Decimal`. Práh sluchu je `Prah(neslysi, db)`, frekvence bez hodnoty
  ve slovníku není (`ucho.vzduch.get(500)` vrátí `None`).
