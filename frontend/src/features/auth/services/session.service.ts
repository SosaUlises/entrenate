import { cookies } from "next/headers";
import type { AuthResponse } from "../types/auth.types";

const authTokenCookieName = "entrenate.auth_token";
const jwtExpirationSeconds = 60 * 60;

export async function createAuthSession(response: AuthResponse): Promise<void> {
  const cookieStore = await cookies();

  cookieStore.set({
    httpOnly: true,
    maxAge: jwtExpirationSeconds,
    name: authTokenCookieName,
    path: "/",
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    value: response.token,
  });
}
