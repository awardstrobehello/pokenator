# Pokénator
Inspired by Akinator, Pokénator is a pokemon guessing game that tries to guess what Pokemon the user is thinking of in a limited number of guesses.

## Core Components
- Fuzzy logic implementation, like Akinator

## TO-DO
- Get fuzzy scores of pokemon as JSON file

## Development Plan
- Most of it will be done locally from the start

- Fuzzy logic engine with existing Pokemon data
    - Console testing app
    - Basic question set (types, colors, legendary, shapes)

- Skip body types and mythical vs legendary

- Start with a purely fuzzy logic implementation
    - For ALL attributes.
    - Deals with poential misconceptions that users may have when answering questions
        - Mega forms changing type?
        - Different "main" color answers?
            - Issues like: Snorlax having primary color be "black" in API when nobody would answer that
    - Take extracted factual data and transfer it to fuzzy scores as 1.0   
- Apply subjective user characteristics (cool, cute, scary, popular) to pokemon characteristics

- `pokemon_gen1.json` from existing extractor
    - Backup maintained
- `questions.json` Contains questions
- Won't have a learning system at first
    - Eventually transfer JSON files -> CosmosDB

- Backend infrastructure using asp.net
    - Prevent repetitive question patterns
    - Find a way to handle similar pokemon (Pikachu, Jolteon)
    - Rough diagram of the guesser's strategy: use immutable characteristics to eliminate pokemon; guess the next question based confidence. 
- React frontend with TypeScript
- Link endpoints to Azure (CosmoDB, Storage)