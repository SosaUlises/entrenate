import Image from "next/image";

export function BrandMark() {
  return (
    <Image
      alt="Entrenate"
      className="h-auto w-56 sm:w-60"
      height={174}
      priority
      src="/branding/logo/entrenate-logo-horizontal-white.png"
      width={1076}
    />
  );
}
