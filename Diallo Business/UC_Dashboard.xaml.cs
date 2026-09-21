using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    public partial class UC_Dashboard : UserControl
    {
        public UC_Dashboard()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            // Charger les services dans le ComboBox
            InpServiceNom.ItemsSource = Utils.GetListeServices();
            InpArtSelection.ItemsSource = Utils.GetArticles();
            // Calculer les stats
            var today = DateTime.Today;
            decimal ventes = Utils.GetFactures().Where(f => f.Date.Date == today).Sum(f => f.MontantTotal);
            decimal dettes = Utils.GetServices().Sum(s => s.Reliquat) + Utils.GetFormations().Sum(f => f.Reliquat);

            TxtSales.Text = $"{ventes:N0} F";
            TxtDebt.Text = $"{dettes:N0} F";

            // Charger l'activité (Les 10 derniers logs)
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData", "Logs", $"log_{DateTime.Now:yyyy_MM_dd}.txt");
                if (System.IO.File.Exists(logPath))
                    LstActivity.ItemsSource = System.IO.File.ReadAllLines(logPath).Reverse().Take(10);
            }
            catch { }
        }

        private void ComboOpType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelService == null) return;
            PanelService.Visibility = Visibility.Collapsed;
            PanelFormation.Visibility = Visibility.Collapsed;
            PanelFlux.Visibility = Visibility.Collapsed;
            PanelArticle.Visibility = Visibility.Collapsed;

            var choice = (ComboOpType.SelectedItem as ComboBoxItem).Content.ToString();
            if (choice.Contains("Service")) PanelService.Visibility = Visibility.Visible;
            else if (choice.Contains("Formation")) PanelFormation.Visibility = Visibility.Visible;
            else if (choice.Contains("Flux")) PanelFlux.Visibility = Visibility.Visible;
            else if (choice.Contains("Article")) PanelArticle.Visibility = Visibility.Visible;
        }

        private void BtnSaveOp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var choice = (ComboOpType.SelectedItem as ComboBoxItem).Content.ToString();

                // 1. GESTION DES SERVICES (MAINTENANCE, COPIE, ETC.)
                if (choice.Contains("Service"))
                {
                    string typeS = InpServiceNom.Text; // Correspond à TypeService
                    decimal total = decimal.TryParse(InpServiceTotal.Text, out decimal t) ? t : 0;
                    decimal paye = decimal.TryParse(InpServiceAvance.Text, out decimal p) ? p : 0;

                    // Validations (bug corrigé : contrôles métier avant enregistrement)
                    if (string.IsNullOrWhiteSpace(typeS) || total <= 0)
                    { MessageBox.Show("Indiquez le type de service et un montant total supérieur à 0."); return; }
                    if (paye < 0 || paye > total)
                    { MessageBox.Show("L'acompte doit être compris entre 0 et le total."); return; }

                    Service s = new Service
                    {
                        TypeService = typeS,
                        NomClient = InpServiceClient.Text,
                        Prix = total,
                        Accompte = paye,
                        Reliquat = total - paye,
                        DateAction = DateTime.Now,
                        Info = "Saisie rapide Dashboard"
                    };

                    Utils.SaveService(s);
                    Utils.AddServiceToList(typeS);
                    Utils.LogAction($"[SERVICE] {typeS} pour {s.NomClient} | Payé: {paye}F");
                }

                // 2. GESTION DES FORMATIONS
                else if (choice.Contains("Formation"))
                {
                    // Récupération et conversion des montants
                    decimal prixTotal = decimal.TryParse(InpFormPrixTotal.Text, out decimal pt) ? pt : 0;
                    decimal accompte = decimal.TryParse(InpFormAccompte.Text, out decimal acc) ? acc : 0;

                    // Validations (bug corrigé)
                    if (string.IsNullOrWhiteSpace(InpFormNomClient.Text) || prixTotal <= 0)
                    { MessageBox.Show("Indiquez le nom de l'élève et un prix total supérieur à 0."); return; }
                    if (accompte < 0 || accompte > prixTotal)
                    { MessageBox.Show("L'acompte doit être compris entre 0 et le prix total."); return; }

                    Formation f = new Formation
                    {
                        Nom = "Formation Standard", // Tu peux changer selon le module choisi
                        NomClient = InpFormNomClient.Text,
                        Prix = prixTotal,
                        Accompte = accompte,
                        Reliquat = prixTotal - accompte, // Calcul automatique du reste à payer
                        Planification = InpFormPlanif.Text,
                        Statut = "En cours",
                        Date = DateTime.Now, // Date d'inscription (utilisée par les Bilans)
                        IdUtilisateur = Utils.CurrentUser?.Id ?? 0
                    };

                    Utils.SaveFormation(f);
                    Utils.LogAction($"[FORMATION] {f.NomClient} inscrit. Accompte: {accompte}F, Reste: {f.Reliquat}F");
                }

                // 3. GESTION DES FLUX (ENTRÉE/SORTIE)
                else if (choice.Contains("Flux"))
                {
                    // Bug corrigé : parsing sécurisé + montant obligatoire
                    decimal montantFlux = decimal.TryParse(InpFluxMontant.Text, out decimal mf) ? mf : 0;
                    if (montantFlux <= 0)
                    { MessageBox.Show("Indiquez un montant supérieur à 0."); return; }

                    FluxDivers flux = new FluxDivers
                    {
                        Type = (InpFluxType.SelectedItem as ComboBoxItem)?.Content.ToString().ToUpper(),
                        Motif = InpFluxMotif.Text,
                        Montant = montantFlux,
                        Date = DateTime.Now
                    };

                    Utils.SaveFlux(flux);
                    Utils.LogAction($"[FLUX] {flux.Type}: {flux.Motif} ({flux.Montant}F)");
                }
                else if (choice.Contains("Article"))
                {
                    if (InpArtSelection.SelectedItem is Article art)
                    {
                        // Bug corrigé : parsing sécurisé de la quantité
                        if (!int.TryParse(InpArtQte.Text, out int qteVendre) || qteVendre <= 0)
                        {
                            MessageBox.Show("Quantité invalide.");
                            return;
                        }

                        // 1. Vérifier si le stock est suffisant
                        if (art.QuantiteDispo < qteVendre)
                        {
                            MessageBox.Show("Stock insuffisant !");
                            return;
                        }

                        // 2. Créer la facture simplifiée
                        Facture f = new Facture
                        {
                            NomClient = string.IsNullOrEmpty(InpArtClient.Text) ? "Client Passant" : InpArtClient.Text,
                            MontantTotal = art.PrixVente * qteVendre,
                            Accompte = art.PrixVente * qteVendre, // Payé comptant
                            Reliquat = 0,
                            Date = DateTime.Now
                        };

                        // 3. Mettre à jour la quantité dans le stock
                        art.QuantiteDispo -= qteVendre;
                        Utils.UpdateArticle(art); // On utilise ta méthode UpdateArticle existante

                        // 4. Sauvegarder la facture
                        Utils.SaveFacture(f);

                        Utils.LogAction($"[VENTE] {qteVendre}x {art.Nom} à {f.NomClient}");
                    }
                }

                MessageBox.Show("Opération enregistrée avec succès !");
                ClearInputs();
                LoadData(); // Rafraîchit les statistiques (TxtSales, TxtDebt)
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : Vérifiez que les montants sont bien des nombres.");
                Utils.LogAction("Erreur Saisie Dashboard : " + ex.Message);
            }
        }
        private void ClearInputs()
        {
            InpServiceTotal.Text = ""; InpServiceAvance.Text = ""; InpServiceClient.Text = "";
            InpFormPrixTotal.Text = ""; InpFormAccompte.Text = ""; InpFormNomClient.Text = ""; InpFormPlanif.Text = "";
            InpFluxMontant.Text = ""; InpFluxMotif.Text = "";
            InpArtQte.Text = "1"; InpArtClient.Text = "";
        }
        private void InpArtSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InpArtSelection.SelectedItem is Article art)
            {
                InpArtPrix.Text = art.PrixVente.ToString();
            }
        }
    }
}