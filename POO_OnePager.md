# 🎓 POO en C# — One Pager

> *Tout ce qu'il faut savoir sur les classes et interfaces, en une page.*

---

## 1. La Classe — le plan de construction

Une classe définit **ce qu'un objet EST** et **ce qu'il POSSÈDE**.

```csharp
public class Vehicule
{
    // ── Propriété : une donnée de l'objet ──
    public string Nom { get; set; }
    public int Vitesse { get; private set; }  // private set = modifiable uniquement depuis la classe

    // ── Constructeur : initialise l'objet à la création ──
    public Vehicule(string nom)
    {
        Nom = nom;
        Vitesse = 0;
    }

    // ── Méthode : un comportement de l'objet ──
    public void Accelerer()
    {
        Vitesse += 10;
    }
}

// Utilisation :
var v = new Vehicule("DeLorean");  // Instanciation
v.Accelerer();                      // Appel de méthode
Console.WriteLine(v.Vitesse);       // 10
```

---

## 2. Les 4 piliers de la POO

| Pilier | En une phrase | Exemple |
|---|---|---|
| **Encapsulation** | Cacher les détails internes, exposer le nécessaire | `private set` sur Vitesse |
| **Abstraction** | Simplifier en ne montrant que l'essentiel | Une interface `IVehicule` cache la complexité |
| **Héritage** | Une classe enfant réutilise le code d'un parent | `class Voiture : Vehicule` |
| **Polymorphisme** | Un même appel, des comportements différents | `vehicule.Accelerer()` → chaque véhicule accélère à sa manière |

---

## 3. L'Héritage — réutiliser du code

```csharp
public class Voiture : Vehicule          // Voiture HÉRITE de Vehicule
{
    public int NbPortes { get; set; }

    public Voiture(string nom, int portes) : base(nom)  // Appelle le constructeur parent
    {
        NbPortes = portes;
    }
}
```

⚠️ **Limite** : en C#, une classe ne peut hériter que d'**une seule** classe parente.

---

## 4. L'Interface — le contrat de comportement

Une interface définit **ce qu'un objet SAIT FAIRE**, sans dire comment.

```csharp
public interface IVolant          // Convention : préfixe "I"
{
    bool EstEnVol { get; }        // Propriété à implémenter
    void Decoller();              // Méthode à implémenter (pas de code ici !)
}
```

Une classe qui **implémente** l'interface **s'engage** à fournir le code :

```csharp
public class DeLorean : Vehicule, IVolant    // Héritage + Interface
{
    public bool EstEnVol { get; private set; }

    public void Decoller()                    // Obligation d'écrire cette méthode
    {
        EstEnVol = true;
    }
}
```

---

## 5. Pourquoi les interfaces ? — Les 3 superpouvoirs

### 🔌 Superpouvoir 1 : Implémentation multiple

```csharp
// INTERDIT : hériter de 2 classes
class DeLorean : Vehicule, VaisseauSpatial { }  // ❌ Erreur !

// AUTORISÉ : implémenter N interfaces
class DeLorean : IVehicule, IVolant, ITurboBoost { }  // ✅
```

### 🎭 Superpouvoir 2 : Polymorphisme par interface

```csharp
// Des objets de types DIFFÉRENTS dans une seule liste !
List<IVehicule> participants =
[
    new DeLorean(),          // IVehicule + IVolant + ITurboBoost
    new MarioKart(),         // IVehicule + ITurboBoost + IArmement
    new VeloDeDeveloppeur()  // IVehicule uniquement
];

// On peut tous les traiter de la même façon :
foreach (var v in participants)
{
    v.Accelerer();  // Chaque véhicule accélère à SA manière
}
```

### 🔍 Superpouvoir 3 : Tester les capacités à l'exécution

```csharp
foreach (var vehicule in participants)
{
    if (vehicule is IVolant volant)        // "Est-ce que tu sais voler ?"
    {
        volant.Decoller();                  // Oui → on utilise cette capacité
    }

    if (vehicule is ITurboBoost turbo)     // "Est-ce que tu as un turbo ?"
    {
        turbo.ActiverTurbo();
    }

    // Le VeloDeDeveloppeur ne rentre dans aucun if → pas grave, pas d'erreur !
}
```

---

## 6. Classe vs Interface — Le résumé

```
┌─────────────────────────┬──────────────────────────────────────┐
│        CLASSE           │           INTERFACE                  │
├─────────────────────────┼──────────────────────────────────────┤
│ Définit ce qu'un        │ Définit ce qu'un objet              │
│ objet EST               │ SAIT FAIRE                          │
│                         │                                      │
│ Contient du code        │ Ne contient PAS de code             │
│ (implémentation)        │ (juste la signature)                │
│                         │                                      │
│ Héritage simple         │ Implémentation multiple             │
│ (1 seul parent)         │ (autant qu'on veut)                 │
│                         │                                      │
│ Peut être instanciée    │ Ne peut PAS être instanciée         │
│ new Vehicule()  ✅      │ new IVehicule()  ❌                 │
│                         │                                      │
│ Mot-clé : class         │ Mot-clé : interface                 │
└─────────────────────────┴──────────────────────────────────────┘
```

---

## 7. Règle d'or 🏆

> **Programmez contre l'abstraction (interface), pas contre l'implémentation (classe).**
>
> La classe `Course` ne connaît que `IVehicule`, `IVolant`, `ITurboBoost`...
> Elle ne sait pas ce qu'est une `DeLorean`. Et c'est **exactement** ce qu'on veut.
> → On peut ajouter un nouveau véhicule **sans modifier** `Course.cs`.

---

*TP associé : Grand Prix Intergalactique des Geeks 🏁*
