import { apiRequest } from "@/services/api-client";
import type {
  AuthResponse,
  CurrentUser,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  ResetPasswordResponse,
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

export function getCurrentUser(authToken: string): Promise<CurrentUser> {
  return apiRequest<CurrentUser>("/api/auth/me", {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}

export function forgotPassword(
  request: ForgotPasswordRequest,
): Promise<ForgotPasswordResponse> {
  return apiRequest<ForgotPasswordResponse, ForgotPasswordRequest>(
    "/api/auth/forgot-password",
    {
      body: request,
      cache: "no-store",
      method: "POST",
    },
  );
}

export function resetPassword(
  request: ResetPasswordRequest,
): Promise<ResetPasswordResponse> {
  return apiRequest<ResetPasswordResponse, ResetPasswordRequest>(
    "/api/auth/reset-password",
    {
      body: request,
      cache: "no-store",
      method: "POST",
    },
  );
}
