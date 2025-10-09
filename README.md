# Pokénator
Inspired by Akinator, Pokénator is a pokemon guessing game. It tries to guess what Pokemon the user is thinking of in a limited number of guesses. Uses pure fuzzy logic for types, colors, and subjective characteristics: cute, cool, intimidating, etc.
- Will specifically ask up to 10 questions then attempt to guess your Pokemon using probabilistic matching across 151 possible Pokemon

## Core Components
- Pure fuzzy logic implementation, like Akinator
- C#, ASP.NET Core
- React, TypeScript, Vite

## Structure
backend /  
————/ Program.cs       — Game control  
————/ Models.cs  
————/ GameState.cs     — Game state management  
————/ LogicEngine.cs  
————/ Data /  
———————/ pokemon.json  
———————/ questions.json  
————/ backend.csproj  

api /                
————/ Program.cs               — API configuration + CORS  
————/ api.csproj  
————/ Controllers /  
——————————/ GameController.cs     — HTTP endpoints  
————/ Data /                     
———————/ pokemon.json         
———————/ questions.json        

frontend/  
————/ src /  
——————/ App.tsx              — Main game component  
——————/ index.css  
——————/ main.tsx  
——————/ types/  
—————————/ game.types.ts       
  
data-extractor/  
——————/ extractor.cs                  

## Running Locally  
### Backend (Console Testing)  
1. `cd backend`  
2. `dotnet restore`  
3. `dotnet run` or `dotnet run --debug` to see part of its thinking process  


### API  
Runs on http://localhost:5051/swagger  
1. `cd api`  
2. `dotnet restore`  
3. `dotnet run`  

### Frontend  
Runs on http://localhost:5173/  
1. `cd frontend`  
2. `npm install`  
3. `npm run dev`  

## API Endpoints  
- POST /api/game        — Start new game. Returns gameId  
- GET /api/game/{gameId}/question        — Get next question  
- POST /api/game/{gameId}/answer        — Submit answer, returns guess if confident  
- POST /api/game/{gameId}/wrong-guess        — Record wrong guess  


## Algorithm
- The algorithm picks questions that reduce uncertainty as much as possible using the highest amount of information gain. 
- Calculates confidence by multiplying scores across all previous questions and answers
- Guesses when confidence is high (15%) and is a clear leader in comparison to the others  

## Development Plan
#### Completed
- Initial local development
- Create json files
    - `pokemon_gen1.json` from existing extractor
    - `questions.json`
- Implement pure fuzzy logic implementation
- Create test game in console to guess pokemon
- ASP.NET Core implementation, game state management
- React TypeScript frontend
- Deploy API, Frontend to Azure

#### To-do (at some point)
- Make the frontend overall nicer to look at (Add sprites, images...)
- Refine the guessing algorithm
- Implement self-learning from incorrect guesses
- CosmosDB migration