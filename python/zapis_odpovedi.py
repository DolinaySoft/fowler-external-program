"""Sestaveni odpovedi (fmt "fowler-eval", popis v docs/format.md) z vysledku hodnoceni (model.Vysledek).

Hodnoty upravi na tvar, ktery prijmou pole okna ve Fowleru (pocet desetinnych mist a znaku, delka textu);
pri doplnovani hodnoceni neni treba menit.
"""

import json
import unicodedata
from datetime import datetime
from decimal import ROUND_HALF_UP, Decimal
from typing import Any, Dict, List, Optional

from cteni_vstupu import FORMAT_CASU
from model import (Cislo, Podnet, SrtKrivek, Trojice, Vstup, Vysledek, VysledekAudia, VysledekDecay, VysledekReflexu,
                   VysledekSlovni, VysledekTympanometrie, VysledekTympUcha)

FMT = "fowler-eval"
VERZE = 1
MAX_ZNAKU_TEXTU = 100   # textova pole okna Hodnotit
MAX_ZNAKU_CISLA = 5     # ciselna pole okna Hodnotit audiogramu (napr. 12.25, -10.5, 100)


def zapis(soubor: str, vstup: Vstup, vysledek: Vysledek, app: str) -> None:
    """zapise odpoved do souboru v UTF-8 bez BOM"""
    with open(soubor, "w", encoding="utf-8", newline="") as f:
        f.write(sestav(vstup, vysledek, app))


def sestav(vstup: Vstup, vysledek: Vysledek, app: str) -> str:
    poznamky = list(vysledek.poznamky)
    odpoved: Dict[str, Any] = {
        "fmt": FMT,
        "v": VERZE,
        "app": app,
        "created": datetime.now().strftime(FORMAT_CASU),
    }
    # identifikace vysetreni presne tak, jak byla predana - podle ni Fowler pozna, ze odpoved patri k vysetreni
    pacient: Dict[str, Any] = {}
    if vstup.pacient.pid is not None:
        pacient["pid"] = vstup.pacient.pid
    if vstup.pacient.rc is not None:
        pacient["rc"] = vstup.pacient.rc
    odpoved["patient"] = pacient
    odpoved["exam"] = {"time": vstup.cas_vysetreni.strftime(FORMAT_CASU)}
    # jen cast, ktera byla predana
    if vstup.audio is not None and vysledek.audio is not None:
        _pridej_neprazdne(odpoved, "audio", _audio(vysledek.audio, poznamky))
    if vstup.tymp is not None and vysledek.tymp is not None:
        _pridej_neprazdne(odpoved, "tymp", _tympanometrie(vysledek.tymp))
    if vstup.slovni is not None and vysledek.slovni is not None:
        _pridej_neprazdne(odpoved, "speech", _slovni(vysledek.slovni, poznamky))
    if poznamky:
        odpoved["note"] = "\n".join(str(p) for p in poznamky)
    return json.dumps(odpoved, ensure_ascii=False, separators=(",", ":"))


def _pridej_neprazdne(objekt: Dict[str, Any], klic: str, hodnota: Optional[Dict[str, Any]]) -> None:
    if hodnota:
        objekt[klic] = hodnota


def _decimal(hodnota: Cislo) -> Decimal:
    if isinstance(hodnota, float):
        return Decimal(repr(hodnota))   # 0.1 -> Decimal("0.1"), ne binarni hodnota
    return Decimal(hodnota)


def _cislo(hodnota: Optional[Cislo], max_desetin: int, max_znaku: int) -> Optional[Decimal]:
    """cislo s nejvyse max_desetin desetinnymi misty a nejvyse max_znaku znaky (zaokrouhli se, kdyz je delsi,
    napr. prumer 106.67 dB na 106.7 dB); None = nelze zapsat"""
    if hodnota is None:
        return None
    cislo = _decimal(hodnota)
    if not cislo.is_finite():
        return None
    for desetin in range(max_desetin, -1, -1):
        zaokrouhleno = cislo.quantize(Decimal(1).scaleb(-desetin), rounding=ROUND_HALF_UP)  # x,5 od nuly
        if zaokrouhleno == 0:
            zaokrouhleno = Decimal(0)
        zaokrouhleno = zaokrouhleno.normalize()  # bez nul na konci (20.10 -> 20.1)
        if len(_text_cisla(zaokrouhleno)) <= max_znaku:
            return zaokrouhleno
    return None


def _text_cisla(cislo: Decimal) -> str:
    return format(cislo, "f")


def _json_cislo(cislo: Decimal):
    return int(cislo) if cislo == cislo.to_integral_value() else float(_text_cisla(cislo))


def _text(text: Optional[str]) -> Optional[str]:
    """text bez ridicich znaku a mezer na okrajich, nejvyse MAX_ZNAKU_TEXTU; None = prazdny"""
    if text is None:
        return None
    text = "".join(c for c in str(text) if unicodedata.category(c) != "Cc").strip()
    if len(text) > MAX_ZNAKU_TEXTU:
        text = text[:MAX_ZNAKU_TEXTU].rstrip()
    return text or None


# cislo do pole okna; hodnota mimo rozsah pole by Fowler odmitl celou odpoved, proto se vynecha s poznamkou
def _pridej_cislo(objekt: Dict[str, Any], klic: str, hodnota: Optional[Cislo], max_desetin: int, max_znaku: int,
                  minimum: int, maximum: int, co: str, poznamky: List[str]) -> None:
    cislo = _cislo(hodnota, max_desetin, max_znaku)
    if cislo is not None and minimum <= cislo <= maximum:
        objekt[klic] = _json_cislo(cislo)
    elif hodnota is not None:
        poznamky.append(f"{co}: hodnotu {hodnota} nelze zapsat do pole okna (rozsah {minimum} až {maximum}).")


def _pridej_text(objekt: Dict[str, Any], klic: str, hodnota: Optional[str]) -> None:
    text = _text(hodnota)
    if text is not None:
        objekt[klic] = text


def _trojice(t: Trojice, minimum: int, maximum: int, co: str, poznamky: List[str]) -> Dict[str, Any]:
    o: Dict[str, Any] = {}
    _pridej_cislo(o, "R", t.R, 2, MAX_ZNAKU_CISLA, minimum, maximum, co + " vpravo", poznamky)
    _pridej_cislo(o, "L", t.L, 2, MAX_ZNAKU_CISLA, minimum, maximum, co + " vlevo", poznamky)
    _pridej_cislo(o, "total", t.celkem, 2, MAX_ZNAKU_CISLA, minimum, maximum, co + " celkem", poznamky)
    return o


def _audio(v: VysledekAudia, poznamky: List[str]) -> Dict[str, Any]:
    o: Dict[str, Any] = {}
    _pridej_neprazdne(o, "fowler", _trojice(v.ztrata_dle_fowlera, 0, 100, "Ztráta dle Fowlera", poznamky))
    _pridej_neprazdne(o, "acAvg", _trojice(v.prumer_vzduch, -10, 130, "Průměr vzduchu", poznamky))
    _pridej_neprazdne(o, "bcAvg", _trojice(v.prumer_kost, -10, 130, "Průměr kosti", poznamky))
    _pridej_text(o, "who", v.who)
    _pridej_text(o, "group", v.skupina)
    sisi: Dict[str, Any] = {}
    _pridej_text(sisi, "R", v.sisi_R)
    _pridej_text(sisi, "L", v.sisi_L)
    _pridej_neprazdne(o, "sisi", sisi)
    return o


def _podnet(podnet: Podnet):
    return podnet.sum if podnet.je_sum else podnet.hz


def _tympanometrie(v: VysledekTympanometrie) -> Dict[str, Any]:
    o: Dict[str, Any] = {}
    _pridej_neprazdne(o, "R", _tymp_ucho(v.R))
    _pridej_neprazdne(o, "L", _tymp_ucho(v.L))
    return o


def _tymp_ucho(v: Optional[VysledekTympUcha]) -> Optional[Dict[str, Any]]:
    if v is None:
        return None
    o: Dict[str, Any] = {}
    typ = _text(v.typ)
    if typ is not None and len(typ) <= 10:
        o["type"] = typ
    _pridej_seznam(o, "ipsi", _reflexy(v.ipsi))
    _pridej_seznam(o, "contra", _reflexy(v.contra))
    _pridej_seznam(o, "decayIpsi", _decay(v.decay_ipsi))
    _pridej_seznam(o, "decayContra", _decay(v.decay_contra))
    return o


def _pridej_seznam(objekt: Dict[str, Any], klic: str, seznam: List[Any]) -> None:
    if seznam:
        objekt[klic] = seznam


# kazdy podnet nejvyse jednou, plati prvni (Fowler opakovany podnet odmitne)
def _reflexy(reflexy: List[VysledekReflexu]) -> List[Any]:
    prvni: Dict[Podnet, VysledekReflexu] = {}
    for r in reflexy:
        prvni.setdefault(r.podnet, r)
    return [{"stim": _podnet(p), "res": "+" if r.vybavny else "-"} for p, r in prvni.items()]


def _decay(seznam: List[VysledekDecay]) -> List[Any]:
    prvni: Dict[Podnet, VysledekDecay] = {}
    for d in seznam:
        prvni.setdefault(d.podnet, d)
    return [{"stim": _podnet(p), "pct": d.procent if d.procent is not None else "-"} for p, d in prvni.items()]


def _slovni(v: VysledekSlovni, poznamky: List[str]) -> Dict[str, Any]:
    o = _srt(v.bez_sluchadla, "", poznamky)
    aided = _srt(v.se_sluchadlem, " se sluchadlem", poznamky)
    if aided:
        o["aided"] = aided
    return o


# SRT: nejvyse 1 desetinne misto (pole SRT ve slovni audiometrii)
def _srt(s: SrtKrivek, co: str, poznamky: List[str]) -> Dict[str, Any]:
    srt: Dict[str, Any] = {}
    _pridej_cislo(srt, "R", s.R, 1, 6, -10, 120, "SRT vpravo" + co, poznamky)
    _pridej_cislo(srt, "L", s.L, 1, 6, -10, 120, "SRT vlevo" + co, poznamky)
    _pridej_cislo(srt, "bin", s.bin, 1, 6, -10, 120, "SRT binaurálně" + co, poznamky)
    return {"srt": srt} if srt else {}
