using System.Windows;
using System.Windows.Controls;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    public partial class UC_Bilans : UserControl
    {
        public UC_Bilans()
        {
            InitializeComponent();
            LoadAll();
        }

        private void LoadAll()
        {
            GridServices.ItemsSource = Utils.GetServices();
            GridFormations.ItemsSource = Utils.GetFormations();
            GridVentes.ItemsSource = Utils.GetFactures();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadAll();

        // --- SUPPRESSION SERVICE ---
        private void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Service s)
            {
                if (MessageBox.Show($"Supprimer le service de {s.NomClient} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Utils.DeleteService(s.Id);
                    Utils.LogAction($"[SUPPRESSION] Service ID {s.Id} supprimé.");
                    LoadAll();
                }
            }
        }

        // --- SUPPRESSION FORMATION ---
        private void DeleteFormation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Formation f)
            {
                if (MessageBox.Show($"Supprimer la formation de {f.NomClient} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Comme tu n'as pas DeleteFormation dans Utils, on le fait manuellement
                    var list = Utils.GetFormations();
                    list.RemoveAll(x => x.Id == f.Id);
                    // On peut ajouter une méthode SaveFormations dans Utils ou le faire ici
                    LoadAll();
                }
            }
        }

        // --- SUPPRESSION VENTE ---
        private void DeleteVente_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Facture f)
            {
                if (MessageBox.Show("Supprimer cette vente ?", "Attention", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Idem pour Facture : list.RemoveAll(x => x.Id == f.Id);
                    LoadAll();
                }
            }
        }

        // --- MODIFICATION ---
        private void EditService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Service s)
            {
                // Ici, on pourrait ouvrir une fenêtre avec les champs pré-remplis
                MessageBox.Show("Fonction de modification bientôt disponible !");
            }
        }
    }
}