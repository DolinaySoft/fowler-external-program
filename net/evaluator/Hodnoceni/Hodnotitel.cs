using System.Collections.Generic;
using Evaluator.Model;

namespace Evaluator.Hodnoceni
{
    /// <summary>
    /// Hodnoceni predanych dat. Zakladni trida nic neurci - vsechny metody Hodnot... vrati null.
    /// Vlastni hodnoceni je potomek, ktery prepise (override) jen metody casti, ktere chce hodnotit,
    /// a pripoji se do programu metodou Pripoj (popis v net/README.md):
    /// <code>
    /// public class MujHodnotitel : Hodnotitel
    /// {
    ///     public override VysledekAudia HodnotAudio(TonovaAudiometrie audio, Pacient pacient, List&lt;string&gt; poznamky)
    ///     {
    ///         ...
    ///     }
    /// }
    ///
    /// public partial class Hodnotitel
    /// {
    ///     static partial void Pripoj(ref Hodnotitel hodnotitel)
    ///     {
    ///         hodnotitel = new MujHodnotitel();
    ///     }
    /// }
    /// </code>
    /// Kazda metoda dostane vsechna predana data sve casti vysetreni a udaje pacienta a vrati vysledek pro pole okna
    /// ve Fowleru; udaj, ktery nelze nebo se nema urcit, necha null (pole v okne pak zustane, jak je). Vrati-li metoda
    /// null, program tuto cast nehodnoti a odpoved to uvede v poznamce. Do seznamu poznamky lze pridat zpravu pro
    /// uzivatele, Fowler ji zobrazi po vlozeni odpovedi.
    /// </summary>
    public partial class Hodnotitel
    {
        /// <summary>tonova audiometrie - pole okna Hodnotit audiogramu</summary>
        public virtual VysledekAudia HodnotAudio(TonovaAudiometrie audio, Pacient pacient, List<string> poznamky)
        {
            return null;
        }

        /// <summary>tympanometrie - pole okna Hodnotit tympanometrie</summary>
        public virtual VysledekTympanometrie HodnotTympano(Tympanometrie tymp, Pacient pacient, List<string> poznamky)
        {
            return null;
        }

        /// <summary>slovni audiometrie - pole SRT ve slovni audiometrii (fslovni)</summary>
        public virtual VysledekSlovni HodnotSlovni(SlovniAudiometrie slovni, Pacient pacient, List<string> poznamky)
        {
            return null;
        }

        //Pripojeni potomka: kdyz program metodu Pripoj neobsahuje (napr. po odstraneni souboru s potomkem),
        //prekladac jeji volani vynecha a pouzije se tato trida, ktera nic neurci. Dve metody Pripoj prekladac odmitne.
        static partial void Pripoj(ref Hodnotitel hodnotitel);

        public static Hodnotitel Vytvor()
        {
            Hodnotitel hodnotitel = null;
            Pripoj(ref hodnotitel);
            return hodnotitel ?? new Hodnotitel();
        }

        /// <summary>ohodnoti predanou cast vysetreni (predava se jen ta, ze ktere uzivatel program spustil)</summary>
        internal Vysledek Hodnot(Vstup vstup)
        {
            Vysledek vysledek = new Vysledek();
            if (vstup.Audio != null)
            {
                vysledek.Audio = HodnotAudio(vstup.Audio, vstup.Pacient, vysledek.Poznamky);
                if (vysledek.Audio == null)
                    vysledek.Poznamky.Add("Program audiogram nehodnotí.");
            }
            if (vstup.Tymp != null)
            {
                vysledek.Tymp = HodnotTympano(vstup.Tymp, vstup.Pacient, vysledek.Poznamky);
                if (vysledek.Tymp == null)
                    vysledek.Poznamky.Add("Program tympanometrii nehodnotí.");
            }
            if (vstup.Slovni != null)
            {
                vysledek.Slovni = HodnotSlovni(vstup.Slovni, vstup.Pacient, vysledek.Poznamky);
                if (vysledek.Slovni == null)
                    vysledek.Poznamky.Add("Program slovní audiometrii nehodnotí.");
            }
            return vysledek;
        }
    }
}
