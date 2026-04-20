const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8000/api/v1";

export async function fetchExpenses({ category = "", page = 1, pageSize = 20 }) {
  const params = new URLSearchParams({
    sort: "date_desc",
    page: String(page),
    page_size: String(pageSize),
  });
  if (category) params.set("category", category);

  const response = await fetch(`${API_BASE_URL}/expenses?${params.toString()}`);
  if (!response.ok) throw new Error("Failed to load expenses");
  return response.json();
}

export async function createExpense(payload) {
  const idempotencyKey = crypto.randomUUID();
  const response = await fetch(`${API_BASE_URL}/expenses`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Idempotency-Key": idempotencyKey,
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({ detail: "Unknown error" }));
    throw new Error(errorBody.detail ?? "Create request failed");
  }
  return response.json();
}
