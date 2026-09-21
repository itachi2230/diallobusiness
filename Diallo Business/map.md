# 🗺️ MAP COMPLET DU PROJET — DIALLO BUSINESS

> **Objectif de ce document** : cartographie exhaustive du projet (but, technos, architecture, fonctionnalités, fichiers, conventions) permettant à toute personne — humaine ou IA — de travailler sur le code avec précision, sans exploration préalable.
>
> **Dernière mise à jour** : 21/09/2026 — commit `9392c20` + **majeure** : écrans Connexion/Facturation/Paramètres ajoutés, 11 bugs corrigés (voir §11), branche `main`.
> **Repo Git** : `https://github.com/itachi2230/diallobusiness.git` — **Ce fichier est référencé par `.clinerules` (racine du repo) : toute modification pertinente du code doit être répercutée ici.**

---

## 1. Identité du projet

| Élément | Valeur |
|---|---|
| Nom | **Diallo Business** (titre de fenêtre : *Diallo Business Center*) |
| Type | Application de bureau **Windows** (WPF) |
| Langage | **C#** sur **.NET Framework 4.7.2** |
| UI | **XAML / WPF**, pattern **code-behind** (pas de MVVM) |
| Persistance | Fichiers **JSON locaux** via **Newtonsoft.Json 13.0.4** — *aucune base de données* |
| Namespace principal | `Diallo_Business` |
| Namespace des modèles | `DialloBusinessCenter.Models` |
| Assembly | `Diallo Business` (WinExe), version 1.0.0.0 |
| Dossier projet | `c:\Users\ITACHI\repos\diallobusiness\Diallo Business` |
| Solution Visual Studio | `c:\Users\ITACHI\repos\diallobusiness\Diallo Business.sln` (à la racine du repo) |
| Historique Git | `initial commit` → `done with stock and dash` → `bilan et debut de Transfert` |

---

## 2. But du projet (contexte métier)

Logiciel de **gestion pour un centre de business / cyber-café** (photocopie, impression, saisie de texte, maintenance PC, formations, vente d'accessoires) dans un contexte ouest-africain : montants en **francs (F CFA)**, moyens d'encaissement **mobile money** (Orange Money, Wave, Moov Money, Sewa, Telecel) et espèces.

Ce que l'application doit permettre au gérant :

1. **Vendre des articles** en stock et suivre les quantités disponibles ;
2. **Enregistrer des services** (photocopie, impression, maintenance…) avec acompte et reliquat par client ;
3. **Inscrire des élèves à des formations** avec paiement échelonné et planification ;
4. **Tracer les mouvements de caisse** du jour (entrées / sorties) ;
5. Faire la **clôture de caisse du soir** : comptage physique des montants par moyen de paiement, comparaison avec le solde théorique et mise en évidence de l'**écart** ;
6. Consulter des **bilans financiers** consolidés et filtrables (CA, encaissé, dettes) et **solder** les comptes clients ;
7. Conserver une **trace horodatée** de toutes les actions (logs journaliers).

**Statut actuel : fonctionnel sur l'ensemble des modules.** Stock, Dashboard, Caisse/Clôture, Facturation multi-lignes, Bilans, Connexion (admin/agent), Paramètres (utilisateurs, listes, sauvegarde). Restes mineurs listés en §11.

---

## 3. Technologies & dépendances

| Technologie | Rôle | Détails |
|---|---|---|
| .NET Framework **4.7.2** | Runtime | `<TargetFrameworkVersion>v4.7.2` dans le `.csproj`, `App.config` (supportedRuntime) |
| **WPF** (PresentationFramework/Core, WindowsBase, System.Xaml) | Interface graphique | Fenêtre sans bordure personnalisée + UserControls |
| **C# 7.3** | Logique métier | Code-behind, LINQ, classes statiques ; **aucun framework MVVM** |
| **Newtonsoft.Json 13.0.4** | Persistance | Sérialisation de `List<T>` dans `LocalData/*.json` — géré par `packages.config` (pas PackageReference) |
| System.Windows.Forms / System.Drawing | Référencés | Interop potentiel, quasi non exploités actuellement |
| MSBuild / Visual Studio | Build | Ouvrir la `.sln` à la racine du repo |

**Aucun** : base de données, serveur web, tests unitaires, CI/CD, thème dynamique, i18n.

---

## 4. Architecture logicielle

```text
┌───────────────────────────────────────────────────────────────────────┐
│ App.xaml.cs (OnStartup)                                               │
│   └── Win_Login (connexion obligatoire) ──► Utils.CurrentUser         │
│         └── MainWindow (SHELL) + sauvegarde quotidienne des données   │
│           ├── Sidebar (boutons de navigation, Tag = titre)            │
│           │     └── NavClick() ──► switch sur btn.Name                │
│           ├── TitleBar custom (réduire / agrandir / fermer, ⏻ logout) │
│           └── MainContainer (ContentControl = zone de page)           │
│                 ├── UC_Dashboard    → stats du jour + saisie rapide   │
│                 ├── UC_Stock        → CRUD articles + recherche       │
│                 ├── UC_Facturation  → factures multi-lignes + impression │
│                 ├── UC_Transferts   → caisse du jour + clôture + flux │
│                 ├── UC_Bilans       → registre consolidé filtrable    │
│                 └── UC_Parametres   → utilisateurs, listes, backup    │
│                                                                       │
│ Model.cs    → tous les POCOs métier (namespace DialloBusinessCenter)  │
│ Utils.cs    → classe statique = couche "repository" (namespace        │
│               Diallo_Business) : CRUD JSON + logs + utilisateurs      │
│               + sauvegardes. ↕ Newtonsoft.Json                        │
│               bin\Debug\LocalData\*.json  +  LocalData\Logs\*.txt     │
└───────────────────────────────────────────────────────────────────────┘
```

**Flux type d'une action :**
`Événement XAML (Click / TextChanged / SelectionChanged)` → `handler dans le code-behind (.xaml.cs)` → `Utils.SaveXxx / GetXxx` → `lecture-écriture du fichier JSON` → `rafraîchissement du DataGrid / des stats`.

**Points clés à retenir :**
- **Pas de MVVM** : chaque `UserControl` manipule directement ses contrôles nommés via `x:Name` et appelle `Utils` ;
- **Utils = unique point d'accès aux données** (classe statique, un fichier JSON par type de donnée) ;
- `Utils.CurrentUser` (`Utilisateur`) est renseigné par **`Win_Login`** au démarrage (et après chaque reconnexion) → il alimente `IdUtilisateur`, l'`Agent` des notes de clôture et le nom affiché dans la carte utilisateur ; sans connexion valide, l'application ne démarre pas ;
- Les Id sont auto-incrémentés « à la main » : `list.Max(x => x.Id) + 1` ;
- Une page = un couple `UC_Xxx.xaml` + `UC_Xxx.xaml.cs` déclaré dans le `.csproj` (`Page` + `Compile`) et branché dans `NavClick`.

---

## 5. Map des fichiers

```text
c:\Users\ITACHI\repos\diallobusiness\            ← RACINE DU REPO GIT
├── .gitignore                # ignore bin/, obj/, packages/, *.user, *.dll, *.exe, *.pdb, *.log…
├── .clinerules               # RÈGLES CLINE : référence map.md + obligation de mise à jour
├── Diallo Business.sln       # solution Visual Studio (1 seul projet)
├── packages\                 # paquets NuGet (Newtonsoft.Json 13.0.4) — non versionné
└── Diallo Business\          ← RACINE DU PROJET
    ├── Diallo Business.csproj  # config MSBuild : net472, WinExe, références (System.Printing, ReachFramework), fichiers
    ├── App.config              # supportedRuntime .NET 4.7.2 — rien d'autre
    ├── packages.config         # Newtonsoft.Json 13.0.4 (net472)
    ├── map.md                  # CE DOCUMENT (carte complète du projet)
    ├── App.xaml                # STYLES GLOBAUX (StartupUri retiré : démarrage géré par App.xaml.cs)
    ├── App.xaml.cs             # OnStartup : Win_Login → MainWindow + AutoBackupDaily + exceptions globales
    ├── Win_Login.xaml(.cs)     # ÉCRAN DE CONNEXION (admin/agent, mot de passe haché SHA-256)
    ├── MainWindow.xaml         # SHELL : sidebar 280px + titlebar custom + MainContainer (ContentControl)
    ├── MainWindow.xaml.cs      # NavClick (Tag = titre), toggle sidebar, drag, min/max/close, déconnexion
    ├── UC_Dashboard.xaml(.cs)  # Page Dashboard : cartes stats + activités + saisie rapide 4 modes (validée)
    ├── UC_Stock.xaml(.cs)      # Page Stock : CRUD articles + recherche (type restauré à l'édition)
    ├── UC_Facturation.xaml(.cs)# Page Facturation : factures multi-lignes + acompte/reliquat + solder + aperçu/impression
    ├── UC_Transferts.xaml(.cs) # Page Caisse : notes + clôture (détails persistés) + FLUX DIVERS consultables
    ├── UC_Bilans.xaml(.cs)     # Page Bilans : filtres + fusion 3 sources + solder (statut formation) + supprimer
    ├── UC_Parametres.xaml(.cs) # Page Paramètres : comptes utilisateurs + listes de référence + sauvegarde des données
    ├── Model.cs                # TOUS les modèles métier (namespace DialloBusinessCenter.Models)
    ├── Utils.cs                # Couche données statique : CRUD JSON + logs + utilisateurs + sauvegardes
    ├── Properties\
    │   ├── AssemblyInfo.cs     # Attributs assembly : titre "Diallo Business", v1.0.0.0, ThemeInfo
    │   ├── Resources.resx      # RESX par défaut vide (+ Resources.Designer.cs généré)
    │   └── Settings.settings   # Settings vide (+ Settings.Designer.cs généré)
    ├── bin\Debug\
    │   ├── Diallo Business.exe / .pdb / Newtonsoft.Json.dll   # artefacts de compilation
    │   └── LocalData\          # ← DONNÉES UTILISATEUR (créées à l'exécution, voir §7)
    └── obj\                    # artefacts intermédiaires MSBuild (généré)
```

> **Note** : `bin\` et `obj\` sont ignorés par Git (`.gitignore`) mais contiennent les vraies données si l'app tourne en Debug depuis VS. Supprimer `LocalData\` remet l'application « à zéro » (le dossier est recréé automatiquement au démarrage, avec le compte admin par défaut).
> **Nettoyage effectué** : `TextFile1.txt` (ancienne copie de MainWindow) et `UC_stock.resx` (resx vide) ont été **supprimés** du projet et du disque.

---

## 6. Détail fichier par fichier (rôle, membres, points d'attention)

### 6.1 `App.xaml` + `App.xaml.cs` — entrée, thème global, sécurité
- `App.xaml` : plus de `StartupUri` — le démarrage est géré par code. **Styles globaux** (couleurs « dark ») : `PrimaryBlue #38BDF8`, `DeepBg #0B1120`, `CardBg #151C2C`, `AccentViolet #7000FF`, texte `#E2E8F0`.
- Styles implicites redéfinis pour **toute l'application** : `TextBlock` (Segoe UI, gris clair), `ScrollBar` (fin, 6px, thumb `#334155` → bleu au survol), `TextBox` (fond `#1E293B`, coins 8px, bordure bleue au focus), **`PasswordBox`** (même rendu que TextBox), `DataGrid` (transparent, lignes 50px, pas de gridlines), `DataGridColumnHeader`, `DataGridCell`, `DataGridRow`, `ComboBox` + `ComboBoxItem` (template custom avec `PART_EditableTextBox` pour les combos éditables), `Button` (violet → bleu au survol, coins 10px).
- `App.xaml.cs` (`OnStartup`) : abonnement aux **exceptions globales** (logging `ERREUR :` + MessageBox, jamais de crash silencieux) → `Utils.EnsureDefaultAdmin()` → **`Win_Login` modal** → si connexion OK : `Utils.AutoBackupDaily()` puis `MainWindow` ; sinon `Shutdown()`.
- ⚠️ Conséquence : tout nouveau contrôle hérite de ces styles ; pour un style ponctuel, définir `x:Key` ou surcharger localement.

### 6.2 `MainWindow.xaml(.cs)` — shell & navigation
- Fenêtre **sans chrome** (`WindowStyle=None`, `AllowsTransparency`, coins arrondis 20px, 1200×750).
- XAML : sidebar (`SideColumn`, 280px), logo « D », 5 boutons `SidebarButton` (Dashboard / Gestion Stock / Facturation / Bilans Financiers / Paramètres), `UserCard`, `TitleBar` (drag + min/max/close), titre de page `TxtPageTitle`, `MainContainer` (ContentControl).
- Code-behind :
  - Constructeur : `MaxHeight` écran ; affiche `Utils.CurrentUser.Nom/Role` dans la carte utilisateur ; **charge `UC_Dashboard` par défaut** ;
  - `BtnToggle_Click` : replie la sidebar (280 → 100px) ;
  - `NavClick` : titre depuis **`btn.Tag`** (plus de découpe fragile du Content), switch sur `btn.Name` ;
  - Mapping : `BtnDashboard→UC_Dashboard`, `BtnStock→UC_Stock`, `BtnFactures→UC_Facturation`, `BtnBilans→UC_Bilans`, `BtnParametres→UC_Parametres` ;
  - `BtnLogout_Click` : confirmation → log « Déconnexion » → rouvre `Win_Login` (nouvelle `MainWindow` si succès, sinon fermeture).

### 6.3 `Model.cs` — modèles métier (namespace `DialloBusinessCenter.Models`)
| Classe | Champs | Remarques |
|---|---|---|
| `Utilisateur` | Id, Nom, Username, MotDePasse, Role | Jamais persisté ni utilisé (pas de login) |
| `Article` | Id, IdUtilisateur, Nom, Description, PrixAchat, PrixVente, QuantiteDispo, DateAjout, Type | `Description` jamais saisie dans l'UI |
| `Service` | Id, IdUtilisateur, TypeService, Prix, DateAction, DateRendezVous?, Accompte, Reliquat, Info, NomClient, TelephoneClient | RDV nullable jamais exploité |
| `Formation` | Id, IdUtilisateur, Nom, Prix, Accompte, Reliquat, NomClient, TelephoneClient, Planification, Statut, **Date** | Nom codé en dur « Formation Standard » côté Dashboard ; `Date` = inscription (fallback `Now` si absent des anciens JSON) |
| `Facture` | Id, IdUtilisateur, Elements (List\<ElementFacture\>), MontantTotal, NomClient, TelephoneClient, Date, Accompte, Reliquat, **HasDebtColor** (calc) | Remplie par UC_Facturation (multi-lignes) ; HasDebtColor pour l'affichage du reliquat |
| `ElementFacture` | Id, IdFacture, Designation, PrixUnitaire, Quantite, TotalLigne (calc) | Prêt pour une facturation multi-lignes future |
| `FluxDivers` | Id, IdUtilisateur, Type («ENTREE»/«SORTIE»), Montant, Motif, Date | |
| `Note` | Id, IdUtilisateur, Titre, Contenu, DateCreation | **Jamais utilisé** dans l'UI (réservé pour une fonction « bloc-notes ») |
| `OMNote` | Id, Date, Type, Montant, Description, Agent, ColorType (calc) | Note de caisse ; couleur verte/rouge selon type |
| `OMCloture` | Id, Date, Agent, SoldeHier, SoldeTheorique, SoldePhysiqueTotal, Ecart, Details (Dict\<string,decimal\>) | `Details` et `SoldeTheorique` non remplis à la sauvegarde |

### 6.4 `Utils.cs` — couche données statique
- **Moteur générique** : `SaveLocal<T>(file, list)` / `LoadLocal<T>(file)` — sérialisation JSON indentée dans `BaseDirectory\LocalData`. Retourne liste vide si fichier absent.
- **CRUD par entité** (chaque entité suit le même schéma : `GetXxx` / `SaveXxx` (Id auto = Max+1, horodatage) / `UpdateXxx` (remplacement par index) / `DeleteXxx`) :
  - Articles (`articles.json`) — Save (log) / Update (log) / Delete (log) ;
  - Services (`services.json`) — Save/Update/Delete ;
  - Formations (`formations.json`) — Save/Update/Delete + log ;
  - FluxDivers (`flux.json`) — Save/Delete ;
  - Factures (`factures.json`) — Save (**attribue Id/IdFacture à chaque ElementFacture** + log) / Update (log) / Delete (log) ;
  - OMNotes (`om_notes.json`) — Save (Agent auto) / Delete ;
  - OMClotures (`om_clotures.json`) — Save (Date=Now, Agent auto) ;
  - **Utilisateurs** (`utilisateurs.json`) — `GetUtilisateurs` / `SaveUtilisateur` / `UpdateUtilisateur` / `DeleteUtilisateur` + `EnsureDefaultAdmin()` (compte `admin`/`admin123` créé au 1er lancement) + `HashPassword` (**SHA-256**, jamais de mot de passe en clair).
- **Listes de référence** : `GetListeServices()/AddServiceToList/DeleteServiceFromList` (`liste_services.json`, défaut : Photocopie, Impression Noir/Blanc, Saisie de texte, Maintenance PC) ; `GetTransfertServices()/AddTransfertService/DeleteTransfertService` (`transfert_services.json`, défaut : CASH (Liquide), Orange Money, Wave, Moov Money, Sewa, Telecel).
- **Sauvegardes** : `BackupData()` copie tous les JSON dans `LocalData\Backups\backup_yyyy_MM_dd_HHmmss` ; `AutoBackupDaily()` une fois par jour au démarrage (après connexion).
- **`LogAction(string)`** : écrit dans `LocalData\Logs\log_AAAA_MM_JJ.txt` une ligne `[HH:mm:ss] Agent : message` ; silencieux en cas d'échec.
- ⚠️ **Limitations connues** : pas de thread-safety (réécriture fichier complet à chaque save), pas de verrou, opérations disque synchrones, aucun chiffrement du dossier de données.

### 6.5 `UC_Stock.xaml(.cs)` — gestion du stock
- XAML : colonne gauche = `TxtSearch` + bouton « + Ajouter un Article » + `DgStock` (Article, Type, P.Achat gris, P.Vente vert, Stock, colonne Actions ✎/🗑) ; colonne droite = `BorderForm` (largeur 0→350 = panneau qui « glisse ») avec formulaire Nom / P.Achat / P.Vente / Quantité / Type (ComboBox : Électronique, Consommable, Boisson/Café).
- Code-behind : `currentEditingArticle` (null = ajout) ; `RefreshGrid` (rebind), `BtnShowAdd/ClearFields/BtnCancel` (show/hide), `BtnSave_Click` (validation : nom + prix de vente obligatoires, **parsing défensif TryParse** avec messages clairs ; branche Save vs Update selon `Id==0`), `BtnEdit_Click` (pré-remplit **y compris le Type dans le ComboBox**), `BtnDelete_Click` (confirmation), `TxtSearch_TextChanged` (filtre sur Nom).

### 6.6 `UC_Dashboard.xaml(.cs)` — tableau de bord + saisie rapide
- XAML : 2 cartes stats (« Ventes du Jour » `TxtSales`, « Reliquats à encaisser » `TxtDebt`) ; `LstActivity` (10 dernières lignes du log du jour, ordre inversé) ; panneau droit « Saisie Rapide » avec `ComboOpType` (4 modes) → panneaux exclusifs : `PanelService` (combo éditable type de service, client, total, acompte), `PanelFormation` (élève, prix total, acompte, planification), `PanelArticle` (combo article, prix unitaire auto, quantité, client optionnel), `PanelFlux` (entrée/sortie, motif, montant) ; bouton « Enregistrer l'opération ».
- Code-behind : `LoadData` (stats : ventes = factures du jour ; dettes = somme des reliquats services + formations ; activité = lecture du log), `ComboOpType_SelectionChanged` (visibilité des panneaux), `BtnSaveOp_Click` (4 branches **avec validations métier** : Service → type + total > 0 + acompte ∈ [0,total] requis, SaveService + AddServiceToList ; Formation → élève + prix > 0 requis, `Date = Now`, SaveFormation ; Flux → montant > 0 requis, SaveFlux ; Article → quantité > 0 contrôlée, contrôle de stock suffisant, création facture payée comptant, décrément du stock via UpdateArticle, SaveFacture), `ClearInputs` (**vide les 4 panneaux**), `InpArtSelection_SelectionChanged` (pré-remplit le prix).

### 6.7 `UC_Transferts.xaml(.cs)` — caisse du jour, clôture & flux divers
- XAML gauche : bandeau « MOUVEMENTS DE CAISSE (HORS TRANSFERTS) », ligne de saisie (Type SORTIE/ENTREE, montant, description) → `GridNotes` (heure, type coloré via `ColorType`, montant, description, 🗑️) ; puis section **« FLUX DIVERS (AUTRES ENTRÉES / SORTIES) »** → `GridFlux` (date, type coloré par DataTrigger, montant, motif, 🗑️) affichant les 50 derniers flux.
- XAML droit « BILAN DU SOIR » : `TxtSoldeHier`, `ContainerSoldes` (**rempli dynamiquement en code** : un TextBox par moyen de paiement), bouton « + Ajouter un nouveau service » (mini-fenêtre codée à la main), bouton « VALIDER ET FAIRE LE COMPTE » → `BorderResultat` (écart coloré + message).
- Code-behind : `ChargerConfig` (services de transfert + solde de la veille = dernière clôture), `GenererChampsSaisie` (dictionnaire `inputs` nom→TextBox), `LoadDailyData` (notes du jour uniquement), `LoadFlux` (50 derniers flux), `BtnAddNote_Click` (montant invalide → message), `BtnCalculer_Click` (total physique = somme des champs ; théorique = soldeHier + entrées − sorties ; écart = physique − théorique ; vert « Caisse Parfaite » / teal « Surplus » / rouge « Manquant » ; **sauvegarde désormais complète de la clôture : `SoldeTheorique` + `Details` (montant par moyen) persistés**), `BtnAddService_Click`, `BtnDeleteNote_Click`, `BtnDeleteFlux_Click`.

### 6.8 `UC_Bilans.xaml(.cs)` — bilans financiers consolidés
- Classe `BilanRow` (définie dans le même fichier) : vue unifiée des 3 sources avec `Categorie` (SERVICE/FORMATION/VENTE), couleurs de badge (`CategoryBrush`) et de dette (`HasDebtColor`), et `SourceObject` (référence l'objet d'origine pour solder/supprimer).
- XAML : barre de filtres (recherche, catégorie, **période très complète** : Aujourd'hui, Hier, Avant-hier, J-3 à J-5, Cette Semaine, Ce Mois, Mois Passé, Mois Surpassé, Cette Année, Année Dernière, date précise via `PickerDate`), `CheckReliquat` (uniquement les dettes), `GridGlobal` (date, badge catégorie, client, libellé, total, reliquat coloré), bandeau de totaux (CA / ENCAISSÉ / DETTES), panneau droit de détails avec boutons **SOLDER LE COMPTE** et **SUPPRIMER**.
- Code-behind : `LoadData` (fusion services + formations + factures, **date formation = `f.Date` avec fallback `Now` pour anciens JSON**), `ApplyFilters` (les 4 filtres cumulés + tri date desc + recalcul des totaux), `GridGlobal_SelectionChanged` (détails, bouton Solder visible seulement si reliquat > 0), `BtnSolder_Click` (acompte += reliquat, reliquat = 0, **`Statut = "Achevé"` pour les formations**, Update sur le bon type + log), `BtnDelete_Click` (supprime dans la bonne source), `ComboPeriode_SelectionChanged` (affiche le DatePicker), **3 handlers distinctement nommés** (`ApplyFilters_Click` / `ApplyFilters_SelectionChanged` / `ApplyFilters_TextChanged` — les 3 surcharges ambiguës `ApplyFilters_Changed` ont été supprimées).

### 6.9 `Win_Login.xaml(.cs)` — écran de connexion (NOUVEAU)
- Fenêtre sans bordure thémée (logo « D », champs Identifiant/Mot de passe, message d'erreur `TxtError`, bouton `IsDefault` + touche Entrée, fenêtre déplaçable).
- Code-behind : vérifie le compte (`Username` insensible à la casse) et compare **`Utils.HashPassword(password)` (SHA-256)** au hash stocké ; succès → `Utils.CurrentUser = user` + log « Connexion » + `DialogResult = true` ; échec → log « Échec de connexion » + message.
- Compte par défaut créé par `EnsureDefaultAdmin()` : **admin / admin123** (à changer dans Paramètres).

### 6.10 `UC_Facturation.xaml(.cs)` — facturation multi-lignes (NOUVEAU)
- Gauche : recherche client + `GridFactures` (N°, date, client, nb lignes, total, payé, reliquat coloré via `Facture.HasDebtColor`) + barre de totaux (Facturé / Encaissé / Reliquats).
- Droite, deux modes : **NOUVELLE FACTURE** (client, téléphone, lignes désignation+PU+qté ajoutées/supprimées une à une, total calculé, acompte, reliquat calculé en direct) et **DÉTAIL** (client/téléphone/date, lignes en lecture seule, totaux, boutons : SOLDER, 🖨 Aperçu/Imprimer, SUPPRIMER, retour à la saisie).
- Impression : `ConstruireVisuelFacture(f)` génère la facture **noir sur blanc** (en-tête, client, tableau des lignes, totaux, mention de reliquat) ; aperçu dans une fenêtre thémée ; impression via `PrintDialog.PrintVisual` (références **System.Printing** + **ReachFramework** ajoutées au `.csproj`).
- `Utils.SaveFacture` attribue les Id des lignes (`ElementFacture.IdFacture`) automatiquement.

### 6.11 `UC_Parametres.xaml(.cs)` — paramètres (NOUVEAU)
- Carte **Comptes utilisateurs** : grille (nom, identifiant, rôle) ; Ajouter / Mettre à jour (mot de passe optionnel — conservé si vide) / Supprimer. Gardes-fous : identifiant unique, impossible de supprimer son propre compte, impossible de rétrograder/supprimer le dernier Admin. Mot de passe haché SHA-256.
- Carte **Listes de référence** : « Services (Saisie rapide) » (`liste_services.json`) et « Moyens d'encaissement (Clôture) » (`transfert_services.json`) — listes générées dynamiquement avec bouton supprimer par ligne + champ d'ajout.
- Carte **Données** : chemin du dossier `LocalData`, bouton « Sauvegarder maintenant » (`Utils.BackupData()`), « Ouvrir le dossier » (explorateur Windows), rappel de la sauvegarde quotidienne automatique.

### 6.12 Fichiers supprimés (nettoyage)
- `TextFile1.txt` (ancienne copie de `MainWindow.xaml.cs`) et `UC_stock.resx` (boilerplate vide) : supprimés du disque ET du `.csproj`.

---

## 7. Persistance — schéma des données locales (`bin\Debug\LocalData\`)

| Fichier JSON | Type stocké | Produit par | Consommé par |
|---|---|---|---|
| `articles.json` | `List<Article>` | UC_Stock, UC_Dashboard (décrément stock) | UC_Stock, UC_Dashboard |
| `services.json` | `List<Service>` | UC_Dashboard (saisie rapide) | UC_Dashboard (dettes), UC_Bilans |
| `formations.json` | `List<Formation>` | UC_Dashboard (saisie rapide) | UC_Dashboard (dettes), UC_Bilans |
| `factures.json` | `List<Facture>` (+ `Elements` : List\<ElementFacture\>) | UC_Dashboard (vente article), **UC_Facturation (multi-lignes)** | UC_Dashboard (ventes du jour), **UC_Facturation**, UC_Bilans |
| `flux.json` | `List<FluxDivers>` | UC_Dashboard (flux divers) | **UC_Transferts (grille Flux divers + suppression)** |
| `liste_services.json` | `List<string>` | UC_Dashboard (service saisi librement), **UC_Parametres** | Combo services du Dashboard, **UC_Parametres** |
| `transfert_services.json` | `List<string>` | UC_Transferts (« + Ajouter un service »), **UC_Parametres** | Champs de la clôture, **UC_Parametres** |
| `om_notes.json` | `List<OMNote>` | UC_Transferts (notes caisse) | UC_Transferts (grille + calcul) |
| `om_clotures.json` | `List<OMCloture>` (avec `SoldeTheorique` + `Details` remplis) | UC_Transferts (validation) | UC_Transferts (solde du matin) |
| `utilisateurs.json` | `List<Utilisateur>` (mot de passe haché SHA-256) | `EnsureDefaultAdmin()`, **UC_Parametres** | **Win_Login**, **UC_Parametres** |
| `Backups\backup_yyyy_MM_dd_HHmmss\*.json` | copies de secours de tous les JSON | `BackupData()` / `AutoBackupDaily()` | restauration manuelle |
| `Logs\log_AAAA_MM_JJ.txt` | texte brut | `Utils.LogAction` | UC_Dashboard (activités récentes) |

**Exemple d'enregistrement (`om_notes.json`) :**
```json
[
  {
    "Id": 1,
    "Date": "2026-09-21T14:30:05.123",
    "Type": "SORTIE",
    "Montant": 2500.0,
    "Description": "Achat papier A4",
    "Agent": "Système"
  }
]
```
(ColorType, TotalLigne, HasDebtColor, CategoryBrush sont des propriétés calculées — non sérialisées utilement.)

---

## 8. Fonctionnalités détaillées (par écran, état actuel)

### 8.1 Connexion & session (Win_Login) — ✅ nouveau
1. **Au démarrage**, `Win_Login` est affiché en modal avant toute autre fenêtre ; impossible d'accéder à l'application sans compte valide.
2. Saisie Identifiant + Mot de passe (touche Entrée valide) ; mot de passe comparé au **hash SHA-256** stocké.
3. Échec → message d'erreur + ligne de log « Échec de connexion ».
4. Succès → `Utils.CurrentUser` renseigné, log « Connexion : Nom (Rôle) », sauvegarde automatique quotidienne des données, ouverture de `MainWindow`.
5. **Carte utilisateur** en bas de la sidebar : nom + rôle réels ; bouton **⏻ Déconnexion** (confirmation → retour à l'écran de connexion).
6. Compte initial : **admin / admin123** (créé automatiquement au tout premier lancement).

### 8.2 Navigation (MainWindow) — ✅ corrigé
- Sidebar repliable (bouton ≡) : 280px ↔ 100px, masque logo + carte utilisateur.
- 5 entrées **toutes fonctionnelles** : Dashboard, Gestion Stock, **Facturation (UC_Facturation)**, Bilans Financiers, **Paramètres (UC_Parametres)**.
- Titre de page lu depuis `btn.Tag` (plus de `Substring(3)` fragile) ; page par défaut = **Dashboard**.
- Contrôles de fenêtre custom : réduire, agrandir (bascule ▢/❐), fermer, déplacement par glisser sur la barre de titre.

### 8.3 Gestion Stock (UC_Stock) — ✅ fonctionnel
1. **Lister** les articles (DataGrid : nom, type, prix d'achat, prix de vente, quantité).
2. **Rechercher** en temps réel (filtre sur le nom, insensible à la casse).
3. **Ajouter** un article (panneau latéral animé : nom*, prix achat, prix vente*, quantité, type parmi Électronique/Consommable/Boisson-Café).
4. **Modifier** un article (✎ → pré-remplissage complet **y compris le type**).
5. **Supprimer** un article (🗑 avec confirmation Yes/No).
- Le stock est automatiquement **décrémenté** lors d'une vente depuis le Dashboard.
- Saisies numériques validées (`TryParse` + messages explicites).

### 8.4 Dashboard (UC_Dashboard) — ✅ fonctionnel
1. **Ventes du Jour** : somme des factures datées d'aujourd'hui.
2. **Reliquats à encaisser** : somme des reliquats services + formations.
3. **Activités récentes** : 10 dernières lignes du journal du jour (du plus récent au plus ancien).
4. **Saisie rapide** (4 modes mutuellement exclusifs via le ComboBox, **tous validés**) :
   - **Nouveau Service** : type (saisie libre + ajout auto à la liste), client, total (> 0 requis), acompte (∈ [0,total]) → reliquat calculé ;
   - **Vente Article** : sélection article (prix pré-rempli), quantité (> 0 requis), contrôle **stock insuffisant**, facture payée comptant, décrément du stock ;
   - **Paiement Formation** : élève + prix total (> 0 requis), acompte (∈ [0,total]) → reliquat, planification, statut « En cours », **date d'inscription enregistrée** ;
   - **Flux Divers** : entrée ou sortie, motif, montant (> 0 requis).
5. Après enregistrement : message de succès, formulaire vidé (les 4 panneaux), statistiques rafraîchies.

### 8.5 Facturation (UC_Facturation) — ✅ nouveau
1. **Lister / rechercher** les factures par client ; colonnes N°, date, client, nombre de lignes, total, payé, reliquat (rouge si dette, vert si soldé).
2. **Barre de totaux** sur la liste filtrée : Facturé / Encaissé / Reliquats.
3. **Créer une facture multi-lignes** : client (obligatoire), téléphone, ajout de lignes une par une (désignation + prix unitaire + quantité, suppression ligne à ligne), **total calculé en direct**, acompte saisi, **reliquat recalculé en direct** (borné 0..total).
4. **Détail d'une facture** : client, téléphone, n° et date, tableau des lignes, totaux ; actions :
   - **SOLDER LA FACTURE** (acompte += reliquat, reliquat = 0) ;
   - **🖨 Aperçu / Imprimer** : facture générée en noir sur blanc (en-tête DIALLO BUSINESS, client, lignes, total/payé/reliquat) → aperçu puis `PrintDialog` ;
   - **SUPPRIMER** la facture (avec confirmation).
5. Chaque création/soldage/impression/suppression est **journalisée** (`LogAction`).
6. ⚠️ Les lignes de facture ne décrémentent **pas** le stock (la vente avec décrément de stock se fait depuis le Dashboard → « Vente Article »).

### 8.6 Caisse / Clôture / Flux (UC_Transferts) — ✅ fonctionnel
1. **Mouvements de caisse** du jour : saisie ENTRÉE/SORTIE (montant + description), grille colorée, suppression (les notes du jour uniquement).
2. **Flux divers** (enregistrés depuis le Dashboard) : **désormais consultables** dans la même page (50 derniers : date, type coloré, montant, motif) avec suppression unitaire.
3. **Clôture du soir** :
   - affichage du **solde du matin** (= solde physique de la dernière clôture) ;
   - un champ par moyen d'encaissement (Cash, Orange Money, Wave, Moov Money, Sewa, Telecel — extensible via « + Ajouter un nouveau service ») ;
   - calcul : `théorique = solde du matin + entrées − sorties` ; `écart = compté − théorique` ;
   - verdict visuel : **Caisse Parfaite** (vert) / **Surplus** (teal) / **Manquant** (rouge) ;
   - enregistrement complet de la clôture : date, agent, **solde hier, solde théorique, solde physique, écart et détail par moyen de paiement (`Details`)**.

### 8.7 Bilans Financiers (UC_Bilans) — ✅ fonctionnel
1. **Registre consolidé** : services + formations + ventes, unifiés en `BilanRow` (badge de catégorie coloré).
2. **Filtres cumulables** : texte (client/libellé), catégorie, période (14 options + date précise), case « Dettes » (reliquat > 0).
3. **Totaux dynamiques** sur la sélection filtrée : CA, encaissé, dettes.
4. **Panneau détails** au clic sur une ligne (client, libellé, total, payé, reste).
5. **SOLDER LE COMPTE** : bascule le reliquat vers l'acompte (visible si reliquat > 0) ; pour une **formation soldée, le statut passe automatiquement à « Achevé »**.
6. **SUPPRIMER** une opération (dans sa source d'origine, avec confirmation).

### 8.8 Paramètres (UC_Parametres) — ✅ nouveau
1. **Comptes utilisateurs** : liste (nom, identifiant, rôle) ; création (mot de passe obligatoire), mise à jour (mot de passe optionnel : vide = conservé), suppression.
2. **Gardes-fous de sécurité** : identifiant unique, impossible de supprimer son propre compte, impossible de supprimer/rétrograder le dernier administrateur. Mots de passe hachés SHA-256.
3. **Listes de référence** : services de la saisie rapide et moyens d'encaissement de la clôture (ajout + suppression ligne à ligne, persistées en JSON).
4. **Données & sauvegarde** : chemin du dossier `LocalData`, bouton « Sauvegarder maintenant » (crée `Backups\backup_...`), bouton « Ouvrir le dossier », rappel de la sauvegarde automatique quotidienne.

### 8.9 Ce qui n'existe PAS encore (à ne pas chercher)
- ❌ Historique/tracé des sessions (au-delà des lignes de log) et rôles avec permissions fines (Admin et Agent ont aujourd'hui les mêmes droits).
- ❌ Export PDF des factures ou bilans (impression directe possible depuis la Facturation).
- ❌ Ventes d'articles facturées avec décrément automatique du stock depuis l'écran Facturation (le décrément se fait via le Dashboard).
- ❌ Rendez-vous des services (`DateRendezVous` du modèle `Service` non exploité).
- ❌ Multi-devises, multi-boutiques, synchro réseau, sauvegarde cloud (sauvegarde locale uniquement).
- ❌ Tests unitaires, CI/CD, journalisation structurée.

---

## 9. Guide de design (à respecter pour toute nouvelle UI)

| Ressource / couleur | Valeur | Usage |
|---|---|---|
| `PrimaryBlue` | `#38BDF8` | Accent principal : titres, focus, survol, P.Vente |
| `DeepBg` | `#0B1120` | Fond principal des pages |
| `CardBg` | `#151C2C` | Fond des cartes / formulaires |
| `AccentViolet` | `#7000FF` | Boutons par défaut |
| Surface secondaire | `#0F172A` | Sidebar, zones internes |
| Bordures / champs | `#1E293B` / `#334155` | Bordures de cartes, fond TextBox |
| Texte normal / atténué | `#E2E8F0` / `#94A3B8` | Contenu / libellés secondaires |
| Succès | `#4ADE80` (ou `#059669` bouton) | Montants payés, caisse parfaite |
| Alerte | `#F59E0B` ambre / `#EF4444` rouge / `#F87171` | Dettes, bouton supprimer, manquant |
| Badges catégories | SERVICE `#6366F1`, FORMATION `#A855F7`, VENTE `#F59E0B` | UC_Bilans |
| Monnaie | suffixe « F », format `{0:N0}` | Tous les montants |
| Police | Segoe UI (style global) | — |
| Rayons | 8px champs, 10px boutons, 15px cartes, 20px fenêtre | — |
| Icônes | emojis dans les textes (📦 🧾 📊 ⚙️ ✎ 🗑 …) | Pas de librairie d'icônes |

---

## 10. Conventions de code & recettes pour développeurs / IA

### 10.1 Conventions observées
- **Langue** : tout (identifiants, commentaires, UI) en **français**.
- **Modèles** : POCOs à propriétés publiques auto-implémentées, namespace `DialloBusinessCenter.Models`, propriétés calculées en expression-bodied (`=>`).
- **Données** : passer **exclusivement** par `Utils` (classe statique) ; ne jamais lire/écrire les JSON directement depuis un UserControl.
- **UI** : code-behind, contrôles identifiés par `x:Name` avec préfixes `Inp` (input), `Txt` (texte), `Btn`, `Combo`, `Dg`/`Grid` (DataGrid), `Panel`, `UC_` (pages) ; affichage conditionnel par `Visibility` ou largeur de colonne (`GridLength(0)` ↔ `GridLength(n)`).
- **Persistance d'une entité** : toujours le trio Get/Save/Update/Delete + Id auto (`Max+1`) + `IdUtilisateur = CurrentUser?.Id ?? 0` + horodatage `DateTime.Now` dans le Save.
- **Traçabilité** : appeler `Utils.LogAction("...")` après toute opération significative (préfixes métier : `[SERVICE]`, `[FORMATION]`, `[VENTE]`, `[FLUX]`).
- **Montants** : `decimal` ; parsing tolérant via `decimal.TryParse(...) ? val : 0` dans Dashboard, `decimal.Parse` brut ailleurs.

### 10.2 Recette : ajouter une nouvelle page
1. Créer `UC_MaPage.xaml` (UserControl) + `UC_MaPage.xaml.cs` dans le dossier projet.
2. L'ajouter au `.csproj` : `<Page Include="UC_MaPage.xaml">` (Generator MSBuild:Compile, SubType Designer) **et** `<Compile Include="UC_MaPage.xaml.cs"><DependentUpon>UC_MaPage.xaml</DependentUpon></Compile>`.
3. Ajouter le bouton dans `MainWindow.xaml` (StackPanel de la sidebar) avec `Content="emoji   Titre"` **et `Tag="Titre de la page"`** — le titre affiché est lu depuis `Tag` (ne plus utiliser de découpage du Content).
4. Ajouter le `case "BtnMaPage": MainContainer.Content = new UC_MaPage(); break;` dans `NavClick`.
5. Si nouvelle donnée : créer la classe dans `Model.cs` + les méthodes CRUD dans `Utils.cs` (un JSON par entité).

### 10.3 Recette : ajouter une entité persistée
1. POCO dans `Model.cs` (namespace `DialloBusinessCenter.Models`).
2. Dans `Utils.cs` : `Get<Entité>() => LoadLocal<...>("entites.json")` + `Save`/`Update`/`Delete` sur le même moule que `Article` (Id auto = `Max+1`, `IdUtilisateur`, horodatage).
3. Brancher l'UI + un `Utils.LogAction(...)` si l'action doit apparaître dans les activités du Dashboard.

### 10.4 Build & exécution
- Ouvrir `Diallo Business.sln` (racine du repo) dans **Visual Studio** (≥ 2017, workload « Développement .NET Desktop ») ; `F5`. Connexion par défaut : `admin` / `admin123`.
- Ou CLI (vérifié fonctionnel) : `"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" "Diallo Business\Diallo Business.csproj" /t:Build /p:Configuration=Debug` (projet de style ancien : préférer MSBuild à `dotnet build`).
- Sortie : `Diallo Business\bin\Debug\Diallo Business.exe` ; les données vivent dans `bin\Debug\LocalData\`.
- ⚠️ Supprimer `bin\` efface les données (elles ne sont pas versionnées) — utiliser la sauvegarde de UC_Parametres (ou `LocalData\Backups`).

---

## 11. Problèmes connus, dette technique & axes d'évolution

### 11.1 Bugs corrigés le 21/09/2026 (ne pas les réintroduire)
1. ✅ **`NavClick` fragile** (`Substring(3)`) → le titre de page vient désormais de **`btn.Tag`**.
2. ✅ **Bouton « Facturation » ouvrant la caisse** → vrai écran `UC_Facturation` (multi-lignes + impression).
3. ✅ **Page de démarrage = UC_Stock** → le **Dashboard** est chargé au démarrage.
4. ✅ **`ApplyFilters_Changed` surchargé 3 fois** (UC_Bilans) → 3 handlers nommés distinctement (`_Click`, `_SelectionChanged`, `_TextChanged`), XAML mis à jour.
5. ✅ **Type d'article non restauré** à l'édition (UC_Stock) → le `ComboBoxItem` correspondant est resélectionné.
6. ✅ **Dashboard sans validations** → contrôles métier (type/prix > 0, acompte ∈ [0,total], quantité > 0) et `TryParse` partout.
7. ✅ **`Formation.Statut` jamais mis à jour** → passe à « Achevé » lors du soldage dans les Bilans.
8. ✅ **Clôture incomplète** → `SoldeTheorique` et `Details` (montant par moyen de paiement) sont persistés.
9. ✅ **Flux divers invisibles** → grille « Flux divers » dans UC_Transferts (consultation + suppression).
10. ✅ **Parsing non défensif** (`decimal.Parse`/`int.Parse`) → `TryParse` + messages explicites (Stock, Dashboard, Facturation, Transferts).
11. ✅ **Fichiers morts** `TextFile1.txt` + `UC_stock.resx` → supprimés du disque et du `.csproj`.
12. ✅ **Aucune gestion d'exception globale** → handlers dans `App.xaml.cs` (log fichier + MessageBox, plus de crash silencieux).
13. ✅ **Aucun écran de connexion** (`CurrentUser` toujours null) → `Win_Login` obligatoire, mots de passe hachés **SHA-256**, déconnexion possible.
14. ✅ **Aucun écran Paramètres** → `UC_Parametres` (utilisateurs, listes de référence, sauvegarde).
15. ✅ **`ElementFacture` jamais utilisé** → factures multi-lignes, `Id`/`IdFacture` attribués par `Utils.SaveFacture`.
16. ✅ **Aucune sauvegarde des données** → `BackupData()` + sauvegarde automatique quotidienne au démarrage.

### 11.2 Points de vigilance restants (non bloquants)
1. **Rôles sans permissions fines** : Admin et Agent ont aujourd'hui les mêmes droits.
2. **Écriture JSON non atomique / non thread-safe** : acceptable pour un poste unique, risqué en cas d'accès concurrent.
3. **« Ventes du Jour » (Dashboard)** ne compte que les factures — pas les acomptes services/formations.
4. **Deux systèmes proches** de mouvements de caisse : notes de caisse (UC_Transferts) et flux divers (Dashboard) — cohérents mais à documenter auprès des utilisateurs.
5. **Pas de « mot de passe oublié »** : la réinitialisation se fait depuis un autre compte Admin (UC_Parametres).
6. **Restauration de sauvegarde manuelle** : il faut copier les fichiers depuis `LocalData\Backups\...` (aucun bouton de restauration).
7. **Impression = `PrintDialog`** uniquement (aucun export PDF).
8. Commentaire résiduel dans `UC_Bilans.xaml.cs` mentionnant l'ancien nom `ApplyFilters_Changed` (inoffensif).

### 11.3 Pistes d'évolution naturelles
- **Permissions par rôle** (Admin : tout ; Agent : pas de suppression / pas de Paramètres).
- **Bouton de restauration** d'une sauvegarde depuis UC_Parametres (copie inverse depuis `LocalData\Backups`).
- **Décrément de stock depuis la Facturation** (lier une ligne à un `Article` et contrôler la quantité).
- **Rapports enrichis** : CA par moyen de paiement, journal des clôtures, statistiques mensuelles.
- Migration **SQLite** (ou .NET 8 / MVVM) — le découpage « Utils = repository » facilite la substitution.
- Validation de formulaires par `ValidationRule` / `IDataErrorInfo` au lieu des MessageBox.

---

## 12. TL;DR pour une IA (résumé opérationnel)

- **Stack** : WPF .NET Framework 4.7.2, C#, Newtonsoft.Json, pas de DB, pas de MVVM, textes en français, montants en F CFA.
- **Démarrage** : `App.xaml.cs OnStartup` → `Win_Login` (obligatoire, admin/admin123 au 1er lancement) → sauvegarde quotidienne → `MainWindow`.
- **Où est quoi** : UI dans les 6 `UC_*.xaml(.cs)` + `Win_Login`, navigation dans `MainWindow.xaml.cs` → `NavClick` (titre via `Tag`), modèles dans `Model.cs`, **toute la donnée dans `Utils.cs`** (JSON dans `bin\Debug\LocalData\`), styles dans `App.xaml`.
- **Écrans** : Dashboard (saisie rapide), Stock (CRUD articles), Facturation (factures multi-lignes + impression), Transferts (caisse, clôture, flux divers), Bilans (registre consolidé + soldage), Paramètres (utilisateurs, listes, sauvegarde).
- **Règle d'or** : lire/écrire les données uniquement via `Utils` ; toute action notable → `Utils.LogAction` ; respecter la palette §9 et la recette §10.2 pour une nouvelle page.
- **⚠️ Obligation** : après toute modification pertinente, **mettre à jour `map.md`** (voir `.clinerules` à la racine du repo).
- **Attention** : les données sont dans `bin/` (non versionnées) ; Admin/Agent n'ont pas de permissions différenciées ; chaque nouveau fichier doit être déclaré dans le `.csproj` (Page + Compile).

*Fin du document.*