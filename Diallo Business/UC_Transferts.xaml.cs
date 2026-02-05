using DialloBusinessCenter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Diallo_Business
{
    public partial class UC_Transferts : UserControl
    {
        private List<string> listeServices;
        private Dictionary<string, TextBox> inputs = new Dictionary<string, TextBox>();
        private List<OMNote> notesDuJour = new List<OMNote>();

        public UC_Transferts()
        {
            InitializeComponent();
            ChargerConfig();
            LoadDailyData();
        }

        private void ChargerConfig()
        {
            // Liste initiale des services (sauvegardée en JSON via Utils)
            listeServices = Utils.GetTransfertServices();
            GenererChampsSaisie();

            // Récupérer le solde de la veille
            var clotures = Utils.GetOMClotures();
            decimal soldeMatin = clotures.Any() ? clotures.OrderByDescending(x => x.Date).First().SoldePhysiqueTotal : 0;
            TxtSoldeHier.Text = $"Solde de départ (matin) : {soldeMatin:N0} F";
        }

        private void GenererChampsSaisie()
        {
            ContainerSoldes.Children.Clear();
            inputs.Clear();
            foreach (var s in listeServices)
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
                sp.Children.Add(new TextBlock { Text = s, Foreground = Brushes.White, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
                var txt = new TextBox { Height = 32, VerticalContentAlignment = VerticalAlignment.Center, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")), Foreground = Brushes.White, BorderThickness = new Thickness(1) };
                sp.Children.Add(txt);
                ContainerSoldes.Children.Add(sp);
                inputs.Add(s, txt);
            }
        }

        private void LoadDailyData()
        {
            notesDuJour = Utils.GetOMNotes().Where(n => n.Date.Date == DateTime.Today).ToList();
            GridNotes.ItemsSource = null;
            GridNotes.ItemsSource = notesDuJour;
        }

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(TxtNoteMontant.Text, out decimal mnt))
            {
                var note = new OMNote
                {
                    Date = DateTime.Now,
                    Type = (ComboTypeNote.SelectedItem as ComboBoxItem).Content.ToString(),
                    Montant = mnt,
                    Description = TxtNoteLibelle.Text,
                    Agent = Utils.CurrentUser?.Nom ?? "Inconnu"
                };
                Utils.SaveOMNote(note);
                TxtNoteMontant.Clear(); TxtNoteLibelle.Clear();
                LoadDailyData();
            }
        }

        private void BtnCalculer_Click(object sender, RoutedEventArgs e)
        {
            decimal totalPhysique = 0;
            foreach (var entry in inputs)
            {
                decimal.TryParse(entry.Value.Text, out decimal val);
                totalPhysique += val;
            }

            var lastCloture = Utils.GetOMClotures().OrderByDescending(x => x.Date).FirstOrDefault();
            decimal soldeHier = lastCloture?.SoldePhysiqueTotal ?? 0;

            // Calcul : Matin + Entrées - Sorties
            decimal totalEntrees = notesDuJour.Where(n => n.Type == "ENTREE").Sum(n => n.Montant);
            decimal totalSorties = notesDuJour.Where(n => n.Type == "SORTIE").Sum(n => n.Montant);
            decimal soldeTheorique = soldeHier + totalEntrees - totalSorties;

            decimal ecart = totalPhysique - soldeTheorique;

            // Affichage Résultat
            BorderResultat.Visibility = Visibility.Visible;
            TxtResultatEcart.Text = (ecart >= 0 ? "+" : "") + ecart.ToString("N0") + " F";

            if (ecart == 0) { BorderResultat.Background = Brushes.DarkGreen; TxtMsgEcart.Text = "Caisse Parfaite"; }
            else if (ecart > 0) { BorderResultat.Background = Brushes.Teal; TxtMsgEcart.Text = "Surplus (Argent en trop)"; }
            else { BorderResultat.Background = Brushes.DarkRed; TxtMsgEcart.Text = "Manquant (Argent perdu)"; }

            // Sauvegarde de la clôture
            if (MessageBox.Show("Voulez-vous enregistrer cette clôture pour aujourd'hui ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Utils.SaveOMCloture(new OMCloture
                {
                    Date = DateTime.Now,
                    SoldeHier = soldeHier,
                    SoldePhysiqueTotal = totalPhysique,
                    Ecart = ecart,
                    Agent = Utils.CurrentUser?.Nom
                });
            }
        }

        private void BtnAddService_Click(object sender, RoutedEventArgs e)
        {
            // On crée une petite boîte de dialogue rapide en code
            Window inputWindow = new Window
            {
                Title = "Nouveau Service",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151C2C")),
                Foreground = Brushes.White
            };

            StackPanel sp = new StackPanel { Margin = new Thickness(10) };
            TextBlock tb = new TextBlock { Text = "Nom du service :", Margin = new Thickness(0, 0, 0, 5) };
            TextBox txt = new TextBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
            Button btn = new Button { Content = "Ajouter", Margin = new Thickness(0, 10, 0, 0), Height = 30, Background = Brushes.Indigo, Foreground = Brushes.White };

            btn.Click += (s, ev) => { inputWindow.DialogResult = true; };

            sp.Children.Add(tb);
            sp.Children.Add(txt);
            sp.Children.Add(btn);
            inputWindow.Content = sp;

            if (inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(txt.Text))
            {
                string nouveauService = txt.Text.ToUpper();
                Utils.AddTransfertService(nouveauService);

                // Rafraîchir l'affichage
                listeServices = Utils.GetTransfertServices();
                GenererChampsSaisie();
            }
        }

        private void BtnDeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (GridNotes.SelectedItem is OMNote n)
            {
                Utils.DeleteOMNote(n.Id);
                LoadDailyData();
            }
        }
    }
}