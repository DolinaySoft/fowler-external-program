"""evaluator - externi program pro Fowler (Python 3.8 a novejsi, jen standardni knihovna).

Spusteni: python evaluator.py "vstupni soubor" "vystupni soubor" (Fowler predava "%VSTUP%" "%VYSTUP%").
Precte predana data, ohodnoti je (hodnotitel.py) a zapise odpoved. Navratovy kod 0 = odpoved je zapsana;
jiny kod Fowler ohlasi a nic nenacte. Chyba pri hodnoceni se oznami v odpovedi (note), aby ji uzivatel videl.
Prubeh se zapisuje do evaluator.log vedle tohoto souboru (bez osobnich udaju).
"""

import os
import sys
import traceback
from datetime import datetime

# moduly programu a balicek vlastni jsou vedle tohoto souboru (Fowler spousti program v jine slozce)
SLOZKA = os.path.dirname(os.path.abspath(__file__))
if SLOZKA not in sys.path:
    sys.path.insert(0, SLOZKA)

from cteni_vstupu import NepodporovanyFormat, precti  # noqa: E402
from hodnotitel import hodnot, vytvor  # noqa: E402
from model import Vysledek  # noqa: E402
from zapis_odpovedi import zapis  # noqa: E402

NAZEV = "evaluator 1.0.0 (Python)"

KOD_OK = 0
KOD_PARAMETRY = 1   # chybi vstupni nebo vystupni soubor
KOD_VSTUP = 2       # vstupni soubor nelze precist nebo neobsahuje platna data
KOD_FORMAT = 3      # data v jinem formatu nebo novejsi verzi, nez program zna
KOD_ZAPIS = 4       # odpoved nelze zapsat

SOUBOR_LOGU = os.path.join(SLOZKA, "evaluator.log")


def log(zprava: str) -> None:
    """zaznam prubehu; nad 1 MB se log prejmenuje na .old"""
    try:
        if os.path.exists(SOUBOR_LOGU) and os.path.getsize(SOUBOR_LOGU) > 1024 * 1024:
            os.replace(SOUBOR_LOGU, SOUBOR_LOGU + ".old")
        with open(SOUBOR_LOGU, "a", encoding="utf-8") as f:
            f.write(f"[{datetime.now():%Y-%m-%d %H:%M:%S}] {zprava}\n")
    except OSError:
        pass    # log neni nutny pro funkci (napr. slozka jen pro cteni)


def spust(parametry) -> int:
    if len(parametry) < 2:
        log('Chybi parametry: evaluator.py "vstupni soubor" "vystupni soubor"')
        print('Použití: evaluator.py "vstupní soubor" "výstupní soubor"', file=sys.stderr)
        return KOD_PARAMETRY
    soubor_vstup, soubor_vystup = parametry[0], parametry[1]

    try:
        vstup = precti(soubor_vstup)
    except NepodporovanyFormat as ex:
        log(f"Nepodporovany format dat: {ex}")
        return KOD_FORMAT
    except Exception as ex:     # ChybaDat, ale i chyba cteni souboru
        log(f"Vstupni data nelze precist: {type(ex).__name__}: {ex}")
        return KOD_VSTUP
    casti = " ".join(n for n, c in (("audio", vstup.audio), ("tymp", vstup.tymp), ("speech", vstup.slovni)) if c)
    log(f"Data z {vstup.app}: {casti}")

    try:
        hodnotitel = vytvor()
        log(f"Hodnotitel {type(hodnotitel).__module__}.{type(hodnotitel).__name__}")
        vysledek = hodnot(hodnotitel, vstup)
    except Exception as ex:
        log("Chyba pri hodnoceni: " + traceback.format_exc())
        vysledek = Vysledek()
        vysledek.poznamky.append(f"Hodnocení se nepodařilo: {ex}")

    try:
        zapis(soubor_vystup, vstup, vysledek, NAZEV)
    except Exception as ex:
        log(f"Odpoved nelze zapsat: {ex}")
        return KOD_ZAPIS
    log("Odpoved zapsana")
    return KOD_OK


if __name__ == "__main__":
    sys.exit(spust(sys.argv[1:]))
