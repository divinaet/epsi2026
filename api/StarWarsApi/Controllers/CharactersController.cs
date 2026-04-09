using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StarWarsApi.Models;

namespace StarWarsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

/// <summary>
/// Récupère un personnage par son identifiant.
/// </summary>
/// <param name="id">Identifiant du personnage.</param>
/// <returns>Le personnage correspondant.</returns>
/// <response code="200">Personnage trouvé.</response>
/// <response code="404">Personnage introuvable.</response>
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public ActionResult<Character> GetById(int id)
    {
        var character = _characters.FirstOrDefault(c => c.Id == id);

        if (character is null)
            return NotFound(new { message = $"Personnage avec l'id {id} introuvable." });

        return Ok(character);
    }


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
}
