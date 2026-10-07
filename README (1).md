# TrocWear — Vente de seconde main entre particuliers

## Description du projet

TrocWear est une application permettant aux particuliers de publier et de consulter des annonces d’articles de seconde main, de négocier leur prix et de suivre leur livraison.

Chaque utilisateur possède **un seul compte et une seule session**, lui permettant d’être à la fois **acheteur et vendeur**. Il peut mettre ses articles en vente et acheter ceux d’autres utilisateurs sans changer de compte.

Le projet comprend les services d’authentification, d’annonces, de négociation, de livraison et de recommandations. Il utilise différents modèles NoSQL selon les besoins de chaque service.
Le suivi de livraison est simulé.

### 1. Agrégat Utilisateur

Il représente le compte d’une personne inscrite.

- **Données principales :** identifiant, pseudonyme, email et mot de passe haché.
- **Règles :** un utilisateur possède un compte lui permettant d’acheter et de vendre. Il peut modifier uniquement son propre profil.
- **Stockage envisagé :** document pour les informations persistantes du compte.

Les sessions sont conservées séparément dans un stockage **clé-valeur** : chaque identifiant de session référence l’utilisateur connecté et possède une expiration.

### 2. Agrégat Annonce

Il représente un article proposé à la vente.

- **Données principales :** identifiant, vendeur, titre, description, prix, catégorie, caractéristiques, références des photos et statut de disponibilité.
- **Règles :** seul le propriétaire peut modifier l’annonce. Un article ne peut être réservé que par un seul acheteur à la fois.
- **Stockage envisagé :** document, pour regrouper les informations de l’article et permettre des caractéristiques différentes selon sa catégorie.

### 3. Agrégat Négociation

Il représente une discussion de prix entre un acheteur et un vendeur pour une annonce précise.

- **Données principales :** identifiant, annonce concernée, participants, proposition active, prix accepté et statut.
- **Règles :** seuls les participants peuvent intervenir. Une proposition doit encore être active pour être acceptée. Le prix accepté et le statut doivent être enregistrés de manière cohérente.
- **Stockage envisagé :** wide-column pour l’historique des propositions, regroupées par négociation et classées par date. L’état courant est géré explicitement avec les garanties nécessaires.

Plusieurs acheteurs peuvent négocier pour la même annonce dans des négociations distinctes. **Un accord sur le prix ne garantit pas la réservation de l’article.**

### 4. Agrégat Livraison

Il représente l’expédition d’un article après confirmation de l’accord et réservation réussie.

- **Données principales :** identifiant, référence de l’accord, article, vendeur, acheteur, adresse de destination, numéro de suivi et statut.
- **Règles :** une livraison doit correspondre à un accord confirmé. Les changements de statut suivent un ordre contrôlé : à préparer, expédiée, en transit, livrée.
- **Stockage envisagé :** document, pour consulter et gérer ensemble les informations de l’expédition.

Le numéro de suivi et l’acheminement sont simulés pour le projet.

### 5. Favoris, abonnements et recommandations

Les favoris et les abonnements représentent les relations entre utilisateurs et annonces.

- **Données principales :** utilisateur, annonce favorite ou vendeur suivi.
- **Règles :** un utilisateur gère ses propres favoris et abonnements. Une même relation ne doit pas être créée plusieurs fois.
- **Stockage envisagé :** graphe, pour parcourir ces relations et suggérer des articles.

**Le graphe entier n’est pas un agrégat unique.** Les relations sont gérées individuellement ; les recommandations constituent un résultat de lecture calculé à partir de ces relations.
