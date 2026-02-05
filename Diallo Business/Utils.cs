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
            list.Add(item);
            SaveLocal("factures.json", list);
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
    }
}