import Image from "next/image";
import { cn } from "@/lib/class-names";

type BrandMarkProps = {
  prominent?: boolean;
};

export function BrandMark({ prominent = false }: BrandMarkProps) {
  return (
    <Image
      alt="Entrenate"
      className={cn(
        "h-auto object-contain",
        prominent ? "w-[320px] sm:w-[352px]" : "w-[256px] sm:w-[288px]",
      )}
      height={724}
      priority
      quality={75}
      sizes={prominent ? "(min-width: 640px) 352px, 320px" : "(min-width: 640px) 288px, 256px"}
      src="/brand/entrenate-wordmark-master.png"
      width={2172}
    />
  );
}
