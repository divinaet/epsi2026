# 📦 Créer et publier sa bibliothèque NuGet — Tutorial complet

> *De zéro au `dotnet add package TonPackage` en 15 minutes.*

---

## 🎯 Objectif

Créer une bibliothèque C# réutilisable, la packager en `.nupkg`, et la publier sur **nuget.org** pour que n'importe qui puisse l'installer.

---

## Étape 1 — Créer le projet bibliothèque

```bash
# Créer un dossier pour la solution
mkdir MaSuperLib && cd MaSuperLib

# Créer la bibliothèque (Class Library, PAS une Console App)
dotnet new classlib -n MaSuperLib -f net10.0

# Créer un projet de tests (bonne pratique !)
dotnet new xunit -n MaSuperLib.Tests -f net10.0

# Créer la solution et y ajouter les projets
dotnet new sln
dotnet sln add MaSuperLib/MaSuperLib.csproj
dotnet sln add MaSuperLib.Tests/MaSuperLib.Tests.csproj

# Ajouter la référence de la lib dans les tests
dotnet add MaSuperLib.Tests reference MaSuperLib
```

**Structure obtenue :**
```
MaSuperLib/
├── MaSuperLib.sln
├── MaSuperLib/
│   ├── MaSuperLib.csproj      ← Ta bibliothèque
│   └── Class1.cs
└── MaSuperLib.Tests/
    ├── MaSuperLib.Tests.csproj
    └── UnitTest1.cs
```

> 💡 **`classlib`** vs **`console`** : une Class Library produit une `.dll` réutilisable,
> pas un `.exe` exécutable. C'est la base d'un package NuGet.

---

## Étape 2 — Écrire du code utile

Remplace `Class1.cs` par quelque chose d'utile. Exemple :

```csharp
namespace MaSuperLib;

/// <summary>
/// Générateur de répliques geek pour vos applications.
/// </summary>
public static class GeekQuotes
{
    private static readonly string[] Quotes =
    [
        "I find your lack of faith disturbing. — Dark Vador",
        "It's dangerous to go alone! Take this. — Old Man (Zelda)",
        "The cake is a lie. — Portal",
        "I used to be an adventurer like you, then I took an arrow in the knee. — Skyrim",
        "Do or do not. There is no try. — Yoda",
        "It's a-me, Mario! — Mario",
        "Stay a while and listen. — Deckard Cain (Diablo)",
        "War. War never changes. — Fallout"
    ];

    /// <summary>
    /// Retourne une citation geek aléatoire.
    /// </summary>
    public static string GetRandom() => Quotes[Random.Shared.Next(Quotes.Length)];

    /// <summary>
    /// Retourne toutes les citations disponibles.
    /// </summary>
    public static IReadOnlyList<string> GetAll() => Quotes;
}
```

---

## Étape 3 — Configurer les métadonnées NuGet

Modifie le fichier **`MaSuperLib.csproj`** :

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- ══════════════════════════════════════════════ -->
    <!--        MÉTADONNÉES NUGET (OBLIGATOIRES)        -->
    <!-- ══════════════════════════════════════════════ -->
    <PackageId>MaSuperLib.GeekQuotes</PackageId>
    <Version>1.0.0</Version>
    <Authors>TonNom</Authors>
    <Description>Une bibliothèque de citations geek pour égayer vos logs et vos apps.</Description>

    <!-- ══════════════════════════════════════════════ -->
    <!--        MÉTADONNÉES NUGET (RECOMMANDÉES)        -->
    <!-- ══════════════════════════════════════════════ -->
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/TonNom/MaSuperLib</PackageProjectUrl>
    <RepositoryUrl>https://github.com/TonNom/MaSuperLib</RepositoryUrl>
    <PackageTags>geek;quotes;fun;csharp</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>

    <!-- Génère le XML de documentation (pour IntelliSense chez l'utilisateur) -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <!-- Inclure le README et l'icône dans le package -->
  <ItemGroup>
    <None Include="..\README.md" Pack="true" PackagePath="" />
    <None Include="..\icon.png" Pack="true" PackagePath="" Condition="Exists('..\icon.png')" />
  </ItemGroup>

</Project>
```

### 📋 Explication des champs

| Champ | Rôle |
|---|---|
| `PackageId` | Nom unique sur nuget.org (comme un nom de domaine) |
| `Version` | Version [SemVer](https://semver.org/lang/fr/) : `MAJEURE.MINEURE.CORRECTIF` |
| `Authors` | Ton nom ou celui de ton équipe |
| `Description` | Texte affiché sur nuget.org |
| `PackageLicenseExpression` | Licence open source (MIT, Apache-2.0...) |
| `PackageTags` | Mots-clés pour la recherche sur nuget.org |
| `PackageReadmeFile` | README affiché sur la page du package |
| `GenerateDocumentationFile` | Génère le XML pour l'IntelliSense |

---

## Étape 4 — Créer le README du package

Crée un fichier `README.md` à la racine de la solution :

```markdown
# 🎮 MaSuperLib.GeekQuotes

Citations geek aléatoires pour vos apps C#.

## Installation

    dotnet add package MaSuperLib.GeekQuotes

## Utilisation

    using MaSuperLib;

    Console.WriteLine(GeekQuotes.GetRandom());
    // → "The cake is a lie. — Portal"
```

---

## Étape 5 — Construire le package

```bash
# S'assurer que tout compile
dotnet build

# Lancer les tests
dotnet test

# Créer le package .nupkg
dotnet pack -c Release
```

Le fichier `.nupkg` est généré dans :
```
MaSuperLib/bin/Release/MaSuperLib.GeekQuotes.1.0.0.nupkg
```

> 💡 **Vérifier le contenu du package :**
> Un `.nupkg` est juste un `.zip` renommé. Tu peux l'ouvrir avec 7-Zip
> ou WinRAR pour vérifier qu'il contient bien ta DLL, le README, etc.

---

## Étape 6 — Créer un compte et obtenir sa clé API

1. Va sur **[nuget.org](https://www.nuget.org/)** et crée un compte (ou connecte-toi avec Microsoft/GitHub).
2. Clique sur ton profil → **API Keys** → **Create**.
3. Configure la clé :
   - **Key Name** : `MaSuperLib`
   - **Push** : ✅ New packages + Update existing
   - **Glob Pattern** : `MaSuperLib.*`
4. Copie la clé générée (elle ne sera affichée qu'**une seule fois** !).

---

## Étape 7 — Publier sur nuget.org 🚀

```bash
# Remplace TA_CLE_API par la clé copiée à l'étape 6
dotnet nuget push MaSuperLib/bin/Release/MaSuperLib.GeekQuotes.1.0.0.nupkg \
    --api-key TA_CLE_API \
    --source https://api.nuget.org/v3/index.json
```

> ⏳ **Validation** : nuget.org met environ **15-30 minutes** pour indexer un nouveau package.
> Tu recevras un email quand il sera disponible.

---

## Étape 8 — Tester l'installation

```bash
# Depuis un autre projet
dotnet add package MaSuperLib.GeekQuotes
```

```csharp
using MaSuperLib;

Console.WriteLine(GeekQuotes.GetRandom());
// → "Do or do not. There is no try. — Yoda"
```

---

## 📌 Commandes essentielles — Résumé

```bash
# Créer une bibliothèque
dotnet new classlib -n NomDuProjet

# Compiler
dotnet build

# Tester
dotnet test

# Packager
dotnet pack -c Release

# Publier
dotnet nuget push chemin/vers/package.nupkg --api-key CLE --source https://api.nuget.org/v3/index.json

# Supprimer un package (dé-lister, pas supprimer)
dotnet nuget delete NomPackage 1.0.0 --api-key CLE --source https://api.nuget.org/v3/index.json
```

---

## 🔄 Publier une mise à jour

1. Modifie le code.
2. Incrémente la `<Version>` dans le `.csproj` :
   ```xml
   <Version>1.1.0</Version>   <!-- Nouvelle fonctionnalité → version mineure -->
   <Version>1.0.1</Version>   <!-- Correction de bug → version correctif -->
   <Version>2.0.0</Version>   <!-- Breaking change → version majeure -->
   ```
3. `dotnet pack -c Release`
4. `dotnet nuget push ...` avec le nouveau `.nupkg`.

---

## 🏭 Bonus : CI/CD avec GitHub Actions

Crée `.github/workflows/nuget-publish.yml` pour publier automatiquement à chaque tag :

```yaml
name: Publish NuGet

on:
  push:
    tags: ['v*']   # Se déclenche quand tu push un tag v1.0.0, v1.1.0...

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Build & Test
        run: |
          dotnet build -c Release
          dotnet test -c Release --no-build

      - name: Pack
        run: dotnet pack -c Release --no-build

      - name: Publish to NuGet
        run: >
          dotnet nuget push **/*.nupkg
          --api-key ${{ secrets.NUGET_API_KEY }}
          --source https://api.nuget.org/v3/index.json
          --skip-duplicate
```

> Ajoute ta clé API dans **GitHub → Settings → Secrets** sous le nom `NUGET_API_KEY`.
>
> Ensuite, pour publier :
> ```bash
> git tag v1.0.0
> git push origin v1.0.0   # → Déclenche la publication automatique !
> ```

---

## ⚠️ Pièges courants

| Piège | Solution |
|---|---|
| `PackageId` déjà pris sur nuget.org | Choisis un nom unique, utilise un préfixe (ex: `TonPseudo.NomLib`) |
| Oublier d'incrémenter la version | NuGet refuse de push une version qui existe déjà |
| Clé API expirée | Les clés expirent (365 jours par défaut), pense à les renouveler |
| Package publié avec un bug | Tu ne peux pas supprimer, seulement **dé-lister** (`nuget delete`) |
| Documentation absente | Sans `GenerateDocumentationFile`, pas d'IntelliSense pour tes utilisateurs |

---

## 🎯 Checklist avant publication

- [ ] Le code compile sans warning
- [ ] Les tests passent
- [ ] `PackageId` est unique sur nuget.org
- [ ] `Version` est incrémentée
- [ ] `Description` est claire
- [ ] `PackageLicenseExpression` est définie
- [ ] `README.md` est inclus et à jour
- [ ] Les `<summary>` XML sont sur les classes/méthodes publiques
- [ ] `dotnet pack -c Release` produit un `.nupkg`
- [ ] Le `.nupkg` contient bien la DLL et le README (ouvrir avec 7-Zip)

---

*Bon packaging ! 📦🚀*
