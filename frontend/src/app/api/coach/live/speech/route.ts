import { NextResponse } from "next/server";
import { getAuthToken } from "@/features/auth/services/session.service";
import { env } from "@/lib/env";

export async function POST(request: Request) {
  const authToken = await getAuthToken();
  if (!authToken) {
    return NextResponse.json({ message: "No autorizado." }, { status: 401 });
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json(
      { message: "Cuerpo de solicitud inválido." },
      { status: 400 },
    );
  }

  const { responseId } = (body as { responseId?: string }) ?? {};
  if (!responseId) {
    return NextResponse.json(
      { message: "El campo responseId es obligatorio." },
      { status: 400 },
    );
  }

  if (!env.apiUrl) {
    return NextResponse.json(
      { message: "La URL de la API no está configurada." },
      { status: 500 },
    );
  }

  const baseUrl = env.apiUrl.replace(/\/+$/, "");

  let backendResponse: Response;
  try {
    backendResponse = await fetch(`${baseUrl}/api/coach/live/speech`, {
      body: JSON.stringify({ responseId }),
      cache: "no-store",
      headers: {
        Accept: "audio/mpeg",
        Authorization: `Bearer ${authToken}`,
        "Content-Type": "application/json",
      },
      method: "POST",
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Error de red con el backend.";
    return NextResponse.json({ message }, { status: 502 });
  }

  if (!backendResponse.ok) {
    const errorBody = await backendResponse.text();
    return new Response(errorBody, {
      headers: {
        "Content-Type": backendResponse.headers.get("Content-Type") || "application/json",
      },
      status: backendResponse.status,
    });
  }

  // Stream raw audio/mpeg binary directly to the browser
  return new Response(backendResponse.body, {
    headers: {
      "Cache-Control": "no-store",
      "Content-Type": "audio/mpeg",
    },
    status: 200,
  });
}

