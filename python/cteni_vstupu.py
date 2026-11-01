"""Cteni predanych dat (fmt "fowler-exam", popis v docs/format.md) do tridy model.Vstup.

Pri doplnovani hodnoceni neni treba menit - hodnoceni pracuje jen s modelem.
"""

import json
from datetime import datetime
from decimal import Decimal
from typing import Any, Dict, List, Optional

from model import (AudioUcho, Decay, EtfPerforovany, EtfTympanogram, Pacient, Podnet, Prah, Reflex, ReflexIntenzita,
                   SlovniAudiometrie, SlovniBod, SlovniKrivka, SlovniKrivky, TonovaAudiometrie, Tympanogram,
                   Tympanometrie, TympUcho, Vstup)

FMT = "fowler-exam"
VERZE = 1                       # nejvyssi verze formatu, kterou program zna
FORMAT_CASU = "%Y-%m-%dT%H:%M:%S"  # cas "2026-10-28T09:42:00" (created, exam.time)


class ChybaDat(Exception):
    """chyba ve vstupnich datech (neplatny JSON, chybejici nebo spatny udaj)"""


class NepodporovanyFormat(Exception):
    """data v jinem formatu nebo novejsi verzi, nez program zna"""


def precti(soubor: str) -> Vstup:
    with open(soubor, "rb") as f:
        data = f.read()
    try:
        text = data.decode("utf-8-sig")
    except UnicodeDecodeError as ex:
        raise ChybaDat("soubor není v kódování UTF-8") from ex
    return precti_text(text)


def precti_text(text: str) -> Vstup:
    try:
        koren = json.loads(text, parse_float=Decimal, parse_constant=_neplatna_konstanta)
    except ValueError as ex:
        raise ChybaDat(f"neplatný JSON: {ex}") from ex
    data = _objekt(koren, "data")
    fmt = _text(data, "fmt", "")
    if fmt != FMT:
        raise NepodporovanyFormat(f"data nejsou ve formátu {FMT} (fmt: {fmt or 'chybí'})")
    verze = _cele_cislo_polozka(data, "v", "")
    if verze is None or verze < 1:
        raise ChybaDat("chybí verze formátu (v)")
    if verze > VERZE:
        raise NepodporovanyFormat(f"data mají novější verzi formátu ({verze}), než program zná ({VERZE})")

    vstup = Vstup()
    vstup.app = _text(data, "app", "")
    vstup.vytvoreno = _cas(_text(data, "created", ""))
    _cti_pacienta(_objekt_polozka(data, "patient", ""), vstup.pacient)
    _cti_vysetreni(_objekt_polozka(data, "exam", ""), vstup)
    cast = _objekt_polozka(data, "audio", "")
    if cast is not None:
        vstup.audio = _cti_audio(cast)
    cast = _objekt_polozka(data, "tymp", "")
    if cast is not None:
        vstup.tymp = _cti_tympanometrii(cast)
    cast = _objekt_polozka(data, "speech", "")
    if cast is not None:
        vstup.slovni = _cti_slovni(cast)
    if vstup.audio is None and vstup.tymp is None and vstup.slovni is None:
        raise ChybaDat("data neobsahují žádnou část vyšetření (audio, tymp, speech)")
    return vstup


def _neplatna_konstanta(nazev: str):
    raise ValueError(f"nepovolená hodnota {nazev}")


# cas "2026-10-28T09:42:00" a datum "1957-04-15"; neplatny = None
def _cas(text: Optional[str]) -> Optional[datetime]:
    if text is None:
        return None
    for fmt in (FORMAT_CASU, "%Y-%m-%d"):
        try:
            return datetime.strptime(text, fmt)
        except ValueError:
            pass
    return None


def _cti_pacienta(o: Optional[Dict[str, Any]], pacient: Pacient) -> None:
    if o is None:
        return
    pacient.pid = _text(o, "pid", "patient")
    pacient.rc = _text(o, "rc", "patient")
    pacient.jmeno = _text(o, "firstName", "patient")
    pacient.prijmeni = _text(o, "lastName", "patient")
    narozeni = _cas(_text(o, "birthDate", "patient"))
    pacient.datum_narozeni = narozeni.date() if narozeni else None
    pacient.pojistovna = _text(o, "insurance", "patient")


# exam: cas vysetreni do vstupu, vek v den vysetreni a pohlavi k pacientovi; cas musi byt presne ve tvaru
# FORMAT_CASU, aby ho odpoved zopakovala tak, jak byl predan (Fowler ho porovnava jako text)
def _cti_vysetreni(o: Optional[Dict[str, Any]], vstup: Vstup) -> None:
    text = _text(o, "time", "exam")
    if not text:
        raise ChybaDat("chybí čas vyšetření (exam.time)")
    try:
        cas = datetime.strptime(text, FORMAT_CASU)
    except ValueError:
        cas = None
    if cas is None or cas.strftime(FORMAT_CASU) != text:
        raise ChybaDat(f"exam.time: neplatný čas vyšetření \"{text}\" (očekáváno RRRR-MM-DDThh:mm:ss)")
    vstup.cas_vysetreni = cas
    vek = _objekt_polozka(o, "age", "exam")
    if vek is not None:
        vstup.pacient.vek_roky = _cele_cislo_polozka(vek, "y", "exam.age")
        vstup.pacient.vek_mesice = _cele_cislo_polozka(vek, "m", "exam.age")
    vstup.pacient.pohlavi = _text(o, "sex", "exam")


# --- tonova audiometrie

def _cti_audio(o: Dict[str, Any]) -> TonovaAudiometrie:
    audio = TonovaAudiometrie()
    audio.legacy = _logicka(o, "legacy", "audio")
    audio.pristroj = _text(o, "device", "audio")
    audio.poznamka = _text(o, "note", "audio")
    audio.hz = _cela_cisla(o, "hz", "audio")
    audio.R = _cti_audio_ucho(_objekt_polozka(o, "R", "audio"), audio.hz, "audio.R")
    audio.L = _cti_audio_ucho(_objekt_polozka(o, "L", "audio"), audio.hz, "audio.L")
    return audio


def _cti_audio_ucho(o: Optional[Dict[str, Any]], hz: List[int], cesta: str) -> Optional[AudioUcho]:
    if o is None:
        return None
    ucho = AudioUcho()
    _cti_prahy(o, "ac", hz, cesta, ucho.vzduch)
    ucho.vzduch_maskovan = _logicka(o, "acMasked", cesta)
    _cti_hodnoty(o, "acMask", hz, cesta, ucho.vzduch_maskovani)
    _cti_prahy(o, "bc", hz, cesta, ucho.kost)
    _cti_hodnoty(o, "bcMask", hz, cesta, ucho.kost_maskovani)
    _cti_hodnoty(o, "ucl", hz, cesta, ucho.ucl)
    _cti_hodnoty(o, "mcl", hz, cesta, ucho.mcl)
    _cti_hodnoty(o, "sisi", hz, cesta, ucho.sisi)
    return ucho


# krivka prahu podle hz: cislo [dB], "NR" = neslysi, null = nezmereno
def _cti_prahy(o: Dict[str, Any], klic: str, hz: List[int], cesta: str, prahy: Dict[int, Prah]) -> None:
    pole = _pole_polozka(o, klic, cesta)
    if pole is None:
        return
    _stejna_delka(len(pole), len(hz), f"{cesta}.{klic} a hz")
    for i, hodnota in enumerate(pole):
        if hodnota is None:
            continue
        if hodnota == "NR":
            prahy[hz[i]] = Prah(neslysi=True)
        else:
            prahy[hz[i]] = Prah(db=_cele_cislo(hodnota, f"{cesta}.{klic}[{i}]"))


def _cti_hodnoty(o: Dict[str, Any], klic: str, hz: List[int], cesta: str, hodnoty: Dict[int, int]) -> None:
    pole = _pole_polozka(o, klic, cesta)
    if pole is None:
        return
    _stejna_delka(len(pole), len(hz), f"{cesta}.{klic} a hz")
    for i, hodnota in enumerate(pole):
        if hodnota is not None:
            hodnoty[hz[i]] = _cele_cislo(hodnota, f"{cesta}.{klic}[{i}]")


# --- tympanometrie

def _cti_tympanometrii(o: Dict[str, Any]) -> Tympanometrie:
    tymp = Tympanometrie()
    tymp.legacy = _logicka(o, "legacy", "tymp")
    tymp.pristroj = _text(o, "device", "tymp")
    tymp.R = _cti_tymp_ucho(_objekt_polozka(o, "R", "tymp"), "tymp.R")
    tymp.L = _cti_tymp_ucho(_objekt_polozka(o, "L", "tymp"), "tymp.L")
    return tymp


def _cti_tymp_ucho(o: Optional[Dict[str, Any]], cesta: str) -> Optional[TympUcho]:
    if o is None:
        return None
    ucho = TympUcho()
    for i, polozka in enumerate(_pole_polozka(o, "tg", cesta) or []):
        c = f"{cesta}.tg[{i}]"
        ucho.tympanogramy.append(_cti_tympanogram(_objekt(polozka, c), c))
    _cti_reflexy(o, "ipsi", cesta, ucho.ipsi)
    _cti_reflexy(o, "contra", cesta, ucho.contra)
    _cti_decay(o, "decayIpsi", cesta, ucho.decay_ipsi)
    _cti_decay(o, "decayContra", cesta, ucho.decay_contra)
    for i, polozka in enumerate(_pole_polozka(o, "etf", cesta) or []):
        c = f"{cesta}.etf[{i}]"
        e = _objekt(polozka, c)
        etf = EtfTympanogram()
        etf.tlak_testu = _cele_cislo_polozka(e, "press", c)
        etf.objem_zvukovodu = _cislo_polozka(e, "ecv", c)
        etf.tlak = _cela_cisla(e, "p", c)
        etf.poddajnost = _cisla(e, "y", c)
        _stejna_delka(len(etf.tlak), len(etf.poddajnost), c + ".p a y")
        ucho.etf.append(etf)
    perf = _objekt_polozka(o, "etfPerf", cesta)
    if perf is not None:
        ucho.etf_perf = EtfPerforovany()
        ucho.etf_perf.cas = _cela_cisla(perf, "t", cesta + ".etfPerf")
        ucho.etf_perf.tlak = _cela_cisla(perf, "p", cesta + ".etfPerf")
        _stejna_delka(len(ucho.etf_perf.cas), len(ucho.etf_perf.tlak), cesta + ".etfPerf.t a p")
    return ucho


def _cti_tympanogram(o: Dict[str, Any], cesta: str) -> Tympanogram:
    tg = Tympanogram()
    tg.sonda_hz = _cele_cislo_polozka(o, "probeHz", cesta)
    tg.vrchol_tlak = _cele_cislo_polozka(o, "peakP", cesta)
    tg.vrchol_poddajnost = _cislo_polozka(o, "peakY", cesta)
    tg.objem_zvukovodu = _cislo_polozka(o, "ecv", cesta)
    tg.gradient_dapa = _cele_cislo_polozka(o, "gradP", cesta)
    tg.gradient_ml = _cislo_polozka(o, "gradY", cesta)
    tg.tlak = _cela_cisla(o, "p", cesta)
    tg.poddajnost = _cisla(o, "y", cesta)
    _stejna_delka(len(tg.tlak), len(tg.poddajnost), cesta + ".p a y")
    return tg


# podnet: frekvence v Hz, nebo sum "LP", "HP", "WB"
def _cti_podnet(o: Dict[str, Any], cesta: str) -> Podnet:
    stim = o.get("stim")
    if stim in ("LP", "HP", "WB"):
        return Podnet(sum=stim)
    if _je_cislo(stim):
        return Podnet(hz=_cele_cislo(stim, cesta + ".stim"))
    raise ChybaDat(f"{cesta}.stim: očekávána frekvence nebo \"LP\", \"HP\", \"WB\"")


def _cti_reflexy(o: Dict[str, Any], klic: str, cesta: str, reflexy: List[Reflex]) -> None:
    for i, polozka in enumerate(_pole_polozka(o, klic, cesta) or []):
        c = f"{cesta}.{klic}[{i}]"
        r = _objekt(polozka, c)
        reflex = Reflex()
        reflex.podnet = _cti_podnet(r, c)
        reflex.tlak = _cele_cislo_polozka(r, "p", c)
        reflex.vysledek = _text(r, "res", c)
        reflex.zdroj = _text(r, "src", c)
        for k, lv_polozka in enumerate(_pole_polozka(r, "lv", c) or []):
            ci = f"{c}.lv[{k}]"
            lv = _objekt(lv_polozka, ci)
            intenzita = ReflexIntenzita()
            db = _cele_cislo_polozka(lv, "db", ci)
            intenzita.db = 0 if db is None else db
            intenzita.cas = _cela_cisla(lv, "t", ci)
            intenzita.zmena = _cisla(lv, "y", ci)
            _stejna_delka(len(intenzita.cas), len(intenzita.zmena), ci + ".t a y")
            if lv.get("found") is not None:
                intenzita.nalezen_pristrojem = _logicka(lv, "found", ci)
            reflex.intenzity.append(intenzita)
        reflexy.append(reflex)


def _cti_decay(o: Dict[str, Any], klic: str, cesta: str, seznam: List[Decay]) -> None:
    for i, polozka in enumerate(_pole_polozka(o, klic, cesta) or []):
        c = f"{cesta}.{klic}[{i}]"
        d = _objekt(polozka, c)
        decay = Decay()
        decay.podnet = _cti_podnet(d, c)
        decay.tlak = _cele_cislo_polozka(d, "p", c)
        decay.db = _cele_cislo_polozka(d, "db", c)
        decay.procent = _cele_cislo_polozka(d, "pct", c)
        decay.cas = _cela_cisla(d, "t", c)
        decay.zmena = _cisla(d, "y", c)
        _stejna_delka(len(decay.cas), len(decay.zmena), c + ".t a y")
        seznam.append(decay)


# --- slovni audiometrie

def _cti_slovni(o: Dict[str, Any]) -> SlovniAudiometrie:
    slovni = SlovniAudiometrie()
    slovni.legacy = _logicka(o, "legacy", "speech")
    slovni.pristroj = _text(o, "device", "speech")
    slovni.poznamka = _text(o, "note", "speech")
    _cti_krivky(o, "speech", slovni.bez_sluchadla)
    aided = _objekt_polozka(o, "aided", "speech")
    if aided is not None:
        _cti_krivky(aided, "speech.aided", slovni.se_sluchadlem)
    return slovni


def _cti_krivky(o: Dict[str, Any], cesta: str, krivky: SlovniKrivky) -> None:
    krivky.R = _cti_krivku(_objekt_polozka(o, "R", cesta), cesta + ".R")
    krivky.L = _cti_krivku(_objekt_polozka(o, "L", cesta), cesta + ".L")
    krivky.bin = _cti_krivku(_objekt_polozka(o, "bin", cesta), cesta + ".bin")


def _cti_krivku(o: Optional[Dict[str, Any]], cesta: str) -> Optional[SlovniKrivka]:
    if o is None:
        return None
    db = _cela_cisla(o, "db", cesta)
    pct = _cela_cisla(o, "pct", cesta)
    _stejna_delka(len(db), len(pct), cesta + ".db a pct")
    mask = _pole_polozka(o, "mask", cesta)
    if mask is not None:
        _stejna_delka(len(db), len(mask), cesta + ".db a mask")
    krivka = SlovniKrivka()
    for i in range(len(db)):
        bod = SlovniBod(db=db[i], procent=pct[i])
        m = None if mask is None else mask[i]
        if m is not None:
            bod.maskovano = True
            if m != "M":
                bod.maskovani_db = _cele_cislo(m, f"{cesta}.mask[{i}]")
        krivka.body.append(bod)
    krivka.body.sort(key=lambda b: b.db)
    return krivka


# --- pristup k JSON (objekt = dict, pole = list, cislo = int nebo Decimal); chyba typu ohlasi i cestu k udaji,
# napr. "audio.R.ac[3]"

def _cesta(cesta: str, klic: str) -> str:
    return f"{cesta}.{klic}" if cesta else klic


def _objekt(hodnota: Any, cesta: str) -> Dict[str, Any]:
    if not isinstance(hodnota, dict):
        raise ChybaDat(f"{cesta}: očekáván objekt")
    return hodnota


def _objekt_polozka(o: Optional[Dict[str, Any]], klic: str, cesta: str) -> Optional[Dict[str, Any]]:
    hodnota = None if o is None else o.get(klic)
    return None if hodnota is None else _objekt(hodnota, _cesta(cesta, klic))


def _pole_polozka(o: Optional[Dict[str, Any]], klic: str, cesta: str) -> Optional[list]:
    hodnota = None if o is None else o.get(klic)
    if hodnota is None:
        return None
    if not isinstance(hodnota, list):
        raise ChybaDat(f"{_cesta(cesta, klic)}: očekáván seznam")
    return hodnota


def _text(o: Optional[Dict[str, Any]], klic: str, cesta: str) -> Optional[str]:
    hodnota = None if o is None else o.get(klic)
    if hodnota is None:
        return None
    if not isinstance(hodnota, str):
        raise ChybaDat(f"{_cesta(cesta, klic)}: očekáván text")
    return hodnota


def _logicka(o: Dict[str, Any], klic: str, cesta: str) -> bool:
    hodnota = o.get(klic)
    if hodnota is None:
        return False
    if not isinstance(hodnota, bool):
        raise ChybaDat(f"{_cesta(cesta, klic)}: očekáváno true nebo false")
    return hodnota


def _je_cislo(hodnota: Any) -> bool:
    return isinstance(hodnota, (int, Decimal)) and not isinstance(hodnota, bool)


def _cislo(hodnota: Any, cesta: str) -> Decimal:
    if not _je_cislo(hodnota):
        raise ChybaDat(f"{cesta}: očekáváno číslo")
    return Decimal(hodnota)


def _cele_cislo(hodnota: Any, cesta: str) -> int:
    cislo = _cislo(hodnota, cesta)
    if cislo != cislo.to_integral_value() or not -2**31 <= cislo <= 2**31 - 1:
        raise ChybaDat(f"{cesta}: očekáváno celé číslo")
    return int(cislo)


def _cele_cislo_polozka(o: Dict[str, Any], klic: str, cesta: str) -> Optional[int]:
    hodnota = o.get(klic)
    return None if hodnota is None else _cele_cislo(hodnota, _cesta(cesta, klic))


def _cislo_polozka(o: Dict[str, Any], klic: str, cesta: str) -> Optional[Decimal]:
    hodnota = o.get(klic)
    return None if hodnota is None else _cislo(hodnota, _cesta(cesta, klic))


def _cela_cisla(o: Dict[str, Any], klic: str, cesta: str) -> List[int]:
    pole = _pole_polozka(o, klic, cesta) or []
    return [_cele_cislo(h, f"{_cesta(cesta, klic)}[{i}]") for i, h in enumerate(pole)]


def _cisla(o: Dict[str, Any], klic: str, cesta: str) -> List[Decimal]:
    pole = _pole_polozka(o, klic, cesta) or []
    return [_cislo(h, f"{_cesta(cesta, klic)}[{i}]") for i, h in enumerate(pole)]


def _stejna_delka(delka1: int, delka2: int, cesta: str) -> None:
    """souběžná pole musí mít stejnou délku (pravidlo formátu)"""
    if delka1 != delka2:
        raise ChybaDat(f"{cesta}: souběžná pole mají různou délku ({delka1} a {delka2})")
