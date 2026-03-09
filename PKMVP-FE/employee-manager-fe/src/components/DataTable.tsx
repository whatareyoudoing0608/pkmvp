import React from "react";

export function DataTable<T>({ columns, rows, rowKey, onRowClick }: {
  columns: Array<{ key: string; title: string; render?: (row: T) => React.ReactNode; width?: string }>;
  rows: T[];
  rowKey: (row: T) => string | number;
  onRowClick?: (row: T) => void;
}) {
  return (
    <table className="table">
      <thead>
        <tr>{columns.map((c) => <th key={c.key} style={{ width: c.width }}>{c.title}</th>)}</tr>
      </thead>
      <tbody>
        {rows.length === 0 ? (
          <tr><td colSpan={columns.length} style={{ color: "var(--muted)", padding: 14 }}>데이터가 없습니다.</td></tr>
        ) : rows.map((r) => (
          <tr key={rowKey(r)} style={{ cursor: onRowClick ? "pointer" : "default" }} onClick={() => onRowClick?.(r)}>
            {columns.map((c) => <td key={c.key}>{c.render ? c.render(r) : (r as any)[c.key]}</td>)}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
