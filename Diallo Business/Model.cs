using System;
using System.Collections.Generic;

namespace DialloBusinessCenter.Models
{
    // --- CLASSE UTILISATEUR ---
    public class Utilisateur
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Username { get; set; }
        public string MotDePasse { get; set; } // Haché en SHA-256 par Utils.HashPassword (jamais en clair)
        public string Role { get; set; } // Admin, Agent
        public string Statut { get; set; } // Actif, En attente, Désactivé
        public DateTime DateCreation { get; set; }
        public DateTime? DerniereConnexion { get; set; }

        // Compatibilité ascendante : les comptes créés avant l'ajout des statuts (Statut vide)
        // sont considérés comme actifs — sinon l'admin existant serait verrouillé hors de l'app.
        public bool EstActif => string.IsNullOrEmpty(Statut) || Statut == "Actif";
        public string StatutAffiche => string.IsNullOrEmpty(Statut) ? "Actif" : Statut;
        public string StatutColor => StatutAffiche == "Actif" ? "#4ADE80"
                                   : (StatutAffiche == "En attente" ? "#F59E0B" : "#F87171");
        public string DerniereConnexionAffiche => DerniereConnexion.HasValue
            ? DerniereConnexion.Value.ToString("dd/MM/yyyy HH:mm")
            : "Jamais";
    }

    // --- CLASSE ARTICLE (STOCK PHYSIQUE) ---
    public class Article
    {
        public int Id { get; set; }
        public int IdUtilisateur { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public decimal PrixAchat { get; set; }
        public decimal PrixVente { get; set; }
        public int QuantiteDispo { get; set; }
        public DateTime DateAjout { get; set; }
        public string Type { get; set; } // Ex: Électronique, Consommable
    }

    // --- CLASSE SERVICE (MAINTENANCE, COPIE, ETC.) ---
    public class Service
    {
        public int Id { get; set; }
        public int IdUtilisateur { get; set; }
        public string TypeService { get; set; }
        public decimal Prix { get; set; }
        public DateTime DateAction { get; set; }
        public DateTime? DateRendezVous { get; set; } // Nullable si pas de RDV
        public decimal Accompte { get; set; }
        public decimal Reliquat { get; set; }
        public string Info { get; set; }
        public string NomClient { get; set; }
        public string TelephoneClient { get; set; }
    }

    // --- CLASSE FORMATION ---
    public class Formation
    {
        public int Id { get; set; }
        public int IdUtilisateur { get; set; }
        public string Nom { get; set; }
        public decimal Prix { get; set; }
        public decimal Accompte { get; set; }
        public decimal Reliquat { get; set; }
        public string NomClient { get; set; }
        public string TelephoneClient { get; set; }
        public string Planification { get; set; } // Ex: "Lundi-Mercredi 14h-16h"
        public string Statut { get; set; } // En cours, Achevé, Annulé
        public DateTime Date { get; set; } // Date d'inscription (fallback Now si absent des anciens JSON)
    }

    // --- CLASSE FACTURE & ELEMENTS ---
    public class Facture
    {
        public int Id { get; set; }
        public int IdUtilisateur { get; set; }
        public List<ElementFacture> Elements { get; set; } = new List<ElementFacture>();
        public decimal MontantTotal { get; set; }
        public string NomClient { get; set; }
        public string TelephoneClient { get; set; }
        public DateTime Date { get; set; }
        public decimal Accompte { get; set; }
        public decimal Reliquat { get; set; }

        // Couleur du reliquat pour les DataGrid (rouge si dette, vert si soldé)
        public string HasDebtColor => Reliquat > 0 ? "#F87171" : "#4ADE80";
    }

    public class ElementFacture
    {
        public int Id { get; set; }
        public int IdFacture { get; set; }
        public string Designation { get; set; }
        public decimal PrixUnitaire { get; set; }
        public int Quantite { get; set; }
        public decimal TotalLigne => PrixUnitaire * Quantite;
    }

    // --- CLASSE MOUVEMENT DE CAISSE (SORTANT/ENTRANT) ---
    public class FluxDivers
    {
        public int Id { get; set; }
        public int IdUtilisateur { get; set; }
        public string Type { get; set; } // "ENTREE" ou "SORTIE"
        public decimal Montant { get; set; }
        public string Motif { get; set; }
        public DateTime Date { get; set; }
    }

    // --- CLASSE NOTE ---
    public class Note
    {
        public int Id { get; set; }
        public int IdUtilisateur { get; set; }
        public string Titre { get; set; }
        public string Contenu { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class OMNote
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public decimal Montant { get; set; }
        public string Description { get; set; }
        public string Agent { get; set; }
        public string ColorType => Type == "ENTREE" ? "#4ADE80" : "#F87171";
    }

    public class OMCloture
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Agent { get; set; }
        public decimal SoldeHier { get; set; } // Somme réelle du soir précédent
        public decimal SoldeTheorique { get; set; } // Calcul : Hier + Entrées - Sorties
        public decimal SoldePhysiqueTotal { get; set; } // Somme de tous les comptes + liquide saisis
        public decimal Ecart { get; set; } // Manquant ou surplus
        public Dictionary<string, decimal> Details { get; set; } // Orange, Wave, Cash, etc.
    }
}