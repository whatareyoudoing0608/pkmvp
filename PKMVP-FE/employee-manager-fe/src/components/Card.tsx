import React from "react";

export function Card({ title, right, children }: { title?: string; right?: React.ReactNode; children: React.ReactNode }) {
  return (
    <section className="card">
      {(title || right) && (
        <div className="card-head">
          <div className="card-title">{title}</div>
          <div>{right}</div>
        </div>
      )}
      <div className="card-body">{children}</div>
    </section>
  );
}
