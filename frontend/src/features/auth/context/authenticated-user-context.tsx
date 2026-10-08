"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { CurrentUser } from "../types/auth.types";

const AuthenticatedUserContext = createContext<CurrentUser | null>(null);

export function AuthenticatedUserProvider({
  children,
  user,
}: {
  children: ReactNode;
  user: CurrentUser;
}) {
  return (
    <AuthenticatedUserContext.Provider value={user}>
      {children}
    </AuthenticatedUserContext.Provider>
  );
}

export function useAuthenticatedUser(): CurrentUser {
  const user = useContext(AuthenticatedUserContext);
  if (!user) {
    throw new Error("useAuthenticatedUser debe usarse dentro de AuthenticatedUserProvider.");
  }

  return user;
}
