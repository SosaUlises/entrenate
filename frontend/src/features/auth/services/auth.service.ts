import { apiRequest } from "@/services/api-client";
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
} from "../types/auth.types";

export function login(request: LoginRequest): Promise<AuthResponse> {
  return apiRequest<AuthResponse, LoginRequest>("/api/auth/login", {
    body: request,
    cache: "no-store",
    method: "POST",
  });
}

export function register(request: RegisterRequest): Promise<AuthResponse> {
  return apiRequest<AuthResponse, RegisterRequest>("/api/auth/register", {
    body: request,
    cache: "no-store",
    method: "POST",
  });
}
