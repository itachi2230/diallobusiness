using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    /// <summary>
    /// Page ADMIN dédiée à la gestion des comptes : création, activation/désactivation,
    /// modification (nom, identifiant, rôle, statut), réinitialisation de mot de passe et suppression.
    /// Garde-fous : pas d'auto-désactivation, toujours au moins un administrateur actif,
    /// identifiants uniques.
    /// </summary>
    public partial class UC_Utilisateurs : UserControl
    {
        private Utilisateur currentEditing = null;

        public UC_Utilisateurs()
        {
            InitializeComponent();
            RefreshGrid();
        }

        // --- LISTE / FILTRES / STATS ---
        private void RefreshGrid()
        {
            var users = Utils.GetUtilisateurs();

            string filtre = (TxtSearch.Text ?? "").Trim().ToLower();
            if (filtre.Length > 0)
                users = users.Where(u => (u.Nom ?? "").ToLower().Contains(filtre) ||
                                         (u.Username ?? "").ToLower().Contains(filtre)).ToList();

            string statut = (ComboFiltre.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Tous";
            if (statut == "Actifs") users = users.Where(u => u.EstActif).ToList();
            else if (statut == "En attente") users = users.Where(u => u.StatutAffiche == "En attente").ToList();
            else if (statut == "Désactivés") users = users.Where(u => u.StatutAffiche == "Désactivé").ToList();

            GridUsers.ItemsSource = null;
            GridUsers.ItemsSource = users.OrderBy(u => u.Nom).ToList();
            UpdateStats();
        }

        private void UpdateStats()
        {
            var tous = Utils.GetUtilisateurs();
            TxtStatTotal.Text = tous.Count.ToString();
            TxtStatActifs.Text = tous.Count(u => u.EstActif).ToString();
            TxtStatAttente.Text = tous.Count(u => u.StatutAffiche == "En attente").ToString();
            TxtStatDesactives.Text = tous.Count(u => u.StatutAffiche == "Désactivé").ToString();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshGrid();

        private void ComboFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridUsers == null) return; // événement déclenché pendant InitializeComponent
            RefreshGrid();
        }

        // --- AIDE À LA DÉCISION / GARDE-FOUS ---
        private bool EstMoi(Utilisateur u) => u != null && u.Id == (Utils.CurrentUser?.Id ?? -1);

        private bool EstDernierAdminActif(Utilisateur u) =>
            u != null && u.Role == "Admin" && u.EstActif && Utils.NombreAdminsActifs() <= 1;

        // --- FORMULAIRE ---
        private void BtnShowAdd_Click(object sender, RoutedEventArgs e)
        {
            currentEditing = null;
            TxtFormTitle.Text = "Nouveau compte";
            TxtLabelPassword.Text = "Mot de passe";
            TxtHint.Text = "Le mot de passe est obligatoire lors de la création d'un compte.";
            InpNom.Text = ""; InpUsername.Text = ""; InpPassword.Password = "";
            InpRole.SelectedIndex = 1;   // Agent par défaut
            InpStatut.SelectedIndex = 0; // Actif par défaut (compte créé par un admin)
            ColFormUser.Width = new GridLength(350);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ColFormUser.Width = new GridLength(0);
            currentEditing = null;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as Button)?.DataContext as Utilisateur;
            if (user == null) return;

            currentEditing = user;
            TxtFormTitle.Text = "Modifier le compte";
            TxtLabelPassword.Text = "Nouveau mot de passe (optionnel)";
            TxtHint.Text = "Laissez le mot de passe vide pour le conserver. Utilisez 🔑 pour une réinitialisation directe.";
            InpNom.Text = user.Nom;
            InpUsername.Text = user.Username;
            InpPassword.Password = "";

            InpRole.SelectedItem = InpRole.Items.OfType<ComboBoxItem>()
                                        .FirstOrDefault(i => i.Content.ToString() == user.Role);
            InpStatut.SelectedItem = InpStatut.Items.OfType<ComboBoxItem>()
                                          .FirstOrDefault(i => i.Content.ToString() == user.StatutAffiche);

            ColFormUser.Width = new GridLength(350);
        }

        // --- ENREGISTRER (CRÉATION OU MODIFICATION) ---
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string nom = InpNom.Text.Trim();
            string username = InpUsername.Text.Trim();
            string password = InpPassword.Password;
            string role = (InpRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Agent";
            string statut = (InpStatut.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Actif";

            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(username))
            { MessageBox.Show("Le nom complet et l'identifiant sont obligatoires."); return; }
            if (username.Contains(" "))
            { MessageBox.Show("L'identifiant ne doit pas contenir d'espace."); return; }

            bool creation = currentEditing == null;

            // Unicité de l'identifiant
            var doublon = Utils.GetUtilisateurs().FirstOrDefault(u => u.Username != null &&
                                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                                (creation || u.Id != currentEditing.Id));
            if (doublon != null)
            { MessageBox.Show("Cet identifiant est déjà utilisé par un autre compte."); return; }

            if (creation && password.Length < 6)
            { MessageBox.Show("Le mot de passe est obligatoire (6 caractères minimum) pour un nouveau compte."); return; }
            if (!creation && !string.IsNullOrEmpty(password) && password.Length < 6)
            { MessageBox.Show("Le nouveau mot de passe doit contenir au moins 6 caractères."); return; }

            Utilisateur cible = creation ? new Utilisateur { DateCreation = DateTime.Now } : Utils.GetUtilisateurParId(currentEditing.Id);
            if (cible == null) { MessageBox.Show("Compte introuvable : rechargez la liste."); return; }

            // ⚠️ Garde-fous : ne jamais se verrouiller soi-même ni supprimer le dernier admin actif
            bool perdAdminActif = cible.Role == "Admin" && cible.EstActif && (role != "Admin" || statut != "Actif");
            if (perdAdminActif && Utils.NombreAdminsActifs() <= 1)
            { MessageBox.Show("Impossible : il doit toujours rester au moins un administrateur actif."); return; }
            if (!creation && EstMoi(cible) && statut != "Actif")
            { MessageBox.Show("Vous ne pouvez pas désactiver votre propre compte."); return; }
            if (!creation && EstMoi(cible) && role != "Admin")
            { MessageBox.Show("Vous ne pouvez pas retirer votre propre rôle d'administrateur."); return; }

            cible.Nom = nom;
            cible.Username = username;
            cible.Role = role;
            cible.Statut = statut;
            if (creation) cible.MotDePasse = Utils.HashPassword(password);
            else if (!string.IsNullOrEmpty(password)) cible.MotDePasse = Utils.HashPassword(password);

            if (creation) Utils.SaveUtilisateur(cible);
            else Utils.UpdateUtilisateur(cible);

            BtnCancel_Click(null, null);
            RefreshGrid();
            MessageBox.Show(creation ? "Compte créé avec succès !" : "Compte mis à jour !");
        }

        // --- ACTIVER / REMETTRE EN ATTENTE / DÉSACTIVER (bouton ⭘ de la ligne) ---
        private void BtnToggleStatut_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as Button)?.DataContext as Utilisateur;
            if (user == null) return;

            var frais = Utils.GetUtilisateurParId(user.Id);
            if (frais == null) return;

            if (EstMoi(frais))
            { MessageBox.Show("Vous ne pouvez pas modifier le statut de votre propre compte."); return; }

            if (!frais.EstActif)
            {
                // Activation (depuis En attente ou Désactivé)
                Utils.ChangerStatutUtilisateur(frais.Id, "Actif");
                MessageBox.Show($"Compte « {frais.Username} » activé. Cette personne peut maintenant se connecter.");
            }
            else
            {
                if (EstDernierAdminActif(frais))
                { MessageBox.Show("Impossible : il doit toujours rester au moins un administrateur actif."); return; }

                // Choix : En attente ou Désactivé
                var reponse = MessageBox.Show(
                    $"Que faire du compte « {frais.Username} » ?\n\n" +
                    "• OUI = le désactiver (connexion bloquée)\n" +
                    "• NON = le remettre « En attente »\n" +
                    "• ANNULER = ne rien changer",
                    "Statut du compte", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (reponse == MessageBoxResult.Yes) Utils.ChangerStatutUtilisateur(frais.Id, "Désactivé");
                else if (reponse == MessageBoxResult.No) Utils.ChangerStatutUtilisateur(frais.Id, "En attente");
                else return;
            }

            RefreshGrid();
        }

        // --- SUPPRIMER ---
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as Button)?.DataContext as Utilisateur;
            if (user == null) return;

            var frais = Utils.GetUtilisateurParId(user.Id);
            if (frais == null) return;

            if (EstMoi(frais))
            { MessageBox.Show("Vous ne pouvez pas supprimer votre propre compte."); return; }
            if (EstDernierAdminActif(frais))
            { MessageBox.Show("Impossible : il doit toujours rester au moins un administrateur actif."); return; }

            if (MessageBox.Show($"Supprimer définitivement le compte « {frais.Nom} » ({frais.Username}) ?",
                "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Utils.DeleteUtilisateur(frais.Id);
                RefreshGrid();
            }
        }

        // --- RÉINITIALISATION DU MOT DE PASSE ---
        private void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as Button)?.DataContext as Utilisateur;
            if (user == null) return;

            string nouveau = DemanderNouveauMotDePasse(user.Username);
            if (string.IsNullOrEmpty(nouveau)) return;

            Utils.ReinitialiserMotDePasse(user.Id, nouveau);
            MessageBox.Show($"Nouveau mot de passe défini pour « {user.Username} ».", "Réinitialisation",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshGrid();
        }

        // Petite fenêtre de saisie de mot de passe (même approche que l'ajout de service dans UC_Transferts)
        private string DemanderNouveauMotDePasse(string username)
        {
            Window dlg = new Window
            {
                Title = "Réinitialiser le mot de passe",
                Width = 380,
                Height = 260,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1120"))
            };

            StackPanel sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock
            {
                Text = "Nouveau mot de passe pour « " + username + " »",
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            Brush fond = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            PasswordBox pwd1 = new PasswordBox { Height = 36, VerticalContentAlignment = VerticalAlignment.Center, Background = fond, Foreground = Brushes.White };
            PasswordBox pwd2 = new PasswordBox { Height = 36, Margin = new Thickness(0, 8, 0, 0), VerticalContentAlignment = VerticalAlignment.Center, Background = fond, Foreground = Brushes.White };
            TextBlock err = new TextBlock
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171")),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Button ok = new Button
            {
                Content = "Valider",
                Height = 34,
                Margin = new Thickness(0, 12, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1120"))
            };

            sp.Children.Add(pwd1);
            sp.Children.Add(new TextBlock
            {
                Text = "Confirmer le mot de passe",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                FontSize = 11,
                Margin = new Thickness(0, 10, 0, 0)
            });
            sp.Children.Add(pwd2);
            sp.Children.Add(err);
            sp.Children.Add(ok);

            ok.Click += (s, ev) =>
            {
                if (pwd1.Password.Length < 6)
                {
                    err.Text = "Le mot de passe doit contenir au moins 6 caractères.";
                    err.Visibility = Visibility.Visible;
                    return;
                }
                if (pwd1.Password != pwd2.Password)
                {
                    err.Text = "Les deux mots de passe ne correspondent pas.";
                    err.Visibility = Visibility.Visible;
                    return;
                }
                dlg.DialogResult = true;
            };

            dlg.Content = sp;
            return dlg.ShowDialog() == true ? pwd1.Password : null;
        }
    }
}