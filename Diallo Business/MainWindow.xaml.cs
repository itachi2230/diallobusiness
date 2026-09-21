using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Diallo_Business
{
    public partial class MainWindow : Window
    {
        private bool isCollapsed = false;
        private bool transitionSession = false; // true pendant une déconnexion / reconnexion

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            AfficherUtilisateur();
            AppliquerDroitsParRole();

            // Charger le Dashboard par défaut au démarrage
            MainContainer.Content = new UC_Dashboard();
            TxtPageTitle.Text = "Dashboard";
        }

        // --- CARTE UTILISATEUR & DROITS PAR RÔLE ---
        public void AfficherUtilisateur()
        {
            if (Utils.CurrentUser != null)
            {
                TxtUserName.Text = Utils.CurrentUser.Nom;
                TxtUserRole.Text = Utils.CurrentUser.Role + " - " + Utils.CurrentUser.StatutAffiche;
            }
        }

        // Les pages d'administration (Utilisateurs, Paramètres) sont réservées aux administrateurs
        private void AppliquerDroitsParRole()
        {
            Visibility visibilite = Utils.EstAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnUtilisateurs.Visibility = visibilite;
            BtnParametres.Visibility = visibilite;
        }

        // --- FERMETURE DE LA FENÊTRE (ShutdownMode = OnExplicitShutdown) ---
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // Pendant une déconnexion, une nouvelle fenêtre est déjà ouverte : on ne quitte pas l'application
            if (!transitionSession) Application.Current.Shutdown();
        }
        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!isCollapsed)
            {
                // RÉDUIRE LA BARRE
                SideColumn.Width = new GridLength(100);
                LogoText.Visibility = Visibility.Collapsed;
                UserInfo.Visibility = Visibility.Collapsed;

                // Ajuster l'alignement du bouton toggle pour qu'il soit centré
                BtnToggle.HorizontalAlignment = HorizontalAlignment.Center;
                BtnToggle.Margin = new Thickness(0, 10, 0, 0);

                isCollapsed = true;
            }
            else
            {
                // AGRANDIR LA BARRE
                SideColumn.Width = new GridLength(280);
                LogoText.Visibility = Visibility.Visible;
                UserInfo.Visibility = Visibility.Visible;

                BtnToggle.HorizontalAlignment = HorizontalAlignment.Right;
                BtnToggle.Margin = new Thickness(0, 10, 10, 0);

                isCollapsed = false;
            }
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                BtnMaximize.Content = "❐";
            }
            else
            {
                this.WindowState = WindowState.Normal;
                BtnMaximize.Content = "▢";
            }
        }

        // --- MOTEUR DE NAVIGATION ---
        private void NavClick(object sender, RoutedEventArgs e)
        {
            AfficherPage(sender as Button);
        }

        // Permet à une page (ex: UC_Parametres) de demander une navigation
        public void NaviguerVers(string nomBouton)
        {
            Button[] boutons = { BtnDashboard, BtnStock, BtnFactures, BtnBilans, BtnUtilisateurs, BtnParametres };
            foreach (var b in boutons)
            {
                if (b.Name == nomBouton) { AfficherPage(b); return; }
            }
        }

        private void AfficherPage(Button btn)
        {
            if (btn == null) return;

            // Le titre vient du Tag (plus de découpe fragile du Content avec Substring)
            TxtPageTitle.Text = btn.Tag?.ToString() ?? "";

            // Switch entre les UserControls
            switch (btn.Name)
            {
                case "BtnDashboard":
                    MainContainer.Content = new UC_Dashboard();
                    break;

                case "BtnStock":
                    MainContainer.Content = new UC_Stock();
                    break;

                case "BtnFactures":
                    MainContainer.Content = new UC_Facturation();
                    break;

                case "BtnBilans":
                    MainContainer.Content = new UC_Bilans();
                    break;

                case "BtnUtilisateurs":
                    if (!Utils.EstAdmin) { MessageBox.Show("Accès réservé aux administrateurs."); return; }
                    MainContainer.Content = new UC_Utilisateurs();
                    break;

                case "BtnParametres":
                    if (!Utils.EstAdmin) { MessageBox.Show("Accès réservé aux administrateurs."); return; }
                    MainContainer.Content = new UC_Parametres();
                    break;
            }
        }

        // --- MON COMPTE (édition de son propre profil / mot de passe) ---
        private void BtnMonCompte_Click(object sender, RoutedEventArgs e)
        {
            var fenetre = new Win_MonCompte { Owner = this };
            if (fenetre.ShowDialog() == true)
            {
                AfficherUtilisateur();                 // nom/rôle mis à jour dans la carte utilisateur
                AppliquerDroitsParRole();              // le rôle a pu changer
                MainContainer.Content = new UC_Dashboard(); // rafraîchit la page courante
                TxtPageTitle.Text = "Dashboard";
            }
        }

        // --- DÉCONNEXION : retour à l'écran de login ---
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Voulez-vous vraiment vous déconnecter ?", "Déconnexion",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            Utils.Deconnecter();

            transitionSession = true; // évite le Shutdown automatique en fermant cette fenêtre

            var login = new Win_Login();
            if (login.ShowDialog() == true && Utils.CurrentUser != null)
            {
                new MainWindow().Show(); // nouvelle session
            }
            this.Close();

            // Si la reconnexion a échoué (l'utilisateur a fermé le login), on quitte proprement
            if (Utils.CurrentUser == null) Application.Current.Shutdown();
        }

        // Permet à une page (ex: UC_Parametres) de demander la déconnexion
        public void DemanderDeconnexion() => BtnLogout_Click(null, null);
    }
}