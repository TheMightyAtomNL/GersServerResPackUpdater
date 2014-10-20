using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Net;
using GersServerResPackUpdater.Properties;

namespace GersServerResPackUpdater
{
    class GersServerResPackUpdater : ApplicationContext
    {
        // NotifyIcon en ContextMenuStrip objecten aanmaken
        NotifyIcon ni = new NotifyIcon();
        ContextMenuStrip cms = new ContextMenuStrip();

        // BackgroundWorker objecten aanmaken
        BackgroundWorker bgwDownloadNewResPackDate = new BackgroundWorker();
        BackgroundWorker bgwDownloadResPack = new BackgroundWorker();

        // Variabelen voor het behouden van de resource pack datum
        // Hier mee kijken we later of de waarde van NewRedPackDate hetzelfde of nieuwer is
        // en dus of er een nieuwe versie van de resource pack beschikbaar is
        DateTime CurrentResPackDate;
        DateTime NewResPackDate;

        // String variabel van het pad naar de .minecraft map onder %appdata%
        string dotMinecraftFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft";

        // String variabel van het pad naar de resourcepack map onder de .minecraft map
        // Nog geen waarde toegekend, dit word in de constructor hieronder gedaan
        string MinecraftResPackFolder;

        bool boolTimedOut = false;
        bool boolGeneralErrorOccured = false;
        string strErrorMessage;

        // Klas constructor
        public GersServerResPackUpdater()
        {
            // ToolStripMenuItem objecten aanmaken en hun Text eigenschappen instellen
            ToolStripMenuItem miStatus = new ToolStripMenuItem(string.Empty);
            ToolStripMenuItem miCheckUpdate = new ToolStripMenuItem("Controleren op updates");
            ToolStripMenuItem miAfsluiten = new ToolStripMenuItem("Afsluiten");

            // Een menu item seperator object aanmaken
            ToolStripSeparator miSeperator = new ToolStripSeparator();

            // Click eventhandlers toekennen aan de ToolStripMenuItem objecten
            miCheckUpdate.Click += miCheckUpdate_Click;
            miAfsluiten.Click += miAfsluiten_Click;

            // ToolStripMenuItem objecten en ToolStripSeparator object toevoegen aan de ContextMenuStrip
            cms.Items.AddRange(new ToolStripItem[] { miStatus, miSeperator, miCheckUpdate, miAfsluiten });

            // NotifyIcon eigenschappen instellen
            ni.Icon = Resources.trayicon;
            ni.BalloonTipIcon = ToolTipIcon.Info;
            ni.BalloonTipTitle = Application.ProductName;
            ni.Visible = true;
            ni.ContextMenuStrip = cms;

            // DoWork eventhandlers toekennen aan de BackgroundWorkers
            bgwDownloadNewResPackDate.DoWork += bgwDownloadNewResPackDate_DoWork;
            bgwDownloadResPack.DoWork += bgwDownloadResPack_DoWork;

            // RunWorkerCompleted eventhandlers toekennen aan de BackgroundWorkers
            bgwDownloadNewResPackDate.RunWorkerCompleted += bgwDownloadVersionFile_RunWorkerCompleted;
            bgwDownloadResPack.RunWorkerCompleted += bgwDownloadResPack_RunWorkerCompleted;

            // String variabel van het pad naar de resourcepack map onder de .minecraft map
            MinecraftResPackFolder = dotMinecraftFolder + "\\resourcepacks";

            // Methode aanroepen dat controleert of de .minecraft map bestaat
            CheckMinecraftFolderExists();

            Test();
        }

        void Test()
        {
            // Get current resourcepack version that was downloaded previously

            //DateTime dateToday = DateTime.Now;
            //int result = DateTime.Compare(resPackDate, dateToday);
            //string relationship;

            //if (result < 0 || result == 0)
            //    relationship = "update available";
            //else
            //    relationship = "no update";

            //MessageBox.Show(string.Format("{0}\n{1}\n\nresult: {2}, {3}", resPackDate.ToLongDateString(), dateToday.ToLongDateString(), result, relationship));
        }

        // Methode dat controleert of de .minecraft map bestaat
        void CheckMinecraftFolderExists()
        {
            // Als de .minecraft map bestaat...
            if (Directory.Exists(dotMinecraftFolder))

                // ...roep een methode aan dat controleert of de resourcepacks map bestaat
                CheckResPackFolderExists();
            else
                // Als de .minecraft map niet bestaat, laat een ballon bericht zien en geef de gebruiken een kans
                // het probleem te verhelpen en het update process opnieuw te laten starten
                ShowMessage("De .minecraft folder kon niet worden gevonden. Zorg dat MC tenminste 1 keer goed is opgestart.\\n" +
                    "Je kunt het update process herstarten door rechts te klikken op het MC pictogram in je tray (naast je Windows klok)\\n" +
                    "en dan op 'Controleren op updates' te klikken.");
        }

        // Methode dat controleert of de resourcepack map onder de .minecraft map bestaat
        void CheckResPackFolderExists()
        {
            // Als de resourcepacks map bestaat...
            if (Directory.Exists(MinecraftResPackFolder))
            {
                // De huidige datum versie van de resource pack ophalen en deze in
                // een globale DateTime variabel opslaan
                CurrentResPackDate = GetCurrentResPackDate();

                // Methode GetNewResPackDate aanroepen dat op zijn beurt weer de 
                // BackgroundWorker aanroept voor het ophalen van de nieuwe datum versie van de resource pack
                GetNewResPackDate();
            }
            else
                // Als de resourcepack map niet bestaat, maak deze dan zelf aan
                Directory.CreateDirectory(MinecraftResPackFolder);
        }

        // Methode voor het inlezen en teruggeven van de huidige datum versie van de resource pack
        DateTime GetCurrentResPackDate()
        {
            using (StreamReader reader = new StreamReader("Ger'sServerCustomPaintingPack.txt"))
            { return DateTime.ParseExact(reader.ReadToEnd(), "dd-MM-yyyy", null); }
        }

        // Methode dat de RunWorkerAsync methode van de bgwDownloadNewResPackDate BackgroundWorker aanroept
        void GetNewResPackDate()
        { bgwDownloadNewResPackDate.RunWorkerAsync(); }

        // BackgroundWorker DoWork methode dat het versie nummer tekstbestandje download
        private void bgwDownloadNewResPackDate_DoWork(object sender, DoWorkEventArgs e)
        {
            string url = "http://www.themightyatom.nl/stuff/";
            string resPack = "Ger%27sServerCustomPaintingPack.zip";

            try
            {
                HttpWebRequest request = (HttpWebRequest)(HttpWebRequest.Create(url + resPack));
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Timeout = 30000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream datastream = response.GetResponseStream();

                    using (StreamReader reader = new StreamReader(datastream))
                    {
                        NewResPackDate = DateTime.ParseExact(reader.ReadToEnd(), "dd-MM-yyyy", null);
                        MessageBox.Show("Argh!\n" + NewResPackDate.ToLongDateString());
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                    boolTimedOut = true;
            }
            catch (Exception ex)
            {
                boolGeneralErrorOccured = true;
                strErrorMessage = ex.Message;
            }
        }

        private void bgwDownloadVersionFile_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show(string.Format("bgwDownloadVersionFile_RunWorkerCompleted fired!" +
                "\n\n Value of boolTimedOut: {0}\nValue of boolGeneralErrorOccured: {1}\n\n" +
                "CurrentResPackDate: {2}\n NewResPackDate: {3}",
                boolTimedOut, boolGeneralErrorOccured, CurrentResPackDate.ToLongDateString(), NewResPackDate.ToLongDateString()));
        }

        // BackgroundWorker DoWork methode dat het nieuwe resource pack zip bestand download
        private void bgwDownloadResPack_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        // 
        void bgwDownloadResPack_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }






























        // Method voor het laten zien van ballon berichten aan de gebruiker
        void ShowMessage(string msg)
        {
            ni.BalloonTipText = msg;
            ni.ShowBalloonTip(10000);
        }

        // Methode voor het opnieuw starten van de updater checker
        // Aangeroepen wanneer miCheckUpdate is geklikt
        void miCheckUpdate_Click(object sender, EventArgs e)
        {
            // Methode aanroepen dat controleert of de .minecraft map bestaat
            CheckMinecraftFolderExists();
        }

        // Methode to exit the application
        // Fired when miAfsluiten is clicked
        void miAfsluiten_Click(object sender, EventArgs e)
        {
            // Dispose the NotifyIcon object
            ni.Dispose();

            // Exit the applcation
            Application.Exit();
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool nieuw;
            System.Threading.Mutex m_Mutex = new System.Threading.Mutex
                (true, "GersServerResPackUpdaterMutex", out nieuw);
            if (nieuw)
                Application.Run(new GersServerResPackUpdater());
            else
                MessageBox.Show("GersServerResPackUpdater draait al.\nKijk naar een pictogram van het Minecraft logo in het systeemvak.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}