using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    public class BilanRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Categorie { get; set; }
        public string Libelle { get; set; }
        public string Client { get; set; }
        public decimal MontantTotal { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal Reliquat { get; set; }
        public string HasDebtColor => Reliquat > 0 ? "#F87171" : "#4ADE80";
        public object SourceObject { get; set; }

        // Couleur du badge par catégorie
        public string CategoryBrush
        {
            get
            {
                switch (Categorie)
                {
                    case "SERVICE":
                        return "#6366F1"; // Indigo
                    case "FORMATION":
                        return "#A855F7"; // Violet
                    case "VENTE":
                        return "#F59E0B"; // Ambre
                    default:
                        return "#475569"; // Gris par défaut
                }
            }
        }
    }

    public partial class UC_Bilans : UserControl
    {
        private List<BilanRow> allRows = new List<BilanRow>();

        public UC_Bilans()
        {
            InitializeComponent();
            LoadData();
        }

        public void LoadData()
        {
            try
            {
                allRows.Clear();

                // 1. Services
                var services = Utils.GetServices();
                if (services != null)
                {
                    allRows.AddRange(services.Where(s => s != null).Select(s => new BilanRow
                    {
                        Id = s.Id,
                        Date = s.DateAction,
                        Categorie = "SERVICE",
                        Libelle = s.TypeService ?? "Service",
                        Client = s.NomClient ?? "Inconnu",
                        MontantTotal = s.Prix,
                        MontantPaye = s.Accompte,
                        Reliquat = s.Reliquat,
                        SourceObject = s
                    }));
                }

                // 2. Formations
                var formations = Utils.GetFormations();
                if (formations != null)
                {
                    allRows.AddRange(formations.Where(f => f != null).Select(f => new BilanRow
                    {
                        Id = f.Id,
                        Date = f.Date != default ? f.Date : DateTime.Now, // Date d'inscription (champ Date ajouté au modèle, fallback pour anciens JSON)
                        Categorie = "FORMATION",
                        Libelle = f.Nom ?? "Formation",
                        Client = f.NomClient ?? "Élève",
                        MontantTotal = f.Prix,
                        MontantPaye = f.Accompte,
                        Reliquat = f.Reliquat,
                        SourceObject = f
                    }));
                }

                // 3. Ventes (Factures)
                var factures = Utils.GetFactures();
                if (factures != null)
                {
                    allRows.AddRange(factures.Where(v => v != null).Select(v => new BilanRow
                    {
                        Id = v.Id,
                        Date = v.Date,
                        Categorie = "VENTE",
                        Libelle = "Articles Divers",
                        Client = v.NomClient ?? "Passant",
                        MontantTotal = v.MontantTotal,
                        MontantPaye = v.Accompte,
                        Reliquat = v.Reliquat,
                        SourceObject = v
                    }));
                }

                if (GridGlobal != null) ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (GridGlobal == null) return;

            var filtered = allRows.AsEnumerable();

            // Filtre Recherche (Client ou Libellé)
            if (!string.IsNullOrEmpty(TxtSearch.Text))
            {
                string search = TxtSearch.Text.ToLower();
                filtered = filtered.Where(x => x.Client.ToLower().Contains(search) || x.Libelle.ToLower().Contains(search));
            }

            // Filtre Catégorie
            if (ComboType.SelectedItem is ComboBoxItem selectedType)
            {
                string type = selectedType.Content.ToString();
                if (type != "Tous")
                    filtered = filtered.Where(x => x.Categorie == type.ToUpper().TrimEnd('S'));
            }

            // FILTRE PÉRIODE AVANCÉ
            if (ComboPeriode.SelectedItem is ComboBoxItem selectedPeriode)
            {
                string p = selectedPeriode.Content.ToString();
                DateTime today = DateTime.Today;

                switch (p)
                {
                    case "Aujourd'hui":
                        filtered = filtered.Where(x => x.Date.Date == today);
                        break;
                    case "Hier":
                        filtered = filtered.Where(x => x.Date.Date == today.AddDays(-1));
                        break;
                    case "Avant-hier":
                        filtered = filtered.Where(x => x.Date.Date == today.AddDays(-2));
                        break;
                    case "Il y a 3 jours":
                        filtered = filtered.Where(x => x.Date.Date == today.AddDays(-3));
                        break;
                    case "Il y a 4 jours":
                        filtered = filtered.Where(x => x.Date.Date == today.AddDays(-4));
                        break;
                    case "Il y a 5 jours":
                        filtered = filtered.Where(x => x.Date.Date == today.AddDays(-5));
                        break;
                    case "Cette Semaine":
                        filtered = filtered.Where(x => x.Date >= today.AddDays(-7));
                        break;
                    case "Ce Mois":
                        filtered = filtered.Where(x => x.Date.Month == today.Month && x.Date.Year == today.Year);
                        break;
                    case "Mois Passé":
                        DateTime lastMonth = today.AddMonths(-1);
                        filtered = filtered.Where(x => x.Date.Month == lastMonth.Month && x.Date.Year == lastMonth.Year);
                        break;
                    case "Mois Surpassé":
                        DateTime twoMonthsAgo = today.AddMonths(-2);
                        filtered = filtered.Where(x => x.Date.Month == twoMonthsAgo.Month && x.Date.Year == twoMonthsAgo.Year);
                        break;
                    case "Cette Année":
                        filtered = filtered.Where(x => x.Date.Year == today.Year);
                        break;
                    case "Année Dernière":
                        filtered = filtered.Where(x => x.Date.Year == today.Year - 1);
                        break;
                    case "Choisir une date...":
                        if (PickerDate.SelectedDate.HasValue)
                            filtered = filtered.Where(x => x.Date.Date == PickerDate.SelectedDate.Value.Date);
                        break;
                }
            }

            // Filtre Dettes
            if (CheckReliquat.IsChecked == true)
                filtered = filtered.Where(x => x.Reliquat > 0);

            var result = filtered.OrderByDescending(x => x.Date).ToList();
            GridGlobal.ItemsSource = result;

            // Mise à jour des Totaux (en bas)
            TxtTotalCa.Text = result.Sum(x => x.MontantTotal).ToString("N0") + " F";
            TxtTotalRecu.Text = result.Sum(x => x.MontantPaye).ToString("N0") + " F";
            TxtTotalDettes.Text = result.Sum(x => x.Reliquat).ToString("N0") + " F";
        }

        private void GridGlobal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridGlobal.SelectedItem is BilanRow row)
            {
                PanelPlaceholder.Visibility = Visibility.Collapsed;
                PanelDetails.Visibility = Visibility.Visible;

                DetClient.Text = row.Client;
                DetLibelle.Text = $"{row.Categorie} : {row.Libelle}";
                DetTotal.Text = row.MontantTotal.ToString("N0") + " F";
                DetPaye.Text = row.MontantPaye.ToString("N0") + " F";
                DetReliquat.Text = row.Reliquat.ToString("N0") + " F";

                BtnSolder.Visibility = row.Reliquat > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                PanelPlaceholder.Visibility = Visibility.Visible;
                PanelDetails.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSolder_Click(object sender, RoutedEventArgs e)
        {
            if (GridGlobal.SelectedItem is BilanRow row)
            {
                if (MessageBox.Show($"Confirmer le règlement de {row.Reliquat:N0} F ?", "Paiement", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (row.SourceObject is Service s) { s.Accompte += s.Reliquat; s.Reliquat = 0; Utils.UpdateService(s); }
                    else if (row.SourceObject is Formation f) { f.Accompte += f.Reliquat; f.Reliquat = 0; f.Statut = "Achevé"; Utils.UpdateFormation(f); }
                    else if (row.SourceObject is Facture v) { v.Accompte += v.Reliquat; v.Reliquat = 0; Utils.UpdateFacture(v); }

                    Utils.LogAction($"Compte soldé : {row.Categorie} #{row.Id} ({row.Client}) — {row.Reliquat:N0} F");
                    LoadData();
                }
            }
        }
        private void ComboPeriode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PickerDate == null) return;

            if (ComboPeriode.SelectedItem is ComboBoxItem item && item.Content.ToString() == "Choisir une date...")
                PickerDate.Visibility = Visibility.Visible;
            else
                PickerDate.Visibility = Visibility.Collapsed;

            ApplyFilters();
        }
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (GridGlobal.SelectedItem is BilanRow row)
            {
                if (MessageBox.Show("Supprimer définitivement cette opération ?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    if (row.Categorie == "SERVICE") Utils.DeleteService(row.Id);
                    else if (row.Categorie == "FORMATION") Utils.DeleteFormation(row.Id);
                    else if (row.Categorie == "VENTE") Utils.DeleteFacture(row.Id);

                    LoadData();
                }
            }
        }

        // Un handler distinct par type d'événement (les 3 surcharges ambiguës ApplyFilters_Changed ont été supprimées)
        private void ApplyFilters_Click(object sender, RoutedEventArgs e) => ApplyFilters();
        private void ApplyFilters_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void ApplyFilters_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
    }
}