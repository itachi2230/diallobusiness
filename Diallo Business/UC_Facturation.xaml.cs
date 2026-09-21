using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    /// <summary>
    /// Écran de facturation multi-lignes : création de factures (lignes désignation/PU/qté),
    /// acompte/reliquat, soldage, suppression, aperçu et impression.
    /// </summary>
    public partial class UC_Facturation : UserControl
    {
        private List<ElementFacture> lignesEnCours = new List<ElementFacture>();

        public UC_Facturation()
        {
            InitializeComponent();
            ModeSaisie();
            RefreshGrid();
        }

        // --- CHARGEMENT DE LA LISTE ---
        private void RefreshGrid()
        {
            var factures = Utils.GetFactures();
            string filtre = (TxtSearch.Text ?? "").Trim().ToLower();
            if (filtre.Length > 0)
                factures = factures.Where(f => (f.NomClient ?? "").ToLower().Contains(filtre)).ToList();

            GridFactures.ItemsSource = factures.OrderByDescending(f => f.Date).ToList();

            TxtTotalFacture.Text = factures.Sum(f => f.MontantTotal).ToString("N0") + " F";
            TxtTotalPaye.Text = factures.Sum(f => f.Accompte).ToString("N0") + " F";
            TxtTotalReliquat.Text = factures.Sum(f => f.Reliquat).ToString("N0") + " F";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshGrid();

        // --- BASCULE DES MODES DU PANNEAU DROIT ---
        private void ModeSaisie()
        {
            PanelDetail.Visibility = Visibility.Collapsed;
            PanelForm.Visibility = Visibility.Visible;

            lignesEnCours.Clear();
            DgLignes.ItemsSource = null;
            InpClient.Text = ""; InpTel.Text = ""; InpDesignation.Text = "";
            InpPU.Text = ""; InpQte.Text = "1"; InpAcompte.Text = "0";
            TxtFormTotal.Text = "0 F"; TxtFormReliquat.Text = "0 F";

            GridFactures.SelectedItem = null;
        }

        private void BtnShowAdd_Click(object sender, RoutedEventArgs e) => ModeSaisie();
        private void BtnCancelForm_Click(object sender, RoutedEventArgs e) => ModeSaisie();

        // --- GESTION DES LIGNES (SAISIE) ---
        private void BtnAddLigne_Click(object sender, RoutedEventArgs e)
        {
            string designation = InpDesignation.Text.Trim();
            if (string.IsNullOrEmpty(designation)) { MessageBox.Show("La désignation de la ligne est obligatoire."); return; }
            if (!decimal.TryParse(InpPU.Text, out decimal pu) || pu < 0) { MessageBox.Show("Prix unitaire invalide."); return; }
            if (!int.TryParse(InpQte.Text, out int qte) || qte <= 0) { MessageBox.Show("Quantité invalide."); return; }

            lignesEnCours.Add(new ElementFacture { Designation = designation, PrixUnitaire = pu, Quantite = qte });
            DgLignes.ItemsSource = null;
            DgLignes.ItemsSource = lignesEnCours;

            InpDesignation.Text = ""; InpPU.Text = ""; InpQte.Text = "1";
            InpDesignation.Focus();
            MajTotauxForm();
        }

        private void BtnDelLigne_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ElementFacture ligne)
            {
                lignesEnCours.Remove(ligne);
                DgLignes.ItemsSource = null;
                DgLignes.ItemsSource = lignesEnCours;
                MajTotauxForm();
            }
        }

        // --- TOTAUX DU FORMULAIRE ---
        private void MajTotauxForm()
        {
            decimal total = lignesEnCours.Sum(l => l.TotalLigne);
            TxtFormTotal.Text = total.ToString("N0") + " F";
            MajReliquatForm(total);
        }

        private void InpAcompte_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Les contrôles peuvent ne pas encore exister pendant InitializeComponent
            if (TxtFormReliquat == null || lignesEnCours == null) return;
            MajReliquatForm(lignesEnCours.Sum(l => l.TotalLigne));
        }

        private void MajReliquatForm(decimal total)
        {
            decimal acompte = decimal.TryParse(InpAcompte.Text, out decimal a) ? a : 0;
            if (acompte < 0) acompte = 0;
            if (acompte > total) acompte = total;

            decimal reliquat = total - acompte;
            TxtFormReliquat.Text = reliquat.ToString("N0") + " F";
            TxtFormReliquat.Foreground = reliquat > 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
        }

        // --- ENREGISTRER LA FACTURE ---
        private void BtnSaveFacture_Click(object sender, RoutedEventArgs e)
        {
            string client = InpClient.Text.Trim();
            if (string.IsNullOrEmpty(client)) { MessageBox.Show("Le nom du client est obligatoire."); return; }
            if (lignesEnCours.Count == 0) { MessageBox.Show("Ajoutez au moins une ligne à la facture."); return; }

            decimal total = lignesEnCours.Sum(l => l.TotalLigne);
            decimal acompte = decimal.TryParse(InpAcompte.Text, out decimal a) ? a : 0;
            if (acompte < 0) acompte = 0;
            if (acompte > total) acompte = total;

            Facture f = new Facture
            {
                NomClient = client,
                TelephoneClient = InpTel.Text.Trim(),
                MontantTotal = total,
                Accompte = acompte,
                Reliquat = total - acompte,
                Elements = new List<ElementFacture>(lignesEnCours)
            };

            Utils.SaveFacture(f); // Attribue Id facture + Id/IdFacture des lignes + log
            RefreshGrid();
            ModeSaisie();
            AfficherDetail(f.Id);
            MessageBox.Show("Facture enregistrée avec succès !");
        }

        // --- DÉTAIL D'UNE FACTURE ---
        private void AfficherDetail(int idFacture)
        {
            var f = Utils.GetFactures().FirstOrDefault(x => x.Id == idFacture);
            if (f == null) return;

            PanelForm.Visibility = Visibility.Collapsed;
            PanelDetail.Visibility = Visibility.Visible;

            DetClient.Text = f.NomClient;
            DetTel.Text = string.IsNullOrEmpty(f.TelephoneClient) ? "Téléphone : —" : "Tél : " + f.TelephoneClient;
            DetDate.Text = "Facture N° " + f.Id + " — " + f.Date.ToString("dd/MM/yyyy HH:mm");

            DgDetailLignes.ItemsSource = f.Elements;
            DetTotal.Text = f.MontantTotal.ToString("N0") + " F";
            DetPaye.Text = f.Accompte.ToString("N0") + " F";
            DetReliquat.Text = f.Reliquat.ToString("N0") + " F";
            BtnSolder.Visibility = f.Reliquat > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Synchroniser la sélection de la grille (si la ligne existe dans la liste affichée)
            if (GridFactures.ItemsSource is List<Facture> liste)
                GridFactures.SelectedItem = liste.FirstOrDefault(x => x.Id == idFacture);
        }

        private void GridFactures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridFactures.SelectedItem is Facture f)
                AfficherDetail(f.Id);
        }

        // --- ACTIONS SUR LA FACTURE SÉLECTIONNÉE ---
        private void BtnSolder_Click(object sender, RoutedEventArgs e)
        {
            if (GridFactures.SelectedItem is Facture f && f.Reliquat > 0)
            {
                if (MessageBox.Show($"Confirmer le règlement de {f.Reliquat:N0} F ?", "Paiement", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    f.Accompte += f.Reliquat;
                    f.Reliquat = 0;
                    Utils.UpdateFacture(f);
                    Utils.LogAction($"Facture soldée : ID {f.Id} (Client: {f.NomClient})");
                    RefreshGrid();
                    AfficherDetail(f.Id);
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (GridFactures.SelectedItem is Facture f)
            {
                if (MessageBox.Show($"Supprimer définitivement la facture N° {f.Id} ({f.NomClient}) ?",
                    "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Utils.DeleteFacture(f.Id);
                    RefreshGrid();
                    ModeSaisie();
                }
            }
        }

        // --- APERÇU / IMPRESSION ---
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (GridFactures.SelectedItem is Facture f)
                OuvrirApercu(f);
        }

        private void OuvrirApercu(Facture f)
        {
            Border factureVisuelle = ConstruireVisuelFacture(f);
            Brush bleu = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            Brush sombre = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1120"));
            Brush gris = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));

            Window apercu = new Window
            {
                Title = "Aperçu - Facture N° " + f.Id,
                Width = 800,
                Height = 660,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = sombre
            };

            Button btnImprimer = new Button { Content = "🖨 Imprimer", Height = 36, Padding = new Thickness(20, 0, 20, 0), Margin = new Thickness(0, 0, 10, 0), Background = bleu, Foreground = sombre };
            Button btnFermer = new Button { Content = "Fermer", Height = 36, Padding = new Thickness(20, 0, 20, 0), Background = gris };
            btnFermer.Click += (s, ev) => apercu.Close();
            btnImprimer.Click += (s, ev) => ImprimerFacture(factureVisuelle, f);

            StackPanel boutons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 12) };
            boutons.Children.Add(btnImprimer);
            boutons.Children.Add(btnFermer);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = factureVisuelle, Margin = new Thickness(20, 0, 20, 20) };

            DockPanel root = new DockPanel();
            DockPanel.SetDock(boutons, Dock.Top);
            root.Children.Add(boutons);
            root.Children.Add(scroll);

            apercu.Content = root;
            apercu.ShowDialog();
        }

        private void ImprimerFacture(Border factureVisuelle, Facture f)
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                factureVisuelle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                factureVisuelle.Arrange(new Rect(new Point(0, 0), factureVisuelle.DesiredSize));
                dlg.PrintVisual(factureVisuelle, "Facture N° " + f.Id);
                Utils.LogAction($"Facture imprimée : ID {f.Id} (Client: {f.NomClient})");
            }
        }

        // Construit la facture en NOIR SUR BLANC (format papier) — exception volontaire au thème sombre
        private Border ConstruireVisuelFacture(Facture f)
        {
            var stack = new StackPanel { Margin = new Thickness(30) };

            stack.Children.Add(new TextBlock { Text = "DIALLO BUSINESS", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.Black });
            stack.Children.Add(new TextBlock { Text = "Business Center — Reçu de facture", FontSize = 12, Foreground = Brushes.DimGray, Margin = new Thickness(0, 0, 0, 15) });

            stack.Children.Add(new TextBlock { Text = "Facture N° " + f.Id, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.Black });
            stack.Children.Add(new TextBlock { Text = "Date : " + f.Date.ToString("dd/MM/yyyy HH:mm"), FontSize = 12, Foreground = Brushes.DimGray });
            stack.Children.Add(new TextBlock { Text = "Client : " + (string.IsNullOrEmpty(f.NomClient) ? "—" : f.NomClient), FontSize = 12, Foreground = Brushes.Black });
            if (!string.IsNullOrEmpty(f.TelephoneClient))
                stack.Children.Add(new TextBlock { Text = "Téléphone : " + f.TelephoneClient, FontSize = 12, Foreground = Brushes.DimGray });

            Grid table = new Grid { Margin = new Thickness(0, 15, 0, 0) };
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            AddGridRow(table, new[] { "DÉSIGNATION", "P.U (F)", "QTÉ", "TOTAL (F)" }, FontWeights.Bold, Brushes.Black);
            foreach (var ligne in f.Elements)
                AddGridRow(table, new[] { ligne.Designation, ligne.PrixUnitaire.ToString("N0"), ligne.Quantite.ToString(), ligne.TotalLigne.ToString("N0") }, FontWeights.Normal, Brushes.Black);
            AddGridRow(table, new[] { "", "", "TOTAL", f.MontantTotal.ToString("N0") }, FontWeights.Bold, Brushes.Black);
            AddGridRow(table, new[] { "", "", "PAYÉ", f.Accompte.ToString("N0") }, FontWeights.Normal, Brushes.DimGray);
            AddGridRow(table, new[] { "", "", "RELIQUAT", f.Reliquat.ToString("N0") }, FontWeights.Bold, Brushes.Black);

            stack.Children.Add(table);

            stack.Children.Add(new TextBlock
            {
                Text = "Acompte versé : " + f.Accompte.ToString("N0") + " F    —    Reste à payer : " + f.Reliquat.ToString("N0") + " F",
                Margin = new Thickness(0, 20, 0, 0),
                FontSize = 12,
                Foreground = Brushes.Black,
                FontWeight = f.Reliquat > 0 ? FontWeights.Bold : FontWeights.Normal
            });
            stack.Children.Add(new TextBlock { Text = "Merci de votre confiance !", Margin = new Thickness(0, 30, 0, 0), FontSize = 12, Foreground = Brushes.DimGray, HorizontalAlignment = HorizontalAlignment.Center });

            return new Border { Background = Brushes.White, Child = stack, Padding = new Thickness(10) };
        }

        // Ajoute une ligne de cellules texte dans la grille de la facture imprimable
        private void AddGridRow(Grid table, string[] cells, FontWeight weight, Brush color)
        {
            int row = table.RowDefinitions.Count;
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int i = 0; i < cells.Length; i++)
            {
                var tb = new TextBlock { Text = cells[i], FontWeight = weight, Foreground = color, Margin = new Thickness(2, 3, 2, 3), FontSize = 12 };
                if (i > 0) tb.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetRow(tb, row);
                Grid.SetColumn(tb, i);
                table.Children.Add(tb);
            }
        }
    }
}