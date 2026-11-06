# Externí program pro Fowler – šablona pro vlastní hodnocení pracoviště

Program **Fowler** (přenos dat z audiometrů a tympanometrů) umí předat uložené vyšetření (tónovou audiometrii,
tympanometrii nebo slovní audiometrii) externímu programu pracoviště a jeho odpověď vložit do okna Hodnotit, kde ji
uživatel zkontroluje.
Tento repozitář obsahuje šablonu takového programu v C# a v Pythonu a popis formátu předávaných dat.

**Šablona neobsahuje žádné hodnocení.** Přečte předaná data, rozdělí je podle části vyšetření a zapíše odpověď.
Samotné hodnocení si doplní pracoviště, a to jen pro části a údaje, které chce.

## Dvě cesty: spouštěč skriptů, nebo vlastní program

Pracoviště bez vývojáře nemusí program psát ani překládat. Hotový externí program
[**spouštěč skriptů**](https://github.com/DolinaySoft/fowler-script-runner) (`spoustec.exe`) vznikl podle této
šablony a pracoviště k němu dodá jen krátké skripty v JavaScriptu, např. s pomocí AI asistenta. Tato šablona je pro
ty, kdo chtějí nebo potřebují vlastní program.

| | [spouštěč skriptů](https://github.com/DolinaySoft/fowler-script-runner) | šablona vlastního programu (tento repozitář) |
|---|---|---|
| pro koho | pracoviště, lékař (i s pomocí AI asistenta) | vývojář pracoviště nebo dodavatel softwaru |
| co pracoviště dodá | jen skripty v JavaScriptu, bez překladu a vývojových nástrojů | vlastní program v C# nebo Pythonu |
| co je hotové | celý program: převzetí dat, kontrola výsledku, testovací režim s protokolem, otisk skriptů v odpovědi | kostra programu: čtení dat a zápis odpovědi |
| volnost | jen data předaná Fowlerem, standardní JavaScript, limity času a paměti | libovolný jazyk a knihovny, napojení na vlastní systémy |
| kdy zvolit | hodnocení chcete naprogramovat co nejjednodušeji | potřebujete víc, než skripty umožní, nebo píšete vlastní implementaci |

Formát předaných dat a odpovědi je pro obě cesty stejný ([docs/format.md](docs/format.md)) a Fowler mezi nimi
nerozlišuje. Žádná z nich neobsahuje hodnocení, to vždy dodá pracoviště. Kdo píše vlastní program, může se
spouštěčem inspirovat: je to jedna hotová implementace této šablony (C#, zdrojový kód v jeho složce `src`).

## Odpovědnost za hodnocení

- Fowler výsledky vyšetření zaznamenává, zobrazuje a předává dál (dekurz, tisk, export). Hodnocení vyšetření,
  např. stupeň ztráty sluchu, klasifikace nebo typ tympanogramu, je odborná interpretace, kterou provádí lékař
  a pracoviště.
- Výrobce Fowleru proto nezveřejňuje ani nedodává kód, který by z naměřených dat hodnocení určoval. Takový kód by byl
  jeho interpretací vyšetření. Šablona v tomto repozitáři proto žádný postup hodnocení neobsahuje a bez doplnění
  nic neurčí.
- Pracoviště, které chce k výsledkům z přístroje doplnit své hodnocení, si zvolí, jak to udělá:
  - hodnocení určí samo podle svých postupů a zapíše ho do okna Hodnotit ve Fowleru,
  - nebo si podle svých možností zvolí míru automatizace, od pomocného výpočtu až po vlastní program podle této
    šablony, který hodnocení navrhne a Fowler ho vloží do okna ke kontrole.
- **Za hodnocení odpovídá lékař a pracoviště**, včetně programu, který k tomu vytvoří nebo použije. Výsledky programu
  je nutné kontrolovat. Fowler je vloží do okna jako návrh, uživatel je zkontroluje, případně opraví a potvrdí.
  Potvrzené hodnocení se uloží s označením „převzato z externího programu“ a se jménem uživatele, který ho potvrdil.
- Fowler odpověď programu kontroluje jen formálně (formát, rozsahy polí, příslušnost k vyšetření), ne věcně.
  Hodnocení jen převezme, zobrazí a předá dál.
- Software, který z naměřených dat určuje diagnostické údaje, může být podle nařízení (EU) 2017/745 (MDR)
  zdravotnickým prostředkem. Za správnost programu a za splnění povinností, které se na něj vztahují, odpovídá ten,
  kdo ho vytvoří a používá, případně poskytuje dalším.

## Obsah

```
README.md         tento popis
AGENTS.md         pokyny pro AI asistenta, který pomáhá hodnocení doprogramovat
docs/format.md    formát předaných dat (fowler-exam) a odpovědi (fowler-eval), nastavení ve Fowleru
priklady/         vzorová předaná data (smyšlený pacient) pro zkoušení
net/              šablona v C# (.NET Framework 4.8, Visual Studio 2022)
python/           šablona v Pythonu (3.8 a novější, jen standardní knihovna)
```

Obě verze dělají totéž a dávají stejné odpovědi. Vyberte jazyk, který vývojář pracoviště zná.

## Co udělat pro vlastní hodnocení

Postup pro vývojáře pracoviště:

1. **Určete, co se má hodnotit:** které části vyšetření a které údaje. Pole, do kterých Fowler odpověď vloží,
   a hodnoty, které přijmou, jsou v [docs/format.md](docs/format.md#odpověď-programu-fowler-eval).
   Postup hodnocení (pravidla, hranice, tabulky) určuje pracoviště.
2. **Napište hodnocení:** ve zvolené verzi ([net](net/README.md) nebo [python](python/README.md)) vytvořte potomka
   třídy `Hodnotitel` a přepište jen metody částí, které hodnotíte (`HodnotAudio`, `HodnotTympano`, `HodnotSlovni`,
   v Pythonu `hodnot_audio`…). Metoda dostane všechna předaná data své části a údaje pacienta a vrátí hodnoty polí.
   Co neurčí, nechá prázdné. Metoda, kterou nepřepíšete, nic nevrátí a Fowler ohlásí, že program tuto část
   nehodnotí.
3. **Vyzkoušejte** program na datech ve složce [priklady](priklady) (příkaz je v README verze).
4. **Ověřte výsledky** na vlastních vyšetřeních: porovnejte je s hodnocením lékaře a ověření si uschovejte.
5. **Nastavte program ve Fowleru:** Nastavení programu → Extra... → Externí program pro hodnocení měření
   (podrobnosti v [docs/format.md](docs/format.md#spuštění-programu) a v README verze).
6. **Při používání** výsledky v okně vždy zkontrolujte, než je potvrdíte.

Vlastní kód, který nechcete zveřejnit (např. ve forku tohoto repozitáře), můžete dát do složky `net/evaluator/Vlastni`
nebo `python/vlastni`. Tyto složky jsou vyloučené z gitu (`.gitignore`) a program je použije automaticky.

### S pomocí AI

Hodnocení lze doprogramovat i s AI asistentem. Pokyny pro něj jsou v [AGENTS.md](AGENTS.md), většina nástrojů je
načte sama. Popište mu postup hodnocení, který používá vaše pracoviště, protože šablona žádný neobsahuje. Nechte ho
upravit jen potomka třídy `Hodnotitel`. Výsledný kód si přečtěte a výsledky ověřte (krok 4).

## Návratové kódy

| kód | význam |
|---|---|
| 0 | odpověď zapsána (i když se nic neurčilo – pak je v ní jen poznámka) |
| 1 | chybí parametry |
| 2 | vstupní soubor nelze přečíst nebo neobsahuje platná data |
| 3 | data v jiném formátu nebo novější verzi, než program zná |
| 4 | odpověď nelze zapsat |

Fowler při jiném kódu než 0 ohlásí chybu a nic nevloží. Příčinu program zapíše do `evaluator.log` ve své složce
(bez osobních údajů).

## Licence

[MIT](LICENSE), © 2026 Viliam Dolinay. Šablona se poskytuje bez záruky. Za hodnocení, které do ní pracoviště
doplní, odpovídá pracoviště (viz [Odpovědnost za hodnocení](#odpovědnost-za-hodnocení)).
