import type { Metadata } from "next";
import { Manrope, Raleway } from "next/font/google";
import type { ReactNode } from "react";
import { TrainingProfileGateProvider } from "@/features/training-profile/gate/training-profile-gate";
import "./globals.css";

const manrope = Manrope({
  variable: "--font-manrope",
  subsets: ["latin"],
  weight: ["400", "500", "600"],
  display: "swap",
});

const raleway = Raleway({
  variable: "--font-raleway",
  subsets: ["latin"],
  weight: ["600", "700", "800"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "Entrenate",
  description: "Seguimiento de fuerza e hipertrofia.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html
      lang="es"
      className={`${manrope.variable} ${raleway.variable} h-full bg-background antialiased`}
    >
      <body className="flex min-h-full flex-col bg-background text-text-primary">
        <TrainingProfileGateProvider>{children}</TrainingProfileGateProvider>
      </body>
    </html>
  );
}
