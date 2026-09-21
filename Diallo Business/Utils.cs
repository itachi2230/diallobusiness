using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using DialloBusinessCenter.Models;

namespace Diallo_Business
{
    public static class Utils
    {
        private static string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
        public static Utilisateur CurrentUser;

        static Utils()
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        }

        // --- MOTEUR GÉNÉRIQUE ---
        private static void SaveLocal<T>(string fileName, List<T> data)
        {
            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        private static List<T> LoadLocal<T>(string fileName)
        {
            string filePath = Path.Combine(folderPath, fileName);
            if (!File.Exists(filePath)) return new List<T>();
            return JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(filePath)) ?? new List<T>();
        }

        // --- MÉTHODES D'ACCÈS ---

        // Articles
        public static List<Article> GetArticles() => LoadLocal<Article>("articles.json");
        public static void SaveArticle(Article item)
        {
            var list = GetArticles();
            if (item.Id == 0) item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            item.IdUtilisateur = CurrentUser?.Id ?? 0;
            list.Add(item);
            SaveLocal("articles.json", list);
            LogAction($"Article ajouté : {item.Nom} ({item.PrixVente:N0} F)");
        }
        public static void UpdateArticle(Article updatedItem)
        {
            var list = GetArticles();
            var index = list.FindIndex(x => x.Id == updatedItem.Id);
            if (index != -1)
            {
                list[index] = updatedItem; // On remplace l'ancien par le nouveau
                SaveLocal("articles.json", list);
                LogAction($"Article modifié : {updatedItem.Nom}");
            }
        }
        // --- GESTION DES SERVICES (LISTE PRÉDÉFINIE) ---
        public static List<string> GetListeServices()
        {
            // On peut stocker ça dans un petit fichier texte ou JSON
            string path = Path.Combine(folderPath, "liste_services.json");
            if (!File.Exists(path)) return new List<string> { "Photocopie", "Impression Noir/Blanc", "Saisie de texte", "Maintenance PC" };

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<string>>(json);
        }
        
        public static void AddServiceToList(string nomService)
        {
            var liste = GetListeServices();
            if (!liste.Contains(nomService))
            {
                liste.Add(nomService);
                SaveLocal("liste_services.json", liste);
            }
        }

        public static void DeleteServiceFromList(string nomService)
        {
            var liste = GetListeServices();
            if (liste.Remove(nomService))
            {
                SaveLocal("liste_services.json", liste);
                LogAction("Service retiré de la liste : " + nomService);
            }
        }
        public static void DeleteArticle(int id)
        {
            var list = GetArticles();
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                SaveLocal("articles.json", list);
                LogAction($"Article supprimé : {item.Nom}");
            }
        }
        // --- SYSTÈME DE LOGS ---
        public static void LogAction(string text)
        {
            try
            {
                // On crée un dossier Logs s'il n'existe pas
                string logDirectory = Path.Combine(folderPath, "Logs");
                if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

                // Un fichier log par jour (ex: log_2023_10_27.txt)
                string fileName = $"log_{DateTime.Now:yyyy_MM_dd}.txt";
                string filePath = Path.Combine(logDirectory, fileName);

                // Format de la ligne : [14:30:05] Admin : Article modifié : Souris sans fil
                string user = CurrentUser != null ? CurrentUser.Nom : "Système";
                string logEntry = $"[{DateTime.Now:HH:mm:ss}] {user} : {text}{Environment.NewLine}";

                File.AppendAllText(filePath, logEntry);
            }
            catch
            {
                // On ne bloque pas l'application si l'écriture du log échoue
            }
        }
        // Services (Maintenance/Copies)
        public static List<Service> GetServices() => LoadLocal<Service>("services.json");
        public static void SaveService(Service item)
        {
            var list = GetServices();
            item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            item.IdUtilisateur = CurrentUser?.Id ?? 0;
            list.Add(item);
            SaveLocal("services.json", list);
        }
        public static void UpdateService(Service updatedItem)
        {
            var list = GetServices();
            var index = list.FindIndex(x => x.Id == updatedItem.Id);
            if (index != -1)
            {
                list[index] = updatedItem;
                SaveLocal("services.json", list);
            }
        }

        public static void DeleteService(int id)
        {
            var list = GetServices();
            list.RemoveAll(x => x.Id == id);
            SaveLocal("services.json", list);
        }

        // Formations
        public static List<Formation> GetFormations() => LoadLocal<Formation>("formations.json");
        public static void SaveFormation(Formation item)
        {
            var list = GetFormations();
            item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            item.IdUtilisateur = CurrentUser?.Id ?? 0;
            list.Add(item);
            SaveLocal("formations.json", list);

        }
        public static void UpdateFormation(Formation updatedItem)
        {
            var list = GetFormations();
            var index = list.FindIndex(x => x.Id == updatedItem.Id);
            if (index != -1)
            {
                list[index] = updatedItem;
                SaveLocal("formations.json", list);
            }
        }
        public static void DeleteFormation(int id)
        {
            var list = GetFormations();
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                SaveLocal("formations.json", list);
                LogAction($"Formation supprimée : {item.Nom} (Client: {item.NomClient})");
            }
        }

        // Flux Divers (Eau, Poubelle, Reliquats oubliés)
        public static List<FluxDivers> GetFlux() => LoadLocal<FluxDivers>("flux.json");
        public static void SaveFlux(FluxDivers item)
        {
            var list = GetFlux();
            item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            item.IdUtilisateur = CurrentUser?.Id ?? 0;
            item.Date = DateTime.Now;
            list.Add(item);
            SaveLocal("flux.json", list);
        }
        public static void DeleteFlux(int id)
        {
            var list = GetFlux();
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                SaveLocal("flux.json", list);
                LogAction($"Flux divers supprimé : {item.Type}");
            }
        }

        // Factures
        public static List<Facture> GetFactures() => LoadLocal<Facture>("factures.json");
        public static void SaveFacture(Facture item)
        {
            var list = GetFactures();
            item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            item.IdUtilisateur = CurrentUser?.Id ?? 0;
            item.Date = DateTime.Now;

            // Attribuer les identifiants des lignes (ElementFacture) rattachées à cette facture
            int nextElementId = list.SelectMany(f => f.Elements).Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;
            foreach (var element in item.Elements)
            {
                element.IdFacture = item.Id;
                element.Id = nextElementId++;
            }

            list.Add(item);
            SaveLocal("factures.json", list);
            LogAction($"Facture créée : ID {item.Id} (Client: {item.NomClient}, Montant: {item.MontantTotal:N0} F)");
        }
        public static void UpdateFacture(Facture updatedItem)
        {
            var list = GetFactures();
            var index = list.FindIndex(x => x.Id == updatedItem.Id);
            if (index != -1)
            {
                list[index] = updatedItem;
                SaveLocal("factures.json", list);
                LogAction($"Facture mise à jour : ID {updatedItem.Id} (Client: {updatedItem.NomClient})");
            }
        }

        public static void DeleteFacture(int id)
        {
            var list = GetFactures();
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                SaveLocal("factures.json", list);
                LogAction($"Facture supprimée : ID {item.Id} (Montant: {item.MontantTotal:N0} F)");
            }
        }

        // --- GESTION ORANGE MONEY / TRANSFERTS ---

        // 1. Gestion de la liste des services (Orange, Wave, etc.)
        public static List<string> GetTransfertServices()
        {
            string path = Path.Combine(folderPath, "transfert_services.json");
            if (!File.Exists(path))
            {
                // Liste par défaut si le fichier n'existe pas encore
                var defaut = new List<string> { "CASH (Liquide)", "Orange Money", "Wave", "Moov Money", "Sewa", "Telecel" };
                SaveLocal("transfert_services.json", defaut);
                return defaut;
            }
            return LoadLocal<string>("transfert_services.json");
        }

        public static void AddTransfertService(string name)
        {
            var list = GetTransfertServices();
            if (!list.Contains(name))
            {
                list.Add(name);
                SaveLocal("transfert_services.json", list);
                LogAction($"Nouveau service de transfert ajouté : {name}");
            }
        }

        public static void DeleteTransfertService(string name)
        {
            var list = GetTransfertServices();
            if (list.Remove(name))
            {
                SaveLocal("transfert_services.json", list);
                LogAction("Moyen d'encaissement retiré : " + name);
            }
        }

        // 2. Gestion des Notes / Incidents (Entrées/Sorties de caisse)
        public static List<OMNote> GetOMNotes() => LoadLocal<OMNote>("om_notes.json");

        public static void SaveOMNote(OMNote item)
        {
            var list = GetOMNotes();
            item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            item.Agent = CurrentUser?.Nom ?? "Système";
            list.Add(item);
            SaveLocal("om_notes.json", list);
            LogAction($"Note de caisse ajoutée : {item.Type} - {item.Montant} F ({item.Description})");
        }

        public static void DeleteOMNote(int id)
        {
            var list = GetOMNotes();
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                SaveLocal("om_notes.json", list);
                LogAction($"Note de caisse supprimée : {item.Description}");
            }
        }

        // 3. Gestion des Clôtures (Bilan de fin de journée)
        public static List<OMCloture> GetOMClotures() => LoadLocal<OMCloture>("om_clotures.json");

        public static void SaveOMCloture(OMCloture item)
        {
            var list = GetOMClotures();
            item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            item.Date = DateTime.Now;
            item.Agent = CurrentUser?.Nom ?? "Système";
            list.Add(item);
            SaveLocal("om_clotures.json", list);
            LogAction($"Clôture de caisse effectuée par {item.Agent}. Écart : {item.Ecart} F");
        }

        // --- GESTION DES UTILISATEURS ---
        public static List<Utilisateur> GetUtilisateurs() => LoadLocal<Utilisateur>("utilisateurs.json");

        // Hachage SHA-256 (le modèle recommande de ne jamais stocker le mot de passe en clair)
        public static string HashPassword(string motDePasse)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(motDePasse ?? string.Empty));
                var sb = new System.Text.StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // Crée le compte administrateur par défaut au premier lancement et RÉPARE les comptes hérités
        public static void EnsureDefaultAdmin()
        {
            var list = GetUtilisateurs();
            bool modifie = false;

            if (list.Count == 0)
            {
                list.Add(new Utilisateur
                {
                    Id = 1,
                    Nom = "Administrateur",
                    Username = "admin",
                    MotDePasse = HashPassword("admin123"),
                    Role = "Admin",
                    Statut = "Actif",
                    DateCreation = DateTime.Now
                });
                modifie = true;
                LogAction("Compte administrateur par défaut créé (admin / admin123) — pensez à changer le mot de passe.");
            }

            // Compatibilité : les comptes créés avant l'ajout des statuts (Statut vide) sont activés
            foreach (var u in list.Where(x => string.IsNullOrEmpty(x.Statut)))
            {
                u.Statut = "Actif";
                modifie = true;
            }

            // Sécurité : il doit toujours exister au moins un administrateur actif
            if (list.Count > 0 && !list.Any(u => u.Role == "Admin" && u.EstActif))
            {
                list[0].Role = "Admin";
                list[0].Statut = "Actif";
                modifie = true;
                LogAction("Aucun administrateur actif détecté : le premier compte a été promu administrateur actif.");
            }

            if (modifie) SaveLocal("utilisateurs.json", list);
        }

        public static void SaveUtilisateur(Utilisateur item)
        {
            var list = GetUtilisateurs();
            item.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            list.Add(item);
            SaveLocal("utilisateurs.json", list);
            LogAction($"Utilisateur créé : {item.Nom} ({item.Username}, {item.Role})");
        }

        public static void UpdateUtilisateur(Utilisateur updatedItem)
        {
            var list = GetUtilisateurs();
            var index = list.FindIndex(x => x.Id == updatedItem.Id);
            if (index != -1)
            {
                list[index] = updatedItem;
                SaveLocal("utilisateurs.json", list);
                LogAction($"Utilisateur modifié : {updatedItem.Nom} ({updatedItem.Username})");
            }
        }

        public static void DeleteUtilisateur(int id)
        {
            var list = GetUtilisateurs();
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                SaveLocal("utilisateurs.json", list);
                LogAction($"Utilisateur supprimé : {item.Nom} ({item.Username})");
            }
        }

        // --- SESSION & AIDE À LA DÉCISION ---

        // L'utilisateur connecté est-il administrateur ?
        public static bool EstAdmin => CurrentUser != null && CurrentUser.Role == "Admin";

        // Nombre d'administrateurs ACTIFS (garde-fou : il doit toujours en rester au moins un)
        public static int NombreAdminsActifs() =>
            GetUtilisateurs().Count(u => u.Role == "Admin" && u.EstActif);

        public static Utilisateur GetUtilisateurParUsername(string username) =>
            GetUtilisateurs().FirstOrDefault(u => u.Username != null &&
                                                  u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        public static Utilisateur GetUtilisateurParId(int id) =>
            GetUtilisateurs().FirstOrDefault(u => u.Id == id);

        // Active / désactive / remet en attente un compte (page admin dédiée)
        public static void ChangerStatutUtilisateur(int id, string nouveauStatut)
        {
            var list = GetUtilisateurs();
            var user = list.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            user.Statut = nouveauStatut;
            SaveLocal("utilisateurs.json", list);
            LogAction($"Compte {nouveauStatut} : {user.Nom} ({user.Username})");
        }

        // Réinitialise le mot de passe d'un compte (action administrateur)
        public static void ReinitialiserMotDePasse(int id, string nouveauMotDePasse)
        {
            var list = GetUtilisateurs();
            var user = list.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            user.MotDePasse = HashPassword(nouveauMotDePasse);
            SaveLocal("utilisateurs.json", list);
            LogAction($"Mot de passe réinitialisé par {CurrentUser?.Nom ?? "?"} pour {user.Username}");
        }

        // Met à jour la date de dernière connexion (sans polluer le journal d'activité)
        public static void EnregistrerConnexion(int id)
        {
            var list = GetUtilisateurs();
            var user = list.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            user.DerniereConnexion = DateTime.Now;
            SaveLocal("utilisateurs.json", list);
        }

        // Ferme la session en cours
        public static void Deconnecter()
        {
            if (CurrentUser != null) LogAction("Déconnexion de " + CurrentUser.Nom);
            CurrentUser = null;
        }

        // Le compte utilise-t-il encore le mot de passe par défaut ?
        public static bool MotDePasseParDefaut(Utilisateur u) =>
            u != null && !string.IsNullOrEmpty(u.Username) &&
            u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) &&
            u.MotDePasse == HashPassword("admin123");

        // --- SAUVEGARDE DES DONNÉES (LocalData\Backups) ---
        // Copie tous les fichiers JSON de LocalData dans un dossier horodaté. Retourne le chemin créé.
        public static string BackupData()
        {
            string backupRoot = Path.Combine(folderPath, "Backups");
            if (!Directory.Exists(backupRoot)) Directory.CreateDirectory(backupRoot);

            string destination = Path.Combine(backupRoot, "backup_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss"));
            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(folderPath, "*.json"))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));

            LogAction("Sauvegarde des données effectuée : " + destination);
            return destination;
        }

        // Crée une sauvegarde une fois par jour (au premier lancement de la journée)
        public static void AutoBackupDaily()
        {
            try
            {
                string backupRoot = Path.Combine(folderPath, "Backups");
                string prefix = "backup_" + DateTime.Now.ToString("yyyy_MM_dd");
                if (!Directory.Exists(backupRoot) ||
                    !Directory.GetDirectories(backupRoot).Any(d => Path.GetFileName(d).StartsWith(prefix)))
                {
                    BackupData();
                }
            }
            catch
            {
                // Ne jamais bloquer le démarrage si la sauvegarde échoue
            }
        }
    }
}