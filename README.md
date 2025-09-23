# Pokénator
Inspired by Akinator, Pokénator is a pokemon guessing game that tries to guess what Pokemon the user is thinking of in a limited number of guesses.

## Core Components
- Learns from user feedback and improves guessing algorithm with weights

## Development Plan
- The guessing system will initially focus on concrete characteristics (types, colors, shapes, evolution stages) from the first generation using binary search(?).
    - Create extractor from PokeAPI that takes characteristics and data from pokemon

- Local development and testing is fine enough for now. probably better computational overhead too
    - Still needs to be ready to ship for production quickly (Microsoft Web Services)

- Reach a conclusion on the backend's development process ASAP.
    - Eventually carry over the backend to a purely fuzzy logic implementation?
        - Binary search (MVP) -> Train weights -> switch
    - Start solely from fuzzy logic implementation
        - On second thought, might not be as bad...

- Start backend's infrastructure using asp.net
    - From the beginning, it'll be a binary question system with hard filtering.
        - Train model with said data; incorporate weights with yes/no questions.
        - Weights will eventually go into a confidence calculation system
    - Prevent repetitive question patterns
    - Find a way to handle similar pokemon (Pikachu, Jolteon)
    - Rough diagram of the guesser's strategy: use immutable characteristics to eliminate pokemon; guess the next question based confidence. 
        - "Am I confident that it's a specific pokemon, or should I continue to guess? What's the best guess to make?" 
            - Weights will help in making that decision
- Think about and study up on fuzzy logic implementation (What Akinator Uses).
    - Probably has to be less than 10 guesses; 2^10 is 1024 if you have the entire Pokedex. If it's going to be a strong guesser, then it should be knocked down to... 
        - Nine (512)? Eight (256)? Seven (128)?

- Create React frontend with TypeScript

- Link endpoints to Azure (CosmoDB, Storage)

- Expand to subjective user characteristics (cool, cute, scary, popular). Brainstorm some more.
    - If fuzzy logic implementation, then likely WAY sooner.