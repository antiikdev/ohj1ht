using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// @author  Ilkka Kotilainen
/// @version 16.8.2021
///
/// <summary>
/// Peli, jossa kerataan putoavia objekteja.
/// </summary>
public class LoveMachine : PhysicsGame
{
    private PhysicsObject alaReuna;

    private IntMeter pisteet;
    private IntMeter tasolaskuri;
    private IntMeter elamat;

    private Timer kokonaisAjastin = new Timer();

    private string kerattavaObjekti = "Heart";
    private int objektejaIlmassa;
    private List<double> tasojenSuoritusAjat = new List<double>();

    // pisteet per taso:             1.  2.  3.  4.   5.
    private int[] tasoPisteet = new int[] { 10, 30, 60, 100, 150 };


    public override void Begin()
    {
        LuoValikko();
    }


    /// <summary>
    /// Alkuvalikko
    /// </summary>
    private void LuoValikko()
    {
        ClearAll();

        MultiSelectWindow alkuValikko = new MultiSelectWindow("Main Menu",
            "Start game", "Select object", "Quit game");
        Add(alkuValikko);

        alkuValikko.AddItemHandler(0, AloitaPeli);
        alkuValikko.AddItemHandler(1, ValitseObjekti);
        alkuValikko.AddItemHandler(2, Exit);
    }


    /// <summary>
    /// Valitaan kerattavan objektin tyyppi
    /// </summary>
    private void ValitseObjekti()
    {
        MultiSelectWindow alkuValikko = new MultiSelectWindow("Select object",
    "Heart", "Sunny side up egg", "Hippo", "Return to Main menu");
        Add(alkuValikko);

        alkuValikko.AddItemHandler(0, delegate { kerattavaObjekti = "Heart"; LuoValikko(); } );
        alkuValikko.AddItemHandler(1, delegate { kerattavaObjekti = "Egg"; LuoValikko(); } );
        alkuValikko.AddItemHandler(2, delegate { kerattavaObjekti = "Hippo"; LuoValikko(); } );
        alkuValikko.AddItemHandler(3, LuoValikko);
    }


    /// <summary>
    /// Kaynnistaa pelin
    /// </summary>
    private void AloitaPeli()
    {
        // nollataan kun peli alkaa uudelleen
        ClearAll();
        kokonaisAjastin.Reset();
        tasojenSuoritusAjat.Clear();
        objektejaIlmassa = 0;

        //luodaan pelin kaynnistyessa
        LuoKentta();
        LuoOhjaimet();
        LuoObjektitAjastimella();
    }


    /// <summary>
    /// Luodaan kentta
    /// </summary>
    private void LuoKentta()
    {
        Camera.ZoomToLevel();
        // Level.Background.Color = Color.LightCyan;
        Level.Background.CreateGradient(Color.White, Color.SkyBlue);

        // painovoima kappaleilla y-suunnassa:
        Gravity = new Vector(0.0, -25.0);

        // pistelaskuri
        pisteet = new IntMeter(0, -1, 1000); // pisteet 0, min., max
        LuoRuutu(pisteet, Screen.Left + 100, Screen.Top - 35, "Points");

        // tasolaskuri
        tasolaskuri = new IntMeter(1, 1, 5); // taso 1., min., max.
        LuoRuutu(tasolaskuri, Screen.Right - 80, Screen.Top - 30, "Level");

        // Elamien laskuri
        elamat = new IntMeter(10, 0, 10);
        LuoRuutu(elamat, Screen.Left + 270, Screen.Top - 35, "Hearts");
        PhysicsObject Heart = new PhysicsObject(50, 50);
        Heart.Shape = Shape.Heart;
        Heart.Color = Color.Red;
        Heart.X = Screen.Left + 365;
        Heart.Y = Screen.Top - 15;
        Heart.IgnoresGravity = true;
        Add(Heart);

        // alareunan luonti pistelaskua varten:
        alaReuna = Level.CreateBottomBorder();
        alaReuna.Restitution = 1.0;
        alaReuna.KineticFriction = 0.5;
        alaReuna.IsVisible = false;

        // vasen reuna pitamaan objektit koossa
        PhysicsObject vasenReuna = Level.CreateLeftBorder();
        vasenReuna.Restitution = 1.0;
        vasenReuna.KineticFriction = 0.5;
        vasenReuna.IsVisible = false;

        // oikea reuna pitamaan objektit koossa
        PhysicsObject oikeaReuna = Level.CreateRightBorder();
        oikeaReuna.Restitution = 1.0;
        oikeaReuna.KineticFriction = 0.5;
        oikeaReuna.IsVisible = false;

        // 3. PELIN KOKONAISAJASTIN
        kokonaisAjastin.Start();
        Label aikaNaytto = new Label();
        aikaNaytto.X = Screen.Right - 180;
        aikaNaytto.Y = Screen.Top - 30;
        aikaNaytto.TextColor = Color.Black;
        aikaNaytto.BorderColor = Color.Gray;
        aikaNaytto.Color = Level.BackgroundColor;
        aikaNaytto.TextScale = new Vector(1.5, 1.5);
        aikaNaytto.DecimalPlaces = 1;
        aikaNaytto.BindTo(kokonaisAjastin.SecondCounter);
        Add(aikaNaytto);
    }


    /// <summary>
    /// Ajastin 1) synnyttaa objekteja
    /// Ajastin 2) kiihdytetaan 1-ajastinta ja objektien ilmestymista
    /// </summary>
    private void LuoObjektitAjastimella()
    {
        // 1. AJASTIN
        Timer ajastin = new Timer();
        // intervalli ajastimen tapahtumille
        ajastin.Interval = 3; // aloitusaika (s) objektin pudotukseen
        // ajastimen intevallin tapahtuma: luodaan objekteja maaritellyn ajan valein
        ajastin.Timeout += delegate { LuoObjekti(); };
        ajastin.Start();

        // 2) AJASTIN, joka on 1. AJASTIMEN NOPEUTUS
        Timer ajastimenNopeutus = new Timer();
        // ajastimen nopeutuksen intevalli
        ajastimenNopeutus.Interval = 3; // muutoksen intervalli (s)
        // nopeutetaan ajastinta
        ajastimenNopeutus.Timeout += delegate
        {
            if (ajastin.Interval <= 0.2) return;
            ajastin.Interval -= 0.2; // muutosnopeus (s)
        };
        ajastimenNopeutus.Start();
    }



    /// <summary>
    /// Aliohjelma luo pelin fysiikkaobjekteja
    /// satunnaiseen sijaintiin koordinaatistolla:
    /// x-koord: min. 30, max. 970
    /// y-koord: min. 70, max. 730
    /// ylatason koordinaatit objektien ilmestymiselle korkeammalla:
    /// y-koord: min. 500, max. 730
    /// Omenoiden luonnin lkm riippuu tasolaskurin arvosta (silmukassa).
    /// </summary>
    private void LuoObjekti()
    {
        if (objektejaIlmassa == tasoPisteet[tasolaskuri.Value - 1]) return;

        PhysicsObject objekti = new PhysicsObject(100, 100);
        objekti.Image = LoadImage(kerattavaObjekti);
        objekti.X = RandomGen.NextDouble(Level.Left + 30, Level.Right - 30);
        objekti.Y = Level.Top - RandomGen.NextDouble(100,200);
        //objekti.Tag = "objekti"; // jos .tag kerattaisi objekteja
        Add(objekti);

        // Mouse.ListenOn: jokaista luotua objektia kuunnellaan taman luonnin jalkeen:
        Mouse.ListenOn<IPhysicsObject>(objekti, HoverState.On, MouseButton.Left, ButtonState.Pressed,
            delegate (IPhysicsObject objekti) { PoimiObjekti(objekti); }, "Pick", objekti);

        objekti.CollisionIgnoreGroup = 1;
        // objekti.CanRotate = false; // jos pyoriminen poistetaan

        //  objektien tormays alareunaan:
        AddCollisionHandler(objekti, ObjektinTormays);

        // lasketaan luotujen objektien maaraa
        objektejaIlmassa++;
    }


    /// <summary>
    /// Luo numeroita esittavan nayttoruudun
    /// </summary>
    /// <param name="laskuri">laskuri jota lasketaan</param>
    /// <param name="x">sijainti x-koord</param>
    /// <param name="y">sijainti y-koord</param>
    /// <param name="teksti">ruutukentan kayttajalle nakyva nimi</param>
    private void LuoRuutu(IntMeter laskuri, double x, double y, string teksti)
    {
        Label ruutu = new Label();
        ruutu.X = x;
        ruutu.Y = y;
        ruutu.Title = teksti;
        ruutu.BorderColor = Color.Gray;
        ruutu.TextColor = Color.Black;
        ruutu.BorderColor = Color.Gray;
        ruutu.Color = Level.BackgroundColor;
        ruutu.TextScale = new Vector(1.5, 1.5);
        ruutu.BindTo(laskuri);
        Add(ruutu);
    }


    /// <summary>
    /// Asettaa nappaimiston ja hiiren ohjaimet kuunteluun
    /// </summary>
    private void LuoOhjaimet()
    {
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Quit game");
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Instructions");
        Keyboard.Listen(Key.Enter, ButtonState.Pressed, LuoValikko, "New Game");
        PhoneBackButton.Listen(ConfirmExit, "Quit game");
        
        /* // vaihtoehtoinen toteutus kuunnella hiirta ja klikata objekteja 
        Mouse.Listen(MouseButton.Left...);
        List<GameObject> objektit = GetObjectsWithTag("Objekti");
        foreach (var objekti in objektit)
        {
            if (Mouse.IsCursorOn(objekti))
            {
                objekti.Destroy();
                pistelaskuri...
            }
        }*/
    }


    /// <summary>
    /// Poimii objektin, eli poistaa sen, ja lisaa pisteen
    /// </summary>
    /// <param name="olio">olio joka tuhotaan</param>
    private void PoimiObjekti(IPhysicsObject olio)
    {
        olio.Destroy();
        pisteet.Value += 1;

        // JOS voitetaan, eli saavutetaan maksimipisteet, TAI JOS taso vaihtuu
        if (pisteet.Value == tasoPisteet[tasoPisteet.Length - 1])
        {
            AjastintenTallennus();
            PelinLoppu();
        }
        else if (tasoPisteet[tasolaskuri.Value - 1] == pisteet.Value)
        {
            AjastintenTallennus();
            tasolaskuri.Value++;
            kokonaisAjastin.Start();
            LuoObjektitAjastimella();
        }
    }


    /// <summary>
    /// Objektien alareunaan tormayksen kasittely:
    /// poistetaan elama ja varmistetaan loppuuko peli.
    /// </summary>
    /// <param name="objekti">objekti</param>
    /// <param name="kohde"></param>
    private void ObjektinTormays(PhysicsObject objekti, PhysicsObject kohde)
    {
        if (kohde == alaReuna) elamat.Value -= 1;
        if (elamat.Value <= 0)
        {
            // tallennetaan viimeisen tason pelin loppumisaika
            AjastintenTallennus();
            PelinLoppu();
        }
    }


    /// <summary>
    /// Lopettaa pelin
    /// </summary>
    private void PelinLoppu()
    {
        ClearControls();
        ClearTimers();

        Label viestiLoppu = new Label("Game Over! You collected "
            + pisteet.Value + " points from " + tasolaskuri.Value + " / " +
            tasoPisteet.Length + " levels. Press Enter for a new game.");
        viestiLoppu.HorizontalAlignment = HorizontalAlignment.Center;
        Add(viestiLoppu);

        TulostaAjat();

        Keyboard.Listen(Key.Enter, ButtonState.Pressed, Begin, "New Game");
    }


    /// <summary>
    /// Tulostaa listaan tallennetut double-tyyppiset
    /// ajat ruutuun.
    /// </summary>
    private void TulostaAjat()
    {
        if (tasojenSuoritusAjat == null || tasojenSuoritusAjat[0] == 0) return;

        int level = 1;


        for (int i = 0; i < tasolaskuri.Value; i++)
        {
            double aikaD = Math.Round(tasojenSuoritusAjat[i],2);
            Label aika = new Label("Level " + level + ": " +  aikaD + " s");
            aika.Position = new Vector(0, -100) - new Vector(0, i * 25);
            Add(aika);

            level++;
        }

        // lasketaan kokonaisaika
        double kokonaisaika = LaskeKokonaisaika(tasojenSuoritusAjat);
        kokonaisaika = Math.Round(kokonaisaika, 2);
        Label loppuaika = new Label("Your total time was " + kokonaisaika + " s");
        loppuaika.Position = new Vector(0, 60);
        Add(loppuaika);

        Label viestiAjoista = new Label("Your time per level:");
        viestiAjoista.Position = new Vector(0, -70);
        Add(viestiAjoista);
    }


    /// <summary>
    /// Lasketaan kokonaisaika sekunneista
    /// </summary>
    /// <param name="ajat">listassa ajat sekunneissa</param>
    /// <returns></returns>
    private static double LaskeKokonaisaika(List<double> ajat)
    {
        double laskettuAika = 0;

        for (int i = 0; i < ajat.Count; i++)
        {
            laskettuAika += ajat[i];
        }
        return laskettuAika;
    }


    /// <summary>
    /// Tallentaa (tason aika) seka pysayttaa ja nollaa ajastimen
    /// </summary>
    private void AjastintenTallennus()
    {
        tasojenSuoritusAjat.Add(kokonaisAjastin.SecondCounter.Value);
        kokonaisAjastin.Stop();
        kokonaisAjastin.Reset();
    }

}

