using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Diallo_Business
{
        /// <summary>
    /// Écran de connexion : fixe Utils.CurrentUser si le compte existe, est ACTIF et que le mot de passe est correct.
    /// Compte par défaut créé au premier lancement : admin / admin123.
    /// Les comptes créés via « Créer un compte » restent « En attente » jusqu'à activation par un administrateur.
    /// </summary>
    public partial class Win_Login : Window
    {
        // Verrouillage anti-double-appel : le bouton IsDefault peut être déclenché en même
        // temps que InpPassword_KeyDown. Ce drapeau garantit qu'une seule tentative de
        // connexion est traitée, évitant les crashes liés à des appels simultanés sur
        // une fenêtre en cours de fermeture.
        private bool _connexionEnCours = false;

        public Win_Login()
        {
            InitializeComponent();
            Loaded += (s, e) => InpUsername.Focus();
            // Rappel du compte par défaut uniquement s'il s'agit du tout premier lancement
            TxtRappelDefault.Visibility = Utils.GetUtilisateurs().Count <= 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void InpPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Bloque le routage vers le bouton IsDefault pour éviter un second appel
                e.Handled = true;
                BtnLogin_Click(sender, e);
            }
        }

                        
        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            // DialogResult reste null → App.OnStartup appellera Shutdown()
            Close();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Anti-double-appel : le bouton IsDefault peut être déclenché simultanément
            // avec InpPassword_KeyDown. Ce verrou garantit qu'une seule tentative est exécutée.
            if (_connexionEnCours) return;
            _connexionEnCours = true;

            try
            {
                string username = InpUsername.Text.Trim();
                string password = InpPassword.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowMessage("Veuillez saisir un nom d'utilisateur et un mot de passe.", false);
                    _connexionEnCours = false;
                    return;
                }

                var user = Utils.GetUtilisateurParUsername(username);

                // Mot de passe stocké haché (SHA-256) — on compare les empreintes
                if (user == null || user.MotDePasse != Utils.HashPassword(password))
                {
                    Utils.LogAction("Échec de connexion : " + username);
                    ShowMessage("Identifiants incorrects.", false);
                    InpPassword.Clear();
                    InpPassword.Focus();
                    _connexionEnCours = false;
                    return;
                }

                // ⛔ Comptes non activés : connexion refusée avec message explicite
                if (!user.EstActif)
                {
                    string message = user.StatutAffiche == "En attente"
                        ? "Votre compte est en attente d'activation par un administrateur."
                        : "Ce compte a été désactivé. Contactez un administrateur.";
                    Utils.LogAction("Connexion refusée (" + user.StatutAffiche + ") : " + user.Username);
                    ShowMessage(message, false);
                    _connexionEnCours = false;
                    return;
                }

                Utils.CurrentUser = user;
                Utils.EnregistrerConnexion(user.Id);
                Utils.LogAction("Connexion : " + user.Nom + " (" + user.Role + ")");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                // En dernier recours, on loggue et on évite le crash de l'app lié à ShowDialog() qui retournerait null
                Utils.LogAction("ERREUR login : " + ex.Message);
                ShowMessage("Une erreur est survenue lors de la connexion.", false);
                _connexionEnCours = false;
            }
        }

        private void BtnCreerCompte_Click(object sender, RoutedEventArgs e)
        {
            var inscription = new Win_Register { Owner = this };
            if (inscription.ShowDialog() == true && !string.IsNullOrEmpty(inscription.UsernameCree))
            {
                InpUsername.Text = inscription.UsernameCree;
                InpPassword.Clear();
                InpPassword.Focus();
                ShowMessage("Compte créé. Il doit être activé par un administrateur avant la première connexion.", true);
                TxtRappelDefault.Visibility = Visibility.Collapsed;
            }
        }

        // Message unique : rouge = erreur, vert = information
        private void ShowMessage(string message, bool succes)
        {
            TxtMessage.Text = message;
            TxtMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(succes ? "#4ADE80" : "#F87171"));
            TxtMessage.Visibility = Visibility.Visible;
        }
    }
}