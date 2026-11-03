# Pokyny pro AI asistenta

Repozitář je šablona externího programu pro program Fowler (popis v README.md). Program přečte data
vyšetření předaná Fowlerem, předá je hodnocení a zapíše odpověď. Šablona žádné hodnocení neobsahuje. Tvým úkolem
bývá doplnit hodnocení podle postupu, který určí uživatel (lékař nebo vývojář pracoviště).

## Pravidla

- **Postup hodnocení neurčuj sám.** Hranice, tabulky, klasifikace a vzorce musí pocházet od uživatele (postup jeho
  pracoviště). Když v zadání chybí, zeptej se. Nedoplňuj je z obecných znalostí ani nevybírej „obvyklé“ hodnoty.
  Za hodnocení odpovídá pracoviště.
- **Měň jen potomka třídy `Hodnotitel`:**
  - C#: nová třída v `net/evaluator/Vlastni/` (soubory se přeloží samy), připojená metodou
    `static partial void Pripoj(ref Hodnotitel hodnotitel)` v `partial class Hodnotitel` (vzor v
    `net/evaluator/Hodnoceni/Hodnotitel.cs`);
  - Python: balíček `python/vlastni/` s funkcí `pripoj()`, která vrátí instanci potomka (vzor v
    `python/hodnotitel.py`).
  
  Složky `Vlastni`/`vlastni` jsou v `.gitignore`. Má-li být kód veřejný, uživatel je z `.gitignore` odebere.
- **Neměň** čtení vstupu, zápis odpovědi, model ani základní třídu (`net/evaluator/Format`, `Model`, `Beh.cs`,
  `Hodnoceni/Hodnotitel.cs`; `python/cteni_vstupu.py`, `zapis_odpovedi.py`, `model.py`, `hodnotitel.py`,
  `evaluator.py`). Odpovídají formátu, který čte Fowler.
- **Data:** metoda dostane celý blok své části (`TonovaAudiometrie`, `Tympanometrie`, `SlovniAudiometrie`) a pacienta
  (věk v den vyšetření, pohlaví). Popis je v `net/evaluator/Model/Vstup.cs` nebo `python/model.py`, význam a jednotky
  v `docs/format.md`. Používej jen předaná data. Chybějící hodnoty nedoplňuj ani neodhaduj. U `legacy` audiometrie
  může být kostní vedení odhadnuté (viz `docs/format.md`).
- **Výsledek:** pole oken Fowleru (`Model/Vysledek.cs`, `model.py`, rozsahy v `docs/format.md`, část Odpověď).
  - Údaj, který nelze určit, nech `null`/`None` a důvod napiš do `poznamky`.
  - Metoda pro část, kterou program nehodnotí, vrací `null`/`None`, tedy ji nepřepisuj.
  - Zaokrouhlení na tvar polí a kontrolu rozsahů dělá zápis odpovědi.
- **Osobní údaje** (jméno, rodné číslo) nezapisuj do logu ani do poznámek.
- **Vyzkoušej** na `priklady/*.json` (příkazy v `net/README.md`, `python/README.md`) a ukaž uživateli výsledky,
  aby je mohl porovnat se svým hodnocením.
