"""Hodnoceni predanych dat.

Zakladni trida Hodnotitel nic neurci - vsechny metody hodnot_... vrati None. Vlastni hodnoceni je potomek, ktery
prepise jen metody casti, ktere chce hodnotit, a pripoji se do programu funkci pripoj() v balicku vlastni
(slozka vlastni/ vedle evaluator.py, popis v python/README.md):

    # vlastni/__init__.py
    from hodnotitel import Hodnotitel

    class MujHodnotitel(Hodnotitel):
        def hodnot_audio(self, audio, pacient, poznamky):
            ...

    def pripoj():
        return MujHodnotitel()

Kazda metoda dostane vsechna predana data sve casti vysetreni a udaje pacienta a vrati vysledek pro pole okna
ve Fowleru; udaj, ktery nelze nebo se nema urcit, necha None (pole v okne pak zustane, jak je). Vrati-li metoda
None, program tuto cast nehodnoti a odpoved to uvede v poznamce. Do seznamu poznamky lze pridat zpravu pro
uzivatele, Fowler ji zobrazi po vlozeni odpovedi.
"""

from typing import List, Optional

from model import (Pacient, SlovniAudiometrie, TonovaAudiometrie, Tympanometrie, Vstup, Vysledek, VysledekAudia,
                   VysledekSlovni, VysledekTympanometrie)


class Hodnotitel:
    def hodnot_audio(self, audio: TonovaAudiometrie, pacient: Pacient,
                     poznamky: List[str]) -> Optional[VysledekAudia]:
        """tonova audiometrie - pole okna Hodnotit audiogramu"""
        return None

    def hodnot_tympano(self, tymp: Tympanometrie, pacient: Pacient,
                       poznamky: List[str]) -> Optional[VysledekTympanometrie]:
        """tympanometrie - pole okna Hodnotit tympanometrie"""
        return None

    def hodnot_slovni(self, slovni: SlovniAudiometrie, pacient: Pacient,
                      poznamky: List[str]) -> Optional[VysledekSlovni]:
        """slovni audiometrie - pole SRT ve slovni audiometrii"""
        return None


def vytvor() -> Hodnotitel:
    """potomek z balicku vlastni (funkce pripoj); kdyz balicek neni, zakladni Hodnotitel, ktery nic neurci"""
    try:
        import vlastni
    except ModuleNotFoundError as ex:
        if ex.name != "vlastni":
            raise   # chyba uvnitr balicku vlastni (napr. chybi jeho modul) se nesmi skryt
        return Hodnotitel()
    return vlastni.pripoj()


def hodnot(hodnotitel: Hodnotitel, vstup: Vstup) -> Vysledek:
    """ohodnoti predanou cast vysetreni (predava se jen ta, ze ktere uzivatel program spustil)"""
    vysledek = Vysledek()
    if vstup.audio is not None:
        vysledek.audio = hodnotitel.hodnot_audio(vstup.audio, vstup.pacient, vysledek.poznamky)
        if vysledek.audio is None:
            vysledek.poznamky.append("Program audiogram nehodnotí.")
    if vstup.tymp is not None:
        vysledek.tymp = hodnotitel.hodnot_tympano(vstup.tymp, vstup.pacient, vysledek.poznamky)
        if vysledek.tymp is None:
            vysledek.poznamky.append("Program tympanometrii nehodnotí.")
    if vstup.slovni is not None:
        vysledek.slovni = hodnotitel.hodnot_slovni(vstup.slovni, vstup.pacient, vysledek.poznamky)
        if vysledek.slovni is None:
            vysledek.poznamky.append("Program slovní audiometrii nehodnotí.")
    return vysledek
