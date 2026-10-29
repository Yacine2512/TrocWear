# TrocWear

Plateforme de seconde main entre particuliers : annonces, négociation de prix et livraison simulée, avec un seul compte pour acheter et vendre.

> Projet pédagogique. Une **première version exécutable** est implémentée : authentification complète, et gestion des annonces, négociations et livraisons sous forme de CRUD. Les sections « Conception » décrivent la cible définie dans le rapport ; l'écart entre cette cible et le code est résumé dans [État du projet](#état-du-projet).

## Contexte et périmètre

Un utilisateur publie des articles, consulte ceux des autres et négocie leur prix. Il peut acheter et vendre avec le même compte et la même session. Après un accord sur le prix, il confirme son achat ; la réservation doit réussir avant la création d'une livraison.

Le transporteur, le suivi et les points relais sont simulés. L'acheteur peut modifier sa destination avant expédition, ou choisir un relais après une tentative échouée.

## Démarrage rapide (Docker)

**Prérequis :** Docker Desktop. Aucun SDK .NET ni Node n'est nécessaire sur la machine.

Toutes les commandes se lancent depuis le dossier qui contient `docker-compose.yml`.

```powershell
Copy-Item .env.example .env     # puis remplacer les CHANGE_ME par vos mots de passe
docker compose up -d --build     # premier lancement : quelques minutes
docker compose ps                # les 5 services doivent être "running"
```

| Adresse | Contenu |
|---|---|
| http://localhost:4200 | Application Angular |
| http://localhost:5080/swagger | Documentation de l'API (Swagger) |
| http://localhost:5080/health/ready | Santé : PostgreSQL, Redis et MongoDB |

Commandes utiles :

```powershell
docker compose logs --tail=80 api   # logs de l'API (erreurs, exceptions)
docker compose down                 # arrêter (les données sont conservées)
docker compose down -v              # arrêter ET effacer les bases
```

Changer un mot de passe dans `.env` ne modifie pas une base déjà initialisée : en développement, recréer les volumes avec `docker compose down -v`.

**Lancer l'API hors Docker** (développement) : démarrer uniquement les bases avec `docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d postgres redis mongo`, renseigner les mots de passe dans `backend/TrocWear.Api/appsettings.json`, puis `dotnet run --project backend/TrocWear.Api` (port 5080) et, dans `frontend/trocwear-web`, `npm ci` puis `npm start`.

## Architecture implémentée

| Élément | Technologie |
|---|---|
| API | ASP.NET Core (.NET 10), MediatR (CQRS), FluentValidation, Swagger |
| Frontend | Angular 21 (composants standalone, Angular Material) |
| Comptes et outbox | PostgreSQL 17 : EF Core pour les écritures, Dapper pour les lectures |
| Sessions et événements | Redis 7 |
| Annonces, négociations, livraisons | MongoDB 8 : trois bases distinctes |

Le backend comporte **quatre projets C#**, en architecture en couches :

```text
backend/
  TrocWear.Api/             Contrôleurs REST, authentification par session Redis, Program.cs
  TrocWear.Application/     Commandes, requêtes, handlers, interfaces, validation
  TrocWear.Domain/          Modèles, DTOs, événements, règles métier
  TrocWear.Infrastructure/  EF Core, Dapper, Redis, MongoDB, outbox
frontend/trocwear-web/      Application Angular
scripts/                    Tests de bout en bout et contrôles statiques
```

Dépendances : `Api → Application, Infrastructure, Domain` ; `Infrastructure → Application, Domain` ; `Application → Domain` ; `Domain → aucune`.

Les domaines Authentification, Annonces, Négociations et Livraisons sont des **modules d'une même API** (monolithe modulaire), et non des microservices déployés séparément.

### Sécurité

- Mots de passe hachés en **PBKDF2-HMAC-SHA256** (600 000 itérations, sel de 128 bits).
- Session dans un cookie `trocwear.sid` **HttpOnly**, SameSite=Lax, Secure hors développement. Redis ne stocke que le **SHA-256 du jeton** : clé `session:<hash>`, expiration 3600 s.
- Protection CSRF : les requêtes `POST`, `PATCH`, `PUT` et `DELETE` exigent l'en-tête `X-TrocWear-Request: 1` (sauf inscription et connexion) et une origine autorisée.
- Chaque utilisateur ne peut lire, modifier ou supprimer que ses propres éléments (réponse 403 sinon).

### Endpoints

Authentification :

| Méthode | Route | Rôle |
|---|---|---|
| POST | `/api/auth/register` | Inscription (`pseudonyme`, `email`, `password` ≥ 12 caractères, `adresseParDefaut` optionnelle) |
| POST | `/api/auth/login` | Connexion : pose le cookie de session |
| GET | `/api/auth/me` | Utilisateur connecté |
| PATCH | `/api/auth/profile` | Modification du pseudonyme avec contrôle de version (`versionAttendue`, 409 si obsolète) |
| POST | `/api/auth/logout` | Déconnexion : supprime la session Redis et le cookie |

Pour chacun des modules `annonces`, `negociations` et `livraisons` (session requise) :

| Méthode | Route | Rôle |
|---|---|---|
| POST | `/api/{module}` | Création |
| GET | `/api/{module}` | Éléments **de l'utilisateur connecté** |
| GET | `/api/{module}/{id}` | Détail |
| PATCH | `/api/{module}/{id}/statut` | Changement de statut |
| DELETE | `/api/{module}/{id}` | Suppression |

Exemple de création : `{ "titre": "Veste", "description": "Bon état", "prixCentimes": 2300 }`. Les champs `description`, `annonceId`, `acheteurId`, `vendeurId` et `adresse` sont optionnels.

### Correspondance interface / API

| Écran Angular | Appels API |
|---|---|
| Inscription | `POST /api/auth/register` |
| Connexion | `POST /api/auth/login`, puis `GET /api/auth/me` |
| Mon compte | `GET /api/auth/me`, `PATCH /api/auth/profile`, `POST /api/auth/logout` |
| Annonces, Nouvelle annonce | `GET` et `POST /api/annonces`, `PATCH …/statut`, `DELETE` |
| Négociations | `GET` et `POST /api/negociations`, `PATCH …/statut`, `DELETE` |
| Livraisons | `GET` et `POST /api/livraisons`, `PATCH …/statut`, `DELETE` |

## Services et stockage

| Service | Responsabilité | Conception | Implémenté |
|---|---|---|---|
| Authentification | Comptes et sessions | SQL : PostgreSQL ; clé-valeur : Redis |  PostgreSQL + Redis, comme prévu |
| Annonces | Publication, recherche et réservation | Document : MongoDB |  MongoDB `trocwear_annonces` : CRUD et statut uniquement |
| Négociation | Offres, contre-offres, accord | Wide-column : Cassandra |  MongoDB `trocwear_negociations` : CRUD simplifié, pas de Cassandra |
| Livraison | Destination, tentatives et suivi | Document : MongoDB |  MongoDB `trocwear_livraisons` : CRUD simplifié |

Ces choix illustrent différents modèles ; SQL pourrait aussi répondre à de nombreux besoins. Le service de recommandation (favoris, graphe Neo4j) envisagé au départ a été retiré du périmètre.

---

# Conception (rapport)

Les sections suivantes décrivent l'architecture cible. Les encadrés « Dans le code » signalent les différences avec l'implémentation actuelle.

## Commandes — étape 2

Une commande demande un traitement et peut être refusée. L'acteur est déterminé par la session vérifiée côté serveur.

| Service | Commandes |
|---|---|
| Authentification | InscrireUtilisateur, ConnecterUtilisateur, DeconnecterUtilisateur, ModifierProfil |
| Annonces | PublierAnnonce, ModifierAnnonce, ReserverAnnonce, LibererReservation |
| Négociation | OuvrirNegociation, SoumettreProposition, AccepterProposition, RefuserProposition, ConfirmerAchat |
| Livraison | CreerLivraisonDepuisAccord, ModifierDestination, DeclarerExpedition, SimulerProgression, SignalerEchecLivraison, ChoisirPointRelais, ConfirmerReception |

> **Dans le code :** les quatre commandes d'Authentification sont implémentées (`RegisterUserCommand`, `LoginUserCommand`, `LogoutUserCommand`, `UpdateProfileCommand`). Pour les autres services, seules des opérations génériques existent : créer, changer de statut, supprimer.

Exemple de contrat JSON ; tous les autres contrats sont détaillés dans le rapport :

```json
{
  "commandeId": "CMD-101",
  "type": "SoumettreProposition",
  "service": "negociation",
  "cibleId": "N45",
  "versionAttendue": 2,
  "donnees": {
    "propositionId": "P3",
    "montantCentimes": 3500,
    "devise": "EUR"
  }
}
```

## Agrégats — étape 4

Un agrégat rassemble les données et les règles qui doivent rester cohérentes lors d'une modification. Un service gère plusieurs instances ; un agrégat n'est pas toute une base.

| Agrégat | Règle principale |
|---|---|
| Utilisateur | Un compte permet d'acheter et de vendre ; email unique |
| Annonce | Un seul acheteur peut réserver l'article |
| Négociation | Seule une proposition active peut être acceptée |
| Livraison | Un dossier par accord confirmé ; transitions contrôlées |

Les sessions expirent indépendamment des comptes.

### Utilisateur — SQL

```json
{
  "utilisateurId": "U12",
  "version": 1,
  "pseudonyme": "Yacine",
  "email": "yacine@example.com",
  "motDePasseHash": "HASH_ARGON2ID_EXEMPLE",
  "adresseParDefaut": {
    "ligne1": "10 rue Exemple",
    "codePostal": "75013",
    "ville": "Paris",
    "pays": "FR"
  },
  "statut": "actif"
}
```

Les sessions sont stockées en clé-valeur avec une expiration. Les mots de passe hachés restent dans Authentification.

> **Dans le code :** table `users`, avec email unique insensible à la casse et `Version` comme jeton de concurrence. Le hachage est fait en **PBKDF2-SHA256** au lieu d'Argon2id, pour éviter une dépendance tierce. `adresseParDefaut` est une simple chaîne et non un objet structuré. La clé de session est `session:<SHA-256 du jeton>` → identifiant utilisateur, avec `SET … EX 3600` : le jeton lui-même n'est jamais stocké.

### Annonce — document

```json
{
  "annonceId": "A20",
  "version": 1,
  "vendeurId": "U30",
  "titre": "Veste noire",
  "description": "Veste en bon etat",
  "prixCentimes": 4000,
  "devise": "EUR",
  "categorie": "veste",
  "caracteristiques": {
    "taille": "M",
    "couleur": "noir",
    "etat": "bon"
  },
  "photos": [
    "photos/A20-1.jpg"
  ],
  "statut": "disponible",
  "reservation": null
}
```

> **Dans le code :** les trois modules utilisent pour l'instant un même document générique (`MetierDocument`) : `id`, `ownerId`, `titre`, `description`, `prixCentimes`, `statut` (par défaut `nouveau`), `annonceId`, `acheteurId`, `vendeurId`, `adresse`, `createdAt` et `updatedAt`. Catégorie, caractéristiques, photos, version et réservation ne sont pas encore modélisées.

### Négociation — wide-column

```json
{
  "negociationId": "N45",
  "version": 3,
  "annonceId": "A20",
  "acheteurId": "U12",
  "vendeurId": "U30",
  "statut": "prix_accepte",
  "propositionActive": null,
  "propositionAccepteeId": "P3",
  "prixAccepteCentimes": 3500,
  "devise": "EUR",
  "reservationId": null,
  "destinationValidee": null
}
```

L'historique est une table séparée : partition par négociation, tri par date et identifiant d'événement. L'état courant nécessite un contrôle de concurrence, par exemple une LWT Cassandra ; ajouter une ligne d'historique ne suffit pas à garantir l'accord.

> **Dans le code :** stockage dans MongoDB (`trocwear_negociations`) avec le document générique. Cassandra, l'historique et le contrôle de concurrence ne sont pas implémentés.

### Livraison — document

```json
{
  "livraisonId": "L9",
  "version": 1,
  "accordSnapshot": {
    "negociationId": "N45",
    "versionSource": 4,
    "annonceId": "A20",
    "reservationId": "R8",
    "acheteurId": "U12",
    "vendeurId": "U30",
    "prixAccepteCentimes": 3500,
    "devise": "EUR"
  },
  "destinationInitiale": {
    "type": "domicile",
    "adresse": {
      "ligne1": "10 rue Exemple",
      "codePostal": "75013",
      "ville": "Paris",
      "pays": "FR"
    }
  },
  "destinationEffective": {
    "type": "domicile",
    "adresse": {
      "ligne1": "10 rue Exemple",
      "codePostal": "75013",
      "ville": "Paris",
      "pays": "FR"
    }
  },
  "statut": "a_preparer",
  "numeroSuivi": "SIM-L9",
  "tentatives": []
}
```

> **Dans le code :** document générique dans `trocwear_livraisons`, avec une adresse texte. L'interface la pré-remplit avec l'adresse par défaut du profil.

## Événements et réplicas — étape 5

Un événement décrit un fait enregistré. Un réplica est une copie locale qui permet au consommateur de lire les données sans appeler leur service source à chaque fois.

| Source | Consommateur | Événement de création | Événement de mise à jour | Copie locale |
|---|---|---|---|---|
| Authentification | Annonces, Négociation | UtilisateurCree | ProfilPublicModifie | Identifiant et pseudonyme |
| Négociation | Livraison | PrixAccepte | AccordConfirme | Pré-dossier d'accord, puis accord confirmé |
| Livraison | Négociation | LivraisonCreee | LivraisonStatutModifie | Résumé de suivi |



> **Dans le code :** `UtilisateurCree` et `ProfilPublicModifie` sont produits par Authentification. Ils sont écrits dans une table **outbox** PostgreSQL, dans la même transaction que le compte, puis publiés dans la liste Redis `trocwear:events:published` par un service d'arrière-plan, avec nouvelles tentatives et délai croissant. **Aucun consommateur ni réplica n'est encore implémenté.** Les autres événements sont seulement déclarés comme contrats (`EventContracts.cs`).

Exemple de création :

```json
{
  "evenementId": "EV-U12-1",
  "type": "UtilisateurCree",
  "schemaVersion": 1,
  "date": "2026-10-07T12:00:00Z",
  "serviceEmetteur": "authentification",
  "servicesRecepteurs": [
    "annonces",
    "negociation"
  ],
  "agregatSource": {
    "type": "Utilisateur",
    "id": "U12",
    "version": 1
  },
  "donneesModifiees": [
    "utilisateurId",
    "pseudonyme"
  ],
  "nouvellesValeurs": {
    "utilisateurId": "U12",
    "pseudonyme": "Yacine"
  }
}
```

Exemple de mise à jour :

```json
{
  "evenementId": "EV-U12-2",
  "type": "ProfilPublicModifie",
  "schemaVersion": 1,
  "date": "2026-10-07T12:00:00Z",
  "serviceEmetteur": "authentification",
  "servicesRecepteurs": [
    "annonces",
    "negociation"
  ],
  "agregatSource": {
    "type": "Utilisateur",
    "id": "U12",
    "version": 2
  },
  "donneesModifiees": [
    "pseudonyme"
  ],
  "nouvellesValeurs": {
    "utilisateurId": "U12",
    "pseudonyme": "YacineB"
  }
}
```

Réplica public obtenu :

```json
{
  "utilisateurId": "U12",
  "source": "authentification",
  "versionSource": 2,
  "pseudonyme": "YacineB"
}
```

Les événements possèdent un identifiant et une version source. Les consommateurs ignorent les doublons et les anciennes versions. Les contrats détaillés du rapport fournissent des instantanés complets du sous-ensemble répliqué.

> **Dans le code :** la publication durable (outbox) est en place ; la consommation, la déduplication et la procédure de reprise côté consommateurs restent à implémenter. Dans les événements émis, le récepteur s'écrit `negociations` (au pluriel).

## Projections — étape 6

> **Dans le code :** les projections ci-dessous ne sont pas encore implémentées.

### Accord vers Livraison

Le service copie les références de l'accord et la destination validée, génère un numéro de suivi simulé et initialise son propre état. La destination effective devient une donnée de Livraison : une modification du profil ne doit pas l'écraser.

Requête de lecture :

```json
{"requete":"ConsulterLivraison","parametres":{"livraisonId":"L9"}}
```

Après un échec, l'acheteur choisit un relais valide :

```json
{
  "type": "ChoisirPointRelais",
  "cibleId": "L9",
  "versionAttendue": 5,
  "donnees": {"pointRelaisId": "PR-PARIS-01"}
}
```

Livraison passe de `en_attente_nouvelle_destination` à `reacheminement_planifie`. Le service conserve la tentative échouée et remplace la destination effective par le relais. Il ne modifie ni l'adresse du compte ni l'accord initial.


## Parcours principal

1. Le vendeur publie l'article.
2. L'acheteur et le vendeur négocient puis acceptent un prix ; l'article reste disponible.
3. L'acheteur confirme avec une destination validée.
4. Annonces réserve l'article de manière atomique.
5. Négociation confirme l'accord ; Livraison crée un dossier unique.
6. Le vendeur déclare l'expédition et le suivi évolue par simulation.
7. En cas d'échec, l'acheteur peut choisir un point relais.

Une réservation refusée empêche la confirmation. Une réservation réussie suivie d'une confirmation impossible nécessite une compensation coordonnée. Une panne temporaire de Livraison se traite par reprise, sans créer de doublon.


---

## État du projet

### Implémenté

- Authentification complète : inscription, connexion, session Redis, profil avec contrôle de version, déconnexion.
- Outbox PostgreSQL et publication Redis de `UtilisateurCree` et `ProfilPublicModifie`.
- CRUD MongoDB des annonces, négociations et livraisons, avec contrôle du propriétaire.
- Interface Angular couvrant l'ensemble de ces endpoints.
- Endpoints de santé (`/health/live`, `/health/ready`) et Swagger.

### Reste à faire

- **Recherche des annonces** : chaque utilisateur ne voit que ses propres annonces ; il n'existe pas encore d'endpoint pour consulter celles des autres.
- Réservation atomique d'une annonce, workflow de négociation (propositions, contre-offres, acceptation, confirmation d'achat).
- Workflow de livraison : création depuis un accord, expédition, simulation du suivi, point relais.
- Consommateurs d'événements et réplicas, projections de l'étape 6.
- Cassandra pour Négociation.

