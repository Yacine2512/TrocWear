# Plateforme de vente entre particuliers

## Contexte

Ce projet consiste à développer une application de vente d’articles de seconde main entre particuliers, inspirée de Vinted. Un utilisateur pourra consulter des annonces, publier ses propres articles, négocier un prix et suivre une livraison.

L’application reposera sur plusieurs services, avec un modèle de données adapté aux opérations de chacun. Le projet vise à mettre en pratique les modèles NoSQL clé-valeur, document, wide-column et graphe.

**Le projet ne comprend pas de paiement.** La livraison et le transporteur seront simulés pour permettre de tester le parcours complet. Ce README présente l’architecture prévue, et non des fonctionnalités déjà développées.

## Un seul compte pour acheter et vendre

Chaque utilisateur possède un seul compte et utilise une seule session de connexion. Les rôles acheteur et vendeur sont cumulables : ils correspondent à ce que l’utilisateur fait dans une transaction, pas à deux comptes séparés.

- Lorsqu’il consulte une annonce, propose un prix ou confirme une acquisition, il agit comme acheteur.
- Lorsqu’il publie un article, répond à une proposition ou prépare un colis, il agit comme vendeur.
- Il peut passer de ses achats à ses ventes sans se déconnecter ni créer une nouvelle session.

Par exemple, Yacine peut négocier l’achat d’une veste et, dans la même session, publier une paire de chaussures à vendre. L’interface peut proposer les rubriques « Mes achats » et « Mes ventes », accessibles depuis le même profil.

## Services et modèles de données

| Service | Modèle associé | Justification |
| --- | --- | --- |
| Authentification | Clé-valeur pour les sessions | Retrouver directement un utilisateur connecté à partir de sa clé de session. |
| Annonces | Document | Regrouper les informations d’un article et adapter ses caractéristiques à sa catégorie. |
| Négociation de prix | Wide-column | Regrouper les échanges d’une négociation dans une partition et les lire dans l’ordre chronologique. |
| Livraison | Document | Regrouper les informations d’une expédition dans un dossier consultable par identifiant. |
| Relations et recommandations | Graphe | Relier utilisateurs, annonces et vendeurs pour parcourir les favoris et abonnements. |

Les trois services centraux du parcours demandé sont l’authentification, la négociation et la livraison. Le service Annonces fournit les articles concernés. Le service Relations et recommandations complète l’utilisation des quatre modèles étudiés.

Un modèle n’est pas exclusif à un service : les annonces et les livraisons peuvent toutes deux utiliser des documents. Inversement, l’association clé-valeur de l’authentification concerne les sessions ; les comptes doivent aussi disposer d’un stockage persistant.

## 1. Service d’authentification — clé-valeur

### Responsabilités

- Créer un compte utilisateur.
- Vérifier les identifiants à la connexion.
- Créer, retrouver, faire expirer et supprimer les sessions.
- Donner accès aux fonctions d’achat et de vente dans la même session.

### Organisation des données

La clé représente un identifiant de session aléatoire et non prévisible. La valeur référence le compte connecté et ses rôles.

```text
Clé : session:identifiant_aleatoire
Valeur :
{
  "utilisateurId": "U42",
  "roles": ["acheteur", "vendeur"]
}
```

Une expiration automatique limite la durée de vie de la session. À la déconnexion, elle est supprimée. Le navigateur transmet l’identifiant de session via un cookie sécurisé HttpOnly.

Les comptes, emails et mots de passe hachés sont stockés de manière persistante, par exemple dans des documents utilisateurs. Aucun mot de passe en clair n’est enregistré dans une session.

**Requête principale :** « Quel utilisateur correspond à cette session ? »

### Autorisations

Posséder les deux rôles ne donne pas accès aux données de tous les utilisateurs. Le serveur vérifie aussi la propriété et la participation : un utilisateur ne modifie que ses annonces et ne consulte que les négociations et livraisons auxquelles il participe.

## 2. Service Annonces — document

### Responsabilités

- Publier et modifier ses articles.
- Consulter et rechercher des annonces.
- Gérer la disponibilité des articles et leur réservation.

### Organisation des données

```json
{
  "annonceId": "A100",
  "vendeurId": "U10",
  "titre": "Veste noire",
  "prixCentimes": 4000,
  "devise": "EUR",
  "categorie": "vetement",
  "caracteristiques": {
    "taille": "M",
    "etat": "tres bon"
  },
  "statut": "disponible"
}
```

Un document regroupe les données de l’annonce. Les caractéristiques varient selon l’article : taille pour un vêtement, pointure pour des chaussures ou dimensions pour un sac. Les montants sont enregistrés en centimes.

**Requêtes principales :** consulter une annonce par identifiant, afficher les annonces d’un vendeur et rechercher des articles selon des critères définis. Les index seront choisis en fonction de ces recherches.

## 3. Service de négociation de prix — wide-column

### Responsabilités

- Permettre à un acheteur de proposer un prix.
- Permettre au vendeur d’accepter, de refuser ou de faire une contre-proposition.
- Permettre à l’acheteur de répondre à une contre-proposition.
- Conserver l’historique des échanges et l’accord final.

### Exemple

Une veste est affichée à 40 €. L’acheteur propose 30 €, le vendeur demande 35 € et l’acheteur accepte.

| Négociation | Heure | Auteur | Action | Prix |
| --- | --- | --- | --- | --- |
| NEG-01 | 10:00 | Acheteur U42 | Proposition | 30 € |
| NEG-01 | 10:05 | Vendeur U10 | Contre-proposition | 35 € |
| NEG-01 | 10:08 | Acheteur U42 | Acceptation | 35 € |

### Organisation des données

- **Clé de partition :** `negociation_id`.
- **Colonnes de clustering :** date de l’événement, puis identifiant unique de l’événement.
- **Ordre prévu :** du plus récent au plus ancien.
- **Données de chaque échange :** auteur, action, proposition concernée et montant.

Tous les échanges d’une négociation sont regroupés dans la même partition. Plusieurs acheteurs peuvent négocier sur une annonce, mais chacun dispose d’une négociation distincte.

**Requête principale :** « Afficher les échanges de NEG-01 dans l’ordre chronologique. »

Pour afficher « Mes négociations », une table de lecture supplémentaire organisée par utilisateur sera prévue. Cette duplication permet de répondre à un autre mode d’accès.

### État courant et réservation

Le service conserve également les participants, la proposition active, le statut et le prix accepté dans un enregistrement d’état courant. Cet état peut rester dans une table du même stockage wide-column ; sa mise à jour doit empêcher les acceptations contradictoires. L’historique seul ne garantit pas cette cohérence.

Accepter un prix n’équivaut pas à réserver l’article. Après confirmation de l’acheteur, le service Négociation demande une réservation au service Annonces. Ce dernier effectue une vérification et une modification atomiques de la disponibilité : un seul acheteur peut obtenir l’article.

Une fois la réservation réussie, l’accord conserve l’identité de l’acheteur, du vendeur et le prix convenu. Le parcours ne nécessite aucun paiement.

## 4. Service de livraison — document

### Responsabilités

- Créer une livraison à partir d’un accord confirmé et d’un article réservé.
- Enregistrer l’expéditeur et le destinataire.
- Conserver l’adresse de destination et le numéro de suivi simulé.
- Faire évoluer le statut jusqu’à la réception.
- Permettre aux deux participants de consulter le suivi.

### Organisation des données

```json
{
  "livraisonId": "LIV-01",
  "negociationId": "NEG-01",
  "reservationId": "RES-01",
  "annonceId": "A100",
  "vendeurId": "U10",
  "acheteurId": "U42",
  "statut": "a_preparer",
  "numeroSuivi": "SIM-001",
  "destination": {
    "ville": "Paris",
    "codePostal": "75013"
  }
}
```

Cet exemple simplifie l’adresse : le dossier réel contiendra les informations nécessaires à la livraison. Les données personnelles ne sont accessibles qu’aux acteurs autorisés.

Le modèle document convient parce que les informations de l’expédition sont généralement consultées ensemble. Des index permettront de retrouver les livraisons d’un acheteur ou d’un vendeur.

| Statut | Signification |
| --- | --- |
| `a_preparer` | Le vendeur doit préparer le colis. |
| `expediee` | Le vendeur a déclaré l’envoi. |
| `en_transit` | Le colis est en cours d’acheminement simulé. |
| `livree` | La réception est confirmée. |

**Requêtes principales :** retrouver une livraison par identifiant et afficher ses achats ou ses ventes en cours de livraison.

Les transitions autorisées seront contrôlées par le serveur. Le suivi sera simulé ; aucune intégration à un transporteur réel n’est requise.

## 5. Service Relations et recommandations — graphe

### Responsabilités

- Ajouter et retirer des annonces des favoris.
- Suivre des vendeurs.
- Suggérer des articles à partir des relations entre utilisateurs et annonces.

### Organisation des données

Les utilisateurs et les annonces sont des nœuds. Les favoris, les abonnements et les publications sont des relations.

| Départ | Relation | Arrivée |
| --- | --- | --- |
| Utilisateur U42 | AIME | Annonce A100 |
| Utilisateur U55 | AIME | Annonce A100 |
| Utilisateur U55 | AIME | Annonce A205 |
| Utilisateur U42 | SUIT | Utilisateur U10 |
| Utilisateur U10 | A_PUBLIE | Annonce A300 |

Le même nœud utilisateur peut aimer une annonce et en publier une autre : cela reflète le compte unique acheteur-vendeur.

**Requête principale :** « Quels autres articles sont aimés par les utilisateurs qui partagent mes favoris ? »

Dans cet exemple, A205 peut être proposée à U42. Les articles indisponibles doivent être écartés. Le service peut aussi retrouver les annonces des vendeurs suivis.

## Parcours complet

1. Un utilisateur se connecte : une seule session lui donne accès à l’achat et à la vente.
2. Il publie ses articles ou consulte ceux d’autres vendeurs.
3. Il ouvre une négociation sur une annonce.
4. Les participants échangent des propositions ; l’historique est enregistré dans le stockage wide-column.
5. Ils acceptent un prix, puis l’acheteur confirme.
6. Le service Annonces réserve l’article si celui-ci est encore disponible.
7. Le service Négociation confirme l’accord et émet un événement permettant de créer la livraison.
8. Le vendeur prépare et expédie le colis ; l’acheteur suit puis confirme sa réception.
9. Le même utilisateur peut poursuivre ses propres ventes dans la même session.

## Communication et règles de cohérence

Les opérations nécessitant une réponse immédiate passent par des API : connexion, consultation, proposition de prix et réservation. Les événements propagent les changements validés, par exemple `AccordConfirme`, `LivraisonEffectuee` et `AnnonceIndisponible`.

- Chaque service reste responsable de ses données.
- Une réservation réussie est nécessaire avant de déclencher la livraison.
- Chaque événement possède un identifiant permettant de reconnaître un traitement déjà effectué.
- La création d’une livraison doit être idempotente : recevoir deux fois le même événement ne crée pas deux colis.
- En cas d’échec entre réservation et confirmation de l’accord, une reprise ou une libération de la réservation doit être prévue.
- Les suggestions peuvent accepter un léger retard ; la réservation exige une décision cohérente sur la disponibilité.

## Périmètre et prochaines étapes

Le périmètre principal comprend le compte unique, les annonces, les négociations, les livraisons simulées et les relations utiles aux recommandations. Le service de retour discuté initialement reste une extension possible, à ajouter après ce parcours principal. Aucun remboursement n’est prévu.

Les moteurs de bases de données, les langages backend et les commandes de lancement seront définis lors de l’implémentation. L’association des modèles décrit ici les besoins de stockage, sans imposer une technologie précise.
