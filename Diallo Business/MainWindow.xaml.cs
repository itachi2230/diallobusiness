using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Diallo_Business
{
    public partial class MainWindow : Window
    {
        private bool isCollapsed = false;
        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            // Charger le Dashboard par défaut au démarrage
            // Comme UC_Dashboard n'existe pas encore, on peut charger UC_Stock par défaut pour tester
            MainContainer.Content = new UC_Stock();
            TxtPageTitle.Text = "Gestion Stock";
        }
        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!isCollapsed)
            {
                // RÉDUIRE LA BARRE
                SideColumn.Width = new GridLength(100);
                LogoText.Visibility = Visibility.Collapsed;
                UserInfo.Visibility = Visibility.Collapsed;

                // Ajuster l'alignement du bouton toggle pour qu'il soit centré
                BtnToggle.HorizontalAlignment = HorizontalAlignment.Center;
                BtnToggle.Margin = new Thickness(0, 10, 0, 0);

                isCollapsed = true;
            }
            else
            {
                // AGRANDIR LA BARRE
                SideColumn.Width = new GridLength(280);
                LogoText.Visibility = Visibility.Visible;
                UserInfo.Visibility = Visibility.Visible;

                BtnToggle.HorizontalAlignment = HorizontalAlignment.Right;
                BtnToggle.Margin = new Thickness(0, 10, 10, 0);

                isCollapsed = false;
            }
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                BtnMaximize.Content = "❐";
            }
            else
            {
                this.WindowState = WindowState.Normal;
                BtnMaximize.Content = "▢";
            }
        }

        // --- MOTEUR DE NAVIGATION ---
        private void NavClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            // Mise à jour visuelle du titre (on enlève l'émoji devant le texte)
            string tag = btn.Content.ToString().Substring(3);
            TxtPageTitle.Text = tag;

            // Switch entre les UserControls
            switch (btn.Name)
            {
                case "BtnDashboard":
                    MainContainer.Content = new UC_Dashboard(); // À créer
                    break;

                case "BtnStock":
                    MainContainer.Content = new UC_Stock(); // Déjà créé
                    break;

                case "BtnFactures":
                    MainContainer.Content = new UC_Transferts(); // À créer
                    break;

                case "BtnBilans":
                    MainContainer.Content = new UC_Bilans(); // À créer
                    break;

                case "BtnParametres":
                    // MainContainer.Content = new UC_Parametre(); // À créer
                    break;
            }
        }
    }
}