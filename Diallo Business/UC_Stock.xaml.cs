using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    public partial class UC_Stock : UserControl
    {
        private Article currentEditingArticle = null;

        public UC_Stock()
        {
            InitializeComponent();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            DgStock.ItemsSource = null;
            DgStock.ItemsSource = Utils.GetArticles();
        }

        // --- GESTION DU FORMULAIRE ---
        private void BtnShowAdd_Click(object sender, RoutedEventArgs e)
        {
            currentEditingArticle = null;
            TxtFormTitle.Text = "Nouvel Article";
            ClearFields();
            ColForm.Width = new GridLength(350); // Affiche le formulaire
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ColForm.Width = new GridLength(0); // Cache le formulaire
        }

        private void ClearFields()
        {
            InpNom.Text = ""; InpPrixAchat.Text = ""; InpPrixVente.Text = "";
            InpQtite.Text = ""; InpType.SelectedIndex = 0;
        }

        // --- ENREGISTRER (AJOUT OU MODIF) ---
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(InpNom.Text) || string.IsNullOrEmpty(InpPrixVente.Text))
            {
                MessageBox.Show("Le nom et le prix de vente sont obligatoires !");
                return;
            }
            // Parsing défensif : messages clairs au lieu d'exceptions brutales
            if (!decimal.TryParse(InpPrixAchat.Text, out decimal prixAchat) || prixAchat < 0) prixAchat = 0;
            if (!decimal.TryParse(InpPrixVente.Text, out decimal prixVente) || prixVente < 0)
            {
                MessageBox.Show("Prix de vente invalide !");
                return;
            }
            if (!int.TryParse(InpQtite.Text, out int quantite) || quantite < 0)
            {
                MessageBox.Show("Quantité invalide !");
                return;
            }

            if (currentEditingArticle == null) currentEditingArticle = new Article();

            currentEditingArticle.Nom = InpNom.Text.Trim();
            currentEditingArticle.PrixAchat = prixAchat;
            currentEditingArticle.PrixVente = prixVente;
            currentEditingArticle.QuantiteDispo = quantite;
            currentEditingArticle.Type = (InpType.SelectedItem as ComboBoxItem)?.Content.ToString();
            currentEditingArticle.DateAjout = DateTime.Now;

            if (currentEditingArticle.Id == 0)
                Utils.SaveArticle(currentEditingArticle); // Nouveau
            else
                Utils.UpdateArticle(currentEditingArticle); // Modif

            RefreshGrid();
            BtnCancel_Click(null, null);
            MessageBox.Show("Stock mis à jour avec succès !");
        }

        // --- MODIFIER ---
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var art = (sender as Button).DataContext as Article;
            if (art != null)
            {
                currentEditingArticle = art;
                TxtFormTitle.Text = "Modifier Article";
                InpNom.Text = art.Nom;
                InpPrixAchat.Text = art.PrixAchat.ToString();
                InpPrixVente.Text = art.PrixVente.ToString();
                InpQtite.Text = art.QuantiteDispo.ToString();

                // Restaurer aussi le type dans le ComboBox (correction du bug d'édition)
                InpType.SelectedItem = null;
                foreach (ComboBoxItem item in InpType.Items)
                {
                    if (item.Content.ToString() == art.Type)
                    {
                        InpType.SelectedItem = item;
                        break;
                    }
                }

                ColForm.Width = new GridLength(350);
            }
        }

        // --- SUPPRIMER ---
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var art = (sender as Button).DataContext as Article;
            if (art != null && MessageBox.Show($"Supprimer {art.Nom} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Utils.DeleteArticle(art.Id);
                RefreshGrid();
            }
        }

        // --- RECHERCHE FILTRÉE ---
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = TxtSearch.Text.ToLower();
            DgStock.ItemsSource = Utils.GetArticles().Where(x => x.Nom.ToLower().Contains(filter)).ToList();
        }
    }
}