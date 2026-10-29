# Quatre couches et dépendances

```text
backend/
  TrocWear.Api/            Controllers, Security, Program.cs
  TrocWear.Application/    Authentification/{Commands,Queries}, Utilisateurs, Metiers/{Commands,Queries}, Interfaces
  TrocWear.Domain/         Models, DTOs, Events, ValueObjects
  TrocWear.Infrastructure/ Persistence, Repositories, SqlQueries, Messaging, Metiers (Mongo + MongoConfiguration)
  TrocWear.sln
```

Dépendances: Api -> Application,Domain,Infrastructure ; Infrastructure -> Application,Domain ; Application -> Domain ; Domain -> rien.

Modules (non-microservices) : Authentification (PostgreSQL et Redis), Annonces (Mongo `trocwear_annonces`), Négociations (Mongo `trocwear_negociations`), Livraisons (Mongo `trocwear_livraisons`).

Pour respecter strictement l'exigence universitaire « au moins deux services », déployer des API exécutables distinctes ou des applications de service séparées; cette édition impose volontairement une seule API et quatre projets C#.
