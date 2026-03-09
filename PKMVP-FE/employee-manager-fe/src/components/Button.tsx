import React from "react";

export function Button({ variant, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "ghost" | "danger" }) {
  const cls = ["btn", variant ? variant : ""].filter(Boolean).join(" ");
  return <button {...props} className={cls} />;
}
