const decimalFormatter = new Intl.NumberFormat("es-AR", {
  maximumFractionDigits: 2,
});

const compactPercentageFormatter = new Intl.NumberFormat("es-AR", {
  maximumFractionDigits: 1,
});

const localDateFormatter = new Intl.DateTimeFormat("es-AR", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

const shortLocalDateFormatter = new Intl.DateTimeFormat("es-AR", {
  day: "numeric",
  month: "short",
});

const compactLocalDateWithYearFormatter = new Intl.DateTimeFormat("es-AR", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

export function formatProgressValue(value: number): string {
  return decimalFormatter.format(value);
}

export function formatProgressDate(horaInicio: string): string {
  return formatDate(horaInicio, localDateFormatter);
}

export function formatShortProgressDate(horaInicio: string): string {
  return formatDate(horaInicio, shortLocalDateFormatter);
}

export function formatCompactProgressDate(horaInicio: string): string {
  const date = new Date(horaInicio);
  if (Number.isNaN(date.getTime())) return "";
  const formatter = date.getFullYear() === new Date().getFullYear()
    ? shortLocalDateFormatter
    : compactLocalDateWithYearFormatter;
  return formatter.format(date).replaceAll(".", "");
}

export function formatProgressPercentage(value: number): string {
  const prefix = value > 0 ? "+" : "";
  return `${prefix}${formatProgressValue(value)}%`;
}

export function formatCompactProgressPercentage(value: number): string {
  return compactPercentageFormatter.format(value);
}

function formatDate(value: string, formatter: Intl.DateTimeFormat): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return formatter.format(date).replaceAll(".", "");
}
