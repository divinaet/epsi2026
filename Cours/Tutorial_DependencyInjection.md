# 💉 L'injection de dépendances en C# — Tutorial complet

> *Comprendre et maîtriser le DI Container de .NET en 20 minutes.*

---

## 🎯 Objectif

Comprendre **pourquoi** et **comment** utiliser l'injection de dépendances (Dependency Injection — DI) en C#, depuis le problème qu'elle résout jusqu'à son utilisation concrète dans une API ASP.NET Core.

---

## Étape 1 — Le problème SANS injection de dépendances

### Un exemple concret

Imaginons un service qui envoie des notifications :

```csharp
public class EmailService
{
    public void Envoyer(string destinataire, string message)
    {
        // Envoie un vrai email via SMTP
        Console.WriteLine($"📧 Email envoyé à {destinataire} : {message}");
    }
}

public class CommandeService
{
    private readonly EmailService _emailService;

    public CommandeService()
    {
        // ❌ Le service CRÉE lui-même sa dépendance
        _emailService = new EmailService();
    }

    public void PasserCommande(string produit, string clientEmail)
    {
        // ... logique de commande ...
        Console.WriteLine($"Commande passée : {produit}");

        _emailService.Envoyer(clientEmail, $"Votre commande de {produit} est confirmée !");
    }
}
```

### Pourquoi c'est un problème ?

```
  CommandeService
  │
  └── new EmailService()     ← Couplage FORT
      └── Connexion SMTP     ← Impossible à tester sans serveur mail
```

| Problème | Conséquence |
|---|---|
| **Couplage fort** | `CommandeService` dépend directement de `EmailService` — impossible de changer |
| **Impossible à tester** | Pour tester `PasserCommande`, il faut un vrai serveur SMTP |
| **Pas flexible** | Si on veut envoyer un SMS au lieu d'un email, il faut modifier `CommandeService` |
| **Responsabilité floue** | Le service décide **comment** créer ses dépendances |

---

## Étape 2 — La solution : Inverser le contrôle

### Le principe (Inversion of Control — IoC)

> **"Ne crée pas tes dépendances toi-même, reçois-les de l'extérieur."**

```
  AVANT (couplage fort)                 APRÈS (injection)
  ───────────────────                   ──────────────────
  CommandeService                       CommandeService
  │                                     │
  └── new EmailService()  ❌            └── INotificationService  ✅
                                            (reçu par le constructeur)
```

### Étape 2a — Définir une interface (contrat)

```csharp
/// <summary>
/// Contrat : "je sais envoyer des notifications".
/// </summary>
public interface INotificationService
{
    void Envoyer(string destinataire, string message);
}
```

### Étape 2b — Implémenter l'interface

```csharp
/// <summary>
/// Implémentation par email.
/// </summary>
public class EmailService : INotificationService
{
    public void Envoyer(string destinataire, string message)
    {
        Console.WriteLine($"📧 Email envoyé à {destinataire} : {message}");
    }
}

/// <summary>
/// Implémentation par SMS.
/// </summary>
public class SmsService : INotificationService
{
    public void Envoyer(string destinataire, string message)
    {
        Console.WriteLine($"📱 SMS envoyé à {destinataire} : {message}");
    }
}
```

### Étape 2c — Injecter via le constructeur

```csharp
public class CommandeService
{
    private readonly INotificationService _notificationService;

    // ✅ La dépendance est INJECTÉE par le constructeur
    public CommandeService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void PasserCommande(string produit, string clientEmail)
    {
        Console.WriteLine($"Commande passée : {produit}");
        _notificationService.Envoyer(clientEmail, $"Votre commande de {produit} est confirmée !");
    }
}
```

### Utilisation manuelle (sans conteneur)

```csharp
// On choisit l'implémentation AU MOMENT DE LA CONSTRUCTION
INotificationService notif = new EmailService();   // ← ou new SmsService()
var service = new CommandeService(notif);

service.PasserCommande("Pokéball", "sacha@pokemon.com");
// → 📧 Email envoyé à sacha@pokemon.com : Votre commande de Pokéball est confirmée !
```

### Le schéma mental

```
  ┌────────────────────┐
  │  INotificationService  │  ← Interface (contrat)
  └──────────┬─────────┘
             │ implémente
     ┌───────┴────────┐
     │                │
  EmailService    SmsService     ← Implémentations concrètes
     │                │
     └───────┬────────┘
             │ est injecté dans
  ┌──────────┴─────────┐
  │  CommandeService   │         ← Consommateur
  └────────────────────┘
```

> 💡 `CommandeService` ne sait **pas** quelle implémentation il utilise.
> Il connaît uniquement le **contrat** (`INotificationService`).
> C'est le principe **D** de SOLID : *Dependency Inversion Principle*.

---

## Étape 3 — Le conteneur DI de .NET

Créer les objets à la main (`new EmailService()` → `new CommandeService(notif)`) fonctionne, mais dans une vraie application avec des dizaines de services, ça devient ingérable.

**Le conteneur DI** gère tout automatiquement :

```
  CONTENEUR DI (.NET)
  ┌───────────────────────────────────────────────┐
  │                                               │
  │  "Quand quelqu'un demande INotificationService"│
  │        → "donne-lui un EmailService"          │
  │                                               │
  │  "Quand quelqu'un demande CommandeService"    │
  │        → "crée-le et injecte ses dépendances" │
  │                                               │
  └───────────────────────────────────────────────┘
```

### Enregistrer les services

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ═══════════════════════════════════════
//  ENREGISTREMENT DES SERVICES
// ═══════════════════════════════════════

// "Quand on demande INotificationService, donne un EmailService"
builder.Services.AddTransient<INotificationService, EmailService>();

// "CommandeService dépend de INotificationService → résoudre automatiquement"
builder.Services.AddTransient<CommandeService>();

var app = builder.Build();

// ═══════════════════════════════════════
//  RÉSOLUTION (le conteneur crée tout)
// ═══════════════════════════════════════
var service = app.Services.GetRequiredService<CommandeService>();
service.PasserCommande("Pokéball", "sacha@pokemon.com");
```

> 💡 Le conteneur voit que `CommandeService` a besoin d'un `INotificationService`
> dans son constructeur, il crée un `EmailService` et l'injecte automatiquement.

---

## Étape 4 — Les durées de vie (Lifetime)

C'est **le concept le plus important** de la DI en .NET.

### Les 3 durées de vie

```csharp
// ── Transient : nouvelle instance À CHAQUE demande ──
builder.Services.AddTransient<INotificationService, EmailService>();

// ── Scoped : une instance PAR REQUÊTE HTTP (ou par scope) ──
builder.Services.AddScoped<INotificationService, EmailService>();

// ── Singleton : UNE SEULE instance pour toute l'application ──
builder.Services.AddSingleton<INotificationService, EmailService>();
```

### Schéma visuel

```
  Requête HTTP 1                    Requête HTTP 2
  ──────────────                    ──────────────

  Transient:
  ├── Instance A                    ├── Instance C
  └── Instance B (re-demandé)      └── Instance D (re-demandé)
      → Nouvelle à CHAQUE fois          → Nouvelle à CHAQUE fois

  Scoped:
  ├── Instance A                    ├── Instance B
  └── Instance A (même requête)     └── Instance B (même requête)
      → UNE par requête                 → UNE par requête

  Singleton:
  ├── Instance A                    ├── Instance A
  └── Instance A                    └── Instance A
      → TOUJOURS la même               → TOUJOURS la même
```

### Quand utiliser quoi ?

| Lifetime | Quand l'utiliser | Exemples typiques |
|---|---|---|
| **Transient** | Service léger, sans état | Validateurs, formatteurs, factories |
| **Scoped** | Service avec état lié à une requête | `DbContext`, Unit of Work, service métier |
| **Singleton** | Service partagé, thread-safe, coûteux à créer | Cache, configuration, `HttpClient`, loggers |

### ⚠️ La règle du Captive Dependency

> **Un service ne peut PAS dépendre d'un service avec une durée de vie plus courte.**

```
  ✅ Transient  → dépend de → Scoped       OK
  ✅ Transient  → dépend de → Singleton    OK
  ✅ Scoped     → dépend de → Singleton    OK

  ❌ Singleton  → dépend de → Scoped       INTERDIT !
  ❌ Singleton  → dépend de → Transient    INTERDIT !
```

Pourquoi ? Un Singleton vit toute la durée de l'application. S'il capture un Scoped (ex: `DbContext`), ce `DbContext` ne sera jamais recréé et causera des bugs.

```
  DURÉE DE VIE (du plus long au plus court)
  ═══════════════════════════════════════════
  Singleton ████████████████████████████████  ← Vit tout le temps
  Scoped    ████████                          ← Vit le temps d'une requête
  Transient ██                                ← Vit un instant

  → Un service peut dépendre de services au-dessus, jamais en-dessous.
```

---

## Étape 5 — DI dans ASP.NET Core (le cas le plus courant)

Dans une API ASP.NET Core, la DI est déjà configurée. Il suffit d'enregistrer ses services.

### `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════
//  ENREGISTREMENT DES SERVICES
// ═══════════════════════════════════════
builder.Services.AddControllers();

// Nos services métier
builder.Services.AddScoped<INotificationService, EmailService>();
builder.Services.AddScoped<CommandeService>();

// Le DbContext (Scoped par défaut avec AddDbContext)
builder.Services.AddDbContext<PokemonDbContext>(options =>
    options.UseSqlite("Data Source=pokemon.db"));

var app = builder.Build();

app.MapControllers();
app.Run();
```

### Injection dans un contrôleur

```csharp
[ApiController]
[Route("api/[controller]")]
public class CommandesController : ControllerBase
{
    private readonly CommandeService _commandeService;

    // ✅ ASP.NET Core injecte automatiquement les dépendances
    public CommandesController(CommandeService commandeService)
    {
        _commandeService = commandeService;
    }

    [HttpPost]
    public IActionResult Passer(string produit, string email)
    {
        _commandeService.PasserCommande(produit, email);
        return Ok("Commande passée !");
    }
}
```

### Ce qui se passe sous le capot

```
  Requête HTTP POST /api/commandes
  │
  ▼
  ASP.NET Core reçoit la requête
  │
  ├── Crée un SCOPE (durée de vie = cette requête)
  │
  ├── Doit instancier CommandesController
  │   └── Constructeur demande CommandeService
  │       └── Constructeur demande INotificationService
  │           └── Résolu → EmailService (new)
  │       └── Résolu → CommandeService(emailService)
  │   └── Résolu → CommandesController(commandeService)
  │
  ├── Exécute l'action Passer()
  │
  └── Fin de requête → le SCOPE est détruit
      └── Tous les services Scoped/Transient sont Disposed
```

---

## Étape 6 — Cas pratiques courants

### Enregistrer plusieurs implémentations

```csharp
// Enregistrer TOUTES les implémentations
builder.Services.AddTransient<INotificationService, EmailService>();
builder.Services.AddTransient<INotificationService, SmsService>();

// Injecter TOUTES les implémentations
public class NotificationManager
{
    private readonly IEnumerable<INotificationService> _services;

    public NotificationManager(IEnumerable<INotificationService> services)
    {
        _services = services; // → [EmailService, SmsService]
    }

    public void NotifierTous(string destinataire, string message)
    {
        foreach (var service in _services)
        {
            service.Envoyer(destinataire, message);
        }
    }
}
```

### Enregistrer avec une factory (logique de création)

```csharp
builder.Services.AddTransient<INotificationService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    string mode = config["NotificationMode"] ?? "email";

    return mode switch
    {
        "sms"   => new SmsService(),
        "email" => new EmailService(),
        _       => new EmailService()
    };
});
```

### Enregistrer une instance existante

```csharp
var config = new AppConfig { NomApp = "PokemonAPI", Version = "1.0" };

// Singleton avec une instance déjà créée
builder.Services.AddSingleton(config);
```

### Enregistrer un service qui s'auto-enregistre (sans interface)

```csharp
// Pas besoin d'interface si le service est concret
builder.Services.AddScoped<CommandeService>();

// Injection directe dans le constructeur
public MyController(CommandeService commandeService) { ... }
```

---

## Étape 7 — Tester grâce à la DI

Le **vrai pouvoir** de la DI : pouvoir remplacer les dépendances dans les tests.

### Créer un faux service (mock)

```csharp
/// <summary>
/// Faux service de notification pour les tests.
/// </summary>
public class FakeNotificationService : INotificationService
{
    public List<(string Destinataire, string Message)> MessagesEnvoyes { get; } = [];

    public void Envoyer(string destinataire, string message)
    {
        // Pas d'email réel → on stocke juste l'appel
        MessagesEnvoyes.Add((destinataire, message));
    }
}
```

### Test unitaire

```csharp
[Fact]
public void PasserCommande_EnvoieUneNotification()
{
    // Arrange — on injecte le FAUX service
    var fakeNotif = new FakeNotificationService();
    var service = new CommandeService(fakeNotif);

    // Act
    service.PasserCommande("Pokéball", "sacha@pokemon.com");

    // Assert — on vérifie que la notification a été envoyée
    Assert.Single(fakeNotif.MessagesEnvoyes);
    Assert.Equal("sacha@pokemon.com", fakeNotif.MessagesEnvoyes[0].Destinataire);
    Assert.Contains("Pokéball", fakeNotif.MessagesEnvoyes[0].Message);
}
```

> 💡 **Sans DI**, il aurait fallu un vrai serveur SMTP pour tester.
> **Avec DI**, on remplace par un faux en une ligne : `new CommandeService(fakeNotif)`.

### Comparaison testabilité

```
  SANS DI                              AVEC DI
  ───────                              ───────
  CommandeService                      CommandeService
  └── new EmailService()               └── INotificationService
      └── Serveur SMTP réel 💥             │
          (impossible en test)             ├── EmailService (prod)
                                           └── FakeService  (test) ✅
```

---

## Étape 8 — Organiser ses services proprement

Dans un vrai projet, on n'écrit pas tout dans `Program.cs`. On utilise des **méthodes d'extension**.

### `Extensions/ServiceCollectionExtensions.cs`

```csharp
namespace MonProjet.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre tous les services métier de l'application.
    /// </summary>
    public static IServiceCollection AddMetierServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, EmailService>();
        services.AddScoped<CommandeService>();
        services.AddScoped<FactureService>();
        services.AddScoped<StockService>();

        return services;
    }
}
```

### `Program.cs` (propre et lisible)

```csharp
using MonProjet.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMetierServices();   // ← Une seule ligne, tout est rangé

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

## 📋 Toutes les méthodes d'enregistrement

| Méthode | Lifetime | Exemple |
|---|---|---|
| `AddTransient<I, C>()` | Transient | `AddTransient<INotif, EmailService>()` |
| `AddScoped<I, C>()` | Scoped | `AddScoped<IRepo, PokemonRepo>()` |
| `AddSingleton<I, C>()` | Singleton | `AddSingleton<ICache, MemoryCache>()` |
| `AddTransient<C>()` | Transient | `AddTransient<CommandeService>()` |
| `AddSingleton(instance)` | Singleton | `AddSingleton(new AppConfig())` |
| `AddTransient<I>(factory)` | Transient | `AddTransient<I>(p => new C(...))` |
| `AddDbContext<T>()` | Scoped | `AddDbContext<PokemonDbContext>(...)` |
| `AddHttpClient<T>()` | Transient* | `AddHttpClient<PokemonClient>(...)` |

> \* `AddHttpClient` enregistre le service en Transient mais le `HttpClient` interne
> est géré par un pool (via `IHttpClientFactory`).

---

## 📐 Les principes SOLID liés à la DI

| Principe | Lien avec la DI |
|---|---|
| **S** — Single Responsibility | Chaque service a une seule responsabilité |
| **O** — Open/Closed | On ajoute de nouvelles implémentations sans modifier le code existant |
| **L** — Liskov Substitution | Toute implémentation de l'interface peut remplacer une autre |
| **I** — Interface Segregation | Des interfaces petites et spécifiques, pas un gros contrat |
| **D** — Dependency Inversion | Dépendre des abstractions (interfaces), pas des implémentations |

```
  ┌─────────────────────────────────────────────────┐
  │                                                 │
  │   "Depend upon abstractions,                    │
  │    do not depend upon concretions."             │
  │                                                 │
  │                    — Robert C. Martin (SOLID)   │
  │                                                 │
  └─────────────────────────────────────────────────┘
```

---

## ⚠️ Pièges courants

| Piège | Problème | Solution |
|---|---|---|
| `new MonService()` dans un contrôleur | Court-circuite la DI | Toujours injecter via le constructeur |
| Singleton qui dépend d'un Scoped | Captive Dependency → `DbContext` jamais recréé | Respecter la hiérarchie des lifetimes |
| Service non enregistré | `InvalidOperationException` au runtime | Vérifier l'enregistrement dans `Program.cs` |
| Interface oubliée | Couplage fort, pas testable | Extraire une interface pour chaque dépendance |
| Trop de paramètres dans le constructeur (> 4-5) | Service avec trop de responsabilités | Découper en services plus petits |
| `IServiceProvider` injecté partout | Anti-pattern "Service Locator" | Injecter directement les services nécessaires |
| Travail lourd dans le constructeur | Ralentit la résolution DI | Déplacer la logique dans les méthodes |

### L'anti-pattern Service Locator

```csharp
// ❌ MAUVAIS — Service Locator (anti-pattern)
public class CommandeService
{
    private readonly IServiceProvider _provider;

    public CommandeService(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void PasserCommande(string produit)
    {
        // Le service résout ses dépendances lui-même → impossible à tester facilement
        var notif = _provider.GetRequiredService<INotificationService>();
        notif.Envoyer("...", "...");
    }
}

// ✅ BON — Injection directe
public class CommandeService
{
    private readonly INotificationService _notificationService;

    public CommandeService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
}
```

---

## 🎯 Checklist

- [ ] Les dépendances sont injectées par le **constructeur** (pas `new`)
- [ ] Les services dépendent d'**interfaces**, pas de classes concrètes
- [ ] Chaque service est enregistré dans `Program.cs` (ou via une méthode d'extension)
- [ ] Le **lifetime** est choisi correctement (Transient / Scoped / Singleton)
- [ ] Pas de Singleton qui dépend d'un Scoped
- [ ] Pas d'utilisation directe de `IServiceProvider` (sauf cas avancés)
- [ ] Les services sont **testables** grâce à l'injection de faux (mocks)
- [ ] Les constructeurs n'ont pas plus de 4-5 paramètres

---

*Bonne injection ! 💉🚀*
