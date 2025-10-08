export interface Question {
    id: string;
    text: string;
}
export const UserResponse = {
    Yes: 'Yes',
    Somewhat: 'Somewhat',
    NotReally: 'NotReally',
    No: 'No',
    DontKnow: 'DontKnow'
} as const;

export type UserResponse = typeof UserResponse[keyof typeof UserResponse];

export interface Candidate {
    pokemon: {
        name: string;
    };
    confidence: number;  
}