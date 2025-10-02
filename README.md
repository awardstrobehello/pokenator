# Pokénator
Inspired by Akinator, Pokénator is a pokemon guessing game. It tries to guess what Pokemon the user is thinking of in a limited number of guesses. Uses pure fuzzy logic for types, colors, and subjective characteristics: cute, cool, intimidating, etc.

## Core Components
- Pure fuzzy logic implementation, like Akinator


## Structure
backend /  
————/ Program.cs  — Game control    
————/ Models.cs  
————/ GameState.cs  — What's been asked, what's possible, user answers  
————/ LogicEngine.cs
  
————/ Data /  — see sample .json documents
———————/ pokemon.json  
———————/ questions.json  

————/ PokenatorBackend.csproj  

frontend/  
————/ (TO-DO) 
  
data-extractor/  
——————/ extractor.cs                  

## Development Plan
- Initial local development
- Create json files
    - `pokemon_gen1.json` from existing extractor ✓
    - `questions.json` ✓
- Implement pure fuzzy logic implementation ✓
- Create test game in console to guess pokemon ✓

- Implement self-learning functions through feedback/correct or wrong answers
    - "What pokemon were you thinking of?"
- Make function to update json file w/ pokemon and attribute values...?


## TO-DO
- Start the frontend with React
- Double-check whether or not TypeScript is necessary 

- Build on backend infrastructure using asp.net to communicate with Azure
    - Transfer JSON files -> CosmosDB
- Decision tree at some point...?