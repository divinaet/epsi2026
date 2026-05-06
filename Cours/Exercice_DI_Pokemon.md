# 🏋️ Exercice — Injection de dépendances : Le Centre Pokémon

> *Le Professeur Chen a besoin de toi pour moderniser le Centre Pokémon.*
> *L'Infirmière Joëlle en a marre de tout faire à la main avec des `new`.*

---

## 📖 Scénario

Le **Centre Pokémon** de Bourg Palette propose plusieurs services :

1. **Soigner** les pokémon blessés
2. **Stocker** les pokémon dans le PC
3. **Notifier** les dresseurs quand leur pokémon est prêt

Le problème ? L'ancien développeur (Team Rocket, évidemment) a tout codé avec des `new` partout.
Le code est **intestable**, **rigide**, et l'Infirmière Joëlle ne peut même pas changer de système de notification sans recompiler.

**Ta mission : refactorer le Centre Pokémon avec l'injection de dépendances.**

---

## 🏗️ Étape 0 — Créer la solution

```bash
mkdir CentrePokemon && cd CentrePokemon
dotnet new sln -n CentrePokemon

# Projet principal (Class Library)
dotnet new classlib -n CentrePokemon.Core -f net10.0

# Projet de tests ReqNroll (Gherkin)
dotnet new reqnroll-project -n CentrePokemon.Tests -f net10.0

# Ajouter les projets à la solution
dotnet sln add CentrePokemon.Core/CentrePokemon.Core.csproj
dotnet sln add CentrePokemon.Tests/CentrePokemon.Tests.csproj

# Référencer Core dans les Tests
dotnet add CentrePokemon.Tests reference CentrePokemon.Core
```

> 💡 Si le template `reqnroll-project` n'est pas installé :
> ```bash
> dotnet new install ReqNroll.Templates
> ```

Supprime les fichiers générés par défaut (`Class1.cs`, etc.).

---

## 🧱 Étape 1 — Le modèle (fourni)

Crée le fichier `CentrePokemon.Core/Models/Pokemon.cs` :

```csharp
namespace CentrePokemon.Core.Models;

public class Pokemon
{
    public required string Nom { get; set; }
    public required string Type { get; set; }
    public int PointsDeVie { get; set; }
    public int PointsDeVieMax { get; set; }
    public required string NomDresseur { get; set; }

    /// <summary>Le pokémon est KO si ses PV sont à 0.</summary>
    public bool EstKO => PointsDeVie <= 0;

    /// <summary>Le pokémon est en pleine forme si PV == PV Max.</summary>
    public bool EstEnPleineForme => PointsDeVie >= PointsDeVieMax;
}
```

---

## 💀 Étape 2 — Le code "Team Rocket" (le MAUVAIS code)

Voici le code que tu dois **refactorer**. Lis-le, comprends les problèmes, puis passe à l'étape 3.

```csharp
// ❌ NE PAS REPRODUIRE — C'est l'exemple de ce qu'il NE FAUT PAS faire

namespace CentrePokemon.Core;

public class CentrePokemonService
{
    public void Soigner(Pokemon pokemon)
    {
        // Soigne toujours avec une Potion classique (pas flexible)
        pokemon.PointsDeVie = Math.Min(pokemon.PointsDeVie + 20, pokemon.PointsDeVieMax);

        // Crée le logger à la main (couplage fort)
        var logger = new ConsoleLogger();
        logger.Log($"🏥 {pokemon.Nom} a été soigné. PV: {pokemon.PointsDeVie}/{pokemon.PointsDeVieMax}");

        // Crée le notifieur à la main (impossible à tester)
        var notifieur = new EmailNotifieur();
        notifieur.Notifier(pokemon.NomDresseur, $"{pokemon.Nom} est prêt !");

        // Crée le stockage à la main (dépend d'une vraie BDD)
        var stockage = new SqliteStockage();
        stockage.Sauvegarder(pokemon);
    }
}
```

### ❓ Questions préliminaires (réfléchis avant de coder)

1. Combien de dépendances `CentrePokemonService` crée-t-il lui-même ?
2. Que se passe-t-il si on veut remplacer `EmailNotifieur` par un `SmsNotifieur` ?
3. Comment tester `Soigner()` sans envoyer de vrais emails ?
4. La quantité de PV soignés est codée en dur (`+20`). Comment rendre ça configurable ?

<details>
<summary>🔑 Réponses</summary>

1. **3 dépendances** : `ConsoleLogger`, `EmailNotifieur`, `SqliteStockage` — toutes créées avec `new`.
2. Il faut **modifier le code source** de `CentrePokemonService` → violation du principe Open/Closed.
3. **Impossible** sans un vrai serveur mail → le code est **intestable**.
4. Il faudrait une interface `ISoinStrategy` avec différentes implémentations (Potion, SuperPotion, etc.).

</details>

---

## ✅ Étape 3 — À toi de jouer ! Crée les interfaces

Crée les **4 interfaces** suivantes dans `CentrePokemon.Core/Interfaces/` :

### 3.1 — `ILogger`

```
Méthode : void Log(string message)
Rôle    : Journaliser les événements du centre
```

### 3.2 — `INotifieur`

```
Méthode : void Notifier(string dresseur, string message)
Rôle    : Prévenir le dresseur que son pokémon est prêt
```

### 3.3 — `IStockage`

```
Méthode : void Sauvegarder(Pokemon pokemon)
Méthode : Pokemon? Recuperer(string nom)
Rôle    : Stocker les pokémon (PC de Léo)
```

### 3.4 — `ISoinStrategy`

```
Propriété : string Nom { get; }              → Nom du soin (ex: "Potion")
Propriété : int PvRendus { get; }            → Quantité de PV restaurés
Méthode   : void Appliquer(Pokemon pokemon)  → Applique le soin
```

<details>
<summary>🔑 Solution — Interfaces</summary>

```csharp
// Interfaces/ILogger.cs
namespace CentrePokemon.Core.Interfaces;

public interface ILogger
{
    void Log(string message);
}

// Interfaces/INotifieur.cs
namespace CentrePokemon.Core.Interfaces;

public interface INotifieur
{
    void Notifier(string dresseur, string message);
}

// Interfaces/IStockage.cs
using CentrePokemon.Core.Models;

namespace CentrePokemon.Core.Interfaces;

public interface IStockage
{
    void Sauvegarder(Pokemon pokemon);
    Pokemon? Recuperer(string nom);
}

// Interfaces/ISoinStrategy.cs
using CentrePokemon.Core.Models;

namespace CentrePokemon.Core.Interfaces;

public interface ISoinStrategy
{
    string Nom { get; }
    int PvRendus { get; }
    void Appliquer(Pokemon pokemon);
}
```

</details>

---

## ✅ Étape 4 — Crée les implémentations

Dans `CentrePokemon.Core/Services/` :

### 4.1 — Implémentations de `ISoinStrategy`

Crée **3 stratégies de soin** :

| Classe | Nom | PV rendus | Comportement |
|---|---|---|---|
| `Potion` | "Potion" | 20 | Rend 20 PV (sans dépasser le max) |
| `SuperPotion` | "Super Potion" | 50 | Rend 50 PV (sans dépasser le max) |
| `RappelTotal` | "Rappel Total" | ∞ | Remet les PV au maximum (même si KO) |

### 4.2 — Implémentation de `ILogger`

| Classe | Comportement |
|---|---|
| `ConsoleLogger` | Écrit dans `Console.WriteLine` avec un horodatage |

### 4.3 — Implémentation de `INotifieur`

| Classe | Comportement |
|---|---|
| `PokemonCenterNotifieur` | Écrit `"📢 [dresseur] : [message]"` dans la console |

### 4.4 — Implémentation de `IStockage`

| Classe | Comportement |
|---|---|
| `StockageEnMemoire` | Stocke les pokémon dans un `Dictionary<string, Pokemon>` |

<details>
<summary>🔑 Solution — Implémentations</summary>

```csharp
// Services/Potion.cs
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Models;

namespace CentrePokemon.Core.Services;

public class Potion : ISoinStrategy
{
    public string Nom => "Potion";
    public int PvRendus => 20;

    public void Appliquer(Pokemon pokemon)
    {
        pokemon.PointsDeVie = Math.Min(pokemon.PointsDeVie + PvRendus, pokemon.PointsDeVieMax);
    }
}

// Services/SuperPotion.cs
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Models;

namespace CentrePokemon.Core.Services;

public class SuperPotion : ISoinStrategy
{
    public string Nom => "Super Potion";
    public int PvRendus => 50;

    public void Appliquer(Pokemon pokemon)
    {
        pokemon.PointsDeVie = Math.Min(pokemon.PointsDeVie + PvRendus, pokemon.PointsDeVieMax);
    }
}

// Services/RappelTotal.cs
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Models;

namespace CentrePokemon.Core.Services;

public class RappelTotal : ISoinStrategy
{
    public string Nom => "Rappel Total";
    public int PvRendus => int.MaxValue;

    public void Appliquer(Pokemon pokemon)
    {
        pokemon.PointsDeVie = pokemon.PointsDeVieMax;
    }
}

// Services/ConsoleLogger.cs
using CentrePokemon.Core.Interfaces;

namespace CentrePokemon.Core.Services;

public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}

// Services/PokemonCenterNotifieur.cs
using CentrePokemon.Core.Interfaces;

namespace CentrePokemon.Core.Services;

public class PokemonCenterNotifieur : INotifieur
{
    public void Notifier(string dresseur, string message)
    {
        Console.WriteLine($"📢 {dresseur} : {message}");
    }
}

// Services/StockageEnMemoire.cs
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Models;

namespace CentrePokemon.Core.Services;

public class StockageEnMemoire : IStockage
{
    private readonly Dictionary<string, Pokemon> _pc = [];

    public void Sauvegarder(Pokemon pokemon)
    {
        _pc[pokemon.Nom] = pokemon;
    }

    public Pokemon? Recuperer(string nom)
    {
        return _pc.GetValueOrDefault(nom);
    }
}
```

</details>

---

## ✅ Étape 5 — Refactore `CentrePokemonService`

Crée `CentrePokemon.Core/Services/CentrePokemonService.cs`.

**Consignes :**
- Toutes les dépendances doivent être **injectées par le constructeur**
- Le constructeur prend : `ISoinStrategy`, `ILogger`, `INotifieur`, `IStockage`
- La méthode `Soigner(Pokemon pokemon)` :
  1. Applique le soin via `ISoinStrategy`
  2. Log le résultat via `ILogger`
  3. Notifie le dresseur via `INotifieur`
  4. Sauvegarde le pokémon via `IStockage`
- La méthode retourne un `ResultatSoin` (voir ci-dessous)

### Modèle `ResultatSoin`

```csharp
// Models/ResultatSoin.cs
namespace CentrePokemon.Core.Models;

public class ResultatSoin
{
    public required string NomPokemon { get; set; }
    public required string SoinUtilise { get; set; }
    public int PvAvant { get; set; }
    public int PvApres { get; set; }
    public bool EstEnPleineForme { get; set; }
}
```

<details>
<summary>🔑 Solution — CentrePokemonService</summary>

```csharp
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Models;

namespace CentrePokemon.Core.Services;

public class CentrePokemonService
{
    private readonly ISoinStrategy _soin;
    private readonly ILogger _logger;
    private readonly INotifieur _notifieur;
    private readonly IStockage _stockage;

    public CentrePokemonService(
        ISoinStrategy soin,
        ILogger logger,
        INotifieur notifieur,
        IStockage stockage)
    {
        _soin = soin;
        _logger = logger;
        _notifieur = notifieur;
        _stockage = stockage;
    }

    public ResultatSoin Soigner(Pokemon pokemon)
    {
        int pvAvant = pokemon.PointsDeVie;

        // 1. Appliquer le soin
        _soin.Appliquer(pokemon);

        // 2. Logger
        _logger.Log(
            $"🏥 {pokemon.Nom} soigné avec {_soin.Nom}. " +
            $"PV: {pvAvant} → {pokemon.PointsDeVie}/{pokemon.PointsDeVieMax}");

        // 3. Notifier le dresseur
        string message = pokemon.EstEnPleineForme
            ? $"{pokemon.Nom} est en pleine forme ! Prêt au combat !"
            : $"{pokemon.Nom} a été soigné mais n'est pas encore à 100%.";

        _notifieur.Notifier(pokemon.NomDresseur, message);

        // 4. Sauvegarder
        _stockage.Sauvegarder(pokemon);

        return new ResultatSoin
        {
            NomPokemon = pokemon.Nom,
            SoinUtilise = _soin.Nom,
            PvAvant = pvAvant,
            PvApres = pokemon.PointsDeVie,
            EstEnPleineForme = pokemon.EstEnPleineForme
        };
    }
}
```

</details>

---

## ✅ Étape 6 — Écrire les tests en Gherkin (ReqNroll)

C'est là que la DI montre sa puissance ! On va injecter des **faux services** pour tester sans console, sans email, sans base de données.

### 6.1 — Crée les faux services pour les tests

Crée `CentrePokemon.Tests/Fakes/` :

```csharp
// Fakes/FakeLogger.cs
using CentrePokemon.Core.Interfaces;

namespace CentrePokemon.Tests.Fakes;

/// <summary>
/// Faux logger qui stocke les messages au lieu de les afficher.
/// Parfait pour vérifier qu'un log a bien été écrit.
/// </summary>
public class FakeLogger : ILogger
{
    public List<string> Messages { get; } = [];

    public void Log(string message)
    {
        Messages.Add(message);
    }
}

// Fakes/FakeNotifieur.cs
using CentrePokemon.Core.Interfaces;

namespace CentrePokemon.Tests.Fakes;

/// <summary>
/// Faux notifieur qui stocke les notifications.
/// L'Infirmière Joëlle n'embête plus personne pendant les tests.
/// </summary>
public class FakeNotifieur : INotifieur
{
    public List<(string Dresseur, string Message)> Notifications { get; } = [];

    public void Notifier(string dresseur, string message)
    {
        Notifications.Add((dresseur, message));
    }
}

// Fakes/FakeStockage.cs
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Models;

namespace CentrePokemon.Tests.Fakes;

/// <summary>
/// Faux PC de Léo — stocke en mémoire sans SQLite.
/// Léo peut enfin dormir tranquille.
/// </summary>
public class FakeStockage : IStockage
{
    public Dictionary<string, Pokemon> Donnees { get; } = [];

    public void Sauvegarder(Pokemon pokemon)
    {
        Donnees[pokemon.Nom] = pokemon;
    }

    public Pokemon? Recuperer(string nom)
    {
        return Donnees.GetValueOrDefault(nom);
    }
}
```

### 6.2 — Écris les scénarios Gherkin

Crée `CentrePokemon.Tests/Features/CentrePokemon.feature` :

```gherkin
# language: fr
Fonctionnalité: Centre Pokémon — Soigner les pokémon
    En tant qu'Infirmière Joëlle
    Je veux soigner les pokémon des dresseurs
    Afin que ces petites bêtes retournent se battre (ah, la violence…)

    Contexte:
        Étant donné le pokémon suivant
            | Nom      | Type     | PV  | PV Max | Dresseur |
            | Pikachu  | Électrik | 10  | 100    | Sacha    |

    Scénario: Soigner un pokémon avec une Potion
        Étant donné que l'Infirmière Joëlle utilise une "Potion"
        Quand elle soigne le pokémon
        Alors les PV du pokémon sont 30
        Et le pokémon n'est pas en pleine forme
        Et le dresseur "Sacha" est notifié que son pokémon a été soigné

    Scénario: Soigner un pokémon avec une Super Potion
        Étant donné que l'Infirmière Joëlle utilise une "Super Potion"
        Quand elle soigne le pokémon
        Alors les PV du pokémon sont 60
        Et le pokémon n'est pas en pleine forme

    Scénario: Rappel Total sur un pokémon KO
        Étant donné le pokémon suivant
            | Nom       | Type | PV | PV Max | Dresseur |
            | Dracaufeu | Feu  | 0  | 150    | Sacha    |
        Et que l'Infirmière Joëlle utilise un "Rappel Total"
        Quand elle soigne le pokémon
        Alors les PV du pokémon sont 150
        Et le pokémon est en pleine forme
        Et le dresseur "Sacha" est notifié que son pokémon est prêt au combat

    Scénario: Potion sur un pokémon presque full
        Étant donné le pokémon suivant
            | Nom      | Type     | PV | PV Max | Dresseur |
            | Pikachu  | Électrik | 95 | 100    | Sacha    |
        Et que l'Infirmière Joëlle utilise une "Potion"
        Quand elle soigne le pokémon
        Alors les PV du pokémon sont 100
        Et le pokémon est en pleine forme
        Et l'infirmière a loggé le soin

    Scénario: Le pokémon est sauvegardé dans le PC après le soin
        Étant donné que l'Infirmière Joëlle utilise une "Potion"
        Quand elle soigne le pokémon
        Alors le pokémon "Pikachu" est sauvegardé dans le PC
```

### 6.3 — Implémente les Step Definitions

Crée `CentrePokemon.Tests/Steps/CentrePokemonSteps.cs`.

**Consignes :**
- Utilise les **faux services** créés à l'étape 6.1
- Instancie `CentrePokemonService` en **injectant** les fakes
- Stocke le `Pokemon` et le `ResultatSoin` dans des champs pour les assertions

<details>
<summary>🔑 Solution — Step Definitions</summary>

```csharp
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Models;
using CentrePokemon.Core.Services;
using CentrePokemon.Tests.Fakes;

namespace CentrePokemon.Tests.Steps;

[Binding]
public class CentrePokemonSteps
{
    // ── Fakes (nos doublures de test) ──
    private readonly FakeLogger _fakeLogger = new();
    private readonly FakeNotifieur _fakeNotifieur = new();
    private readonly FakeStockage _fakeStockage = new();

    // ── État du scénario ──
    private Pokemon _pokemon = null!;
    private ISoinStrategy _soinStrategy = null!;
    private ResultatSoin _resultat = null!;

    // ═══════════════════════════════════════
    //  GIVEN — Étant donné
    // ═══════════════════════════════════════

    [Given(@"le pokémon suivant")]
    public void EtantDonneLePokemonSuivant(Table table)
    {
        var row = table.Rows[0];
        _pokemon = new Pokemon
        {
            Nom = row["Nom"],
            Type = row["Type"],
            PointsDeVie = int.Parse(row["PV"]),
            PointsDeVieMax = int.Parse(row["PV Max"]),
            NomDresseur = row["Dresseur"]
        };
    }

    [Given(@"que l'Infirmière Joëlle utilise une? ""(.*)""")]
    public void EtantDonneQueLInfirmiereUtilise(string nomSoin)
    {
        _soinStrategy = nomSoin switch
        {
            "Potion" => new Potion(),
            "Super Potion" => new SuperPotion(),
            "Rappel Total" => new RappelTotal(),
            _ => throw new ArgumentException($"Soin inconnu : {nomSoin}. Team Rocket a encore frappé !")
        };
    }

    // ═══════════════════════════════════════
    //  WHEN — Quand
    // ═══════════════════════════════════════

    [When(@"elle soigne le pokémon")]
    public void QuandElleSoigneLePokemon()
    {
        // ✅ Injection des fakes dans le service
        var service = new CentrePokemonService(
            _soinStrategy,
            _fakeLogger,
            _fakeNotifieur,
            _fakeStockage
        );

        _resultat = service.Soigner(_pokemon);
    }

    // ═══════════════════════════════════════
    //  THEN — Alors
    // ═══════════════════════════════════════

    [Then(@"les PV du pokémon sont (.*)")]
    public void AlorsLesPVSont(int pvAttendus)
    {
        Assert.Equal(pvAttendus, _pokemon.PointsDeVie);
    }

    [Then(@"le pokémon est en pleine forme")]
    public void AlorsLePokemonEstEnPleineForme()
    {
        Assert.True(_resultat.EstEnPleineForme,
            "Le pokémon devrait être en pleine forme mais ne l'est pas. Encore un coup de la Team Rocket !");
    }

    [Then(@"le pokémon n'est pas en pleine forme")]
    public void AlorsLePokemonNEstPasEnPleineForme()
    {
        Assert.False(_resultat.EstEnPleineForme,
            "Le pokémon ne devrait pas être en pleine forme. Il a besoin de plus de soins !");
    }

    [Then(@"le dresseur ""(.*)"" est notifié que son pokémon a été soigné")]
    public void AlorsLeDresseurEstNotifieSoin(string dresseur)
    {
        Assert.Contains(_fakeNotifieur.Notifications,
            n => n.Dresseur == dresseur && n.Message.Contains("soigné"));
    }

    [Then(@"le dresseur ""(.*)"" est notifié que son pokémon est prêt au combat")]
    public void AlorsLeDresseurEstNotifiePret(string dresseur)
    {
        Assert.Contains(_fakeNotifieur.Notifications,
            n => n.Dresseur == dresseur && n.Message.Contains("pleine forme"));
    }

    [Then(@"l'infirmière a loggé le soin")]
    public void AlorsLInfirmiereALogge()
    {
        Assert.NotEmpty(_fakeLogger.Messages);
        Assert.Contains(_fakeLogger.Messages, m => m.Contains("soigné"));
    }

    [Then(@"le pokémon ""(.*)"" est sauvegardé dans le PC")]
    public void AlorsLePokemonEstDansLePC(string nomPokemon)
    {
        Assert.True(_fakeStockage.Donnees.ContainsKey(nomPokemon),
            $"{nomPokemon} n'est pas dans le PC ! Léo a encore fait des siennes.");
    }
}
```

</details>

---

## ✅ Étape 7 — Lancer les tests

```bash
dotnet test
```

**Résultat attendu :**
```
  ✅ Soigner un pokémon avec une Potion
  ✅ Soigner un pokémon avec une Super Potion
  ✅ Rappel Total sur un pokémon KO
  ✅ Potion sur un pokémon presque full
  ✅ Le pokémon est sauvegardé dans le PC après le soin

Test Run Successful.
Total tests: 5
     Passed: 5
```

---

## 🌟 Étape 8 — BONUS : Configurer le conteneur DI

Maintenant que tout fonctionne avec de l'injection manuelle, branche le **vrai conteneur DI de .NET**.

Crée `CentrePokemon.Core/Extensions/ServiceCollectionExtensions.cs` :

```csharp
using CentrePokemon.Core.Interfaces;
using CentrePokemon.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CentrePokemon.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre tous les services du Centre Pokémon.
    /// L'Infirmière Joëlle approuve cette configuration.
    /// </summary>
    public static IServiceCollection AddCentrePokemon(this IServiceCollection services)
    {
        services.AddTransient<ISoinStrategy, Potion>();        // Soin par défaut
        services.AddSingleton<ILogger, ConsoleLogger>();       // Un seul logger
        services.AddSingleton<INotifieur, PokemonCenterNotifieur>();
        services.AddSingleton<IStockage, StockageEnMemoire>(); // Un seul PC
        services.AddTransient<CentrePokemonService>();

        return services;
    }
}
```

**Question bonus :** Pourquoi `IStockage` est en Singleton et `ISoinStrategy` en Transient ?

<details>
<summary>🔑 Réponse</summary>

- `IStockage` (Singleton) → Le PC de Léo est **unique**. Tous les services partagent le même stockage, sinon chacun aurait son propre dictionnaire et les pokémon se perdraient.
- `ISoinStrategy` (Transient) → Chaque soin est **indépendant**. On pourrait vouloir changer de stratégie selon le contexte (Potion pour les blessures légères, Rappel Total pour les KO).

</details>

---

## 📐 Structure finale

```
CentrePokemon/
├── CentrePokemon.sln
│
├── CentrePokemon.Core/
│   ├── CentrePokemon.Core.csproj
│   ├── Interfaces/
│   │   ├── ILogger.cs
│   │   ├── INotifieur.cs
│   │   ├── IStockage.cs
│   │   └── ISoinStrategy.cs
│   ├── Models/
│   │   ├── Pokemon.cs
│   │   └── ResultatSoin.cs
│   ├── Services/
│   │   ├── CentrePokemonService.cs
│   │   ├── ConsoleLogger.cs
│   │   ├── PokemonCenterNotifieur.cs
│   │   ├── StockageEnMemoire.cs
│   │   ├── Potion.cs
│   │   ├── SuperPotion.cs
│   │   └── RappelTotal.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
│
└── CentrePokemon.Tests/
    ├── CentrePokemon.Tests.csproj
    ├── Fakes/
    │   ├── FakeLogger.cs
    │   ├── FakeNotifieur.cs
    │   └── FakeStockage.cs
    ├── Features/
    │   └── CentrePokemon.feature
    └── Steps/
        └── CentrePokemonSteps.cs
```

---

## 🧠 Ce que tu as appris

| Concept | Où dans l'exercice |
|---|---|
| **Interface = contrat** | `ILogger`, `INotifieur`, `IStockage`, `ISoinStrategy` |
| **Injection par constructeur** | `CentrePokemonService` reçoit ses 4 dépendances |
| **Principe Open/Closed** | Ajouter un `HyperPotion` sans modifier `CentrePokemonService` |
| **Strategy Pattern** | `ISoinStrategy` → `Potion`, `SuperPotion`, `RappelTotal` |
| **Testabilité** | Fakes injectés dans les tests → pas de console, pas de BDD |
| **Gherkin / ReqNroll** | Scénarios lisibles en français, step definitions en C# |
| **Conteneur DI** | `AddCentrePokemon()` enregistre tout automatiquement |
| **Lifetimes** | Singleton pour le stockage, Transient pour les soins |

---

## 🚀 Défis supplémentaires (si tu veux aller plus loin)

1. **Ajouter un `HyperPotion`** (rend 200 PV) — tu ne dois modifier QUE les tests et ajouter une classe. Pas toucher à `CentrePokemonService`.

2. **Scénario Gherkin `Scenario Outline`** — refactore les scénarios de soin en un seul avec des exemples :
    ```gherkin
    Plan du Scénario: Soigner avec différents objets
        Étant donné le pokémon suivant
            | Nom     | Type     | PV   | PV Max | Dresseur |
            | Pikachu | Électrik | <PV> | 100    | Sacha    |
        Et que l'Infirmière Joëlle utilise une "<Soin>"
        Quand elle soigne le pokémon
        Alors les PV du pokémon sont <PV Résultat>

        Exemples:
            | PV | Soin          | PV Résultat |
            | 10 | Potion        | 30          |
            | 10 | Super Potion  | 60          |
            | 0  | Rappel Total  | 100         |
            | 95 | Potion        | 100         |
    ```

3. **Injecter `IEnumerable<ISoinStrategy>`** — crée un `CentrePokemonAvance` qui choisit automatiquement le meilleur soin selon les PV manquants.

4. **Ajouter un scénario d'erreur** :
    ```gherkin
    Scénario: Impossible de soigner un pokémon déjà en pleine forme
        Étant donné le pokémon suivant
            | Nom     | Type     | PV  | PV Max | Dresseur |
            | Pikachu | Électrik | 100 | 100    | Sacha    |
        Et que l'Infirmière Joëlle utilise une "Potion"
        Quand elle essaie de soigner le pokémon
        Alors l'infirmière dit "Pikachu est déjà en pleine forme, pas besoin de gâcher une Potion !"
    ```

---

*Que l'injection de dépendances soit avec toi, dresseur ! 💉⚡*
