// Use of getters and setters to pull from PokeAPI


// TO-DO: 
// - ADD GENERATION CHECKER AT SOME POINT
// - INITIALIZE WEIGHTS...?
// - SPECIAL MOVE?
// - Additional variety that isn't Mega or Gigantimax.
// - Triple check if Evolution ID applies to single-stage pokemon
// - DNE, IsFinalEvolution, EvolutionStage


using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PokemonExtractor
{
    class Extractor
    {
        private static readonly HttpClient client = new HttpClient();
        static async Task Main()
        {
            var allPokemon = new List<Pokemon>();

            for (int i = 1; i < 5; i++)
            {
                try
                {
                    string pokemonData = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon/{i}");

                    using JsonDocument doc = JsonDocument.Parse(pokemonData);
                    JsonElement root = doc.RootElement;

                    // Get types; determine if monotype. Types is string list.
                    var types = root.GetProperty("types").EnumerateArray().Select(typeElement => typeElement.GetProperty("type").GetProperty("name").GetString() ?? "unknown").ToList();


                    var pokemon = new Pokemon
                    {
                        Id = i,
                        Name = root.GetProperty("name").GetString() ?? "Unknown",
                        Types = types,
                        IsMonotype = types.Count == 1,
                    };


                    // Additional data not in v2/pokemon/{i} 
                    string speciesData = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon-species/{i}");
                    JsonDocument speciesDoc = JsonDocument.Parse(speciesData);
                    JsonElement speciesRoot = speciesDoc.RootElement;


                    pokemon.PrimaryColor = speciesRoot.GetProperty("color").GetProperty("name").GetString() ?? "unknown";
                    pokemon.BodyShape = speciesRoot.GetProperty("shape").GetProperty("name").GetString() ?? "unknown";
                    pokemon.IsLegendary = speciesRoot.GetProperty("is_legendary").GetBoolean();
                    pokemon.IsMythical = speciesRoot.GetProperty("is_mythical").GetBoolean();
                    pokemon.IsBaby = speciesRoot.GetProperty("is_baby").GetBoolean();
                    pokemon.HasForms = speciesRoot.GetProperty("forms_switchable").GetBoolean();


                    // Does it have a different variety? Mega? Gigantimax? Region?
                    if (speciesRoot.TryGetProperty("varieties", out var varieties))
                    {
                        foreach (var variety in varieties.EnumerateArray())
                        {
                            if (variety.TryGetProperty("pokemon", out var temp) &&
                                temp.TryGetProperty("name", out var name))
                            {
                                string pokemonName = name.GetString() ?? "";
                                if (pokemonName == root.GetProperty("name").GetString()) continue; // Varieties contains the default name of the pokemon
                                if (pokemonName.Contains("-mega")) pokemon.HasMegaEvolution = true;
                                if (pokemonName.Contains("-gmax")) pokemon.HasGigantimax = true;
                                pokemon.HasVarieties = true;
                            }
                        }
                    }



                    Console.WriteLine($"    {pokemon.Id:D4} {pokemon.Name} - " +
                                    $"{pokemon.PrimaryColor} | {string.Join("/", pokemon.Types)} | " +
                                    $"{(pokemon.IsLegendary ? "Legendary " : "")} | " +
                                    $"{(pokemon.IsMythical ? "Mythical" : "")} | " +
                                    $"{(pokemon.IsBaby ? "Baby" : "")} | " + // Babies start at gen II but since the variable's here it's worth keeping
                                    $"{(pokemon.IsMonotype ? "Monotype" : " Dual-type")} | " +
                                    $"{(pokemon.HasMegaEvolution ? "Mega" : "")} | " +
                                    $"{(pokemon.HasGigantimax ? "Gmax" : "" ) } | " +
                                    $"{pokemon.BodyShape} | Stage: {pokemon.EvolutionStage}");


                    allPokemon.Add(pokemon);

                    await Task.Delay(2000);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\n\nException!! {0}\n\n", e.Message);
                    return;
                }
            }

            Console.WriteLine($"\nFinished extraction. Writing to JSON file....\n");
            string jsonString = JsonSerializer.Serialize(allPokemon, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync("pokemon-data.json", jsonString);
            Console.WriteLine($"\nFinished write to JSON.\n");

        }
    }

    public class Pokemon
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Types { get; set; } = new List<string>();
        public string PrimaryColor { get; set; } = "unknown";
        public string BodyShape { get; set; } = "unknown";

        // Special characteristics
        public bool IsLegendary { get; set; } = false;
        public bool IsMythical { get; set; } = false; // Does not contain Legendary
        public bool IsMonotype { get; set; } = false;

        // Has a different form
        public bool HasVarieties { get; set; } = false; // Contains Regional, Mega, DMax, GMax
        public bool HasForms { get; set; } = false; // Does not contain Regional

        public bool HasMegaEvolution { get; set; } = false;
        public bool HasGigantimax { get; set; } = false;

        // Additional metadata
        public int Generation { get; set; } = 1; // To-do

        // Evolution data. TO-DO.
        public int EvolutionStage { get; set; } = 0; // 1 = first, 2 = second, 3 = final, -1 = unknown
        public bool IsBaby { get; set; } = false; // Gen II Implementation
        public bool IsFinalEvolution { get; set; } = false;
        public bool DoesNotEvolve { get; set; } = false;
    }

}