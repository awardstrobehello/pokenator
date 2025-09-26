# Pokénator
Inspired by Akinator, Pokénator is a pokemon guessing game. It tries to guess what Pokemon the user is thinking of in a limited number of guesses. Uses fuzzy logic to handle uncertainty and subjective questions (cute, cool, intimidating, etc).

## Core Components
- Fuzzy logic implementation, like Akinator

## TO-DO
- Get fuzzy scores of pokemon as JSON file (done)
- Link endpoints to Azure (CosmoDB, Storage)

## Structure (Planned)
Pokenator/
├── README.md
├── .gitignore
│
├── backend/
│   ├── Program.cs                    # Game control
│   ├── Models.cs                     # Data structures (question, pokemon)
│   ├── GameState.cs                  # What's been asked, what's possible, user answers
│   ├── LogicEngine.cs                # Core game logic
│   │
│   ├── Data/
│   │   ├── pokemon.json              # Where the 150 scored Pokemon are
│   │   └── questions.json            # What questions will be asked
│   └── PokenatorBackend.csproj
│
├── frontend/                         # React app
│   └── TO-DO
│
└── data-extractor/                   # API extractor for archive/reference
    └── extractor.cs                  

## Development Plan
- Initial local development

- Console testing app

- Start with purely fuzzy logic implementation
    - Every Pokemon gets scored on every attribute, avoiding issues with a binary system
    - Deals with potential misconceptions that users may have when answering questions
        - Mega forms changing type?
        - Different "main" color answers?
            - Snorlax having primary color be "black" in API when nobody would answer that
    - Take extracted factual data and transfer it to fuzzy scores as 1.0   

- `pokemon_gen1.json` from existing extractor
    - Backup maintained
- `questions.json` Contains questions

- Eventually transfer JSON files -> CosmosDB

- Backend infrastructure using asp.net
    - Prevent repetitive question patterns
    - Find a way to handle similar pokemon (Pikachu, Jolteon) ?
    - Rough diagram of the guesser's strategy: use immutable characteristics to eliminate pokemon; guess the next question based on confidence?
- React frontend with TypeScript