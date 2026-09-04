export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
};

export type ForgotPasswordRequest = {
  email: string;
};

export type ForgotPasswordResponse = {
  message: string;
};

export type ResetPasswordRequest = {
  email: string;
  newPassword: string;
  token: string;
};

export type ResetPasswordResponse = {
  message: string;
};

export type AuthResponse = {
  email: string;
  token: string;
  userId: string;
};
