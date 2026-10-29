# TrocWear — version quatre projets C#

Cette version contient **exactement quatre projets C#** sous `backend/`: `TrocWear.Api`, `TrocWear.Application`, `TrocWear.Domain` et `TrocWear.Infrastructure`. Les domaines Authentification, Annonces, Négociations et Livraisons sont des fonctionnalités dans ces projets. **Il n'existe plus de microservices autonomes** : une seule API les héberge.

## Bases de données et mots de passe

1. Copier `.env.example` vers `.env`.
2. Modifier **POSTGRES_PASSWORD** et **MONGO_PASSWORD** dans `.env`, et les identifiants `POSTGRES_USER`, `MONGO_USER` si nécessaire.
3. Les chaînes d'accès à PostgreSQL, Redis et MongoDB sont injectées par `docker-compose.yml`, section `api.environment`.
4. Les valeurs locales de l'API sont dans `backend/TrocWear.Api/appsettings.json` et `appsettings.Development.json` : ne jamais y commiter des secrets.
5. Changer le mot de passe dans `.env` **ne modifie pas** le mot de passe d'un volume PostgreSQL/MongoDB déjà initialisé. Faire une rotation en base, ou recréer les volumes uniquement si les données peuvent être perdues.

## Docker (aucun SDK .NET requis sur Windows)

```powershell
Copy-Item .env.example .env
notepad .env
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 api
```

Frontend : http://localhost:4200 ; Swagger : http://localhost:5080/swagger ; santé (PostgreSQL + Redis + MongoDB) : http://localhost:5080/health/ready.

### API lancée hors Docker (`dotnet run`)

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d postgres redis mongo
# Renseigner les mots de passe (CHANGE_ME) dans backend/TrocWear.Api/appsettings.json, ou via variables d'environnement :
#   $env:ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=trocwear_auth;Username=trocwear;Password=..."
dotnet run --project backend/TrocWear.Api      # écoute sur http://localhost:5080 (launchSettings.json)
cd frontend/trocwear-web; npm ci; npm start   # proxy /api -> http://localhost:5080
```

## Couches

- **API** : contrôleurs REST et MediatR (`ISender`).
- **Application** : Commands, Queries, handlers, interfaces de repositories/services et validation.
- **Domain** : modèles, DTOs, événements et règles métier.
- **Infrastructure** : EF Core PostgreSQL, Dapper, Redis, MongoDB, mapping et repositories.

Les références vont de `Api -> Application, Infrastructure, Domain`, `Infrastructure -> Application, Domain`, `Application -> Domain`, `Domain -> aucune`.

## Endpoints disponibles

Authentification : `POST /api/auth/register` (`pseudonyme`, `email`, `password`, `adresseParDefaut` optionnelle), `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/auth/logout`, `PATCH /api/auth/profile`. Les mutations avec cookie requièrent l'en-tête `X-TrocWear-Request: 1`.

Avec session active, chacun des contrôleurs `annonces`, `negociations`, `livraisons` expose :

- `POST /api/{nom}` création MongoDB
- `GET /api/{nom}` liste des éléments de l'utilisateur connecté
- `GET /api/{nom}/{id}` détail
- `PATCH /api/{nom}/{id}/statut` modification de statut
- `DELETE /api/{nom}/{id}` suppression

Exemple création : `{ "titre": "Veste", "description": "Bon état", "prixCentimes": 2300 }` (`description`, `annonceId`, `acheteurId`, `vendeurId`, `adresse` optionnels).

## Correspondance front / back

| Écran Angular | Appels API |
|---|---|
| Inscription | `POST /api/auth/register` |
| Connexion | `POST /api/auth/login` puis `GET /api/auth/me` |
| Mon compte | `GET /api/auth/me`, `PATCH /api/auth/profile`, `POST /api/auth/logout` |
| Annonces / Nouvelle annonce | `GET`, `POST /api/annonces`, `PATCH …/{id}/statut`, `DELETE …/{id}` |
| Négociations | `GET`, `POST /api/negociations`, `PATCH …/{id}/statut`, `DELETE …/{id}` |
| Livraisons | `GET`, `POST /api/livraisons`, `PATCH …/{id}/statut`, `DELETE …/{id}` |

Les modèles TypeScript (`shared/models/user.model.ts`, `shared/models/metier.model.ts`) reprennent les DTOs C# (`UserResponse`, `RegisterRequest`, `UpdateProfileRequest`, `MetierDocument`, `MetierRequest`, `StatutRequest`).

Les trois fonctionnalités utilisent trois **bases Mongo distinctes** : `trocwear_annonces`, `trocwear_negociations`, `trocwear_livraisons`. Redis est utilisé pour les sessions et la publication des événements d'authentification ; PostgreSQL conserve les comptes et outbox.

## Limites connues et travail restant

- Les opérations Mongo ci-dessus sont de vrais CRUD, **pas** encore les workflows métier complets (réservation, propositions, acceptation, progression, traçabilité).
- **Le réplica interservices entretenu par événements demandé par le professeur reste à développer**. L'architecture monolithique modulaire n'est pas équivalente à « au moins deux services indépendants » : vérifier l'acceptation par le professeur ou redéployer deux exécutables distincts.
- Migration EF Core conservée du projet précédent : la vérification `PendingModelChangesWarning` est ignorée temporairement en développement. Régénérer les migrations et vérifier le schéma avant toute production.
- Pas de garantie de compilation ou de tests réels dans cet environnement sans `dotnet` ni Docker.
