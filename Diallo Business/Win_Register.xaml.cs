using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    /// <summary>
    /// Auto-inscription d'un agent depuis l'écran de connexion.
    /// Le compte est créé avec le statut « En attente » : il ne peut pas se connecter
    /// tant qu'un administrateur ne l'a pas activé (page Utilisateurs).
    /// </summary>
    public partial class Win_Register : Window
    {
        /// <summary>Identifiant du compte créé (renvoyé à Win_Login pour pré-remplissage).</summary>
        public string UsernameCree { get; private set; }

        public Win_Register()
        {
            InitializeComponent();
            Loaded += (s, e) => InpNom.Focus();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void InpConfirm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnCreer_Click(sender, e);
            }
        }

        private void BtnCreer_Click(object sender, RoutedEventArgs e)
        {
            string nom = InpNom.Text.Trim();
            string username = InpUsername.Text.Trim();
            string password = InpPassword.Password;
            string confirmation = InpConfirm.Password;

            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowMessage("Tous les champs sont obligatoires.");
                return;
            }
            if (username.Length < 4 || username.Contains(" "))
            {
                ShowMessage("L'identifiant doit contenir au moins 4 caractères et aucun espace.");
                return;
            }
            if (password.Length < 6)
            {
                ShowMessage("Le mot de passe doit contenir au moins 6 caractères.");
                return;
            }
            if (password != confirmation)
            {
                ShowMessage("Les deux mots de passe ne correspondent pas.");
                return;
            }
            if (Utils.GetUtilisateurParUsername(username) != null)
            {
                ShowMessage("Cet identifiant est déjà utilisé. Choisissez-en un autre.");
                return;
            }

            Utils.SaveUtilisateur(new Utilisateur
            {
                Nom = nom,
                Username = username,
                MotDePasse = Utils.HashPassword(password),
                Role = "Agent",
                Statut = "En attente", // ⛔ inactif jusqu'à activation par un administrateur
                DateCreation = DateTime.Now
            });

            Utils.LogAction("Demande de compte (en attente d'activation) : " + username);

            UsernameCree = username;
            MessageBox.Show("Votre compte a été créé.\n\nIl est « En attente » : un administrateur doit l'activer " +
                            "avant que vous puissiez vous connecter.", "Compte créé",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowMessage(string message)
        {
            TxtMessage.Text = message;
            TxtMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
            TxtMessage.Visibility = Visibility.Visible;
        }
    }
}