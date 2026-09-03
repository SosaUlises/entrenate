import Image from "next/image";

export function BrandMark() {
  return (
    <Image
      alt="Entrenate"
      className="h-auto w-[256px] sm:w-[288px]"
      height={127}
      priority
      src="/branding/logo/entrenate-logo-white.png"
      width={936}
    />
  );
}
