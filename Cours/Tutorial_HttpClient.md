# 🌐 Appeler une API en C# — Tutorial complet

> *De `HttpClient` aux appels REST en 15 minutes.*

---

## 🎯 Objectif

Apprendre à **consommer une API REST** depuis du code C# en utilisant `HttpClient`, la classe standard du framework .NET.

On utilisera comme exemple l'API publique **PokéAPI** (`https://pokeapi.co/api/v2/`) et l'API créée dans le tutorial Entity Framework.

---

## Étape 1 — Comprendre les bases

### Qu'est-ce qu'un appel API ?

```
    TON PROGRAMME C#                         SERVEUR API
  ┌──────────────────┐     Requête HTTP     ┌──────────────────┐
  │                  │ ──────────────────► │                  │
  │   HttpClient     │   GET /api/pokemons  │   API REST       │
  │                  │                      │                  │
  │                  │ ◄────────────────── │                  │
  │                  │     Réponse JSON     │                  │
  └──────────────────┘                      └──────────────────┘
```

### Les verbes HTTP

| Verbe | Action | Exemple |
|---|---|---|
| `GET` | Lire des données | Récupérer la liste des pokémon |
| `POST` | Créer une ressource | Ajouter un nouveau dresseur |
| `PUT` | Modifier une ressource | Mettre à jour un pokémon |
| `DELETE` | Supprimer une ressource | Supprimer un dresseur |

### Les codes de retour importants

| Code | Signification | Quand ? |
|---|---|---|
| `200 OK` | Succès | GET réussi |
| `201 Created` | Ressource créée | POST réussi |
| `204 No Content` | Succès sans contenu | PUT / DELETE réussi |
| `400 Bad Request` | Requête invalide | Données manquantes ou incorrectes |
| `401 Unauthorized` | Non authentifié | Token manquant ou invalide |
| `404 Not Found` | Ressource inexistante | Mauvais Id ou URL |
| `500 Internal Server Error` | Erreur serveur | Bug côté API |

---

## Étape 2 — Créer un projet console

```bash
dotnet new console -n ApiClient -f net10.0
cd ApiClient
```

---

## Étape 3 — Premier appel GET (le plus simple)

### `Program.cs` — Version minimale

```csharp
// Créer un HttpClient
using HttpClient client = new();

// Appeler une API publique (PokéAPI)
string reponse = await client.GetStringAsync("https://pokeapi.co/api/v2/pokemon/pikachu");

// Afficher le JSON brut
Console.WriteLine(reponse);
```

> 💡 **C'est tout !** Trois lignes suffisent pour appeler une API.
> Mais en pratique on veut **désérialiser** le JSON en objets C#.

---

## Étape 4 — Désérialiser le JSON en objets C#

### Le concept

```
  JSON reçu de l'API                     Objet C#
  ─────────────────                     ────────
  {                                     pokemon.Name = "pikachu"
    "name": "pikachu",        ──►       pokemon.Height = 4
    "height": 4,                        pokemon.Weight = 60
    "weight": 60
  }
```

### Créer un modèle C# qui correspond au JSON

```csharp
using System.Text.Json.Serialization;

/// <summary>
/// Représente un pokémon renvoyé par PokéAPI.
/// Seules les propriétés qui nous intéressent sont mappées.
/// </summary>
public class PokemonInfo
{
    [JsonPropertyName("name")]
    public string Nom { get; set; } = "";

    [JsonPropertyName("height")]
    public int Taille { get; set; }

    [JsonPropertyName("weight")]
    public int Poids { get; set; }

    [JsonPropertyName("base_experience")]
    public int ExperienceBase { get; set; }
}
```

> 💡 **`[JsonPropertyName]`** fait le lien entre le nom JSON (`"name"`) et
> la propriété C# (`Nom`). Si les noms sont identiques, l'attribut est facultatif.

### Appeler et désérialiser

```csharp
using System.Net.Http.Json;  // ← Nécessaire pour GetFromJsonAsync

using HttpClient client = new();

// Appel GET + désérialisation automatique
PokemonInfo? pokemon = await client.GetFromJsonAsync<PokemonInfo>(
    "https://pokeapi.co/api/v2/pokemon/pikachu"
);

if (pokemon is not null)
{
    Console.WriteLine($"Nom       : {pokemon.Nom}");
    Console.WriteLine($"Taille    : {pokemon.Taille}");
    Console.WriteLine($"Poids     : {pokemon.Poids}");
    Console.WriteLine($"XP de base: {pokemon.ExperienceBase}");
}
```

**Résultat :**
```
Nom       : pikachu
Taille    : 4
Poids     : 60
XP de base: 112
```

---

## Étape 5 — Appeler sa propre API (CRUD complet)

Supposons que ton API Pokémon du tutorial précédent tourne sur `http://localhost:5000`.

### Modèles correspondants

```csharp
public class Dresseur
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string? Ville { get; set; }
    public List<Pokemon> Pokemons { get; set; } = [];
}

public class Pokemon
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Type { get; set; } = "";
    public int Niveau { get; set; }
    public int DresseurId { get; set; }
}
```

### Programme complet — Toutes les opérations CRUD

```csharp
using System.Net.Http.Json;

const string baseUrl = "http://localhost:5000/api";

using HttpClient client = new();

// ═══════════════════════════════════════
//  GET — Lister tous les dresseurs
// ═══════════════════════════════════════
Console.WriteLine("══ Liste des dresseurs ══");

List<Dresseur>? dresseurs = await client.GetFromJsonAsync<List<Dresseur>>(
    $"{baseUrl}/dresseurs"
);

if (dresseurs is not null)
{
    foreach (var d in dresseurs)
    {
        Console.WriteLine($"  [{d.Id}] {d.Nom} — {d.Ville} ({d.Pokemons.Count} pokémon)");
    }
}

// ═══════════════════════════════════════
//  POST — Créer un nouveau dresseur
// ═══════════════════════════════════════
Console.WriteLine("\n══ Création d'un dresseur ══");

var nouveau = new Dresseur { Nom = "Régis", Ville = "Bourg Palette" };

HttpResponseMessage reponsePost = await client.PostAsJsonAsync(
    $"{baseUrl}/dresseurs",
    nouveau
);

if (reponsePost.IsSuccessStatusCode)
{
    // Lire le dresseur créé (avec son Id généré)
    Dresseur? cree = await reponsePost.Content.ReadFromJsonAsync<Dresseur>();
    Console.WriteLine($"  Créé : [{cree?.Id}] {cree?.Nom}");
}
else
{
    Console.WriteLine($"  Erreur : {reponsePost.StatusCode}");
}

// ═══════════════════════════════════════
//  GET — Lire un dresseur par Id
// ═══════════════════════════════════════
Console.WriteLine("\n══ Détail du dresseur 1 ══");

Dresseur? sacha = await client.GetFromJsonAsync<Dresseur>(
    $"{baseUrl}/dresseurs/1"
);

if (sacha is not null)
{
    Console.WriteLine($"  {sacha.Nom} de {sacha.Ville}");
    foreach (var p in sacha.Pokemons)
    {
        Console.WriteLine($"    🔹 {p.Nom} ({p.Type}) — Niv. {p.Niveau}");
    }
}

// ═══════════════════════════════════════
//  PUT — Modifier un dresseur
// ═══════════════════════════════════════
Console.WriteLine("\n══ Modification du dresseur 1 ══");

var modifie = new Dresseur { Id = 1, Nom = "Sacha Ketchum", Ville = "Bourg Palette" };

HttpResponseMessage reponsePut = await client.PutAsJsonAsync(
    $"{baseUrl}/dresseurs/1",
    modifie
);

Console.WriteLine($"  Résultat : {reponsePut.StatusCode}");  // 204 NoContent = OK

// ═══════════════════════════════════════
//  DELETE — Supprimer un dresseur
// ═══════════════════════════════════════
Console.WriteLine("\n══ Suppression du dresseur 3 ══");

HttpResponseMessage reponseDelete = await client.DeleteAsync(
    $"{baseUrl}/dresseurs/3"
);

Console.WriteLine($"  Résultat : {reponseDelete.StatusCode}");  // 204 NoContent = OK
```

---

## Étape 6 — Gérer les erreurs proprement

### Le problème

```csharp
// ❌ Si l'API renvoie 404, GetFromJsonAsync lance une exception !
var pokemon = await client.GetFromJsonAsync<Pokemon>($"{baseUrl}/pokemons/999");
```

### Solution 1 — Vérifier le StatusCode manuellement

```csharp
HttpResponseMessage reponse = await client.GetAsync($"{baseUrl}/pokemons/999");

if (reponse.IsSuccessStatusCode)
{
    Pokemon? pokemon = await reponse.Content.ReadFromJsonAsync<Pokemon>();
    Console.WriteLine($"Trouvé : {pokemon?.Nom}");
}
else if (reponse.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine("Pokémon introuvable.");
}
else
{
    Console.WriteLine($"Erreur inattendue : {reponse.StatusCode}");
}
```

### Solution 2 — Try/Catch avec `EnsureSuccessStatusCode()`

```csharp
try
{
    HttpResponseMessage reponse = await client.GetAsync($"{baseUrl}/pokemons/999");

    // Lance une HttpRequestException si le code n'est pas 2xx
    reponse.EnsureSuccessStatusCode();

    Pokemon? pokemon = await reponse.Content.ReadFromJsonAsync<Pokemon>();
    Console.WriteLine($"Trouvé : {pokemon?.Nom}");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine("Pokémon introuvable.");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Erreur HTTP : {ex.StatusCode} — {ex.Message}");
}
catch (TaskCanceledException)
{
    Console.WriteLine("Timeout : le serveur n'a pas répondu à temps.");
}
```

### Les exceptions possibles

| Exception | Cause |
|---|---|
| `HttpRequestException` | Erreur réseau, DNS introuvable, code HTTP d'erreur |
| `TaskCanceledException` | Timeout (le serveur ne répond pas) |
| `JsonException` | Le JSON reçu ne correspond pas au modèle C# |
| `InvalidOperationException` | URL invalide |

---

## Étape 7 — Bonnes pratiques avec `IHttpClientFactory`

### ⚠️ Le problème du `new HttpClient()`

```csharp
// ❌ MAUVAISE PRATIQUE dans une vraie application
using HttpClient client = new();
```

**Pourquoi c'est un problème ?**
- Créer un `HttpClient` par appel cause un **épuisement des sockets** (socket exhaustion).
- Le `using` ferme la connexion trop tôt → pas de réutilisation.

> 💡 Dans un **projet console simple ou un TP**, `new HttpClient()` est acceptable.
> Dans une **API, un service, ou une application** en production, utilise `IHttpClientFactory`.

### Solution : `IHttpClientFactory` (dans une API ou un Worker)

```bash
# Aucun package à installer, c'est inclus dans ASP.NET Core
# Pour un projet console, ajouter :
dotnet add package Microsoft.Extensions.Http
```

#### Configuration dans `Program.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Enregistrer un HttpClient nommé
builder.Services.AddHttpClient("PokemonApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Utiliser le client via la factory
var factory = app.Services.GetRequiredService<IHttpClientFactory>();
using HttpClient client = factory.CreateClient("PokemonApi");

// Maintenant les appels utilisent l'adresse de base
List<Dresseur>? dresseurs = await client.GetFromJsonAsync<List<Dresseur>>("dresseurs");
```

#### Avec l'injection de dépendances (dans un contrôleur ou un service)

```csharp
public class PokemonService
{
    private readonly HttpClient _client;

    // Le HttpClient est injecté automatiquement
    public PokemonService(HttpClient client)
    {
        _client = client;
    }

    public async Task<List<Pokemon>?> GetAllPokemonsAsync()
    {
        return await _client.GetFromJsonAsync<List<Pokemon>>("pokemons");
    }

    public async Task<Pokemon?> GetPokemonAsync(int id)
    {
        return await _client.GetFromJsonAsync<Pokemon>($"pokemons/{id}");
    }
}
```

```csharp
// Dans Program.cs ou Startup
builder.Services.AddHttpClient<PokemonService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/api/");
});
```

### Comparaison des approches

| Approche | Quand l'utiliser |
|---|---|
| `new HttpClient()` | Script, TP, projet console simple |
| `IHttpClientFactory` (nommé) | Application avec plusieurs APIs différentes |
| `IHttpClientFactory` (typé) | Service dédié à une API (la meilleure pratique) |

---

## Étape 8 — Ajouter des headers (authentification, etc.)

### Header simple (API key)

```csharp
client.DefaultRequestHeaders.Add("X-Api-Key", "ma-cle-secrete");
```

### Bearer Token (JWT)

```csharp
client.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "ton-token-jwt-ici");
```

### Headers par requête (pas globaux)

```csharp
var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/dresseurs");
request.Headers.Add("X-Custom-Header", "valeur");

HttpResponseMessage reponse = await client.SendAsync(request);
```

---

## Étape 9 — Envoyer des données complexes

### POST avec un objet (le plus courant)

```csharp
var nouveau = new Pokemon
{
    Nom = "Rondoudou",
    Type = "Normal",
    Niveau = 15,
    DresseurId = 1
};

// Sérialise automatiquement en JSON + ajoute Content-Type: application/json
HttpResponseMessage reponse = await client.PostAsJsonAsync($"{baseUrl}/pokemons", nouveau);
```

### POST avec du JSON brut (si besoin)

```csharp
var json = """
{
    "nom": "Rondoudou",
    "type": "Normal",
    "niveau": 15,
    "dresseurId": 1
}
""";

var contenu = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
HttpResponseMessage reponse = await client.PostAsync($"{baseUrl}/pokemons", contenu);
```

### POST avec un formulaire

```csharp
var formData = new FormUrlEncodedContent(new Dictionary<string, string>
{
    ["username"] = "sacha",
    ["password"] = "pikachu123"
});

HttpResponseMessage reponse = await client.PostAsync("https://example.com/login", formData);
```

---

## Étape 10 — Configurer la sérialisation JSON

### Options globales

```csharp
using System.Text.Json;

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,      // "Name" == "name"
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,  // C# PascalCase → JSON camelCase
    WriteIndented = true,                    // JSON formaté (lisible)
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
};

// Utiliser ces options dans les appels
Pokemon? pokemon = await client.GetFromJsonAsync<Pokemon>(url, options);
```

### Ignorer une propriété

```csharp
public class Pokemon
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";

    [JsonIgnore]  // ← Ne sera pas envoyé / reçu
    public string CacheInterne { get; set; } = "";
}
```

---

## 📋 Récapitulatif des méthodes HttpClient

### Méthodes raccourcies (les plus utilisées)

```csharp
using System.Net.Http.Json;  // ← Toujours ajouter ce using

// ── GET ──
string json            = await client.GetStringAsync(url);
T? objet               = await client.GetFromJsonAsync<T>(url);

// ── POST ──
HttpResponseMessage r  = await client.PostAsJsonAsync(url, objet);

// ── PUT ──
HttpResponseMessage r  = await client.PutAsJsonAsync(url, objet);

// ── DELETE ──
HttpResponseMessage r  = await client.DeleteAsync(url);
```

### Méthodes détaillées (plus de contrôle)

```csharp
// ── GET avec contrôle ──
HttpResponseMessage reponse = await client.GetAsync(url);
reponse.EnsureSuccessStatusCode();
T? objet = await reponse.Content.ReadFromJsonAsync<T>();

// ── POST avec contrôle ──
HttpResponseMessage reponse = await client.PostAsJsonAsync(url, objet);
if (reponse.IsSuccessStatusCode)
{
    T? cree = await reponse.Content.ReadFromJsonAsync<T>();
}

// ── Requête personnalisée ──
var request = new HttpRequestMessage(HttpMethod.Patch, url);
request.Content = JsonContent.Create(objet);
request.Headers.Add("X-Custom", "valeur");
HttpResponseMessage reponse = await client.SendAsync(request);
```

### Arbre de décision

```
  Quel type d'appel ?
  │
  ├── GET simple → GetFromJsonAsync<T>(url)
  │
  ├── GET avec gestion d'erreur
  │   └── GetAsync(url) → vérifier StatusCode → ReadFromJsonAsync<T>()
  │
  ├── POST / PUT → PostAsJsonAsync(url, objet) ou PutAsJsonAsync(url, objet)
  │
  ├── DELETE → DeleteAsync(url)
  │
  └── Requête complexe (headers custom, PATCH…)
      └── new HttpRequestMessage() → SendAsync()
```

---

## ⚠️ Pièges courants

| Piège | Solution |
|---|---|
| `new HttpClient()` dans une boucle | Créer **un seul** `HttpClient` ou utiliser `IHttpClientFactory` |
| Oublier `using System.Net.Http.Json` | Sans ce using, `GetFromJsonAsync` n'existe pas |
| JSON `"name"` vs C# `Name` → `null` | Ajouter `[JsonPropertyName("name")]` ou `PropertyNameCaseInsensitive = true` |
| L'API répond du HTML (page d'erreur) | Vérifier l'URL, ajouter le header `Accept: application/json` |
| Timeout sur `await` sans message | Capturer `TaskCanceledException` |
| Oublier `await` → le programme se termine | Toujours `await` les appels async |
| Certificat HTTPS invalide en dev | Lancer `dotnet dev-certs https --trust` |
| CORS bloqué (depuis un navigateur) | CORS ne concerne **pas** les appels serveur C#, uniquement le navigateur |

---

## 🎯 Checklist

- [ ] `using System.Net.Http.Json;` ajouté en haut du fichier
- [ ] Modèles C# correspondant au JSON de l'API
- [ ] `[JsonPropertyName]` si les noms JSON diffèrent des propriétés C#
- [ ] Gestion des erreurs HTTP (vérifier `IsSuccessStatusCode`)
- [ ] Gestion du timeout (`TaskCanceledException`)
- [ ] `IHttpClientFactory` utilisé en production (pas `new HttpClient()`)
- [ ] Headers d'authentification configurés si nécessaire
- [ ] `await` sur chaque appel async

---

*Bon appel d'API ! 🌐🚀*
