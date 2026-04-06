# 🚀 Cours — Créer une Web API REST avec ASP.NET Core

> *Thème : Star Wars*

---

## Table des matières

1. [Introduction — Qu'est-ce qu'une API REST ?](#1--introduction--quest-ce-quune-api-rest-)
2. [Les verbes HTTP](#2--les-verbes-http)
3. [Les codes de statut HTTP](#3--les-codes-de-statut-http)
4. [Mise en place du projet](#4--mise-en-place-du-projet)
5. [Le modèle de données](#5--le-modèle-de-données)
6. [Le contrôleur — Implémentation des verbes](#6--le-contrôleur--implémentation-des-verbes)
   - [GET — Lire des ressources](#61-get--lire-des-ressources)
   - [POST — Créer une ressource](#62-post--créer-une-ressource)
   - [PUT — Remplacer une ressource](#63-put--remplacer-une-ressource)
   - [PATCH — Modifier partiellement une ressource](#64-patch--modifier-partiellement-une-ressource)
   - [DELETE — Supprimer une ressource](#65-delete--supprimer-une-ressource)
7. [Gestion des erreurs et codes HTTP dans une API REST](#7--gestion-des-erreurs-et-codes-http-dans-une-api-rest)
   - [404 Not Found](#71-404-not-found)
   - [400 Bad Request](#72-400-bad-request)
   - [409 Conflict](#73-409-conflict)
   - [204 No Content](#74-204-no-content)
   - [500 Internal Server Error](#75-500-internal-server-error)
8. [Gestion centralisée des exceptions](#8--gestion-centralisée-des-exceptions)
9. [Configurer Swagger (OpenAPI)](#9--configurer-swagger-openapi)
10. [Tester l'API](#10--tester-lapi)
11. [Récapitulatif](#11--récapitulatif)

---

## 1 — Introduction : Qu'est-ce qu'une API REST ?

**REST** (*Representational State Transfer*) est un style d'architecture pour concevoir des services web. Une API REST expose des **ressources** (ex. des personnages Star Wars) et permet de les manipuler via le protocole **HTTP**.

Principes fondamentaux :

| Principe | Description |
|---|---|
| **Client-Serveur** | Le client (navigateur, appli mobile…) et le serveur sont indépendants. |
| **Sans état (Stateless)** | Chaque requête contient toute l'information nécessaire ; le serveur ne stocke pas de session. |
| **Ressources identifiées par des URI** | Chaque ressource a une adresse unique, ex. `/api/characters/1`. |
| **Utilisation des verbes HTTP** | On utilise GET, POST, PUT, PATCH, DELETE pour exprimer l'action souhaitée. |
| **Représentations** | Les ressources sont échangées sous forme de représentations (JSON le plus souvent). |

### Exemple concret

```
GET /api/characters       → Liste tous les personnages
GET /api/characters/1     → Récupère Luke Skywalker (id=1)
POST /api/characters      → Crée un nouveau personnage
PUT /api/characters/1     → Remplace entièrement le personnage 1
PATCH /api/characters/1   → Modifie partiellement le personnage 1
DELETE /api/characters/1  → Supprime le personnage 1
```

---

## 2 — Les verbes HTTP

Les verbes HTTP (aussi appelés **méthodes**) indiquent l'**intention** de la requête.

| Verbe | Rôle | Idempotent ? | Corps de requête ? | Réponse typique |
|---|---|---|---|---|
| **GET** | Lire une ou plusieurs ressources | ✅ Oui | ❌ Non | `200 OK` + données |
| **POST** | Créer une nouvelle ressource | ❌ Non | ✅ Oui | `201 Created` + ressource créée |
| **PUT** | Remplacer entièrement une ressource existante | ✅ Oui | ✅ Oui | `200 OK` ou `204 No Content` |
| **PATCH** | Modifier partiellement une ressource | ❌ Non | ✅ Oui | `200 OK` + ressource modifiée |
| **DELETE** | Supprimer une ressource | ✅ Oui | ❌ Non | `204 No Content` |

### 💡 Qu'est-ce que l'idempotence ?

Une opération est **idempotente** si l'appeler une ou plusieurs fois produit le **même résultat** côté serveur.

- `DELETE /api/characters/1` appelé 2 fois → le personnage est supprimé la 1ʳᵉ fois, la 2ᵉ fois il n'existe plus (même état final).
- `POST /api/characters` appelé 2 fois → deux personnages sont créés → **pas idempotent**.

---

## 3 — Les codes de statut HTTP

Les codes de statut sont regroupés par famille :

| Famille | Signification | Exemples |
|---|---|---|
| **2xx** | ✅ Succès | `200 OK`, `201 Created`, `204 No Content` |
| **3xx** | ↪️ Redirection | `301 Moved Permanently`, `304 Not Modified` |
| **4xx** | ❌ Erreur client | `400 Bad Request`, `404 Not Found`, `409 Conflict`, `422 Unprocessable Entity` |
| **5xx** | 💥 Erreur serveur | `500 Internal Server Error`, `503 Service Unavailable` |

### Les codes les plus utilisés dans une API REST

| Code | Nom | Quand l'utiliser ? |
|---|---|---|
| `200` | OK | Requête réussie (GET, PUT, PATCH). |
| `201` | Created | Ressource créée avec succès (POST). Inclure un header `Location` avec l'URI. |
| `204` | No Content | Succès sans corps de réponse (DELETE, PUT). |
| `400` | Bad Request | La requête est malformée ou les données sont invalides. |
| `404` | Not Found | La ressource demandée n'existe pas. |
| `409` | Conflict | Conflit avec l'état actuel (ex. doublon). |
| `422` | Unprocessable Entity | Les données sont syntaxiquement correctes mais sémantiquement invalides. |
| `500` | Internal Server Error | Erreur inattendue côté serveur (bug, exception non gérée). |

---

## 4 — Mise en place du projet

### Créer le projet

```bash
dotnet new webapi -n StarWarsApi --use-controllers
cd StarWarsApi
```

### Structure attendue du projet

```
StarWarsApi/
├── Controllers/
│   └── CharactersController.cs
├── Models/
│   ├── Character.cs
│   └── CharacterUpdateDto.cs
├── Program.cs
├── appsettings.json
└── StarWarsApi.csproj
```

### `Program.cs` — Point d'entrée

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

> Ce fichier configure le pipeline HTTP. `AddControllers()` enregistre le système de routage par contrôleurs, `MapControllers()` active le routage.

---

## 5 — Le modèle de données

### `Models/Character.cs`

```csharp
namespace StarWarsApi.Models;

public class Character
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Planet { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public int BirthYear { get; set; }
}
```

### `Models/CharacterUpdateDto.cs`

Un **DTO** (*Data Transfer Object*) permet de contrôler exactement quels champs peuvent être modifiés lors d'un PATCH :

```csharp
namespace StarWarsApi.Models;

/// <summary>
/// DTO pour la mise à jour partielle d'un personnage (PATCH).
/// Les propriétés nulles ne seront pas modifiées.
/// </summary>
public class CharacterUpdateDto
{
    public string? Name { get; set; }
    public string? Planet { get; set; }
    public string? Affiliation { get; set; }
    public int? BirthYear { get; set; }
}
```

> **Pourquoi un DTO ?** En PUT on remplace tout l'objet, donc on réutilise `Character`. En PATCH on veut ne modifier que certains champs → les propriétés **nullable** du DTO permettent de distinguer « non fourni » (null) de « fourni ».

---

## 6 — Le contrôleur — Implémentation des verbes

### `Controllers/CharactersController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using StarWarsApi.Models;

namespace StarWarsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CharactersController : ControllerBase
{
    // Simulation d'une base de données en mémoire
    private static readonly List<Character> _characters = new()
    {
        new Character { Id = 1, Name = "Luke Skywalker", Planet = "Tatooine", Affiliation = "Rebel Alliance", BirthYear = 19 },
        new Character { Id = 2, Name = "Darth Vader",    Planet = "Tatooine", Affiliation = "Galactic Empire", BirthYear = 41 },
        new Character { Id = 3, Name = "Leia Organa",    Planet = "Alderaan", Affiliation = "Rebel Alliance", BirthYear = 19 },
    };

    private static int _nextId = 4;
```

> **`[ApiController]`** active des comportements automatiques : validation du modèle, inférence de `[FromBody]`, réponses `400` automatiques sur modèle invalide.
>
> **`[Route("api/[controller]")]`** → l'URL sera `/api/characters` (le suffixe "Controller" est retiré automatiquement).

---

### 6.1. GET — Lire des ressources

```csharp
    // --------------------------------------------------------
    // GET /api/characters
    // Récupère la liste de tous les personnages.
    // Réponse : 200 OK + tableau JSON
    // --------------------------------------------------------
    [HttpGet]
    public ActionResult<IEnumerable<Character>> GetAll()
    {
        return Ok(_characters);
    }

    // --------------------------------------------------------
    // GET /api/characters/{id}
    // Récupère un personnage par son identifiant.
    // Réponse : 200 OK + objet JSON   |   404 Not Found
    // --------------------------------------------------------
    [HttpGet("{id}")]
    public ActionResult<Character> GetById(int id)
    {
        var character = _characters.FirstOrDefault(c => c.Id == id);

        if (character is null)
            return NotFound(new { message = $"Personnage avec l'id {id} introuvable." });

        return Ok(character);
    }
```

**Points clés :**
- `[HttpGet]` mappe la méthode sur le verbe **GET**.
- `[HttpGet("{id}")]` capture le segment d'URL dans le paramètre `id`.
- `Ok(...)` retourne un **200 OK**.
- `NotFound(...)` retourne un **404 Not Found**.

---

### 6.2. POST — Créer une ressource

```csharp
    // --------------------------------------------------------
    // POST /api/characters
    // Crée un nouveau personnage.
    // Réponse : 201 Created + objet créé + header Location
    // --------------------------------------------------------
    [HttpPost]
    public ActionResult<Character> Create([FromBody] Character character)
    {
        // Vérifier que le nom n'est pas vide
        if (string.IsNullOrWhiteSpace(character.Name))
            return BadRequest(new { message = "Le nom du personnage est obligatoire." });

        // Vérifier qu'il n'existe pas déjà un personnage avec ce nom
        if (_characters.Any(c => c.Name.Equals(character.Name, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = $"Un personnage nommé '{character.Name}' existe déjà." });

        character.Id = _nextId++;
        _characters.Add(character);

        // CreatedAtAction retourne 201 + header Location: /api/characters/{id}
        return CreatedAtAction(nameof(GetById), new { id = character.Id }, character);
    }
```

**Points clés :**
- `[FromBody]` indique que le paramètre vient du corps de la requête (JSON).
- `BadRequest(...)` → **400 Bad Request** quand les données sont invalides.
- `Conflict(...)` → **409 Conflict** quand la ressource existe déjà.
- `CreatedAtAction(...)` → **201 Created** avec le header `Location` pointant vers la nouvelle ressource.

---

### 6.3. PUT — Remplacer une ressource

```csharp
    // --------------------------------------------------------
    // PUT /api/characters/{id}
    // Remplace entièrement un personnage existant.
    // Réponse : 200 OK + objet modifié   |   404 Not Found   |   400 Bad Request
    // --------------------------------------------------------
    [HttpPut("{id}")]
    public ActionResult<Character> Update(int id, [FromBody] Character updatedCharacter)
    {
        // Vérifier la cohérence entre l'URL et le corps
        if (id != updatedCharacter.Id)
            return BadRequest(new { message = "L'id de l'URL ne correspond pas à l'id du corps de la requête." });

        var existing = _characters.FirstOrDefault(c => c.Id == id);
        if (existing is null)
            return NotFound(new { message = $"Personnage avec l'id {id} introuvable." });

        // Remplacement complet de toutes les propriétés
        existing.Name = updatedCharacter.Name;
        existing.Planet = updatedCharacter.Planet;
        existing.Affiliation = updatedCharacter.Affiliation;
        existing.BirthYear = updatedCharacter.BirthYear;

        return Ok(existing);
    }
```

**Points clés :**
- **PUT remplace tout** : si un champ n'est pas fourni, il prendra sa valeur par défaut.
- On vérifie la cohérence `id` URL vs `id` corps → **400** si incohérent.
- On retourne **404** si la ressource n'existe pas.

---

### 6.4. PATCH — Modifier partiellement une ressource

```csharp
    // --------------------------------------------------------
    // PATCH /api/characters/{id}
    // Modifie partiellement un personnage existant.
    // Réponse : 200 OK + objet modifié   |   404 Not Found
    // --------------------------------------------------------
    [HttpPatch("{id}")]
    public ActionResult<Character> Patch(int id, [FromBody] CharacterUpdateDto patch)
    {
        var existing = _characters.FirstOrDefault(c => c.Id == id);
        if (existing is null)
            return NotFound(new { message = $"Personnage avec l'id {id} introuvable." });

        // On ne modifie que les champs non-null du DTO
        if (patch.Name is not null)
            existing.Name = patch.Name;

        if (patch.Planet is not null)
            existing.Planet = patch.Planet;

        if (patch.Affiliation is not null)
            existing.Affiliation = patch.Affiliation;

        if (patch.BirthYear.HasValue)
            existing.BirthYear = patch.BirthYear.Value;

        return Ok(existing);
    }
```

**Points clés :**
- **PATCH ne modifie que les champs fournis** — contrairement à PUT.
- On utilise le `CharacterUpdateDto` avec des propriétés **nullable**.
- `is not null` / `.HasValue` permettent de ne modifier que ce qui est envoyé.

---

### 6.5. DELETE — Supprimer une ressource

```csharp
    // --------------------------------------------------------
    // DELETE /api/characters/{id}
    // Supprime un personnage.
    // Réponse : 204 No Content   |   404 Not Found
    // --------------------------------------------------------
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var character = _characters.FirstOrDefault(c => c.Id == id);
        if (character is null)
            return NotFound(new { message = $"Personnage avec l'id {id} introuvable." });

        _characters.Remove(character);

        // 204 : succès, pas de contenu à retourner
        return NoContent();
    }
}
```

**Points clés :**
- `NoContent()` → **204 No Content** : la suppression a réussi, rien à retourner.
- Le type de retour est `IActionResult` (pas `ActionResult<T>`) car on ne retourne pas de données.

---

## 7 — Gestion des erreurs et codes HTTP dans une API REST

Une API REST bien conçue **communique les erreurs via les codes HTTP**, pas uniquement dans le corps de la réponse. Voici les cas courants et comment les gérer.

### 7.1. 404 Not Found

**Quand ?** La ressource demandée n'existe pas.

```csharp
var character = _characters.FirstOrDefault(c => c.Id == id);
if (character is null)
    return NotFound(new { message = $"Personnage avec l'id {id} introuvable." });
```

> ⚠️ **Erreur courante** : retourner `200 OK` avec un corps `null`. Le client ne peut alors pas distinguer « la ressource est vide » de « la ressource n'existe pas ».

### 7.2. 400 Bad Request

**Quand ?** La requête du client est invalide (données manquantes, format incorrect, incohérence).

```csharp
// Données invalides
if (string.IsNullOrWhiteSpace(character.Name))
    return BadRequest(new { message = "Le nom est obligatoire." });

// Incohérence URL / corps
if (id != character.Id)
    return BadRequest(new { message = "L'id de l'URL ne correspond pas au corps." });
```

> 💡 Avec `[ApiController]`, ASP.NET Core retourne automatiquement un **400** si le modèle échoue à la validation (ex. JSON malformé). Vous n'avez pas à le gérer manuellement.

### 7.3. 409 Conflict

**Quand ?** La requête entre en conflit avec l'état actuel du serveur (doublon, violation de contrainte).

```csharp
if (_characters.Any(c => c.Name.Equals(character.Name, StringComparison.OrdinalIgnoreCase)))
    return Conflict(new { message = $"'{character.Name}' existe déjà." });
```

### 7.4. 204 No Content

**Quand ?** L'opération a réussi mais il n'y a rien à retourner (typiquement DELETE ou PUT).

```csharp
return NoContent();
```

### 7.5. 500 Internal Server Error

**Quand ?** Une erreur inattendue se produit côté serveur (exception non gérée, bug).

> Le client ne devrait **jamais** recevoir le détail d'une exception (stack trace, message technique). On retourne un message générique.

```csharp
// Ne faites PAS ça :
return StatusCode(500, new { message = ex.ToString() }); // ❌ Fuite d'information

// Faites plutôt ça :
return StatusCode(500, new { message = "Une erreur interne est survenue." }); // ✅
```

### Tableau récapitulatif : quel code pour quelle situation ?

| Situation | Code | Méthode helper |
|---|---|---|
| Tout va bien, voici les données | `200` | `Ok(data)` |
| Ressource créée | `201` | `CreatedAtAction(...)` |
| Succès, rien à retourner | `204` | `NoContent()` |
| Requête invalide / données manquantes | `400` | `BadRequest(error)` |
| Ressource introuvable | `404` | `NotFound(error)` |
| Conflit (doublon, etc.) | `409` | `Conflict(error)` |
| Erreur serveur inattendue | `500` | `StatusCode(500, error)` |

---

## 8 — Gestion centralisée des exceptions

Plutôt que de mettre des `try/catch` dans chaque action, on peut configurer un **gestionnaire d'exceptions global** dans `Program.cs`.

### Avec le middleware intégré (ASP.NET Core 8+)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(); // ← Active le format standard RFC 7807

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // En dev : détails complets de l'exception
    app.UseDeveloperExceptionPage();
}
else
{
    // En production : réponse JSON standardisée sans détail technique
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Contrôleur d'erreur pour la production

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StarWarsApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)] // Masqué de la doc OpenAPI
public class ErrorController : ControllerBase
{
    [Route("/error")]
    public IActionResult HandleError()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();

        // Logger l'exception ici (ILogger, Serilog, etc.)

        return Problem(
            title: "Une erreur interne est survenue.",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
}
```

> **`Problem(...)`** retourne une réponse au format **RFC 7807** (Problem Details), un standard pour les erreurs dans les API :
>
> ```json
> {
>   "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
>   "title": "Une erreur interne est survenue.",
>   "status": 500
> }
> ```

---

## 9 — Configurer Swagger (OpenAPI)

**Swagger** (via la spécification **OpenAPI**) permet de générer automatiquement une **documentation interactive** de votre API. Grâce à l'interface **Swagger UI**, vous pouvez visualiser tous les endpoints, leurs paramètres, et les tester directement depuis le navigateur.

### Qu'est-ce qu'OpenAPI / Swagger ?

| Terme | Description |
|---|---|
| **OpenAPI** | Spécification standard (anciennement Swagger Specification) décrivant une API REST au format JSON/YAML. |
| **Swagger UI** | Interface web interactive générée à partir de la spécification OpenAPI. |
| **Swashbuckle** | Bibliothèque .NET qui génère la spécification OpenAPI et intègre Swagger UI dans ASP.NET Core. |

### Installer Swashbuckle

```bash
dotnet add package Swashbuckle.AspNetCore
```

### Configurer `Program.cs`

Remplacez les appels `AddOpenApi()` / `MapOpenApi()` par la configuration Swashbuckle :

```csharp
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ── Swagger / OpenAPI ──────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Star Wars API",
        Version = "v1",
        Description = "Une API REST pour gérer les personnages Star Wars.",
        Contact = new OpenApiContact
        {
            Name  = "Padawan Dev",
            Email = "padawan@jedi-temple.sw"
        }
    });
});
// ───────────────────────────────────────────────────────────

builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Active Swagger UI uniquement en développement
    app.UseSwagger();      // Génère /swagger/v1/swagger.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Star Wars API v1");
        options.RoutePrefix = string.Empty; // Swagger UI accessible à la racine /
    });

    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Détail des éléments :**

| Élément | Rôle |
|---|---|
| `AddEndpointsApiExplorer()` | Enregistre le service qui découvre automatiquement les endpoints de l'API. |
| `AddSwaggerGen(...)` | Configure la génération du document OpenAPI (titre, version, description…). |
| `UseSwagger()` | Expose le fichier JSON de la spécification à `/swagger/v1/swagger.json`. |
| `UseSwaggerUI(...)` | Sert l'interface Swagger UI. `RoutePrefix = string.Empty` la rend accessible à `https://localhost:5001/`. |

### Accéder à Swagger UI

Après `dotnet run`, ouvrez votre navigateur :

```
https://localhost:5001/
```

Vous verrez une interface interactive listant tous vos endpoints :

```
┌─────────────────────────────────────────────────────────────┐
│  Star Wars API v1                                           │
├─────────────────────────────────────────────────────────────┤
│  GET    /api/characters        Liste tous les personnages   │
│  GET    /api/characters/{id}   Récupère un personnage       │
│  POST   /api/characters        Crée un personnage           │
│  PUT    /api/characters/{id}   Remplace un personnage       │
│  PATCH  /api/characters/{id}   Modifie partiellement        │
│  DELETE /api/characters/{id}   Supprime un personnage       │
└─────────────────────────────────────────────────────────────┘
```

### Enrichir la documentation avec les commentaires XML

Vous pouvez ajouter des **commentaires XML** sur vos actions pour qu'ils apparaissent dans Swagger UI.

#### 1. Activer les commentaires XML dans le `.csproj`

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Supprime les warnings pour les membres non documentés -->
</PropertyGroup>
```

#### 2. Configurer Swashbuckle pour lire le fichier XML

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Star Wars API",
        Version     = "v1",
        Description = "Une API REST pour gérer les personnages Star Wars."
    });

    // Inclure les commentaires XML dans la documentation Swagger
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
```

#### 3. Annoter vos actions

```csharp
/// <summary>
/// Récupère la liste de tous les personnages Star Wars.
/// </summary>
/// <returns>Liste des personnages.</returns>
/// <response code="200">Liste retournée avec succès.</response>
[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK)]
public ActionResult<IEnumerable<Character>> GetAll()
{
    return Ok(_characters);
}

/// <summary>
/// Récupère un personnage par son identifiant.
/// </summary>
/// <param name="id">Identifiant du personnage.</param>
/// <returns>Le personnage correspondant.</returns>
/// <response code="200">Personnage trouvé.</response>
/// <response code="404">Personnage introuvable.</response>
[HttpGet("{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public ActionResult<Character> GetById(int id)
{
    // ...
}
```

> 💡 L'attribut **`[ProducesResponseType]`** indique à Swagger quels codes HTTP sont possibles pour chaque action. Cela enrichit la documentation avec les réponses attendues.

### Structure mise à jour du projet

```
StarWarsApi/
├── Controllers/
│   ├── CharactersController.cs
│   └── ErrorController.cs
├── Models/
│   ├── Character.cs
│   └── CharacterUpdateDto.cs
├── Program.cs              ← Swagger configuré ici
├── appsettings.json
└── StarWarsApi.csproj      ← GenerateDocumentationFile activé
```

---

## 10 — Tester l'API

### Lancer l'API

```bash
dotnet run
```

L'API sera disponible sur `https://localhost:5001` (ou le port indiqué dans la console).

### Tester avec `curl`

```bash
# GET — Liste tous les personnages
curl -X GET https://localhost:5001/api/characters

# GET — Un personnage par id
curl -X GET https://localhost:5001/api/characters/1

# GET — Personnage inexistant → 404
curl -X GET https://localhost:5001/api/characters/999

# POST — Créer un personnage
curl -X POST https://localhost:5001/api/characters \
  -H "Content-Type: application/json" \
  -d '{"name":"Yoda","planet":"Dagobah","affiliation":"Jedi Order","birthYear":896}'

# POST — Doublon → 409
curl -X POST https://localhost:5001/api/characters \
  -H "Content-Type: application/json" \
  -d '{"name":"Luke Skywalker","planet":"Tatooine","affiliation":"Rebel Alliance","birthYear":19}'

# PUT — Remplacer un personnage
curl -X PUT https://localhost:5001/api/characters/1 \
  -H "Content-Type: application/json" \
  -d '{"id":1,"name":"Luke Skywalker","planet":"Tatooine","affiliation":"New Jedi Order","birthYear":19}'

# PUT — Id incohérent → 400
curl -X PUT https://localhost:5001/api/characters/1 \
  -H "Content-Type: application/json" \
  -d '{"id":99,"name":"Luke","planet":"Tatooine","affiliation":"Jedi","birthYear":19}'

# PATCH — Modifier uniquement l'affiliation
curl -X PATCH https://localhost:5001/api/characters/2 \
  -H "Content-Type: application/json" \
  -d '{"affiliation":"Redeemed"}'

# DELETE — Supprimer un personnage
curl -X DELETE https://localhost:5001/api/characters/3

# DELETE — Personnage déjà supprimé → 404
curl -X DELETE https://localhost:5001/api/characters/3
```

### Tester avec le fichier `.http` (Visual Studio / VS Code)

Créez un fichier `StarWarsApi.http` :

```http
@host = https://localhost:5001

### Liste tous les personnages
GET {{host}}/api/characters

### Récupère Luke Skywalker
GET {{host}}/api/characters/1

### Personnage inexistant
GET {{host}}/api/characters/999

### Crée Yoda
POST {{host}}/api/characters
Content-Type: application/json

{
  "name": "Yoda",
  "planet": "Dagobah",
  "affiliation": "Jedi Order",
  "birthYear": 896
}

### Met à jour Luke (PUT complet)
PUT {{host}}/api/characters/1
Content-Type: application/json

{
  "id": 1,
  "name": "Luke Skywalker",
  "planet": "Tatooine",
  "affiliation": "New Jedi Order",
  "birthYear": 19
}

### Modifie partiellement Vader (PATCH)
PATCH {{host}}/api/characters/2
Content-Type: application/json

{
  "affiliation": "Redeemed"
}

### Supprime Leia
DELETE {{host}}/api/characters/3
```

---

## 11 — Récapitulatif

### Les 5 verbes HTTP

```
┌──────────┬──────────────────────────┬─────────────────┐
│  Verbe   │  Action                  │  Code succès    │
├──────────┼──────────────────────────┼─────────────────┤
│  GET     │  Lire                    │  200 OK         │
│  POST    │  Créer                   │  201 Created    │
│  PUT     │  Remplacer (complet)     │  200 / 204      │
│  PATCH   │  Modifier (partiel)      │  200 OK         │
│  DELETE  │  Supprimer               │  204 No Content │
└──────────┴──────────────────────────┴─────────────────┘
```

### Les codes d'erreur essentiels

```
┌───────┬────────────────────┬──────────────────────────────────────┐
│ Code  │  Nom               │  Quand ?                             │
├───────┼────────────────────┼──────────────────────────────────────┤
│  400  │  Bad Request       │  Données invalides, JSON malformé    │
│  404  │  Not Found         │  Ressource inexistante               │
│  409  │  Conflict          │  Doublon, violation de contrainte    │
│  500  │  Internal Error    │  Bug côté serveur                    │
└───────┴────────────────────┴──────────────────────────────────────┘
```

### Méthodes helper ASP.NET Core

| Helper | Code HTTP |
|---|---|
| `Ok(data)` | 200 |
| `CreatedAtAction(...)` | 201 |
| `NoContent()` | 204 |
| `BadRequest(error)` | 400 |
| `NotFound(error)` | 404 |
| `Conflict(error)` | 409 |
| `StatusCode(500, error)` | 500 |
| `Problem(...)` | RFC 7807 |

---

> *« Do. Or do not. There is no try. »* — Yoda
>
> *(Sauf dans un try/catch, bien sûr.)*
