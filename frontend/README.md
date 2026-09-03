# Entrenate Frontend

Frontend Next.js para Entrenate.

## Stack

- Next.js App Router
- React
- TypeScript estricto
- Tailwind CSS
- Raleway para marca, headings y metricas
- Manrope para UI y texto

## Comandos

```bash
npm run dev
npm run lint
npm run build
```

## Variables de entorno

Copiar `.env.example` a `.env.local` y ajustar la URL cuando corresponda.

```env
NEXT_PUBLIC_API_URL=http://localhost:5268
```

No guardar secretos en variables `NEXT_PUBLIC_*`.
