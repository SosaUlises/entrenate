import { env } from "@/lib/env";

type JsonBody =
  | Record<string, unknown>
  | Array<unknown>
  | string
  | number
  | boolean
  | null;

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  message?: string;
};

type ApiRequestOptions<TBody extends JsonBody | undefined = undefined> = Omit<
  RequestInit,
  "body" | "headers"
> & {
  authToken?: string;
  body?: TBody;
  headers?: HeadersInit;
};

type ApiClientErrorOptions = {
  code: string;
  details?: unknown;
  status?: number;
};

const statusMessages: Record<number, string> = {
  400: "Revisa los datos ingresados.",
  401: "Necesitas iniciar sesion para continuar.",
  403: "No tenes permisos para realizar esta accion.",
  404: "No encontramos el recurso solicitado.",
  409: "No se pudo completar la accion por el estado actual.",
  500: "Ocurrio un error inesperado. Intentalo nuevamente.",
};

export class ApiClientError extends Error {
  readonly code: string;
  readonly details?: unknown;
  readonly status?: number;

  constructor(message: string, options: ApiClientErrorOptions) {
    super(message);
    this.name = "ApiClientError";
    this.code = options.code;
    this.details = options.details;
    this.status = options.status;
  }
}

export async function apiRequest<
  TResponse,
  TBody extends JsonBody | undefined = undefined,
>(path: string, options: ApiRequestOptions<TBody> = {}): Promise<TResponse> {
  const { authToken, body, headers, ...requestOptions } = options;
  const requestHeaders = new Headers(headers);

  if (body !== undefined && !requestHeaders.has("Content-Type")) {
    requestHeaders.set("Content-Type", "application/json");
  }

  if (!requestHeaders.has("Accept")) {
    requestHeaders.set("Accept", "application/json");
  }

  if (authToken) {
    requestHeaders.set("Authorization", `Bearer ${authToken}`);
  }

  const response = await fetch(buildApiUrl(path), {
    ...requestOptions,
    body: body === undefined ? undefined : JSON.stringify(body),
    headers: requestHeaders,
  });

  const payload = await parseResponsePayload(response);

  if (!response.ok) {
    throw createApiError(response, payload);
  }

  return payload as TResponse;
}

function buildApiUrl(path: string): string {
  if (!env.apiUrl) {
    throw new ApiClientError("La URL de la API no esta configurada.", {
      code: "missing_api_url",
    });
  }

  const baseUrl = env.apiUrl.replace(/\/+$/, "");
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;

  return `${baseUrl}${normalizedPath}`;
}

async function parseResponsePayload(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined;
  }

  const contentType = response.headers.get("Content-Type");

  if (!contentType?.includes("application/json")) {
    return undefined;
  }

  return response.json();
}

function createApiError(response: Response, payload: unknown): ApiClientError {
  const problemDetails = getProblemDetails(payload);
  const message =
    statusMessages[response.status] ??
    problemDetails?.title ??
    "No se pudo completar la solicitud.";

  return new ApiClientError(message, {
    code: `http_${response.status}`,
    details: problemDetails?.errors,
    status: response.status,
  });
}

function getProblemDetails(payload: unknown): ProblemDetails | undefined {
  if (!payload || typeof payload !== "object") {
    return undefined;
  }

  return payload as ProblemDetails;
}
