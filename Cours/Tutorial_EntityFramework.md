# 🗃️ API + Entity Framework + SQLite — Tutorial complet

> *Du `dotnet new` au CRUD Pokémon en 20 minutes.*

---

## 🎯 Objectif

Créer une solution avec **deux projets** :

| Projet | Rôle |
|---|---|
| `PokemonApi` | API REST avec Swagger (ASP.NET Core Web API) |
| `PokemonData` | Modèles + DbContext + Migrations (Entity Framework Core + SQLite) |

**Modèle métier :**
- Un **Dresseur** peut avoir **plusieurs Pokémon** (1 → N)
- Un **Pokémon** n'appartient qu'à **un seul Dresseur**

---

## Étape 1 — Créer la solution et les projets

```bash
# Créer le dossier et s'y placer
mkdir PokemonSolution && cd PokemonSolution

# Créer la solution
dotnet new sln -n PokemonSolution

# Créer le projet Data (Class Library)
dotnet new classlib -n PokemonData -f net10.0

# Créer le projet API (Web API)
dotnet new webapi -n PokemonApi -f net10.0 --use-controllers

# Ajouter les projets à la solution
dotnet sln add PokemonData/PokemonData.csproj
dotnet sln add PokemonApi/PokemonApi.csproj

# L'API dépend du Data → ajouter la référence
dotnet add PokemonApi reference PokemonData
```

**Structure obtenue :**
```
PokemonSolution/
├── PokemonSolution.sln
├── PokemonData/                ← Modèles + DbContext
│   ├── PokemonData.csproj
│   └── Class1.cs              (à supprimer)
└── PokemonApi/                 ← API REST + Swagger
    ├── PokemonApi.csproj
    ├── Program.cs
    └── Controllers/
```

> 💡 **Pourquoi séparer Data et API ?**
> Le projet `Data` est indépendant : on peut le réutiliser dans une app console,
> un Blazor, des tests… sans embarquer toute l'API.

---

## Étape 2 — Installer les packages NuGet

### Dans le projet `PokemonData` (Entity Framework + SQLite)

```bash
cd PokemonData

dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

cd ..
```

### Dans le projet `PokemonApi` (outils EF pour les migrations)

```bash
cd PokemonApi

dotnet add package Microsoft.EntityFrameworkCore.Design

cd ..
```

### Installer l'outil CLI Entity Framework (global)

```bash
dotnet tool install --global dotnet-ef
```

> 💡 Si déjà installé : `dotnet tool update --global dotnet-ef`

---

## Étape 3 — Créer les modèles (projet `PokemonData`)

Supprime `Class1.cs` et crée un dossier `Models/`.

### `PokemonData/Models/Dresseur.cs`

```csharp
namespace PokemonData.Models;

/// <summary>
/// Un dresseur de Pokémon.
/// </summary>
public class Dresseur
{
    /// <summary>Identifiant unique.</summary>
    public int Id { get; set; }

    /// <summary>Nom du dresseur.</summary>
    public required string Nom { get; set; }

    /// <summary>Ville d'origine.</summary>
    public string? Ville { get; set; }

    // ═══════════════════════════════════════
    //  NAVIGATION : Un dresseur a N pokémon
    // ═══════════════════════════════════════

    /// <summary>Liste des pokémon du dresseur.</summary>
    public List<Pokemon> Pokemons { get; set; } = [];
}
```

### `PokemonData/Models/Pokemon.cs`

```csharp
namespace PokemonData.Models;

/// <summary>
/// Un Pokémon appartenant à un dresseur.
/// </summary>
public class Pokemon
{
    /// <summary>Identifiant unique.</summary>
    public int Id { get; set; }

    /// <summary>Nom du pokémon (ex: Pikachu).</summary>
    public required string Nom { get; set; }

    /// <summary>Type élémentaire (ex: Électrik, Feu, Eau…).</summary>
    public required string Type { get; set; }

    /// <summary>Niveau du pokémon (1-100).</summary>
    public int Niveau { get; set; } = 1;

    // ═══════════════════════════════════════
    //  NAVIGATION : Un pokémon a 1 dresseur
    // ═══════════════════════════════════════

    /// <summary>Clé étrangère vers le dresseur.</summary>
    public int DresseurId { get; set; }

    /// <summary>Référence de navigation vers le dresseur.</summary>
    public Dresseur Dresseur { get; set; } = null!;
}
```

> 📐 **Relation 1-N expliquée :**
>
> ```
> ┌──────────────┐        ┌──────────────┐
> │   Dresseur   │        │   Pokemon    │
> ├──────────────┤        ├──────────────┤
> │ Id (PK)      │◄──┐    │ Id (PK)      │
> │ Nom          │   │    │ Nom          │
> │ Ville        │   │    │ Type         │
> │              │   │    │ Niveau       │
> │ Pokemons [ ] │   └────│ DresseurId(FK)│
> └──────────────┘        └──────────────┘
> ```
>
> - `DresseurId` est la **clé étrangère** (FK) côté Pokémon.
> - `Pokemons` est la **propriété de navigation** côté Dresseur (la collection).
> - `Dresseur` est la **propriété de navigation** côté Pokémon (la référence).

---

## Étape 4 — Créer le DbContext (projet `PokemonData`)

### `PokemonData/PokemonDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PokemonData.Models;

namespace PokemonData;

/// <summary>
/// Contexte Entity Framework pour la base de données Pokémon.
/// </summary>
public class PokemonDbContext : DbContext
{
    // ═══════════════════════════════════════
    //  TABLES (DbSet = une table en BDD)
    // ═══════════════════════════════════════

    public DbSet<Dresseur> Dresseurs => Set<Dresseur>();
    public DbSet<Pokemon> Pokemons => Set<Pokemon>();

    // ═══════════════════════════════════════
    //  CONSTRUCTEUR (reçoit les options de l'injection de dépendances)
    // ═══════════════════════════════════════

    public PokemonDbContext(DbContextOptions<PokemonDbContext> options)
        : base(options)
    {
    }

    // ═══════════════════════════════════════
    //  CONFIGURATION DU MODÈLE
    // ═══════════════════════════════════════

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Relation 1-N : Dresseur → Pokémon ──
        modelBuilder.Entity<Pokemon>()
            .HasOne(p => p.Dresseur)           // Un pokémon a UN dresseur
            .WithMany(d => d.Pokemons)         // Un dresseur a PLUSIEURS pokémon
            .HasForeignKey(p => p.DresseurId)  // La FK est DresseurId
            .OnDelete(DeleteBehavior.Cascade); // Supprimer un dresseur → supprime ses pokémon

        // ── Seed : données initiales ──
        modelBuilder.Entity<Dresseur>().HasData(
            new Dresseur { Id = 1, Nom = "Sacha",  Ville = "Bourg Palette" },
            new Dresseur { Id = 2, Nom = "Ondine", Ville = "Azuria" },
            new Dresseur { Id = 3, Nom = "Pierre", Ville = "Argenta" }
        );

        modelBuilder.Entity<Pokemon>().HasData(
            new Pokemon { Id = 1, Nom = "Pikachu",    Type = "Électrik", Niveau = 42, DresseurId = 1 },
            new Pokemon { Id = 2, Nom = "Dracaufeu",  Type = "Feu",      Niveau = 50, DresseurId = 1 },
            new Pokemon { Id = 3, Nom = "Stari",      Type = "Eau",      Niveau = 35, DresseurId = 2 },
            new Pokemon { Id = 4, Nom = "Psykokwak",  Type = "Eau",      Niveau = 28, DresseurId = 2 },
            new Pokemon { Id = 5, Nom = "Onix",       Type = "Roche",    Niveau = 40, DresseurId = 3 }
        );
    }
}
```

### 📋 Explication des concepts

| Concept | Rôle |
|---|---|
| `DbContext` | Représente une session avec la base de données |
| `DbSet<T>` | Représente une table — chaque entité `T` = une ligne |
| `OnModelCreating` | Configure les relations, contraintes, et données initiales |
| `HasOne` / `WithMany` | Décrit la relation 1-N entre deux entités |
| `HasForeignKey` | Indique quelle propriété est la clé étrangère |
| `HasData` | Insère des données de **seed** à la création de la BDD |
| `DeleteBehavior.Cascade` | Si on supprime le parent, les enfants sont supprimés aussi |

---

## Étape 5 — Configurer l'API (projet `PokemonApi`)

### `PokemonApi/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PokemonData;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════
//  SERVICES
// ═══════════════════════════════════════

// Enregistrer le DbContext avec SQLite
builder.Services.AddDbContext<PokemonDbContext>(options =>
    options.UseSqlite("Data Source=pokemon.db"));

// Ajouter les contrôleurs
builder.Services.AddControllers();

// Ajouter Swagger (documentation interactive)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ═══════════════════════════════════════
//  MIDDLEWARE
// ═══════════════════════════════════════

// Swagger actif en développement
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// ═══════════════════════════════════════
//  CRÉATION AUTO DE LA BDD AU DÉMARRAGE
// ═══════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();
    db.Database.EnsureCreated();  // Crée la BDD + les tables si elles n'existent pas
}

app.Run();
```

> 💡 **`EnsureCreated()` vs Migrations :**
> - `EnsureCreated()` → crée la BDD d'un coup (pratique pour le dev/démo).
> - Les **migrations** → versionnent le schéma (indispensable en production).
> On verra les migrations à l'étape 8.

---

## Étape 6 — Créer les contrôleurs CRUD

### `PokemonApi/Controllers/DresseursController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonData;
using PokemonData.Models;

namespace PokemonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DresseursController : ControllerBase
{
    private readonly PokemonDbContext _context;

    public DresseursController(PokemonDbContext context)
    {
        _context = context;
    }

    // ══════════════════════════════════════════
    //  GET /api/dresseurs
    //  → Liste tous les dresseurs (avec leurs pokémon)
    // ══════════════════════════════════════════
    [HttpGet]
    public async Task<ActionResult<List<Dresseur>>> GetAll()
    {
        return await _context.Dresseurs
            .Include(d => d.Pokemons)   // ← Charge les pokémon liés
            .ToListAsync();
    }

    // ══════════════════════════════════════════
    //  GET /api/dresseurs/5
    //  → Un dresseur par son Id
    // ══════════════════════════════════════════
    [HttpGet("{id}")]
    public async Task<ActionResult<Dresseur>> GetById(int id)
    {
        var dresseur = await _context.Dresseurs
            .Include(d => d.Pokemons)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dresseur is null)
            return NotFound();

        return dresseur;
    }

    // ══════════════════════════════════════════
    //  POST /api/dresseurs
    //  → Créer un nouveau dresseur
    // ══════════════════════════════════════════
    [HttpPost]
    public async Task<ActionResult<Dresseur>> Create(Dresseur dresseur)
    {
        _context.Dresseurs.Add(dresseur);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dresseur.Id }, dresseur);
    }

    // ══════════════════════════════════════════
    //  PUT /api/dresseurs/5
    //  → Modifier un dresseur existant
    // ══════════════════════════════════════════
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Dresseur dresseur)
    {
        if (id != dresseur.Id)
            return BadRequest("L'id de l'URL ne correspond pas à l'id du body.");

        _context.Entry(dresseur).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Dresseurs.AnyAsync(d => d.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    // ══════════════════════════════════════════
    //  DELETE /api/dresseurs/5
    //  → Supprimer un dresseur (et ses pokémon en cascade)
    // ══════════════════════════════════════════
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var dresseur = await _context.Dresseurs.FindAsync(id);

        if (dresseur is null)
            return NotFound();

        _context.Dresseurs.Remove(dresseur);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
```

### `PokemonApi/Controllers/PokemonsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonData;
using PokemonData.Models;

namespace PokemonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokemonsController : ControllerBase
{
    private readonly PokemonDbContext _context;

    public PokemonsController(PokemonDbContext context)
    {
        _context = context;
    }

    // ══════════════════════════════════════════
    //  GET /api/pokemons
    //  → Liste tous les pokémon
    // ══════════════════════════════════════════
    [HttpGet]
    public async Task<ActionResult<List<Pokemon>>> GetAll()
    {
        return await _context.Pokemons
            .Include(p => p.Dresseur)   // ← Inclut le dresseur lié
            .ToListAsync();
    }

    // ══════════════════════════════════════════
    //  GET /api/pokemons/5
    //  → Un pokémon par son Id
    // ══════════════════════════════════════════
    [HttpGet("{id}")]
    public async Task<ActionResult<Pokemon>> GetById(int id)
    {
        var pokemon = await _context.Pokemons
            .Include(p => p.Dresseur)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pokemon is null)
            return NotFound();

        return pokemon;
    }

    // ══════════════════════════════════════════
    //  GET /api/pokemons/dresseur/5
    //  → Tous les pokémon d'un dresseur
    // ══════════════════════════════════════════
    [HttpGet("dresseur/{dresseurId}")]
    public async Task<ActionResult<List<Pokemon>>> GetByDresseur(int dresseurId)
    {
        if (!await _context.Dresseurs.AnyAsync(d => d.Id == dresseurId))
            return NotFound("Dresseur introuvable.");

        return await _context.Pokemons
            .Where(p => p.DresseurId == dresseurId)
            .ToListAsync();
    }

    // ══════════════════════════════════════════
    //  POST /api/pokemons
    //  → Ajouter un pokémon (à un dresseur existant)
    // ══════════════════════════════════════════
    [HttpPost]
    public async Task<ActionResult<Pokemon>> Create(Pokemon pokemon)
    {
        // Vérifier que le dresseur existe
        if (!await _context.Dresseurs.AnyAsync(d => d.Id == pokemon.DresseurId))
            return BadRequest("Le dresseur spécifié n'existe pas.");

        _context.Pokemons.Add(pokemon);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = pokemon.Id }, pokemon);
    }

    // ══════════════════════════════════════════
    //  PUT /api/pokemons/5
    //  → Modifier un pokémon
    // ══════════════════════════════════════════
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Pokemon pokemon)
    {
        if (id != pokemon.Id)
            return BadRequest("L'id de l'URL ne correspond pas à l'id du body.");

        if (!await _context.Dresseurs.AnyAsync(d => d.Id == pokemon.DresseurId))
            return BadRequest("Le dresseur spécifié n'existe pas.");

        _context.Entry(pokemon).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Pokemons.AnyAsync(p => p.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    // ══════════════════════════════════════════
    //  DELETE /api/pokemons/5
    //  → Supprimer un pokémon
    // ══════════════════════════════════════════
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pokemon = await _context.Pokemons.FindAsync(id);

        if (pokemon is null)
            return NotFound();

        _context.Pokemons.Remove(pokemon);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
```

### 📋 Explication des verbes HTTP

| Verbe | Route exemple | Action | Code retour |
|---|---|---|---|
| `GET` | `/api/dresseurs` | Lister tout | `200 OK` |
| `GET` | `/api/dresseurs/5` | Lire un élément | `200 OK` ou `404 Not Found` |
| `POST` | `/api/dresseurs` | Créer | `201 Created` |
| `PUT` | `/api/dresseurs/5` | Modifier | `204 No Content` |
| `DELETE` | `/api/dresseurs/5` | Supprimer | `204 No Content` |

---

## Étape 7 — Lancer et tester 🚀

```bash
cd PokemonApi
dotnet run
```

L'API démarre et affiche une URL du type :
```
Now listening on: http://localhost:5xxx
```

### Ouvrir Swagger

Va sur **`http://localhost:5xxx/swagger`** dans ton navigateur.

Tu verras une interface interactive avec tous tes endpoints :

```
┌─────────────────────────────────────────────────┐
│  Swagger UI — PokemonApi                        │
├─────────────────────────────────────────────────┤
│                                                 │
│  Dresseurs                                      │
│  ├── GET    /api/dresseurs        → Liste       │
│  ├── GET    /api/dresseurs/{id}   → Détail      │
│  ├── POST   /api/dresseurs        → Créer       │
│  ├── PUT    /api/dresseurs/{id}   → Modifier    │
│  └── DELETE /api/dresseurs/{id}   → Supprimer   │
│                                                 │
│  Pokemons                                       │
│  ├── GET    /api/pokemons                       │
│  ├── GET    /api/pokemons/{id}                  │
│  ├── GET    /api/pokemons/dresseur/{dresseurId} │
│  ├── POST   /api/pokemons                       │
│  ├── PUT    /api/pokemons/{id}                  │
│  └── DELETE /api/pokemons/{id}                  │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Tester avec Swagger

1. Clique sur **`GET /api/dresseurs`** → **Try it out** → **Execute**
2. Tu devrais voir les 3 dresseurs avec leurs pokémon (données du seed)

### Tester en ligne de commande

```bash
# Lister les dresseurs
curl http://localhost:5xxx/api/dresseurs

# Créer un nouveau dresseur
curl -X POST http://localhost:5xxx/api/dresseurs \
  -H "Content-Type: application/json" \
  -d '{"nom": "Régis", "ville": "Bourg Palette"}'

# Ajouter un pokémon au dresseur créé (id=4)
curl -X POST http://localhost:5xxx/api/pokemons \
  -H "Content-Type: application/json" \
  -d '{"nom": "Évoli", "type": "Normal", "niveau": 25, "dresseurId": 4}'
```

---

## Étape 8 — Les migrations (versionner le schéma)

> ⚠️ **Important :** Pour utiliser les migrations, remplace `EnsureCreated()` par
> les commandes de migration. Les deux approches ne sont **pas compatibles** entre elles.

### Pourquoi les migrations ?

| `EnsureCreated()` | Migrations |
|---|---|
| Crée la BDD d'un coup | Versionnent chaque changement |
| Pas de mise à jour du schéma | Peuvent modifier un schéma existant |
| Parfait pour les démos | **Indispensable en production** |

### Commandes de migration

```bash
# Depuis la racine de la solution

# 1. Créer la migration initiale
dotnet ef migrations add InitialCreate --project PokemonData --startup-project PokemonApi

# 2. Appliquer la migration (crée/met à jour la BDD)
dotnet ef database update --project PokemonData --startup-project PokemonApi
```

> 💡 `--project` = le projet qui contient le DbContext.
> `--startup-project` = le projet qui configure la connexion (l'API).

### Modifier le modèle ? Ajouter une migration !

```bash
# Exemple : tu ajoutes une propriété "PointsDeVie" à Pokemon
# 1. Modifier le code
# 2. Créer la migration
dotnet ef migrations add AjoutPointsDeVie --project PokemonData --startup-project PokemonApi

# 3. Appliquer
dotnet ef database update --project PokemonData --startup-project PokemonApi
```

### Mettre à jour `Program.cs` pour utiliser les migrations

Remplace `EnsureCreated()` par :

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();
    db.Database.Migrate();  // ← Applique les migrations automatiquement au démarrage
}
```

---

## Étape 9 — Éviter les références circulaires (JSON)

Quand Swagger sérialise un `Dresseur` qui contient des `Pokemon` qui contiennent un `Dresseur`…
c'est une **boucle infinie** ! 💥

### Solution : configurer le sérialiseur JSON

Dans `Program.cs`, remplace `AddControllers()` par :

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Gère les références circulaires
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
```

> 💡 **Alternative propre :** utiliser des **DTOs** (Data Transfer Objects) pour
> contrôler exactement ce qui est renvoyé par l'API. C'est la bonne pratique
> en production, mais `IgnoreCycles` suffit pour un TP.

---

## 📁 Structure finale du projet

```
PokemonSolution/
├── PokemonSolution.sln
│
├── PokemonData/                        ← Projet "Data"
│   ├── PokemonData.csproj
│   ├── PokemonDbContext.cs             ← DbContext (session BDD)
│   ├── Models/
│   │   ├── Dresseur.cs                 ← Entité Dresseur
│   │   └── Pokemon.cs                  ← Entité Pokemon
│   └── Migrations/                     ← Générées par dotnet ef
│       ├── 20250101_InitialCreate.cs
│       └── PokemonDbContextModelSnapshot.cs
│
├── PokemonApi/                         ← Projet "API"
│   ├── PokemonApi.csproj
│   ├── Program.cs                      ← Configuration + démarrage
│   ├── Controllers/
│   │   ├── DresseursController.cs      ← CRUD Dresseurs
│   │   └── PokemonsController.cs       ← CRUD Pokemons
│   └── pokemon.db                      ← Fichier SQLite (généré)
│
└── README.md
```

---

## 📌 Commandes essentielles — Résumé

```bash
# ── Projet ──
dotnet new classlib -n NomData           # Créer le projet Data
dotnet new webapi -n NomApi              # Créer le projet API
dotnet add NomApi reference NomData      # Lier API → Data

# ── Packages ──
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

# ── Migrations ──
dotnet ef migrations add NomMigration --project Data --startup-project Api
dotnet ef database update --project Data --startup-project Api
dotnet ef migrations remove --project Data --startup-project Api   # Annuler dernière migration

# ── Lancer ──
dotnet build
dotnet run --project NomApi
```

---

## 🧠 Concepts clés Entity Framework

### Le DbContext en image

```
        TON CODE C#                     BASE DE DONNÉES SQLite
    ┌─────────────────┐               ┌─────────────────┐
    │  PokemonDbContext│  ◄──────────► │   pokemon.db    │
    │  ───────────────│               │  ─────────────  │
    │  DbSet<Dresseur>│  ◄──────────► │  Table Dresseurs│
    │  DbSet<Pokemon> │  ◄──────────► │  Table Pokemons │
    └─────────────────┘               └─────────────────┘
           │                                   │
           │    SaveChangesAsync()              │
           │  ──────────────────────►          │
           │    INSERT / UPDATE / DELETE        │
           │                                   │
           │    ToListAsync()                  │
           │  ◄──────────────────────          │
           │    SELECT * FROM ...              │
```

### Les méthodes LINQ importantes

| Méthode | SQL équivalent | Exemple |
|---|---|---|
| `ToListAsync()` | `SELECT *` | `_context.Pokemons.ToListAsync()` |
| `FirstOrDefaultAsync(p => ...)` | `SELECT TOP 1 ... WHERE` | `_context.Pokemons.FirstOrDefaultAsync(p => p.Id == 5)` |
| `FindAsync(id)` | `SELECT ... WHERE Id = @id` | `_context.Pokemons.FindAsync(5)` |
| `Where(p => ...)` | `WHERE` | `_context.Pokemons.Where(p => p.Niveau > 30)` |
| `Include(p => p.Dresseur)` | `JOIN` | Charge la navigation liée |
| `AnyAsync(p => ...)` | `EXISTS` | `_context.Pokemons.AnyAsync(p => p.Id == 5)` |
| `CountAsync()` | `COUNT(*)` | `_context.Pokemons.CountAsync()` |
| `OrderBy(p => p.Niveau)` | `ORDER BY` | Trier les résultats |

### Les états d'une entité

```
                    Add()
    Detached ──────────────► Added ─────────┐
                                            │
                 Attach()                   │ SaveChanges()
    Detached ──────────────► Unchanged ◄────┘
                                │
                          Modifier une     Remove()
                          propriété           │
                                │             ▼
                            Modified      Deleted
                                │             │
                                └─────┬───────┘
                                      │ SaveChanges()
                                      ▼
                                  Unchanged
```

---

## ⚠️ Pièges courants

| Piège | Solution |
|---|---|
| Référence circulaire JSON (boucle infinie) | `ReferenceHandler.IgnoreCycles` ou utiliser des DTOs |
| `Include()` oublié → navigation `null` | Toujours `Include()` les propriétés de navigation nécessaires |
| `EnsureCreated()` + Migrations = conflit | Choisir l'un OU l'autre, pas les deux |
| Oublier `SaveChangesAsync()` | Aucun changement n'est écrit en BDD sans cet appel |
| `DbContext` utilisé dans plusieurs threads | Le DbContext n'est **pas thread-safe**, un par requête |
| Modifier l'Id d'une entité | L'Id est la clé primaire, on ne la modifie **jamais** |
| FK inexistante (ex: DresseurId = 999) | Vérifier que le parent existe avant d'insérer l'enfant |

---

## 🎯 Checklist

- [ ] Solution avec 2 projets (API + Data) séparés
- [ ] Référence : API → Data
- [ ] Packages EF Core + SQLite installés dans Data
- [ ] Modèles `Dresseur` et `Pokemon` avec relation 1-N
- [ ] `PokemonDbContext` avec `DbSet` + configuration de la relation
- [ ] Données de seed dans `OnModelCreating`
- [ ] `Program.cs` configure le DbContext avec SQLite
- [ ] Contrôleurs CRUD pour Dresseurs et Pokemons
- [ ] `IgnoreCycles` configuré pour éviter les boucles JSON
- [ ] Swagger accessible sur `/swagger`
- [ ] `dotnet run` → les données de seed apparaissent dans Swagger

---

*Attrapez-les tous ! 🎮⚡*
