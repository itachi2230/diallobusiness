using System;
using System.Windows;
using System.Windows.Threading;

namespace Diallo_Business
{
    /// <summary>
    /// Logique de démarrage : écran de connexion puis fenêtre principale,
    /// et gestion globale des exceptions non interceptées.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ⚠️ CORRECTIF MAJEUR : sans OnExplicitShutdown, WPF arrête l'application dès que la
            // dernière fenêtre se ferme. Comme le dialogue de connexion (Win_Login) est la seule
            // fenêtre ouverte au moment de sa fermeture, l'application s'arrêtait subitement
            // juste après un login réussi, avant l'affichage de MainWindow.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Gestion globale des erreurs (aucune exception ne doit planter l'application en silence)
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            // Garantit un compte administrateur actif (et répare les comptes hérités sans statut)
            Utils.EnsureDefaultAdmin();

                                                                        // 1) Écran de connexion (modal, aucun accès à l'app sans compte actif)
            var login = new Win_Login();
            bool connecte = false;
            try
            {
                connecte = login.ShowDialog() == true && Utils.CurrentUser != null;
            }
            catch (Exception ex)
            {
                // Protection contre tout crash du dialogue de connexion
                Utils.LogAction("ERREUR fatale au login : " + ex.Message);
                MessageBox.Show("Une erreur critique est survenue au démarrage : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            if (!connecte)
            {
                Shutdown();
                return;
            }

            // 2) Sauvegarde quotidienne
            Utils.AutoBackupDaily();

            // 3) Fenêtre principale
            new MainWindow().Show();

            // 4) Avertissement sécurité non bloquant, affiché par-dessus la fenêtre principale
            //    (évite d'interférer avec l'appui sur Entrée qui vient de valider la connexion)
            Dispatcher.BeginInvoke(new Action(AvertirMotDePasseParDefaut), DispatcherPriority.ApplicationIdle);
        }

        // Avertit une fois par session si le compte admin utilise encore le mot de passe par défaut
        private void AvertirMotDePasseParDefaut()
        {
            if (Utils.MotDePasseParDefaut(Utils.CurrentUser))
            {
                MessageBox.Show(
                    "Sécurité : le compte « admin » utilise encore le mot de passe par défaut (admin123).\n\n" +
                    "Ouvrez « Mon compte » (carte utilisateur, en bas de la barre latérale) pour le changer dès maintenant.",
                    "Mot de passe à changer", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Utils.LogAction("ERREUR : " + e.Exception.Message);
            MessageBox.Show("Une erreur inattendue s'est produite : " + e.Exception.Message,
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // On évite le crash brutal de l'application
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Utils.LogAction("ERREUR FATALE : " + (ex != null ? ex.Message : "inconnue"));
        }
    }
}
