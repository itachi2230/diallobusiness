using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Diallo_Business
{
    /// <summary>
    /// Paramètres : accès et sécurité (renvois vers la page Utilisateurs et « Mon compte »),
    /// listes de référence (services de saisie rapide, moyens d'encaissement) et sauvegarde des données.
    /// La gestion détaillée des comptes se trouve dans la page dédiée UC_Utilisateurs.
    /// </summary>
    public partial class UC_Parametres : UserControl
    {
        public UC_Parametres()
        {
            InitializeComponent();
            LoadStats();
            LoadListes();
            TxtDataPath.Text = "Dossier de données : " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
        }

        // --- STATISTIQUES UTILISATEURS ---
        private void LoadStats()
        {
            var users = Utils.GetUtilisateurs();
            int attente = users.Count(u => u.StatutAffiche == "En attente");

            TxtStatTotal.Text = users.Count.ToString();
            TxtStatActifs.Text = users.Count(u => u.EstActif).ToString();
            TxtStatAttente.Text = attente.ToString();
            TxtStatDesactives.Text = users.Count(u => u.StatutAffiche == "Désactivé").ToString();

            if (attente > 0)
            {
                TxtAttenteInfo.Text = attente == 1
                    ? "1 compte attend votre activation."
                    : attente + " comptes attendent votre activation.";
                TxtAttenteInfo.Visibility = Visibility.Visible;
            }
        }

        // --- NAVIGATION / SESSION ---
        private void BtnGererUsers_Click(object sender, RoutedEventArgs e)
        {
            var principale = Window.GetWindow(this) as MainWindow;
            if (principale != null) principale.NaviguerVers("BtnUtilisateurs");
        }

        private void BtnMonCompte_Click(object sender, RoutedEventArgs e)
        {
            var principale = Window.GetWindow(this) as MainWindow;
            var fenetre = new Win_MonCompte { Owner = principale };
            if (fenetre.ShowDialog() == true && principale != null) principale.AfficherUtilisateur();
        }

        private void BtnDeconnexion_Click(object sender, RoutedEventArgs e)
        {
            var principale = Window.GetWindow(this) as MainWindow;
            if (principale != null) principale.DemanderDeconnexion();
        }

        // (Les opérations CRUD sur les comptes sont désormais dans UC_Utilisateurs — page admin dédiée)

        // --- LISTES DE RÉFÉRENCE (générées dynamiquement, même style que UC_Transferts) ---
        private void LoadListes()
        {
            GenererListe(PanelServices, Utils.GetListeServices(), nom => Utils.DeleteServiceFromList(nom));
            GenererListe(PanelTransferts, Utils.GetTransfertServices(), nom => Utils.DeleteTransfertService(nom));
        }

        private void GenererListe(StackPanel container, System.Collections.Generic.List<string> items, Action<string> supprimer)
        {
            container.Children.Clear();
            foreach (var item in items)
            {
                Grid row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txt = new TextBlock { Text = item, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
                var btn = new Button { Content = "🗑", Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171")), Padding = new Thickness(8, 0, 8, 0) };
                string capture = item; // éviter la closure sur la variable de boucle
                btn.Click += (s, ev) =>
                {
                    if (MessageBox.Show($"Supprimer « {capture} » de la liste ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        supprimer(capture);
                        LoadListes();
                    }
                };

                Grid.SetColumn(txt, 0);
                Grid.SetColumn(btn, 1);
                row.Children.Add(txt);
                row.Children.Add(btn);
                container.Children.Add(row);
            }
        }

        private void BtnAddService_Click(object sender, RoutedEventArgs e)
        {
            string nom = InpNewService.Text.Trim();
            if (string.IsNullOrEmpty(nom)) { MessageBox.Show("Saisissez le nom du service."); return; }
            Utils.AddServiceToList(nom);
            InpNewService.Text = "";
            LoadListes();
            Utils.LogAction("Service ajouté à la liste : " + nom);
        }

        private void BtnAddTransfert_Click(object sender, RoutedEventArgs e)
        {
            string nom = InpNewTransfert.Text.Trim();
            if (string.IsNullOrEmpty(nom)) { MessageBox.Show("Saisissez le nom du moyen d'encaissement."); return; }
            Utils.AddTransfertService(nom);
            InpNewTransfert.Text = "";
            LoadListes();
        }

        // --- DONNÉES & SAUVEGARDE ---
        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string chemin = Utils.BackupData();
                TxtBackupInfo.Text = "Dernière sauvegarde : " + chemin;
                MessageBox.Show("Sauvegarde créée avec succès !", "Sauvegarde", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Échec de la sauvegarde : " + ex.Message);
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string dossier = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dossier)) Directory.CreateDirectory(dossier);
            Process.Start("explorer.exe", "\"" + dossier + "\"");
        }
    }
}