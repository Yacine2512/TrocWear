# TrocWear

Plateforme de seconde main entre particuliers : annonces, négociation de prix, favoris et livraison simulée, avec un seul compte pour acheter et vendre.

> Projet pédagogique en phase de conception. Les exemples décrivent les contrats prévus ; l’application n’est pas encore implémentée.

## Équipe et dépôt — étape 1

- Équipe : deux à quatre personnes, noms à compléter.
- Dépôt Git : TrocWear.
- Action à vérifier par l’équipe : inviter le compte GitHub `charroux`.
- Rapport détaillé : [rapport_trocwear.tex](rapport_trocwear.tex).

## Contexte et périmètre

Un utilisateur publie des articles, consulte ceux des autres et négocie leur prix. Il peut acheter et vendre avec le même compte et la même session. Après un accord sur le prix, il confirme son achat ; la réservation doit réussir avant la création d’une livraison.

Le projet ne comporte **ni paiement, ni retours, ni abonnements aux vendeurs**. Les favoris servent aux recommandations. Le transporteur, le suivi et les points relais sont simulés. L’acheteur peut modifier sa destination avant expédition, ou choisir un relais après une tentative échouée.

## Services et stockage

| Service | Responsabilité | Modèle et moteur proposés |
|---|---|---|
| Authentification | Comptes et sessions | SQL : PostgreSQL ; clé-valeur : Redis |
| Annonces | Publication, recherche et réservation | Document : MongoDB |
| Négociation | Offres, contre-offres, accord | Wide-column : Cassandra |
| Livraison | Destination, tentatives et suivi | Document : MongoDB |
| Recommandation | Favoris et suggestions | Graphe : Neo4j |

Ces choix illustrent différents modèles ; SQL pourrait aussi répondre à de nombreux besoins. Les versions, le langage serveur, le frontend et la messagerie restent à fixer.

## Commandes — étape 2

Une commande demande un traitement et peut être refusée. L’acteur est déterminé par la session vérifiée côté serveur.

| Service | Commandes |
|---|---|
| Authentification | InscrireUtilisateur, ConnecterUtilisateur, DeconnecterUtilisateur, ModifierProfil |
| Annonces | PublierAnnonce, ModifierAnnonce, ReserverAnnonce, LibererReservation |
| Négociation | OuvrirNegociation, SoumettreProposition, AccepterProposition, RefuserProposition, ConfirmerAchat |
| Livraison | CreerLivraisonDepuisAccord, ModifierDestination, DeclarerExpedition, SimulerProgression, SignalerEchecLivraison, ChoisirPointRelais, ConfirmerReception |
| Recommandation | AjouterFavori, RetirerFavori |

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

Un agrégat rassemble les données et les règles qui doivent rester cohérentes lors d’une modification. Un service gère plusieurs instances ; un agrégat n’est pas toute une base.

| Agrégat | Règle principale |
|---|---|
| Utilisateur | Un compte permet d’acheter et de vendre ; email unique |
| Annonce | Un seul acheteur peut réserver l’article |
| Négociation | Seule une proposition active peut être acceptée |
| Livraison | Un dossier par accord confirmé ; transitions contrôlées |
| Favori | Un seul favori par couple utilisateur/annonce |

Les sessions expirent indépendamment des comptes. Le graphe entier et le résultat des recommandations ne sont pas des agrégats uniques.

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

Les sessions sont stockées en clé-valeur, par exemple `session:jeton` → `{"utilisateurId":"U12"}`, avec une expiration. Les mots de passe hachés restent dans Authentification.

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

L’historique est une table séparée : partition par négociation, tri par date et identifiant d’événement. L’état courant nécessite un contrôle de concurrence, par exemple une LWT Cassandra ; ajouter une ligne d’historique ne suffit pas à garantir l’accord.

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

### Favori — graphe

```json
{
  "favoriId": "U12:A20",
  "utilisateurId": "U12",
  "annonceId": "A20",
  "creeLe": "2026-10-07T11:00:00Z"
}
```

## Événements et réplicas — étape 5

Un événement décrit un fait enregistré. Un réplica est une copie locale qui permet au consommateur de lire les données sans appeler leur service source à chaque fois.

| Source | Consommateur | Événement de création | Événement de mise à jour | Copie locale |
|---|---|---|---|---|
| Authentification | Annonces, Négociation | UtilisateurCree | ProfilPublicModifie | Identifiant et pseudonyme |
| Annonces | Recommandation | AnnoncePubliee | AnnonceModifiee, AnnonceReservee | Description publique et disponibilité |
| Négociation | Livraison | PrixAccepte | AccordConfirme | Pré-dossier d’accord, puis accord confirmé |
| Livraison | Négociation | LivraisonCreee | LivraisonStatutModifie | Résumé de suivi |

PrixAccepte crée seulement une copie d’accord dans Livraison, **aucune expédition**. AccordConfirme ajoute la réservation et la destination validée ; il permet de créer le dossier de livraison. Cette séparation rend explicites la création et la mise à jour du réplica demandées par le cours.

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

Les événements possèdent un identifiant et une version source. Les consommateurs ignorent les doublons et les anciennes versions. Les contrats détaillés du rapport fournissent des instantanés complets du sous-ensemble répliqué. Une publication durable et une procédure de reprise devront être implémentées.

## Projections — étape 6

### Accord vers Livraison

Le service copie les références de l’accord et la destination validée, génère un numéro de suivi simulé et initialise son propre état. La destination effective devient une donnée de Livraison : une modification du profil ne doit pas l’écraser.

Requête de lecture :

```json
{"requete":"ConsulterLivraison","parametres":{"livraisonId":"L9"}}
```

Après un échec, l’acheteur choisit un relais valide :

```json
{
  "type": "ChoisirPointRelais",
  "cibleId": "L9",
  "versionAttendue": 5,
  "donnees": {"pointRelaisId": "PR-PARIS-01"}
}
```

Livraison passe de `en_attente_nouvelle_destination` à `reacheminement_planifie`. Le service conserve la tentative échouée et remplace la destination effective par le relais. Il ne modifie ni l’adresse du compte ni l’accord initial.

### Annonce vers Recommandation

Le service transforme les annonces en nœuds Article, normalise les catégories et calcule l’éligibilité à partir du statut. Les favoris deviennent des relations AIME. Les articles réservés, déjà favoris ou appartenant à l’utilisateur sont exclus des suggestions.

```json
{
  "articleId": "A20",
  "versionAnnonce": 2,
  "vendeurId": "U30",
  "categorieNormalisee": "veste",
  "tailleNormalisee": "M",
  "prixCentimes": 3800,
  "eligible": true
}
```

Requête :

```json
{"requete":"RecommanderArticles","parametres":{"limite":10}}
```

L’utilisateur est déduit de la session. Exemple de règle : recommander les articles aimés par d’autres utilisateurs ayant des favoris en commun avec lui.

## Exemples d’accès aux stockages

| Modèle | Accès principal | Exemple |
|---|---|---|
| SQL | Retrouver le compte par email unique | SELECT paramétré |
| Clé-valeur | Retrouver la session par jeton | Redis GET, SET avec EX |
| Document | Consulter une annonce ; réserver sous condition | MongoDB findOneAndUpdate |
| Wide-column | Lire les 20 derniers échanges de N45 | Partition N45 et tri temporel |
| Graphe | Parcourir les favoris communs | Cypher MATCH |

Le rapport contient le SQL, les commandes Redis, les requêtes MongoDB, le CQL Cassandra et une requête Cypher complète.

## Parcours principal

1. Le vendeur publie l’article.
2. L’acheteur et le vendeur négocient puis acceptent un prix ; l’article reste disponible.
3. L’acheteur confirme avec une destination validée.
4. Annonces réserve l’article de manière atomique.
5. Négociation confirme l’accord ; Livraison crée un dossier unique.
6. Le vendeur déclare l’expédition et le suivi évolue par simulation.
7. En cas d’échec, l’acheteur peut choisir un point relais.

Une réservation refusée empêche la confirmation. Une réservation réussie suivie d’une confirmation impossible nécessite une compensation coordonnée. Une panne temporaire de Livraison se traite par reprise, sans créer de doublon.

## État du projet et vérifications prévues

Les API, la messagerie, les bases et l’interface restent à développer. Les vérifications porteront notamment sur les réservations concurrentes, les événements dupliqués ou désordonnés, l’isolation des destinations et les autorisations. Il n’existe pas encore de commande de lancement de l’application.

Pour compiler le rapport, placer `rapport_trocwear.tex` dans un projet Overleaf et choisir **XeLaTeX**, ou exécuter deux fois `xelatex rapport_trocwear.tex` dans un environnement LaTeX équipé. Le fichier est autonome et ne nécessite pas d’images externes.

## Documentation officielle

- [Redis SET](https://redis.io/docs/latest/commands/set/)
- [MongoDB : atomicité](https://www.mongodb.com/docs/manual/core/write-operations-atomicity/)
- [Cassandra : modélisation](https://cassandra.apache.org/doc/stable/cassandra/developing/data-modeling/intro.html)
- [Neo4j : MATCH](https://neo4j.com/docs/cypher-manual/current/clauses/match/)
