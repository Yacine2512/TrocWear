// Miroir de AuthService.Domain.DTOs (UserResponse, RegisterRequest, UpdateProfileRequest).
export interface User {
  id: string;
  pseudonyme: string;
  email: string;
  adresseParDefaut: string | null;
  status: string;
  version: number;
  createdAt: string;
  updatedAt: string;
}

export interface RegisterRequest {
  pseudonyme: string;
  email: string;
  password: string;
  adresseParDefaut?: string | null;
}

export interface UpdateProfileRequest {
  pseudonyme: string;
  versionAttendue: number;
}
