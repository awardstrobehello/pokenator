export interface Question {
    id: string;
    text: string;
}

export enum UserResponse {
    Yes = 'Yes',
    Somewhat = 'Somewhat',
    NotReally = 'NotReally',
    No = 'No',
    DontKnow = 'DontKnow'
}

export interface Candidate {
    pokemon: {
        name: string;
    };
    confidence: number;  
}