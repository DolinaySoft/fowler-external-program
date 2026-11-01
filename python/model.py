"""Predana data vysetreni a vysledek hodnoceni.

Vstup: data predana Fowlerem (fmt "fowler-exam", popis v docs/format.md). Plni je cteni_vstupu.py,
hodnoceni (potomek tridy Hodnotitel v hodnotitel.py) pracuje jen s temito tridami a nemusi resit JSON.
Co v datech nebylo, je None (u seznamu a slovniku prazdny seznam nebo slovnik).
Desetinna cisla (ml, mmho) jsou Decimal, ostatni cisla int.

Vysledek: pole oken Fowleru, do kterych Fowler odpoved vlozi ke kontrole uzivatelem (fmt "fowler-eval").
U kazdeho udaje je popisek pole v okne a hodnota, kterou pole prijme; jak udaj urcit, je vec pracoviste.
None = udaj se nehodnoti, Fowler pole v okne necha, jak je.
"""

from dataclasses import dataclass, field
from datetime import date, datetime
from decimal import Decimal
from typing import Dict, List, Optional, Union

Cislo = Union[int, float, Decimal]


# ---------------------------------------------------------------------------------------------------------------
# Vstup

@dataclass
class Pacient:
    pid: Optional[str] = None               # GUID pacienta ve Fowleru (odpoved ho musi zopakovat)
    rc: Optional[str] = None                # rodne cislo bez lomitka (odpoved ho musi zopakovat)
    jmeno: Optional[str] = None
    prijmeni: Optional[str] = None
    datum_narozeni: Optional[date] = None
    pojistovna: Optional[str] = None
    vek_roky: Optional[int] = None          # vek v den vysetreni: dokoncene roky (exam.age)
    vek_mesice: Optional[int] = None        # a mesice (0-11)
    pohlavi: Optional[str] = None           # "M", "F", None = neznamo (exam.sex)


# --- tonova audiometrie (audio)

@dataclass(frozen=True)
class Prah:
    """prah sluchu na jedne frekvenci: hodnota v dB HL, nebo neslysi ("NR", ve Fowleru "-")"""
    neslysi: bool = False
    db: Optional[int] = None                # None u neslysi

    def __str__(self) -> str:
        return "NR" if self.neslysi else str(self.db)


@dataclass
class AudioUcho:
    """hodnoty jednoho ucha podle frekvence [Hz]; frekvence bez hodnoty ve slovniku neni"""
    vzduch: Dict[int, Prah] = field(default_factory=dict)          # ac [dB HL]
    vzduch_maskovan: bool = False                                  # acMasked
    vzduch_maskovani: Dict[int, int] = field(default_factory=dict) # acMask [dB]
    kost: Dict[int, Prah] = field(default_factory=dict)            # bc [dB HL]
    kost_maskovani: Dict[int, int] = field(default_factory=dict)   # bcMask [dB]
    ucl: Dict[int, int] = field(default_factory=dict)              # prah nepohodli [dB]
    mcl: Dict[int, int] = field(default_factory=dict)              # nejprijemnejsi hlasitost [dB]
    sisi: Dict[int, int] = field(default_factory=dict)             # SISI [%]


@dataclass
class TonovaAudiometrie:
    legacy: bool = False                    # True = vysetreni ulozene verzi Fowleru pred MDR; kost mohla byt doplnena odhadem
    pristroj: Optional[str] = None
    poznamka: Optional[str] = None
    hz: List[int] = field(default_factory=list)  # frekvence, na kterych je aspon jedna hodnota
    R: Optional[AudioUcho] = None           # None = ucho bez hodnot
    L: Optional[AudioUcho] = None


# --- tympanometrie (tymp)

@dataclass
class Tympanogram:
    """tympanogram; poddajnost a objem u sondy 226 Hz v ml, u 1000 Hz v mmho; poddajnost je hodnota pristroje
    vcetne objemu zvukovodu (graf Fowleru ukazuje poddajnost - objem zvukovodu)"""
    sonda_hz: Optional[int] = None          # 226 nebo 1000; None = hodnoty zapsane rucne v panelu (bez krivky)
    vrchol_tlak: Optional[int] = None       # peakP [daPa]
    vrchol_poddajnost: Optional[Decimal] = None  # peakY
    objem_zvukovodu: Optional[Decimal] = None    # ecv
    gradient_dapa: Optional[int] = None     # gradP
    gradient_ml: Optional[Decimal] = None   # gradY (tympanometr udava gradient v ml)
    tlak: List[int] = field(default_factory=list)            # p [daPa]
    poddajnost: List[Decimal] = field(default_factory=list)  # y

    @property
    def ma_krivku(self) -> bool:
        return len(self.tlak) > 0


@dataclass(frozen=True)
class Podnet:
    """podnet reflexu: frekvence [Hz], nebo sum ("LP", "HP", "WB")"""
    hz: int = 0                             # 0 u sumu
    sum: Optional[str] = None               # None u tonu

    @property
    def je_sum(self) -> bool:
        return self.sum is not None

    def __str__(self) -> str:
        return self.sum if self.je_sum else f"{self.hz} Hz"


@dataclass
class ReflexIntenzita:
    """zaznam jedne intenzity podnetu: cas je poloha v zaznamu 0-1000 (u MAICO ms),
    zmena poddajnosti v ml (kladna = pokles poddajnosti)"""
    db: int = 0
    cas: List[int] = field(default_factory=list)             # t
    zmena: List[Decimal] = field(default_factory=list)       # y
    nalezen_pristrojem: Optional[bool] = None                # found - jen kdyz vysledek posila pristroj


@dataclass
class Reflex:
    podnet: Podnet = field(default_factory=Podnet)
    tlak: Optional[int] = None              # tlak v uchu behem mereni [daPa]
    vysledek: Optional[str] = None          # "+", "-" nebo None - z panelu Fowleru (zadal uzivatel nebo pristroj)
    zdroj: Optional[str] = None             # "dev" = pristroj, "user" = uzivatel
    intenzity: List[ReflexIntenzita] = field(default_factory=list)


@dataclass
class Decay:
    podnet: Podnet = field(default_factory=Podnet)
    tlak: Optional[int] = None
    db: Optional[int] = None
    procent: Optional[int] = None           # decay v % z pristroje (None = pristroj hodnotu nedal)
    cas: List[int] = field(default_factory=list)             # 0-10000
    zmena: List[Decimal] = field(default_factory=list)


@dataclass
class EtfTympanogram:
    tlak_testu: Optional[int] = None        # press [daPa]
    objem_zvukovodu: Optional[Decimal] = None
    tlak: List[int] = field(default_factory=list)
    poddajnost: List[Decimal] = field(default_factory=list)


@dataclass
class EtfPerforovany:
    cas: List[int] = field(default_factory=list)             # ms
    tlak: List[int] = field(default_factory=list)            # daPa


@dataclass
class TympUcho:
    tympanogramy: List[Tympanogram] = field(default_factory=list)  # tg
    ipsi: List[Reflex] = field(default_factory=list)
    contra: List[Reflex] = field(default_factory=list)
    decay_ipsi: List[Decay] = field(default_factory=list)
    decay_contra: List[Decay] = field(default_factory=list)
    etf: List[EtfTympanogram] = field(default_factory=list)
    etf_perf: Optional[EtfPerforovany] = None


@dataclass
class Tympanometrie:
    legacy: bool = False
    pristroj: Optional[str] = None
    R: Optional[TympUcho] = None            # None = ucho bez dat
    L: Optional[TympUcho] = None


# --- slovni audiometrie (speech)

@dataclass
class SlovniBod:
    db: int = 0                             # intenzita podnetu [dB]
    procent: int = 0                        # srozumitelnost [%]
    maskovano: bool = False
    maskovani_db: Optional[int] = None      # uroven maskovani, None = neuvedena


@dataclass
class SlovniKrivka:
    body: List[SlovniBod] = field(default_factory=list)      # serazene podle intenzity


@dataclass
class SlovniKrivky:
    R: Optional[SlovniKrivka] = None        # None = krivka nebyla zmerena
    L: Optional[SlovniKrivka] = None
    bin: Optional[SlovniKrivka] = None      # binauralne


@dataclass
class SlovniAudiometrie:
    legacy: bool = False
    pristroj: Optional[str] = None
    poznamka: Optional[str] = None
    bez_sluchadla: SlovniKrivky = field(default_factory=SlovniKrivky)
    se_sluchadlem: SlovniKrivky = field(default_factory=SlovniKrivky)  # aided


@dataclass
class Vstup:
    """predana data jednoho vysetreni jednoho pacienta"""
    app: Optional[str] = None               # program, ktery data vytvoril, napr. "Fowler 3.3.0.0"
    vytvoreno: Optional[datetime] = None    # created
    cas_vysetreni: Optional[datetime] = None  # exam.time (odpoved ho musi zopakovat)
    pacient: Pacient = field(default_factory=Pacient)
    # predava se jen cast podle okna, ze ktereho uzivatel program spustil; ostatni jsou None
    audio: Optional[TonovaAudiometrie] = None
    tymp: Optional[Tympanometrie] = None
    slovni: Optional[SlovniAudiometrie] = None


# ---------------------------------------------------------------------------------------------------------------
# Vysledek

@dataclass
class Trojice:
    """hodnota pro prave ucho, leve ucho a celkem"""
    R: Optional[Cislo] = None
    L: Optional[Cislo] = None
    celkem: Optional[Cislo] = None


@dataclass
class VysledekAudia:
    """pole okna Hodnotit audiogramu (odpoved "audio")"""
    ztrata_dle_fowlera: Trojice = field(default_factory=Trojice)  # "ztrata sluchu dle Fowlera" (fowler): 0 az 100 [%]
    prumer_vzduch: Trojice = field(default_factory=Trojice)       # "prumer vzdusneho vedeni" (acAvg): -10 az 130 [dB]
    prumer_kost: Trojice = field(default_factory=Trojice)         # "prumer kostniho vedeni" (bcAvg): -10 az 130 [dB]
    who: Optional[str] = None               # "ztrata sluchu dle WHO" (who): text
    skupina: Optional[str] = None           # "skupina ztraty sluchu dle veku" (group): text
    sisi_R: Optional[str] = None            # "vysledek SISI testu" (sisi): text, napr. jako v nabidce okna
    sisi_L: Optional[str] = None            #   "negativní", "neurčitý", "pozitivní"


@dataclass
class VysledekReflexu:
    podnet: Podnet = field(default_factory=Podnet)
    vybavny: bool = False                   # True = "+", False = "-"


@dataclass
class VysledekDecay:
    podnet: Podnet = field(default_factory=Podnet)
    procent: Optional[int] = None           # cele cislo [%]; None = "-" (nemeritelne)


@dataclass
class VysledekTympUcha:
    typ: Optional[str] = None               # "typ krivky" (type): text do 10 znaku, nabidka okna A, B, C, C1, C2, "-"
    ipsi: List[VysledekReflexu] = field(default_factory=list)     # "reflexy IPSI"
    contra: List[VysledekReflexu] = field(default_factory=list)   # "reflexy CONTRA"
    decay_ipsi: List[VysledekDecay] = field(default_factory=list)
    decay_contra: List[VysledekDecay] = field(default_factory=list)


@dataclass
class VysledekTympanometrie:
    """pole okna Hodnotit tympanometrie (odpoved "tymp")"""
    R: Optional[VysledekTympUcha] = None
    L: Optional[VysledekTympUcha] = None


@dataclass
class SrtKrivek:
    R: Optional[Cislo] = None               # "SRT": -10 az 120 [dB]
    L: Optional[Cislo] = None
    bin: Optional[Cislo] = None


@dataclass
class VysledekSlovni:
    """pole SRT ve slovni audiometrii (odpoved "speech")"""
    bez_sluchadla: SrtKrivek = field(default_factory=SrtKrivek)
    se_sluchadlem: SrtKrivek = field(default_factory=SrtKrivek)


@dataclass
class Vysledek:
    audio: Optional[VysledekAudia] = None
    tymp: Optional[VysledekTympanometrie] = None
    slovni: Optional[VysledekSlovni] = None
    # zprava pro uzivatele (note): proc nektery udaj neni, upozorneni k datum apod., kazda polozka na radek
    poznamky: List[str] = field(default_factory=list)
