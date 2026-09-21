using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Diallo_Business
{
    /// <summary>
    /// Édition de son propre compte : nom, identifiant et changement de mot de passe.
    /// Le rôle et le statut ne sont pas modifiables ici (réservés à la page Utilisateurs d'un admin).
    /// </summary>
    public partial class Win_MonCompte : Window
    {
        public Win_MonCompte()
        {
            InitializeComponent();
            Charger();
        }

        private void Charger()
        {
            var user = Utils.CurrentUser;
            if (user == null) { Close(); return; }

            // Toujours retravailler sur la version fraîche du fichier JSON
            var frais = Utils.GetUtilisateurParId(user.Id) ?? user;

            InpNom.Text = frais.Nom;
            InpUsername.Text = frais.Username;
            TxtEntete.Text = $"Rôle : {frais.Role}   •   Statut : {frais.StatutAffiche}   •   Dernière connexion : {frais.DerniereConnexionAffiche}";
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
                BtnEnregistrer_Click(sender, e);
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            var frais = Utils.GetUtilisateurParId(Utils.CurrentUser?.Id ?? 0);
            if (frais == null) { Close(); return; }

            string nom = InpNom.Text.Trim();
            string username = InpUsername.Text.Trim();
            string ancien = InpAncien.Password;
            string nouveau = InpNouveau.Password;
            string confirmation = InpConfirm.Password;

            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(username))
            {
                ShowMessage("Le nom complet et l'identifiant sont obligatoires.");
                return;
            }
            if (username.Contains(" "))
            {
                ShowMessage("L'identifiant ne doit pas contenir d'espace.");
                return;
            }
            // Unicité de l'identifiant (hors compte courant)
            var doublon = Utils.GetUtilisateurs().FirstOrDefault(u => u.Id != frais.Id && u.Username != null &&
                                                                 u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (doublon != null)
            {
                ShowMessage("Cet identifiant est déjà utilisé par un autre compte.");
                return;
            }

            bool changementMdp = !string.IsNullOrEmpty(ancien) || !string.IsNullOrEmpty(nouveau) || !string.IsNullOrEmpty(confirmation);
            if (changementMdp)
            {
                if (Utils.HashPassword(ancien) != frais.MotDePasse)
                {
                    ShowMessage("Le mot de passe actuel est incorrect.");
                    return;
                }
                if (nouveau.Length < 6)
                {
                    ShowMessage("Le nouveau mot de passe doit contenir au moins 6 caractères.");
                    return;
                }
                if (nouveau != confirmation)
                {
                    ShowMessage("Les deux nouveaux mots de passe ne correspondent pas.");
                    return;
                }
            }

            frais.Nom = nom;
            frais.Username = username;
            if (changementMdp) frais.MotDePasse = Utils.HashPassword(nouveau);

            Utils.UpdateUtilisateur(frais);
            Utils.CurrentUser = frais; // la session reste valide avec les nouvelles informations
            if (changementMdp) Utils.LogAction("Mot de passe modifié par " + frais.Username);

            MessageBox.Show("Votre compte a été mis à jour.", "Mon compte", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
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